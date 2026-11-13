using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using Microsoft.Win32;

namespace IpTzTray
{
    sealed class DnsReport
    {
        public bool Leak;
        public bool Held;
        public string Where;
        public string Summary;
        public string Detail;
        public string Error;
    }

    static class DnsGuard
    {
        const string ValueName = "DisableSmartNameResolution";
        const string ServicePath = @"SYSTEM\CurrentControlSet\Services\Dnscache\Parameters";
        const string PolicyPath = @"SOFTWARE\Policies\Microsoft\Windows NT\DNSClient";

        public static DnsReport Check()
        {
            var report = new DnsReport();
            var detail = new StringBuilder();
            try
            {
                bool parallel = ParallelOn();
                detail.AppendLine(parallel ? Lang.T("dns.parallel.on") : Lang.T("dns.parallel.off"));
                report.Held = SettingsStore.Current.DnsClosed || SettingsStore.Current.DnsSnapshotTaken;
                if (!parallel)
                {
                    report.Summary = report.Held
                        ? Lang.T("dns.closed")
                        : Lang.T("dns.noparallel");
                    report.Detail = detail.ToString().Trim();
                    return report;
                }

                uint best;
                if (!TryBestIndex(out best)) throw new Exception(Lang.T("dns.noadapter"));
                string bestName = FindName(best);
                if (!string.IsNullOrEmpty(bestName)) detail.AppendLine(Lang.F("dns.adapter", bestName));

                // Свой запрос в обход службы DNS: так видно, отвечает ли чужой резолвер.
                // После закрытия служба Windows его больше не спрашивает, даже если путь жив.
                string where = null;
                bool answered = false;
                int probed = 0;
                foreach (SideDns side in ListSides(best))
                {
                    if (probed >= 3) break;
                    probed++;
                    string error;
                    bool ok = Probe(side.Local, side.Index, side.Dns, out error);
                    string line = side.Name + " → " + side.Dns + (ok ? Lang.T("dns.answer") : Lang.T("dns.quiet"));
                    if (!ok && !string.IsNullOrEmpty(error)) line += " (" + Short(error) + ")";
                    detail.AppendLine(line);
                    if (ok && !answered)
                    {
                        answered = true;
                        where = side.Name + " → " + side.Dns;
                    }
                }

                report.Where = where;
                report.Leak = MeansLeak(true, answered);
                if (report.Leak) report.Summary = Lang.F("dns.leak", where);
                else if (probed > 0) report.Summary = Lang.T("dns.silent");
                else report.Summary = Lang.T("dns.clean");
            }
            catch (Exception ex)
            {
                report.Error = ex.Message;
                report.Summary = ex.Message;
                detail.AppendLine(ex.Message);
            }
            report.Detail = detail.ToString().Trim();
            return report;
        }

        public static string Apply()
        {
            if (AppFlags.DryRun)
            {
                TakeSnapshot();
                SettingsStore.Current.DnsClosed = true;
                SettingsStore.Current.DnsCloseRequested = false;
                SettingsStore.Save();
                Log.Write("просмотр: закрыл бы утечку DNS");
                return Lang.T("dns.dry.apply");
            }
            if (!Admin.Elevated) throw new Exception(Lang.T("err.noadmin"));

            TakeSnapshot();
            WriteValue(ServicePath, 1, false);
            WriteValue(PolicyPath, 1, true);
            SealSideDns();
            SettingsStore.Current.DnsClosed = true;
            SettingsStore.Current.DnsCloseRequested = false;
            SettingsStore.Current.DnsRevertRequested = false;
            SettingsStore.Save();
            Log.Write("закрыл утечку DNS, служба была "
                + (SettingsStore.Current.DnsSvcPresent ? SettingsStore.Current.DnsSvcValue.ToString() : "пустая")
                + ", политика была "
                + (SettingsStore.Current.DnsPolicyPresent ? SettingsStore.Current.DnsPolicyValue.ToString() : "пустая"));
            return Lang.F("dns.fixed", RestartDns());
        }

        public static string Revert()
        {
            return RestoreApplied(false);
        }

        public static string Release()
        {
            return RestoreApplied(true);
        }

        public static bool IsHolding()
        {
            return SettingsStore.Current.DnsSnapshotTaken || SettingsStore.Current.DnsClosed;
        }

        public static bool RouteIsTunnel()
        {
            uint index;
            if (!TryBestIndex(out index)) return false;
            NetworkInterface[] all = NetworkInterface.GetAllNetworkInterfaces();
            for (int i = 0; i < all.Length; i++)
            {
                NetworkInterface ni = all[i];
                try
                {
                    if ((uint)ni.GetIPProperties().GetIPv4Properties().Index != index) continue;
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel) return true;
                    string text = (ni.Description ?? "") + " " + (ni.Name ?? "");
                    if (text.IndexOf("WireGuard", StringComparison.OrdinalIgnoreCase) >= 0) return true;
                    if (text.IndexOf("Amnezia", StringComparison.OrdinalIgnoreCase) >= 0) return true;
                    if (text.IndexOf("TAP-Windows", StringComparison.OrdinalIgnoreCase) >= 0) return true;
                    return false;
                }
                catch (NetworkInformationException)
                {
                }
            }
            return false;
        }

        static string RestoreApplied(bool keepWatch)
        {
            if (!IsHolding())
            {
                if (!keepWatch)
                {
                    ClearFlags();
                    SettingsStore.Save();
                    Log.Write("слежение за утечкой DNS выключено");
                    return Lang.T("dns.watch.off");
                }
                return Lang.T("dns.home.none");
            }
            if (AppFlags.DryRun)
            {
                if (keepWatch) ClearHold();
                else ClearFlags();
                SettingsStore.Save();
                Log.Write(keepWatch ? "просмотр: вернул бы DNS дома" : "просмотр: вернул бы настройку DNS");
                return keepWatch
                    ? Lang.T("dns.dry.home")
                    : Lang.T("dns.dry.revert");
            }
            if (!Admin.Elevated) throw new Exception(Lang.T("err.noadmin"));

            bool wasOff = !ParallelOn();
            UnsealSideDns();
            RestoreService();
            RestorePolicy();
            if (keepWatch) ClearHold();
            else ClearFlags();
            SettingsStore.Save();
            Log.Write(keepWatch ? "домашняя сеть: вернул DNS" : "вернул прежнюю настройку DNS");
            string again = SettingsStore.Current.DnsMode == 2
                ? Lang.T("dns.again.watch")
                : Lang.T("dns.again.once");
            string head = keepWatch
                ? Lang.F("dns.home.restored", again)
                : Lang.T("dns.reverted");
            if (!wasOff) return head;
            return head + " " + RestartDns();
        }

        public static bool RestoreNeedsAdmin()
        {
            return IsHolding();
        }

        public static bool TryBestIndex(out uint index)
        {
            index = 0;
            try
            {
                byte[] bytes = IPAddress.Parse("8.8.8.8").GetAddressBytes();
                uint dest = BitConverter.ToUInt32(bytes, 0);
                uint found;
                if (GetBestInterface(dest, out found) != 0) return false;
                index = found;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        internal static void SelfCheck()
        {
            if (!MeansLeak(true, true) || MeansLeak(false, true) || MeansLeak(true, false))
                throw new Exception("dns leak decision");
            if (!IsIgnoredAdapter("Hyper-V Virtual Ethernet Adapter", "vEthernet (WSL)", NetworkInterfaceType.Ethernet))
                throw new Exception("dns ignore virtual");
            if (IsIgnoredAdapter("Intel(R) Wi-Fi 6 AX203", "Беспроводная сеть 2", NetworkInterfaceType.Wireless80211))
                throw new Exception("dns ignore wifi");
            if (IsIgnoredAdapter("WireGuard Tunnel", "AmneziaVPN", NetworkInterfaceType.Tunnel) == false)
                throw new Exception("dns ignore tunnel");
            if (!SkipDns(IPAddress.Parse("127.0.0.1")) || SkipDns(IPAddress.Parse("192.168.31.1")))
                throw new Exception("dns skip");
            if (!SkipDns(IPAddress.Parse("fec0::1")))
                throw new Exception("dns skip v6");
            string mode, adapter, savedIps;
            SplitBackup(JoinBackup("Беспроводная сеть 2", true, "192.168.31.1"), out mode, out adapter, out savedIps);
            if (mode != "dhcp" || adapter != "Беспроводная сеть 2" || savedIps != "192.168.31.1")
                throw new Exception("dns backup");
            if (Field(JoinBackup("x", false, "1.1.1.1", "2001:db8::1"), 3) != "2001:db8::1")
                throw new Exception("dns backup v6");
            var packet = new byte[] { 0x12, 0x34, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            if (!ResponseMatches(packet, 0x1234) || ResponseMatches(packet, 1))
                throw new Exception("dns packet");
        }

        internal static bool MeansLeak(bool parallelOn, bool sideAnswered)
        {
            return parallelOn && sideAnswered;
        }

        static void TakeSnapshot()
        {
            if (SettingsStore.Current.DnsSnapshotTaken) return;
            int? service = ReadValue(ServicePath);
            bool keyExisted;
            int? policy = ReadPolicy(out keyExisted);
            SettingsStore.Current.DnsSvcPresent = service.HasValue;
            SettingsStore.Current.DnsSvcValue = service ?? 0;
            SettingsStore.Current.DnsPolicyPresent = policy.HasValue;
            SettingsStore.Current.DnsPolicyValue = policy ?? 0;
            SettingsStore.Current.DnsPolicyKeyExisted = keyExisted;
            SettingsStore.Current.DnsSnapshotTaken = true;
            SettingsStore.Save();
        }

        static void ClearFlags()
        {
            ClearHold();
            SettingsStore.Current.DnsMode = 0;
            SettingsStore.Current.DnsAutoFix = false;
            SettingsStore.Current.DnsWatch = false;
        }

        static void ClearHold()
        {
            SettingsStore.Current.DnsClosed = false;
            SettingsStore.Current.DnsSnapshotTaken = false;
            SettingsStore.Current.DnsCloseRequested = false;
            SettingsStore.Current.DnsRevertRequested = false;
            SettingsStore.Current.DnsSvcPresent = false;
            SettingsStore.Current.DnsSvcValue = 0;
            SettingsStore.Current.DnsPolicyPresent = false;
            SettingsStore.Current.DnsPolicyValue = 0;
            SettingsStore.Current.DnsPolicyKeyExisted = false;
            SettingsStore.Current.DnsAdapterBackup = null;
        }

        static void RestoreService()
        {
            if (SettingsStore.Current.DnsSvcPresent) WriteValue(ServicePath, SettingsStore.Current.DnsSvcValue, false);
            else DeleteValue(ServicePath);
        }

        static void RestorePolicy()
        {
            if (SettingsStore.Current.DnsPolicyPresent)
            {
                WriteValue(PolicyPath, SettingsStore.Current.DnsPolicyValue, true);
                return;
            }
            bool empty;
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(PolicyPath, true))
            {
                if (key == null) return;
                try { key.DeleteValue(ValueName, false); } catch (ArgumentException) { }
                empty = key.ValueCount == 0 && key.SubKeyCount == 0;
            }
            if (empty && !SettingsStore.Current.DnsPolicyKeyExisted)
            {
                try { Registry.LocalMachine.DeleteSubKey(PolicyPath, false); }
                catch (ArgumentException) { }
                catch (InvalidOperationException) { }
            }
        }

        static bool ParallelOn()
        {
            bool keyExisted;
            int? policy = ReadPolicy(out keyExisted);
            if (policy.HasValue) return policy.Value != 1;
            int? service = ReadValue(ServicePath);
            if (service.HasValue) return service.Value != 1;
            return true;
        }

        static int? ReadValue(string path)
        {
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(path, false))
            {
                if (key == null) return null;
                object raw = key.GetValue(ValueName);
                if (raw is int) return (int)raw;
                return null;
            }
        }

        static int? ReadPolicy(out bool keyExisted)
        {
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(PolicyPath, false))
            {
                keyExisted = key != null;
                if (key == null) return null;
                object raw = key.GetValue(ValueName);
                if (raw is int) return (int)raw;
                return null;
            }
        }

        static void WriteValue(string path, int value, bool create)
        {
            using (RegistryKey key = create
                ? Registry.LocalMachine.CreateSubKey(path)
                : Registry.LocalMachine.OpenSubKey(path, true))
            {
                if (key == null) throw new Exception(Lang.T("dns.nokey"));
                key.SetValue(ValueName, value, RegistryValueKind.DWord);
            }
        }

        static void DeleteValue(string path)
        {
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(path, true))
            {
                if (key == null) return;
                try { key.DeleteValue(ValueName, false); }
                catch (ArgumentException) { }
            }
        }

        const string DohIps = "8.8.8.8,8.8.4.4,1.1.1.1,1.0.0.1,9.9.9.9,149.112.112.112,77.88.8.8,77.88.8.1,2001:4860:4860::8888,2001:4860:4860::8844,2606:4700:4700::1111,2606:4700:4700::1001,2620:fe::fe,2a02:6b8::feed:0ff,2a02:6b8:0:1::feed:0ff";

        static void SealSideDns()
        {
            if (string.IsNullOrEmpty(SettingsStore.Current.DnsAdapterBackup))
            {
                SettingsStore.Current.DnsAdapterBackup = CaptureSideDns();
                SettingsStore.Save();
            }
            string tunnelDns = TunnelDns();
            string blocked = RedirectSideDns(SettingsStore.Current.DnsAdapterBackup, tunnelDns);
            ReplaceRule("IpTzTray-DNS", "UDP", "53", blocked);
            ReplaceRule("IpTzTray-DNS-TCP", "TCP", "53", blocked);
            ReplaceRule("IpTzTray-DoH", "TCP", "443", DohIps);
            ReplaceRule("IpTzTray-DoT", "TCP", "853", DohIps);
            RefreshNrpt(tunnelDns);
        }

        static void UnsealSideDns()
        {
            string backup = SettingsStore.Current.DnsAdapterBackup;
            if (!string.IsNullOrEmpty(backup) && !RestoreSideDns(backup))
                throw new Exception(Lang.T("dns.norestore"));
            Exec("netsh.exe", "advfirewall firewall delete rule name=\"IpTzTray-DNS\"");
            Exec("netsh.exe", "advfirewall firewall delete rule name=\"IpTzTray-DNS-TCP\"");
            Exec("netsh.exe", "advfirewall firewall delete rule name=\"IpTzTray-DoH\"");
            Exec("netsh.exe", "advfirewall firewall delete rule name=\"IpTzTray-DoT\"");
            RemoveNrpt();
            SettingsStore.Current.DnsAdapterBackup = null;
            SettingsStore.Save();
        }

        static string CaptureSideDns()
        {
            uint best;
            TryBestIndex(out best);
            var lines = new StringBuilder();
            NetworkInterface[] all = NetworkInterface.GetAllNetworkInterfaces();
            for (int i = 0; i < all.Length; i++)
            {
                NetworkInterface ni = all[i];
                if (ni.OperationalStatus != OperationalStatus.Up) continue;
                if (IsIgnoredAdapter(ni.Description, ni.Name, ni.NetworkInterfaceType)) continue;
                try
                {
                    IPInterfaceProperties props = ni.GetIPProperties();
                    if ((uint)props.GetIPv4Properties().Index == best) continue;
                    string ips = SideDnsList(props);
                    string v6 = SideDnsV6(props);
                    if (string.IsNullOrEmpty(ips) && string.IsNullOrEmpty(v6)) continue;
                    string shown = ExecText("netsh.exe", "interface ipv4 show dnsservers name=\"" + Quote(ni.Name) + "\"");
                    bool dhcp = shown.IndexOf("DHCP", StringComparison.OrdinalIgnoreCase) >= 0;
                    if (lines.Length > 0) lines.Append('\n');
                    lines.Append(JoinBackup(ni.Name, dhcp, ips, v6));
                }
                catch (NetworkInformationException)
                {
                }
            }
            return lines.ToString();
        }

        static string SideDnsList(IPInterfaceProperties props)
        {
            var ips = new StringBuilder();
            foreach (IPAddress dns in props.DnsAddresses)
            {
                if (SkipDns(dns)) continue;
                if (ips.Length > 0) ips.Append(',');
                ips.Append(dns.ToString());
            }
            return ips.ToString();
        }

        static string SideDnsV6(IPInterfaceProperties props)
        {
            var ips = new StringBuilder();
            foreach (IPAddress dns in props.DnsAddresses)
            {
                if (dns == null || dns.AddressFamily != AddressFamily.InterNetworkV6) continue;
                if (IPAddress.IsLoopback(dns) || dns.Equals(IPAddress.IPv6Any)) continue;
                if (ips.Length > 0) ips.Append(',');
                ips.Append(dns.ToString());
            }
            return ips.ToString();
        }

        static string TunnelDns()
        {
            uint best;
            if (TryBestIndex(out best))
            {
                NetworkInterface[] all = NetworkInterface.GetAllNetworkInterfaces();
                for (int i = 0; i < all.Length; i++)
                {
                    try
                    {
                        IPInterfaceProperties props = all[i].GetIPProperties();
                        if ((uint)props.GetIPv4Properties().Index != best) continue;
                        foreach (IPAddress dns in props.DnsAddresses)
                        {
                            if (!SkipDns(dns)) return dns.ToString();
                        }
                    }
                    catch (NetworkInformationException)
                    {
                    }
                }
            }
            return "172.29.172.254";
        }

        static string RedirectSideDns(string backup, string tunnelDns)
        {
            var blocked = new StringBuilder();
            if (string.IsNullOrEmpty(backup)) return "";
            string[] lines = backup.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string mode, name, ips;
                SplitBackup(lines[i], out mode, out name, out ips);
                if (string.IsNullOrEmpty(name)) continue;
                string quoted = Quote(name);
                int code = Exec("netsh.exe", "interface ipv4 set dnsservers name=\"" + quoted + "\" static " + tunnelDns + " primary validate=no");
                if (code != 0) Log.Write("не сменил DNS " + name + ", код " + code);
                Exec("netsh.exe", "interface ipv6 set dnsservers name=\"" + quoted + "\" static ::1 primary validate=no");
                AddIps(blocked, ips);
                AddIps(blocked, Field(lines[i], 3));
            }
            return blocked.ToString();
        }

        static bool RestoreSideDns(string backup)
        {
            string[] lines = backup.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string mode, name, ips;
                SplitBackup(lines[i], out mode, out name, out ips);
                if (string.IsNullOrEmpty(name)) continue;
                string quoted = Quote(name);
                bool ok = true;
                if (mode == "dhcp")
                    ok = Exec("netsh.exe", "interface ipv4 set dnsservers name=\"" + quoted + "\" source=dhcp") == 0;
                else
                {
                    string[] list = string.IsNullOrEmpty(ips) ? new string[0] : ips.Split(',');
                    if (list.Length > 0)
                    {
                        ok = Exec("netsh.exe", "interface ipv4 set dnsservers name=\"" + quoted + "\" static " + list[0] + " primary validate=no") == 0;
                        for (int n = 1; n < list.Length; n++)
                            Exec("netsh.exe", "interface ipv4 add dnsservers name=\"" + quoted + "\" " + list[n] + " index=" + (n + 1) + " validate=no");
                    }
                }
                string v6 = Field(lines[i], 3);
                if (string.IsNullOrEmpty(v6))
                    Exec("netsh.exe", "interface ipv6 set dnsservers name=\"" + quoted + "\" source=dhcp");
                else
                {
                    string[] list6 = v6.Split(',');
                    Exec("netsh.exe", "interface ipv6 set dnsservers name=\"" + quoted + "\" static " + list6[0] + " primary validate=no");
                    for (int n = 1; n < list6.Length; n++)
                        Exec("netsh.exe", "interface ipv6 add dnsservers name=\"" + quoted + "\" " + list6[n] + " index=" + (n + 1) + " validate=no");
                }
                if (!ok) return false;
            }
            return true;
        }

        static void ReplaceRule(string rule, string protocol, string port, string ips)
        {
            Exec("netsh.exe", "advfirewall firewall delete rule name=\"" + rule + "\"");
            if (string.IsNullOrEmpty(ips)) return;
            Exec("netsh.exe", "advfirewall firewall add rule name=\"" + rule + "\" dir=out action=block protocol=" + protocol + " remoteport=" + port + " remoteip=" + ips);
        }

        static void RefreshNrpt(string tunnelDns)
        {
            RemoveNrpt();
            Exec("powershell.exe", "-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"Add-DnsClientNrptRule -Namespace '.' -NameServers '" + tunnelDns + "' -DisplayName 'IpTzTray' -Comment 'IpTzTray'\"");
        }

        static void RemoveNrpt()
        {
            Exec("powershell.exe", "-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"Get-DnsClientNrptRule | Where-Object { $_.DisplayName -eq 'IpTzTray' } | Remove-DnsClientNrptRule -Force\"");
        }

        static void AddIps(StringBuilder list, string ips)
        {
            if (string.IsNullOrEmpty(ips)) return;
            string[] parts = ips.Split(',');
            for (int i = 0; i < parts.Length; i++)
            {
                string ip = parts[i].Trim();
                int zone = ip.IndexOf('%');
                if (zone >= 0) ip = ip.Substring(0, zone);
                if (ip.Length == 0) continue;
                if (list.Length > 0) list.Append(',');
                list.Append(ip);
            }
        }

        internal static string JoinBackup(string name, bool dhcp, string ips)
        {
            return JoinBackup(name, dhcp, ips, null);
        }

        internal static string JoinBackup(string name, bool dhcp, string ips, string v6)
        {
            if (name == null) name = "";
            name = name.Replace("\t", " ").Replace("\n", " ").Replace("\r", " ");
            string line = (dhcp ? "dhcp" : "static") + "\t" + name + "\t" + (ips ?? "");
            if (string.IsNullOrEmpty(v6)) return line;
            return line + "\t" + v6.Replace("\t", " ").Replace("\n", " ").Replace("\r", " ");
        }

        static string Field(string line, int index)
        {
            if (string.IsNullOrEmpty(line) || index < 0) return "";
            string[] parts = line.Split('\t');
            if (index >= parts.Length) return "";
            return parts[index];
        }

        internal static void SplitBackup(string line, out string mode, out string name, out string ips)
        {
            mode = "";
            name = "";
            ips = "";
            if (string.IsNullOrEmpty(line)) return;
            string[] parts = line.Split('\t');
            if (parts.Length > 0) mode = parts[0];
            if (parts.Length > 1) name = parts[1];
            if (parts.Length > 2) ips = parts[2];
        }

        static string Quote(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            return name.Replace("\"", "");
        }

        static string RestartDns()
        {
            int stop = Exec("net.exe", "stop dnscache /y");
            int start = Exec("net.exe", "start dnscache");
            int nla = Exec("net.exe", "start nlasvc");
            Exec("net.exe", "start netprofm");
            Exec("ipconfig.exe", "/flushdns");
            if (start == 0 || start == 2)
            {
                if (nla != 0 && nla != 2)
                    return Lang.T("dns.restart.partial");
                return Lang.T("dns.restart.ok");
            }
            Log.Write("не перезапустил dnscache, stop=" + stop + " start=" + start);
            return Lang.T("dns.restart.reboot");
        }

        static int Exec(string file, string args)
        {
            try
            {
                var start = new ProcessStartInfo();
                start.FileName = file;
                start.Arguments = args;
                start.UseShellExecute = false;
                start.CreateNoWindow = true;
                start.RedirectStandardOutput = true;
                start.RedirectStandardError = true;
                Process process = Process.Start(start);
                if (process == null) return -1;
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                if (!process.WaitForExit(30000)) return -1;
                string detail = string.IsNullOrWhiteSpace(error) ? output : error;
                Log.Write(file + " " + args + " → " + process.ExitCode + " " + Short(detail));
                return process.ExitCode;
            }
            catch (Exception ex)
            {
                Log.Write(file + " " + args + " не запустился: " + ex.Message);
                return -1;
            }
        }

        static string ExecText(string file, string args)
        {
            try
            {
                var start = new ProcessStartInfo();
                start.FileName = file;
                start.Arguments = args;
                start.UseShellExecute = false;
                start.CreateNoWindow = true;
                start.RedirectStandardOutput = true;
                start.RedirectStandardError = true;
                Process process = Process.Start(start);
                if (process == null) return "";
                string output = process.StandardOutput.ReadToEnd();
                process.StandardError.ReadToEnd();
                if (!process.WaitForExit(30000)) return output ?? "";
                return output ?? "";
            }
            catch (Exception ex)
            {
                Log.Write(file + " " + args + " не запустился: " + ex.Message);
                return "";
            }
        }

        static List<SideDns> ListSides(uint best)
        {
            var list = new List<SideDns>();
            NetworkInterface[] all = NetworkInterface.GetAllNetworkInterfaces();
            for (int i = 0; i < all.Length; i++)
            {
                NetworkInterface ni = all[i];
                if (ni.OperationalStatus != OperationalStatus.Up) continue;
                if (IsIgnoredAdapter(ni.Description, ni.Name, ni.NetworkInterfaceType)) continue;
                try
                {
                    IPInterfaceProperties props = ni.GetIPProperties();
                    int index = props.GetIPv4Properties().Index;
                    if ((uint)index == best) continue;
                    IPAddress local = LocalV4(props);
                    if (local == null) continue;
                    foreach (IPAddress dns in props.DnsAddresses)
                    {
                        if (SkipDns(dns)) continue;
                        var side = new SideDns();
                        side.Name = ni.Name;
                        side.Local = local;
                        side.Index = index;
                        side.Dns = dns;
                        list.Add(side);
                        break;
                    }
                }
                catch (NetworkInformationException)
                {
                }
            }
            return list;
        }

        static IPAddress LocalV4(IPInterfaceProperties props)
        {
            foreach (UnicastIPAddressInformation uni in props.UnicastAddresses)
            {
                if (uni.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                if (IPAddress.IsLoopback(uni.Address)) continue;
                byte[] b = uni.Address.GetAddressBytes();
                if (b[0] == 0 || (b[0] == 169 && b[1] == 254)) continue;
                return uni.Address;
            }
            return null;
        }

        static string FindName(uint index)
        {
            NetworkInterface[] all = NetworkInterface.GetAllNetworkInterfaces();
            for (int i = 0; i < all.Length; i++)
            {
                try
                {
                    if ((uint)all[i].GetIPProperties().GetIPv4Properties().Index == index) return all[i].Name;
                }
                catch (NetworkInformationException)
                {
                }
            }
            return null;
        }

        static bool Probe(IPAddress local, int ifIndex, IPAddress dns, out string error)
        {
            error = null;
            ushort id = (ushort)(Guid.NewGuid().GetHashCode() & 0xffff);
            if (id == 0) id = 1;
            string name = "t" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".invalid";
            byte[] query = BuildQuery(id, name);
            UdpClient client = null;
            try
            {
                client = new UdpClient(new IPEndPoint(local, 0));
                client.Client.ReceiveTimeout = 1000;
                client.Client.SetSocketOption(SocketOptionLevel.IP, (SocketOptionName)31, IPAddress.HostToNetworkOrder(ifIndex));
                client.Send(query, query.Length, new IPEndPoint(dns, 53));
                IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);
                byte[] response = client.Receive(ref remote);
                return ResponseMatches(response, id);
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
            finally
            {
                if (client != null) client.Close();
            }
        }

        static byte[] BuildQuery(ushort id, string host)
        {
            var ms = new MemoryStream();
            ms.WriteByte((byte)(id >> 8));
            ms.WriteByte((byte)(id & 0xff));
            ms.WriteByte(0x01);
            ms.WriteByte(0x00);
            ms.WriteByte(0x00);
            ms.WriteByte(0x01);
            ms.WriteByte(0x00);
            ms.WriteByte(0x00);
            ms.WriteByte(0x00);
            ms.WriteByte(0x00);
            ms.WriteByte(0x00);
            ms.WriteByte(0x00);
            string[] labels = host.Split('.');
            for (int i = 0; i < labels.Length; i++)
            {
                byte[] raw = Encoding.ASCII.GetBytes(labels[i]);
                if (raw.Length == 0 || raw.Length > 63) throw new Exception("плохое имя проверки");
                ms.WriteByte((byte)raw.Length);
                ms.Write(raw, 0, raw.Length);
            }
            ms.WriteByte(0x00);
            ms.WriteByte(0x00);
            ms.WriteByte(0x01);
            ms.WriteByte(0x00);
            ms.WriteByte(0x01);
            return ms.ToArray();
        }

        internal static bool ResponseMatches(byte[] packet, ushort id)
        {
            if (packet == null || packet.Length < 12) return false;
            return packet[0] == (byte)(id >> 8) && packet[1] == (byte)(id & 0xff);
        }

        internal static bool IsIgnoredAdapter(string description, string name, NetworkInterfaceType type)
        {
            if (type == NetworkInterfaceType.Loopback || type == NetworkInterfaceType.Tunnel) return true;
            string text = (description ?? "") + " " + (name ?? "");
            string[] words = new string[] {
                "Hyper-V", "vEthernet", "WSL", "Virtual", "Bluetooth", "TAP-Windows", "Loopback", "Pseudo"
            };
            for (int i = 0; i < words.Length; i++)
            {
                if (text.IndexOf(words[i], StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }
            return false;
        }

        internal static bool SkipDns(IPAddress addr)
        {
            if (addr == null) return true;
            if (IPAddress.IsLoopback(addr)) return true;
            if (addr.AddressFamily != AddressFamily.InterNetwork) return true;
            byte[] b = addr.GetAddressBytes();
            if (b[0] == 0) return true;
            return false;
        }

        static string Short(string message)
        {
            if (string.IsNullOrEmpty(message)) return "";
            message = message.Replace("\r", " ").Replace("\n", " ").Trim();
            if (message.Length > 160) return message.Substring(0, 160);
            return message;
        }

        [System.Runtime.InteropServices.DllImport("iphlpapi.dll")]
        static extern int GetBestInterface(uint destAddr, out uint bestIfIndex);

        sealed class SideDns
        {
            public string Name;
            public IPAddress Local;
            public int Index;
            public IPAddress Dns;
        }
    }
}
