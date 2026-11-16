using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace IpTzTray
{
    sealed class GeoHit
    {
        public string Ip;
        public string City;
        public string Country;
        public string CountryCode;
        public string Iana;
        public string WindowsId;
        public bool FromPin;
        public bool FromCache;
        public string Source;
        public string Error;
    }

    static class Locator
    {
        static Locator()
        {
            ServicePointManager.SecurityProtocol |= (SecurityProtocolType)3072;
            ServicePointManager.Expect100Continue = false;
        }

        public static GeoHit Resolve(bool allowCache, Dictionary<string, string> pins, GeoHit previous)
        {
            string ip = GetPublicIp();
            string pinId;
            if (pins != null && pins.TryGetValue(ip, out pinId) && !string.IsNullOrEmpty(pinId))
            {
                var pinned = new GeoHit();
                pinned.Ip = ip;
                pinned.WindowsId = pinId;
                pinned.FromPin = true;
                pinned.City = "";
                pinned.Source = "pin";
                return pinned;
            }

            if (allowCache && previous != null && !string.IsNullOrEmpty(previous.WindowsId)
                && string.Equals(previous.Ip, ip, StringComparison.OrdinalIgnoreCase)
                && !previous.FromPin)
            {
                var cached = new GeoHit();
                cached.Ip = ip;
                cached.City = previous.City;
                cached.Country = previous.Country;
                cached.CountryCode = previous.CountryCode;
                cached.Iana = previous.Iana;
                cached.WindowsId = previous.WindowsId;
                cached.Source = previous.Source;
                cached.FromCache = true;
                return cached;
            }

            return LookupGeo(ip);
        }

        public static string GetPublicIp()
        {
            // Три разных сервиса, каждый по новому соединению. Берём адрес, который
            // назвали хотя бы два. Один ответ принимаем, только если остальные молчат.
            string nonce = DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture);
            string[] urls = new string[] {
                "https://1.1.1.1/cdn-cgi/trace",
                "https://ipinfo.io/ip?t=" + nonce,
                "http://ip-api.com/json/?fields=status,query&_=" + nonce
            };
            int budget = SettingsStore.TimeoutMs();
            Task<string>[] tasks = new Task<string>[urls.Length];
            for (int i = 0; i < urls.Length; i++)
            {
                string url = urls[i];
                tasks[i] = Task.Factory.StartNew(new Func<string>(delegate { return ReadPublicIp(url, budget); }));
            }

            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var pending = new List<Task<string>>();
            for (int i = 0; i < tasks.Length; i++) pending.Add(tasks[i]);
            DateTime deadline = DateTime.UtcNow.AddMilliseconds(budget);
            while (pending.Count > 0)
            {
                int left = (int)(deadline - DateTime.UtcNow).TotalMilliseconds;
                if (left < 1) break;
                int index = Task.WaitAny(pending.ToArray(), left);
                if (index < 0) break;
                Task<string> done = pending[index];
                pending.RemoveAt(index);
                string ip = null;
                if (done.Status == TaskStatus.RanToCompletion) ip = done.Result;
                if (!IsPublic(ip)) continue;
                int n;
                counts.TryGetValue(ip, out n);
                n++;
                counts[ip] = n;
                if (n >= 2) return ip;
            }

            string chosen = ChooseIp(counts);
            if (chosen != null)
            {
                if (counts.Count == 1 && counts[chosen] == 1)
                    Log.Write("внешний IP подтвердил только один сервис");
                return chosen;
            }
            if (counts.Count > 1) throw new Exception(Lang.T("err.ip.split"));
            throw new Exception(Lang.T("err.ip.none"));
        }

        static string ReadPublicIp(string url, int timeoutMs)
        {
            try
            {
                string ip = ExtractIp(Download(url, timeoutMs));
                if (IsPublic(ip)) return ip;
            }
            catch
            {
            }
            return null;
        }

        internal static string ChooseIp(Dictionary<string, int> counts)
        {
            if (counts == null || counts.Count == 0) return null;
            string best = null;
            int bestCount = 0;
            bool tie = false;
            foreach (KeyValuePair<string, int> pair in counts)
            {
                if (pair.Value > bestCount)
                {
                    best = pair.Key;
                    bestCount = pair.Value;
                    tie = false;
                }
                else if (pair.Value == bestCount)
                {
                    tie = true;
                }
            }
            if (tie) return null;
            if (bestCount >= 2) return best;
            if (bestCount == 1 && counts.Count == 1) return best;
            return null;
        }

        static GeoHit LookupGeo(string ip)
        {
            string[] urls = new string[] {
                "http://ip-api.com/json/" + Uri.EscapeDataString(ip) + "?fields=status,message,country,countryCode,city,timezone,query",
                "https://ipwho.is/" + Uri.EscapeDataString(ip),
                "https://ipinfo.io/" + Uri.EscapeDataString(ip) + "/json",
                "https://get.geojs.io/v1/ip/geo/" + Uri.EscapeDataString(ip) + ".json"
            };
            string last = null;
            DateTime deadline = DateTime.UtcNow.AddMilliseconds(SettingsStore.TimeoutMs());
            for (int i = 0; i < urls.Length; i++)
            {
                int left = (int)(deadline - DateTime.UtcNow).TotalMilliseconds;
                if (left < 500)
                {
                    last = Lang.T("err.geo.timeout");
                    break;
                }
                try
                {
                    string body = Download(urls[i], left);
                    GeoHit hit = ParseGeo(body);
                    if (hit == null || string.IsNullOrEmpty(hit.Iana))
                    {
                        last = Lang.T("err.geo.nozone");
                        continue;
                    }
                    string windowsId;
                    if (!ZoneMap.TryGet(hit.Iana, out windowsId))
                    {
                        last = Lang.F("err.geo.nowin", hit.Iana);
                        continue;
                    }
                    hit.WindowsId = windowsId;
                    if (string.IsNullOrEmpty(hit.Ip)) hit.Ip = ip;
                    hit.Source = new Uri(urls[i]).Host;
                    return hit;
                }
                catch (Exception ex)
                {
                    last = Short(ex.Message);
                }
            }

            var failed = new GeoHit();
            failed.Ip = ip;
            failed.Error = Lang.F("err.geo", last ?? Lang.T("err.geo.empty"));
            return failed;
        }

        internal static GeoHit ParseGeo(string body)
        {
            if (string.IsNullOrEmpty(body)) return null;
            string trim = body.Trim();
            if (trim.Length == 0 || trim[0] != '{') return null;

            var serializer = new JavaScriptSerializer();
            serializer.MaxJsonLength = 200000;
            var root = serializer.DeserializeObject(trim) as Dictionary<string, object>;
            if (root == null) return null;

            string status = AsString(Get(root, "status"));
            if (string.Equals(status, "fail", StringComparison.OrdinalIgnoreCase)) return null;
            object success;
            if (root.TryGetValue("success", out success) && success is bool && (bool)success == false) return null;

            string iana = null;
            object tz;
            if (root.TryGetValue("timezone", out tz)) iana = ReadIana(tz);
            if (iana == null) iana = ReadIana(Get(root, "time_zone"));
            if (!LooksLikeIana(iana)) return null;

            var hit = new GeoHit();
            hit.Iana = iana;
            hit.City = AsString(Get(root, "city"));
            hit.Country = AsString(Get(root, "country"));
            if (string.IsNullOrEmpty(hit.Country)) hit.Country = AsString(Get(root, "country_name"));
            hit.CountryCode = Iso2(AsString(Get(root, "countryCode")));
            if (hit.CountryCode == null) hit.CountryCode = Iso2(AsString(Get(root, "country_code")));
            if (hit.CountryCode == null) hit.CountryCode = Iso2(hit.Country);
            hit.Ip = AsString(Get(root, "ip"));
            if (string.IsNullOrEmpty(hit.Ip)) hit.Ip = AsString(Get(root, "query"));
            return hit;
        }

        internal static string ExtractIp(string body)
        {
            if (string.IsNullOrEmpty(body)) return null;
            string trim = body.Trim();
            IPAddress parsed;
            if (IPAddress.TryParse(trim, out parsed)) return parsed.ToString();

            string[] lines = trim.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.StartsWith("ip=", StringComparison.Ordinal))
                {
                    string value = line.Substring(3).Trim();
                    if (IPAddress.TryParse(value, out parsed)) return parsed.ToString();
                }
            }

            GeoHit geo = ParseGeo(trim);
            if (geo != null && !string.IsNullOrEmpty(geo.Ip) && IPAddress.TryParse(geo.Ip, out parsed))
                return parsed.ToString();
            return null;
        }

        internal static bool IsPublic(string ip)
        {
            IPAddress addr;
            if (!IPAddress.TryParse(ip, out addr)) return false;
            if (IPAddress.IsLoopback(addr)) return false;
            if (addr.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork) return true;
            byte[] b = addr.GetAddressBytes();
            if (b[0] == 0 || b[0] == 10 || b[0] == 127) return false;
            if (b[0] == 192 && b[1] == 168) return false;
            if (b[0] == 172 && b[1] >= 16 && b[1] <= 31) return false;
            if (b[0] == 169 && b[1] == 254) return false;
            return true;
        }

        static bool LooksLikeIana(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            if (value.IndexOf(' ') >= 0) return false;
            if (string.Equals(value, "UTC", StringComparison.OrdinalIgnoreCase)) return true;
            return value.IndexOf('/') > 0;
        }

        static string ReadIana(object value)
        {
            string text = AsString(value);
            if (LooksLikeIana(text)) return text;
            var nested = value as Dictionary<string, object>;
            if (nested == null) return null;
            text = AsString(Get(nested, "id"));
            if (LooksLikeIana(text)) return text;
            return null;
        }

        static object Get(Dictionary<string, object> bag, string key)
        {
            object value;
            if (bag != null && bag.TryGetValue(key, out value)) return value;
            return null;
        }

        internal static string Iso2(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length != 2) return null;
            char a = value[0];
            char b = value[1];
            if (!IsAsciiLetter(a) || !IsAsciiLetter(b)) return null;
            return char.ToUpperInvariant(a).ToString() + char.ToUpperInvariant(b);
        }

        static bool IsAsciiLetter(char c)
        {
            return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
        }

        static string AsString(object value)
        {
            string text = value as string;
            if (text == null) return null;
            text = text.Trim();
            if (text.Length == 0) return null;
            return text;
        }

        static string Download(string url, int timeoutMs)
        {
            if (timeoutMs < 500) timeoutMs = 500;
            using (var client = new TimeoutClient(timeoutMs))
            {
                client.Encoding = Encoding.UTF8;
                client.Headers[HttpRequestHeader.UserAgent] = "IpTzTray/1.1";
                client.Headers[HttpRequestHeader.CacheControl] = "no-cache";
                client.Headers[HttpRequestHeader.Pragma] = "no-cache";
                string body = client.DownloadString(url);
                if (body != null && body.Length > 100000) throw new Exception(Lang.T("err.long"));
                return body;
            }
        }

        static string Short(string message)
        {
            if (string.IsNullOrEmpty(message)) return Lang.T("err.net");
            message = message.Replace("\r", " ").Replace("\n", " ").Trim();
            if (message.Length > 180) return message.Substring(0, 180);
            return message;
        }

        sealed class TimeoutClient : WebClient
        {
            readonly int _timeoutMs;

            public TimeoutClient(int timeoutMs)
            {
                _timeoutMs = timeoutMs < 500 ? 500 : timeoutMs;
            }

            protected override WebRequest GetWebRequest(Uri address)
            {
                WebRequest request = base.GetWebRequest(address);
                if (request == null) return null;
                request.Timeout = _timeoutMs;
                HttpWebRequest http = request as HttpWebRequest;
                if (http == null) return request;
                http.KeepAlive = false;
                http.ProtocolVersion = HttpVersion.Version11;
                // Своя группа на каждый запрос: не брать соединение, открытое до смены VPN.
                http.ConnectionGroupName = Guid.NewGuid().ToString("N");
                http.Timeout = _timeoutMs;
                http.ServicePoint.ConnectionLeaseTimeout = 0;
                http.ServicePoint.MaxIdleTime = 1;
                return http;
            }
        }
    }
}
