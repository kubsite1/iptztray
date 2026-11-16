using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Reflection;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: AssemblyTitle(IpTzTray.AppInfo.Product)]
[assembly: AssemblyProduct(IpTzTray.AppInfo.Product)]
[assembly: AssemblyCompany(IpTzTray.AppInfo.Publisher)]
[assembly: AssemblyVersion("1.1.0.0")]
[assembly: AssemblyFileVersion("1.1.0.0")]
[assembly: AssemblyInformationalVersion(IpTzTray.AppInfo.Version)]

namespace IpTzTray
{
    static class Program
    {
        static Mutex _mutex;
        internal static string OutFile;

        [STAThread]
        static int Main(string[] args)
        {
            try
            {
                ServicePointTls.Enable();
                OutFile = ArgValue(args, "--out");
                if (HasArg(args, "--selftest")) return SelfTest();
                if (HasArg(args, "--map")) return MapCli(ArgValue(args, "--map"));
                if (HasArg(args, "--preview")) return PreviewCli(ArgValue(args, "--preview"));
                if (HasArg(args, "--icon")) return IconCli(ArgValue(args, "--icon"));
                if (HasArg(args, "--check")) { BootLang(); return CheckCli(); }
                if (HasArg(args, "--dns")) { BootLang(); return DnsCli(); }

                AppFlags.DryRun = HasArg(args, "--dry");
                if (AppFlags.DryRun)
                    AppPaths.SetDir(Path.Combine(Path.GetTempPath(), "IpTzTray-dry"));

                if (!AppFlags.DryRun && !Admin.Elevated)
                {
                    if (Admin.TryElevate()) return 0;
                }

                if (!AcquireMutex()) return 0;

                BootLang();
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += delegate(object sender, ThreadExceptionEventArgs e)
                {
                    Log.Write("ошибка интерфейса: " + e.Exception);
                };
                Application.Run(new TrayApp());
                return 0;
            }
            catch (Exception ex)
            {
                try { Log.Write("старт не удался: " + ex); } catch { }
                MessageBox.Show(ex.Message, AppInfo.Product, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 1;
            }
        }

        static void BootLang()
        {
            try
            {
                SettingsStore.Load();
                Lang.Use(SettingsStore.Current == null ? "" : SettingsStore.Current.Lang);
                Lang.KeepLeftToRight();
            }
            catch
            {
                Lang.Use("en");
            }
        }

        static bool AcquireMutex()
        {
            bool created;
            _mutex = new Mutex(true, @"Local\IpTzTray", out created);
            if (created) return true;
            _mutex.Dispose();
            _mutex = null;
            return false;
        }

        internal static void ReleaseMutex()
        {
            if (_mutex == null) return;
            try { _mutex.ReleaseMutex(); } catch (ApplicationException) { }
            _mutex.Dispose();
            _mutex = null;
        }

        internal static bool ReacquireMutex()
        {
            if (_mutex != null) return true;
            return AcquireMutex();
        }

        static int MapCli(string iana)
        {
            using (new ConsoleAttach())
            {
                if (string.IsNullOrEmpty(iana))
                {
                    Console.WriteLine("usage: IpTzTray --map Europe/Amsterdam");
                    return 2;
                }
                string windowsId;
                if (!ZoneMap.TryGet(iana, out windowsId))
                {
                    Console.WriteLine(Lang.F("cli.nomap", iana));
                    return 1;
                }
                Console.WriteLine(iana + " -> " + windowsId);
                return 0;
            }
        }

        static int PreviewCli(string dir)
        {
            if (string.IsNullOrEmpty(dir)) dir = "preview";
            Directory.CreateDirectory(dir);
            Badge.SavePng(Path.Combine(dir, "home.png"), Badge.Home, "+3");
            Badge.SavePng(Path.Combine(dir, "away.png"), Badge.Away, "+1");
            Badge.SavePng(Path.Combine(dir, "india.png"), Badge.Away, "+5:30");
            Badge.SavePng(Path.Combine(dir, "idle.png"), Badge.Idle, "…");
            Badge.SavePng(Path.Combine(dir, "error.png"), Badge.Error, "!");
            return 0;
        }

        static int IconCli(string path)
        {
            if (string.IsNullOrEmpty(path)) return 2;
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            Badge.SaveIco(path);
            return 0;
        }

        static int DnsCli()
        {
            using (new ConsoleAttach())
            {
                try
                {
                    DnsReport report = DnsGuard.Check();
                    Console.WriteLine(report.Summary);
                    if (!string.IsNullOrEmpty(report.Detail)) Console.WriteLine(report.Detail);
                    if (report.Error != null) return 1;
                    return report.Leak ? 2 : 0;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    return 1;
                }
            }
        }

        static int CheckCli()
        {
            using (new ConsoleAttach())
            {
                try
                {
                    string ip = Locator.GetPublicIp();
                    GeoHit hit = Locator.Resolve(false, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase), null);
                    Console.WriteLine("IP: " + ip);
                    Console.WriteLine(Lang.F("cli.place", JoinPlace(hit.City, hit.Country)));
                    Console.WriteLine(Lang.F("cli.iana", hit.Iana));
                    Console.WriteLine(Lang.F("cli.win", hit.WindowsId));
                    Console.WriteLine(Lang.F("cli.source", hit.Source));
                    Console.WriteLine(Lang.F("cli.now", WinTz.CurrentId()));
                    if (!string.IsNullOrEmpty(hit.Error))
                    {
                        Console.WriteLine(hit.Error);
                        return 1;
                    }
                    bool same = string.Equals(WinTz.CurrentId(), hit.WindowsId, StringComparison.OrdinalIgnoreCase);
                    Console.WriteLine(same ? Lang.T("cli.same") : Lang.T("cli.diff"));
                    return 0;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    return 1;
                }
            }
        }

        static int SelfTest()
        {
            using (new ConsoleAttach())
            {
                try
                {
                    return SelfTestCore();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    return 1;
                }
            }
        }

        static int SelfTestCore()
        {
                DnsGuard.SelfCheck();
                ExpectMap("Europe/Amsterdam", "W. Europe Standard Time");
                ExpectMap("Europe/Moscow", "Russian Standard Time");
                ExpectMap("America/New_York", "Eastern Standard Time");
                ExpectMap("Asia/Tokyo", "Tokyo Standard Time");
                ExpectMap("Europe/Kyiv", "FLE Standard Time");
                ExpectZoneExists("W. Europe Standard Time");
                ExpectZoneExists("Russian Standard Time");

                if (FormatOffset(TimeSpan.FromHours(3)) != "+3") throw new Exception("offset +3");
                if (FormatOffset(TimeSpan.FromHours(-5)) != "-5") throw new Exception("offset -5");
                if (FormatOffset(new TimeSpan(5, 30, 0)) != "+5:30") throw new Exception("offset +5:30");
                if (FormatOffset(TimeSpan.Zero) != "+0") throw new Exception("offset 0");

                GeoHit geo = Locator.ParseGeo("{\"status\":\"success\",\"country\":\"The Netherlands\",\"city\":\"Amsterdam\",\"timezone\":\"Europe/Amsterdam\",\"query\":\"203.0.113.5\"}");
                if (geo == null || geo.Iana != "Europe/Amsterdam" || geo.City != "Amsterdam" || geo.Ip != "203.0.113.5")
                    throw new Exception("parse ip-api");
                if (geo.CountryCode != null) throw new Exception("parse ip-api code");
                if (Locator.ParseGeo("{\"status\":\"fail\",\"message\":\"reserved\"}") != null)
                    throw new Exception("parse fail");
                GeoHit coded = Locator.ParseGeo("{\"status\":\"success\",\"country\":\"The Netherlands\",\"countryCode\":\"nl\",\"city\":\"Amsterdam\",\"timezone\":\"Europe/Amsterdam\",\"query\":\"203.0.113.5\"}");
                if (coded == null || coded.Country != "The Netherlands" || coded.CountryCode != "NL")
                    throw new Exception("parse countryCode");
                GeoHit info = Locator.ParseGeo("{\"country\":\"NL\",\"city\":\"Amsterdam\",\"timezone\":\"Europe/Amsterdam\",\"ip\":\"203.0.113.7\"}");
                if (info == null || info.Country != "NL" || info.CountryCode != "NL" || info.Ip != "203.0.113.7")
                    throw new Exception("parse ipinfo");
                GeoHit who = Locator.ParseGeo("{\"success\":true,\"city\":\"Amsterdam\",\"country\":\"Netherlands\",\"country_code\":\"nl\",\"timezone\":{\"id\":\"Europe/Amsterdam\"}}");
                if (who == null || who.Iana != "Europe/Amsterdam" || who.Country != "Netherlands" || who.CountryCode != "NL")
                    throw new Exception("parse ipwho");
                if (Locator.ExtractIp("203.0.113.8\n") != "203.0.113.8") throw new Exception("extract plain");
                if (Locator.ExtractIp("fl=1\nip=203.0.113.9\nloc=NL\n") != "203.0.113.9") throw new Exception("extract trace");
                if (Locator.IsPublic("10.1.1.1")) throw new Exception("private");
                if (Locator.IsPublic("192.168.0.1")) throw new Exception("private lan");
                if (!Locator.IsPublic("203.0.113.5")) throw new Exception("public");
                var same = new Dictionary<string, int>();
                same["203.0.113.5"] = 2;
                same["203.0.113.6"] = 1;
                if (Locator.ChooseIp(same) != "203.0.113.5") throw new Exception("vote majority");
                var split = new Dictionary<string, int>();
                split["203.0.113.5"] = 1;
                split["203.0.113.6"] = 1;
                if (Locator.ChooseIp(split) != null) throw new Exception("vote split");
                var alone = new Dictionary<string, int>();
                alone["203.0.113.5"] = 1;
                if (Locator.ChooseIp(alone) != "203.0.113.5") throw new Exception("vote alone");

                string dir = Path.Combine(Path.GetTempPath(), "IpTzTray-selftest-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(dir);
                try
                {
                    var settings = new AppSettings();
                    settings.HomeTimeZoneId = "Russian Standard Time";
                    settings.Paused = true;
                    settings.Pins["203.0.113.5"] = "Eastern Standard Time";
                    settings.DnsMode = 2;
                    settings.CheckTimeoutSec = 10;
                    settings.RestoreHomeOnExit = false;
                    settings.MatchRegion = true;
                    settings.RestoreRegion = false;
                    settings.HomeGeoName = "ru";
                    settings.DnsWatch = true;
                    settings.DnsClosed = true;
                    settings.DnsSnapshotTaken = true;
                    settings.DnsSvcPresent = false;
                    settings.DnsPolicyKeyExisted = true;
                    settings.DnsPolicyValue = 0;
                    settings.DnsAutoFix = true;
                    settings.DnsAdapterBackup = "dhcp\tБеспроводная сеть 2\t192.168.31.1";
                    string path = Path.Combine(dir, "settings.json");
                    SettingsStore.SaveFile(settings, path);
                    AppSettings loaded = SettingsStore.LoadFile(path);
                    if (loaded.HomeTimeZoneId != "Russian Standard Time" || !loaded.Paused) throw new Exception("settings");
                    if (!loaded.DnsWatch || !loaded.DnsClosed || !loaded.DnsSnapshotTaken) throw new Exception("dns settings");
                    if (loaded.DnsSvcPresent || !loaded.DnsPolicyKeyExisted || loaded.DnsPolicyValue != 0) throw new Exception("dns snapshot");
                    if (!loaded.DnsAutoFix || loaded.DnsAdapterBackup != "dhcp\tБеспроводная сеть 2\t192.168.31.1") throw new Exception("dns auto");
                    if (loaded.DnsMode != 2 || loaded.CheckTimeoutSec != 10 || loaded.RestoreHomeOnExit) throw new Exception("mode timeout");
                    if (!loaded.MatchRegion || loaded.RestoreRegion || loaded.HomeGeoName != "RU") throw new Exception("region settings");
                    string pin;
                    if (!loaded.Pins.TryGetValue("203.0.113.5", out pin) || pin != "Eastern Standard Time")
                        throw new Exception("pin");
                    File.WriteAllText(path, "{", Encoding.UTF8);
                    AppSettings broken = SettingsStore.LoadFile(path);
                    if (broken.Pins == null || !string.IsNullOrEmpty(broken.HomeTimeZoneId) || broken.DnsWatch) throw new Exception("corrupt settings");
                    if (!broken.RestoreHomeOnExit || broken.CheckTimeoutSec != 5 || broken.DnsMode != 0) throw new Exception("corrupt defaults");
                    if (broken.MatchRegion || !broken.RestoreRegion || !string.IsNullOrEmpty(broken.HomeGeoName)) throw new Exception("region defaults");

                    File.WriteAllText(path, "{\"DnsWatch\":true,\"DnsAutoFix\":false}", Encoding.UTF8);
                    AppSettings ask = SettingsStore.LoadFile(path);
                    if (ask.DnsMode != 1 || ask.DnsWatch || ask.DnsAutoFix || !ask.RestoreHomeOnExit || ask.CheckTimeoutSec != 5)
                        throw new Exception("legacy ask");
                    File.WriteAllText(path, "{\"DnsWatch\":true,\"DnsAutoFix\":true}", Encoding.UTF8);
                    AppSettings watch = SettingsStore.LoadFile(path);
                    if (watch.DnsMode != 2 || !watch.DnsWatch || !watch.DnsAutoFix) throw new Exception("legacy watch");
                    File.WriteAllText(path, "{\"DnsWatch\":false}", Encoding.UTF8);
                    AppSettings off = SettingsStore.LoadFile(path);
                    if (off.DnsMode != 0 || off.DnsWatch || off.DnsAutoFix) throw new Exception("legacy off");
                    File.WriteAllText(path, "{\"DnsMode\":0,\"DnsWatch\":true,\"DnsAutoFix\":true,\"CheckTimeoutSec\":7,\"RestoreHomeOnExit\":false}", Encoding.UTF8);
                    AppSettings forced = SettingsStore.LoadFile(path);
                    if (forced.DnsMode != 0 || forced.DnsWatch || forced.DnsAutoFix || forced.CheckTimeoutSec != 5 || forced.RestoreHomeOnExit)
                        throw new Exception("mode wins");
                    File.WriteAllText(path, "{\"DnsMode\":1,\"CheckTimeoutSec\":20,\"RestoreHomeOnExit\":false}", Encoding.UTF8);
                    AppSettings once = SettingsStore.LoadFile(path);
                    if (once.DnsMode != 1 || once.DnsWatch || once.CheckTimeoutSec != 20 || once.RestoreHomeOnExit) throw new Exception("startup mode");
                    if (once.MatchRegion || !once.RestoreRegion) throw new Exception("region default keys");
                }
                finally
                {
                    try { Directory.Delete(dir, true); } catch { }
                }

                Lang.Use("ru");
                if (Lang.T("menu.exit") != "Выход") throw new Exception("lang ru");
                if (Lang.T("set.region") != "Менять страну по IP") throw new Exception("lang region");
                Lang.Use("en");
                if (Lang.T("menu.exit") != "Exit") throw new Exception("lang en");
                Lang.Use("zh");
                if (Lang.T("menu.check") != "立即检查") throw new Exception("lang zh");
                Lang.Use("xx");
                if (Lang.Code != "en") throw new Exception("lang fallback");
                Lang.Use("");
                if (string.IsNullOrEmpty(Lang.Code)) throw new Exception("lang detect");
                WinGeo.SelfCheck();

            Console.WriteLine("selftest ok");
            return 0;
        }

        static void ExpectMap(string iana, string windowsId)
        {
            string actual;
            if (!ZoneMap.TryGet(iana, out actual) || actual != windowsId)
                throw new Exception("map " + iana + " -> " + actual + ", ожидалось " + windowsId);
        }

        static void ExpectZoneExists(string windowsId)
        {
            TimeZoneInfo.FindSystemTimeZoneById(windowsId);
        }

        internal static string FormatOffset(TimeSpan offset)
        {
            string sign = offset < TimeSpan.Zero ? "-" : "+";
            TimeSpan abs = offset.Duration();
            if (abs.Minutes == 0) return sign + abs.Hours.ToString(CultureInfo.InvariantCulture);
            return sign + abs.Hours.ToString(CultureInfo.InvariantCulture) + ":" + abs.Minutes.ToString("00", CultureInfo.InvariantCulture);
        }

        internal static string JoinPlace(string city, string country)
        {
            if (string.IsNullOrEmpty(city)) return country ?? "";
            if (string.IsNullOrEmpty(country)) return city;
            return city + ", " + country;
        }

        static bool HasArg(string[] args, string name)
        {
            if (args == null) return false;
            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        static string ArgValue(string[] args, string name)
        {
            if (args == null) return null;
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
            }
            return null;
        }
    }

    static class AppFlags
    {
        public static bool DryRun;
    }

    static class AppInfo
    {
        public const string Product = "IPTzTray";
        public const string Version = "1.1";
        public const string Publisher = "hello@kubsite.ru";
        public const string SiteUrl = "https://github.com/kubsite1/iptztray";
        public const string Tagline = "Часовой пояс Windows по внешнему IP";
    }

    static class ServicePointTls
    {
        public static void Enable()
        {
            ServicePointManager.SecurityProtocol |= (SecurityProtocolType)3072;
            ServicePointManager.Expect100Continue = false;
        }
    }

    static class Admin
    {
        public static readonly bool Elevated = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);

        public static bool TryElevate()
        {
            try
            {
                var start = new ProcessStartInfo();
                start.FileName = AssemblyPath();
                start.UseShellExecute = true;
                start.Verb = "runas";
                start.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
                Process.Start(start);
                return true;
            }
            catch (System.ComponentModel.Win32Exception)
            {
                return false;
            }
        }

        public static string AssemblyPath()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().Location;
        }
    }

    static class AppPaths
    {
        static string _dir;

        public static void SetDir(string dir)
        {
            Directory.CreateDirectory(dir);
            _dir = dir;
        }

        public static string Dir()
        {
            if (_dir == null)
                SetDir(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "IpTzTray"));
            return _dir;
        }
    }

    public sealed class AppSettings
    {
        public AppSettings()
        {
            Pins = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public bool Paused { get; set; }
        public bool AutoStart { get; set; }
        public bool AutoStartConfigured { get; set; }
        public bool SeenWelcome { get; set; }
        public string HomeTimeZoneId { get; set; }
        public Dictionary<string, string> Pins { get; set; }
        public bool RestoreHomeOnExit { get; set; }
        public bool MatchRegion { get; set; }
        public bool RestoreRegion { get; set; }
        public string HomeGeoName { get; set; }
        public int CheckTimeoutSec { get; set; }
        public int DnsMode { get; set; }
        public bool AutoStartDirty { get; set; }
        public bool DnsWatch { get; set; }
        public bool DnsClosed { get; set; }
        public bool DnsSnapshotTaken { get; set; }
        public bool DnsCloseRequested { get; set; }
        public bool DnsRevertRequested { get; set; }
        public bool DnsSvcPresent { get; set; }
        public int DnsSvcValue { get; set; }
        public bool DnsPolicyPresent { get; set; }
        public int DnsPolicyValue { get; set; }
        public bool DnsPolicyKeyExisted { get; set; }
        public bool DnsAutoFix { get; set; }
        public string DnsAdapterBackup { get; set; }
        public string Lang { get; set; }
    }

    static class SettingsStore
    {
        public static AppSettings Current = new AppSettings();

        public static void Load()
        {
            Current = LoadFile(Path());
        }

        public static void Save()
        {
            SyncDnsFlags(Current);
            SaveFile(Current, Path());
        }

        public static int TimeoutMs()
        {
            int sec = Current == null ? 0 : Current.CheckTimeoutSec;
            if (sec != 5 && sec != 10 && sec != 20) sec = 5;
            return sec * 1000;
        }

        public static void SyncDnsFlags(AppSettings settings)
        {
            if (settings == null) return;
            bool watch = settings.DnsMode == 2;
            settings.DnsWatch = watch;
            settings.DnsAutoFix = watch;
        }

        static string Path()
        {
            return System.IO.Path.Combine(AppPaths.Dir(), "settings.json");
        }

        public static AppSettings LoadFile(string path)
        {
            try
            {
                if (!File.Exists(path)) return Fresh();
                string json = File.ReadAllText(path, Encoding.UTF8);
                var serializer = new JavaScriptSerializer();
                AppSettings loaded = serializer.Deserialize<AppSettings>(json);
                if (loaded == null) return Fresh();
                loaded.Pins = NormalizePins(loaded.Pins);
                ApplyDefaults(loaded, json);
                return loaded;
            }
            catch (Exception ex)
            {
                Log.Write("настройки не прочитались, начну заново: " + ex.Message);
                return Fresh();
            }
        }

        static AppSettings Fresh()
        {
            var settings = new AppSettings();
            settings.RestoreHomeOnExit = true;
            settings.RestoreRegion = true;
            settings.CheckTimeoutSec = 5;
            settings.DnsMode = 0;
            return settings;
        }

        static void ApplyDefaults(AppSettings loaded, string json)
        {
            if (json.IndexOf("\"RestoreHomeOnExit\"", StringComparison.Ordinal) < 0)
                loaded.RestoreHomeOnExit = true;
            if (json.IndexOf("\"RestoreRegion\"", StringComparison.Ordinal) < 0)
                loaded.RestoreRegion = true;
            loaded.HomeGeoName = WinGeo.Normalize(loaded.HomeGeoName);
            if (loaded.CheckTimeoutSec != 5 && loaded.CheckTimeoutSec != 10 && loaded.CheckTimeoutSec != 20)
                loaded.CheckTimeoutSec = 5;
            if (json.IndexOf("\"DnsMode\"", StringComparison.Ordinal) < 0)
            {
                if (loaded.DnsAutoFix) loaded.DnsMode = 2;
                else if (loaded.DnsWatch) loaded.DnsMode = 1;
                else loaded.DnsMode = 0;
            }
            else if (loaded.DnsMode != 1 && loaded.DnsMode != 2)
                loaded.DnsMode = 0;
            if (!Lang.Known(loaded.Lang)) loaded.Lang = "";
            SyncDnsFlags(loaded);
        }

        public static void SaveFile(AppSettings settings, string path)
        {
            if (settings.Pins == null) settings.Pins = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var serializer = new JavaScriptSerializer();
            string json = serializer.Serialize(settings);
            string tmp = path + ".tmp";
            File.WriteAllText(tmp, json, new UTF8Encoding(false));
            if (File.Exists(path)) File.Replace(tmp, path, null);
            else File.Move(tmp, path);
        }

        public static Dictionary<string, string> CopyPins()
        {
            var copy = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (Current.Pins == null) return copy;
            foreach (KeyValuePair<string, string> pair in Current.Pins)
                copy[pair.Key] = pair.Value;
            return copy;
        }

        static Dictionary<string, string> NormalizePins(Dictionary<string, string> pins)
        {
            var copy = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (pins == null) return copy;
            foreach (KeyValuePair<string, string> pair in pins)
            {
                if (!string.IsNullOrEmpty(pair.Key) && !string.IsNullOrEmpty(pair.Value))
                    copy[pair.Key] = pair.Value;
            }
            return copy;
        }
    }

    static class Log
    {
        public static void Write(string message)
        {
            try
            {
                string path = Path.Combine(AppPaths.Dir(), "log.txt");
                var info = new FileInfo(path);
                if (info.Exists && info.Length > 200000)
                {
                    string old = path + ".old";
                    if (File.Exists(old)) File.Delete(old);
                    File.Move(path, old);
                }
                string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
                    + " " + message + Environment.NewLine;
                File.AppendAllText(path, line, new UTF8Encoding(false));
            }
            catch
            {
            }
        }
    }

    static class WinTz
    {
        public static string CurrentId()
        {
            TimeZoneInfo.ClearCachedData();
            return TimeZoneInfo.Local.Id;
        }

        public static string Display(string windowsId)
        {
            if (string.IsNullOrEmpty(windowsId)) return "";
            try { return TimeZoneInfo.FindSystemTimeZoneById(windowsId).DisplayName; }
            catch (TimeZoneNotFoundException) { return windowsId; }
            catch (InvalidTimeZoneException) { return windowsId; }
        }

        public static bool Known(string windowsId)
        {
            try
            {
                TimeZoneInfo.FindSystemTimeZoneById(windowsId);
                return true;
            }
            catch (TimeZoneNotFoundException) { return false; }
            catch (InvalidTimeZoneException) { return false; }
        }

        public static bool DisableAuto()
        {
            if (AppFlags.DryRun || !Admin.Elevated) return false;
            bool changed = false;
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\tzautoupdate", true))
            {
                if (key == null) return false;
                object value = key.GetValue("Start");
                int current = value is int ? (int)value : -1;
                if (current != 4)
                {
                    key.SetValue("Start", 4, RegistryValueKind.DWord);
                    changed = true;
                }
            }
            if (!changed) return false;
            try
            {
                var start = new ProcessStartInfo();
                start.FileName = "sc.exe";
                start.Arguments = "stop tzautoupdate";
                start.CreateNoWindow = true;
                start.UseShellExecute = false;
                Process process = Process.Start(start);
                if (process != null) process.WaitForExit(5000);
            }
            catch (Exception ex)
            {
                Log.Write("не остановил tzautoupdate: " + ex.Message);
            }
            return true;
        }

        public static void Apply(string windowsId)
        {
            if (!Known(windowsId)) throw new Exception(Lang.F("tz.missing", windowsId));
            if (AppFlags.DryRun)
            {
                Log.Write("просмотр: поставил бы " + windowsId);
                return;
            }
            if (!Admin.Elevated) throw new Exception(Lang.T("tz.noadmin"));

            var start = new ProcessStartInfo();
            start.FileName = "tzutil.exe";
            start.Arguments = "/s \"" + windowsId + "\"";
            start.CreateNoWindow = true;
            start.UseShellExecute = false;
            Process process = Process.Start(start);
            if (process == null) throw new Exception(Lang.T("tz.notzutil"));
            if (!process.WaitForExit(10000)) throw new Exception(Lang.T("tz.hang"));
            if (process.ExitCode != 0) throw new Exception(Lang.F("tz.code", process.ExitCode.ToString(CultureInfo.InvariantCulture)));

            string now = CurrentId();
            if (!string.Equals(now, windowsId, StringComparison.OrdinalIgnoreCase))
                throw new Exception(Lang.F("tz.stuck", now));
        }
    }

    static class WinGeo
    {
        const uint WmSettingChange = 0x001A;
        const uint SmtoAbortIfHung = 0x0002;
        const uint GeoFriendlyName = 8;

        public static string Normalize(string value)
        {
            return Locator.Iso2(value);
        }

        public static string Current()
        {
            var buffer = new StringBuilder(16);
            int count = GetUserDefaultGeoName(buffer, buffer.Capacity);
            if (count <= 1) return "";
            return Normalize(buffer.ToString()) ?? "";
        }

        public static string Display(string iso)
        {
            iso = Normalize(iso);
            if (iso == null) return "";
            var buffer = new StringBuilder(128);
            int count = GetGeoInfoEx(iso, GeoFriendlyName, buffer, buffer.Capacity);
            if (count > 1)
            {
                string name = buffer.ToString().Trim();
                if (name.Length > 0) return name;
            }
            return iso;
        }

        public static bool Apply(string iso)
        {
            iso = Normalize(iso);
            if (iso == null) throw new Exception(Lang.F("region.bad", ""));
            if (string.Equals(Current(), iso, StringComparison.OrdinalIgnoreCase)) return false;
            if (AppFlags.DryRun)
            {
                Log.Write("просмотр: страна была бы " + iso);
                return false;
            }
            if (!SetUserGeoName(iso)) throw new Exception(Lang.F("region.bad", iso));
            Broadcast();
            string now = Current();
            if (!string.Equals(now, iso, StringComparison.OrdinalIgnoreCase))
                throw new Exception(Lang.F("region.stuck", now));
            return true;
        }

        internal static void SelfCheck()
        {
            string current = Current();
            if (Normalize(current) == null) throw new Exception("geo read");
            if (string.IsNullOrEmpty(Display(current))) throw new Exception("geo name");
            if (!SetUserGeoName(current)) throw new Exception("geo set");
            Broadcast();
            string after = Current();
            Console.WriteLine("geo " + current + " -> " + after + " " + Display(current));
            if (!string.Equals(after, current, StringComparison.OrdinalIgnoreCase))
                throw new Exception("geo same");
        }

        static void Broadcast()
        {
            UIntPtr result;
            SendMessageTimeout(new IntPtr(0xffff), WmSettingChange, UIntPtr.Zero, "intl", SmtoAbortIfHung, 1000, out result);
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        static extern int GetUserDefaultGeoName(StringBuilder geoName, int geoNameCount);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool SetUserGeoName(string geoName);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        static extern int GetGeoInfoEx(string location, uint geoType, StringBuilder geoData, int geoDataCount);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint msg, UIntPtr wParam, string lParam, uint flags, uint timeout, out UIntPtr result);
    }

    static class AutoStart
    {
        const string TaskName = "IpTzTray";

        public static void Install()
        {
            if (AppFlags.DryRun) return;
            string exe = Admin.AssemblyPath();
            string user = WindowsIdentity.GetCurrent().Name;
            string xml = BuildXml(exe, user);
            string path = Path.Combine(Path.GetTempPath(), "IpTzTray-task.xml");
            File.WriteAllText(path, xml, Encoding.Unicode);
            try
            {
                Run("schtasks.exe", "/Create /TN \"" + TaskName + "\" /XML \"" + path + "\" /F");
            }
            finally
            {
                try { File.Delete(path); } catch { }
            }
        }

        public static void Remove()
        {
            if (AppFlags.DryRun) return;
            Run("schtasks.exe", "/Delete /TN \"" + TaskName + "\" /F");
        }

        static string BuildXml(string exe, string user)
        {
            string cmd = Xml(exe);
            string id = Xml(user);
            return "<?xml version=\"1.0\" encoding=\"UTF-16\"?>\r\n"
                + "<Task version=\"1.3\" xmlns=\"http://schemas.microsoft.com/windows/2004/02/mit/task\">\r\n"
                + "  <RegistrationInfo><Description>" + Xml(Lang.T("task.desc")) + "</Description></RegistrationInfo>\r\n"
                + "  <Triggers><LogonTrigger><Enabled>true</Enabled><UserId>" + id + "</UserId></LogonTrigger></Triggers>\r\n"
                + "  <Principals><Principal id=\"Author\"><UserId>" + id + "</UserId><LogonType>InteractiveToken</LogonType><RunLevel>HighestAvailable</RunLevel></Principal></Principals>\r\n"
                + "  <Settings>\r\n"
                + "    <MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy>\r\n"
                + "    <DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>\r\n"
                + "    <StopIfGoingOnBatteries>false</StopIfGoingOnBatteries>\r\n"
                + "    <AllowHardTerminate>true</AllowHardTerminate>\r\n"
                + "    <StartWhenAvailable>false</StartWhenAvailable>\r\n"
                + "    <Enabled>true</Enabled>\r\n"
                + "    <Hidden>false</Hidden>\r\n"
                + "    <ExecutionTimeLimit>PT0S</ExecutionTimeLimit>\r\n"
                + "    <Priority>7</Priority>\r\n"
                + "  </Settings>\r\n"
                + "  <Actions Context=\"Author\"><Exec><Command>" + cmd + "</Command></Exec></Actions>\r\n"
                + "</Task>\r\n";
        }

        static string Xml(string value)
        {
            if (value == null) return "";
            return value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }

        static void Run(string file, string arguments)
        {
            var start = new ProcessStartInfo();
            start.FileName = file;
            start.Arguments = arguments;
            start.UseShellExecute = false;
            start.CreateNoWindow = true;
            start.RedirectStandardOutput = true;
            start.RedirectStandardError = true;
            Process process = Process.Start(start);
            if (process == null) throw new Exception(Lang.F("err.nostart", file));
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit(20000);
            if (process.ExitCode != 0)
            {
                string detail = string.IsNullOrWhiteSpace(error) ? output : error;
                throw new Exception(Lang.F("err.exit", file, process.ExitCode.ToString(CultureInfo.InvariantCulture), detail.Trim()));
            }
        }
    }

    sealed class ConsoleAttach : IDisposable
    {
        readonly TextWriter _console;
        readonly TextWriter _file;

        public ConsoleAttach()
        {
            if (AttachConsole(-1))
            {
                IntPtr handle = GetStdHandle(-11);
                if (handle != IntPtr.Zero && handle != new IntPtr(-1))
                {
                    var safe = new Microsoft.Win32.SafeHandles.SafeFileHandle(handle, false);
                    var stream = new FileStream(safe, FileAccess.Write);
                    uint codePage = GetConsoleOutputCP();
                    Encoding encoding = codePage == 0 ? Encoding.UTF8 : Encoding.GetEncoding((int)codePage);
                    _console = new StreamWriter(stream, encoding);
                    _console.Flush(); // keep the writer alive; AutoFlush is set below
                    ((StreamWriter)_console).AutoFlush = true;
                }
            }
            if (!string.IsNullOrEmpty(Program.OutFile))
            {
                _file = new StreamWriter(Program.OutFile, false, new UTF8Encoding(false));
                ((StreamWriter)_file).AutoFlush = true;
            }
            if (_console != null || _file != null)
            {
                var tee = new TeeWriter(_console, _file);
                Console.SetOut(tee);
                Console.SetError(tee);
            }
        }

        public void Dispose()
        {
            try { if (_console != null) _console.Flush(); } catch { }
            try { if (_file != null) _file.Flush(); } catch { }
            try { if (_file != null) _file.Dispose(); } catch { }
        }

        [DllImport("kernel32.dll")]
        static extern bool AttachConsole(int dwProcessId);

        [DllImport("kernel32.dll")]
        static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll")]
        static extern uint GetConsoleOutputCP();
    }

    sealed class TeeWriter : TextWriter
    {
        readonly TextWriter _console;
        readonly TextWriter _file;

        public TeeWriter(TextWriter console, TextWriter file)
        {
            _console = console;
            _file = file;
        }

        public override Encoding Encoding { get { return Encoding.UTF8; } }

        public override void WriteLine(string value)
        {
            if (_console != null)
            {
                try { _console.WriteLine(value); } catch (IOException) { }
            }
            if (_file != null) _file.WriteLine(value);
        }
    }
}
