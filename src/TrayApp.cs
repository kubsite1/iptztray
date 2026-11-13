using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace IpTzTray
{
    sealed class TrayApp : ApplicationContext
    {
        readonly NotifyIcon _icon;
        readonly Form _host;
        readonly Timer _timer;
        readonly ToolStripLabel _place;
        readonly ToolStripLabel _ipLine;
        readonly ToolStripLabel _zoneLine;
        readonly ToolStripMenuItem _check;
        readonly ToolStripMenuItem _pause;
        readonly ToolStripMenuItem _home;
        readonly ToolStripMenuItem _unpin;
        readonly ToolStripMenuItem _elevate;
        readonly ToolStripMenuItem _copy;
        readonly ToolStripMenuItem _dnsClose;
        readonly ToolStripMenuItem _settings;
        readonly ToolStripMenuItem _about;
        readonly ToolStripMenuItem _exit;
        readonly Timer _dnsTimer;

        GeoHit _known;
        int _generation;
        bool _busy;
        bool _closing;
        bool _runAgain;
        bool _allowCacheNext = true;
        bool _reportNext;
        int _delayNext;
        bool _checkAgainSoon;
        string _error;
        string _lastLoggedError;
        string _lastNotifiedId;
        string _lastFailedId;
        DateTime _lastFailUtc = DateTime.MinValue;
        DateTime _followNetUntil = DateTime.MinValue;
        uint _dnsRoute;
        int _dnsGeneration;
        bool _dnsBusy;
        bool _dnsAgain;
        bool _dnsSoon;
        bool _dnsSnooze;
        bool _dnsAnnounce;
        bool _dnsDialog;
        bool _dnsOnTunnel;
        bool _dnsReleaseHome;
        bool _dnsVerify;
        int _dnsAct;
        bool _dnsStartupLeft;
        Form _front;
        DnsReport _dnsReport;
        bool _homeRestored;

        public TrayApp()
        {
            SettingsStore.Load();
            Lang.Use(SettingsStore.Current.Lang);
            Lang.KeepLeftToRight();
            if (string.IsNullOrEmpty(SettingsStore.Current.HomeTimeZoneId))
            {
                // До первой подстройки: иначе домашним стал бы пояс VPN.
                SettingsStore.Current.HomeTimeZoneId = WinTz.CurrentId();
                SettingsStore.Save();
                Log.Write("домашний пояс: " + SettingsStore.Current.HomeTimeZoneId);
            }

            if (Admin.Elevated && WinTz.DisableAuto())
                Log.Write("выключил автопояс Windows, чтобы он не спорил с утилитой");

            string bootNote = EnsureAutoStart();
            Log.Write("старт, права=" + Admin.Elevated + ", просмотр=" + AppFlags.DryRun + ", пояс=" + WinTz.CurrentId());
            string dnsNote = FinishDnsRequest();
            uint route;
            if (DnsGuard.TryBestIndex(out route)) _dnsRoute = route;
            _dnsOnTunnel = DnsGuard.RouteIsTunnel();
            if (!_dnsOnTunnel && DnsGuard.IsHolding())
            {
                try
                {
                    dnsNote = JoinNotes(SettingsStore.Current.DnsMode == 0 ? DnsGuard.Revert() : DnsGuard.Release(), dnsNote);
                }
                catch (Exception ex)
                {
                    Log.Write(ex.Message);
                    dnsNote = JoinNotes(ex.Message, dnsNote);
                }
            }

            _host = new Form();
            _host.ShowInTaskbar = false;
            _host.FormBorderStyle = FormBorderStyle.None;
            _host.StartPosition = FormStartPosition.Manual;
            _host.Location = new Point(-32000, -32000);
            _host.Size = new Size(1, 1);
            var handle = _host.Handle;
            if (handle == IntPtr.Zero) throw new InvalidOperationException("нет окна для трея");

            _place = new ToolStripLabel(Lang.T("status.checking"));
            _place.Font = new Font(Lang.UiFont, FontStyle.Bold);
            _place.ForeColor = SystemColors.ControlText;
            _place.Padding = new Padding(0, 5, 0, 1);
            _ipLine = new ToolStripLabel(" ");
            _ipLine.ForeColor = SystemColors.GrayText;
            _ipLine.Padding = new Padding(0, 1, 0, 0);
            _zoneLine = new ToolStripLabel(" ");
            _zoneLine.ForeColor = SystemColors.GrayText;
            _zoneLine.Padding = new Padding(0, 1, 0, 5);
            _check = new ToolStripMenuItem(Lang.T("menu.check"), null, delegate { AskCheck(); });
            _pause = new ToolStripMenuItem(Lang.T("menu.pause"), null, delegate { TogglePause(); });
            _home = new ToolStripMenuItem(Lang.T("menu.home"), null, delegate { GoHome(); });
            _unpin = new ToolStripMenuItem(Lang.T("menu.unpin"), null, delegate { Unpin(); });
            _elevate = new ToolStripMenuItem(Lang.T("menu.elevate"), null, delegate { Handoff(); });
            _copy = new ToolStripMenuItem(Lang.T("menu.copy"), null, delegate { CopyIp(); });
            _dnsClose = new ToolStripMenuItem(Lang.T("menu.dns"), null, delegate { OfferClose(); });
            _settings = new ToolStripMenuItem(Lang.T("menu.settings"), null, delegate { OpenSettings(); });
            _about = new ToolStripMenuItem(Lang.T("menu.about"), null, delegate { OpenAbout(); });
            _exit = new ToolStripMenuItem(Lang.T("menu.exit"), null, delegate { Exit(); });

            var menu = new ContextMenuStrip();
            menu.Font = Lang.UiFont;
            menu.RightToLeft = RightToLeft.No;
            menu.ShowImageMargin = false;
            menu.Renderer = new MenuLook();
            menu.Items.Add(_place);
            menu.Items.Add(_ipLine);
            menu.Items.Add(_zoneLine);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(_check);
            menu.Items.Add(_pause);
            menu.Items.Add(_home);
            menu.Items.Add(_unpin);
            menu.Items.Add(_dnsClose);
            menu.Items.Add(_elevate);
            menu.Items.Add(_copy);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(_settings);
            menu.Items.Add(_about);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(_exit);
            menu.Opening += delegate { RefreshMenu(); };

            _icon = new NotifyIcon();
            _icon.ContextMenuStrip = menu;
            _icon.MouseDoubleClick += delegate(object sender, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left) AskCheck();
            };
            UpdateUi();
            _icon.Visible = true;
            string hello = JoinNotes(dnsNote, bootNote);
            if (!string.IsNullOrEmpty(hello)) Balloon(hello);

            _dnsTimer = new Timer();
            _dnsTimer.Interval = 4000;
            _dnsTimer.Tick += delegate { OnDnsTick(); };
            if (SettingsStore.Current.DnsMode == 2 && _dnsOnTunnel) _dnsTimer.Start();
            else if (SettingsStore.Current.DnsMode == 1)
            {
                _dnsStartupLeft = true;
                _host.BeginInvoke((MethodInvoker)delegate { StartDnsCheck(); });
            }

            _timer = new Timer();
            _timer.Interval = 800;
            _timer.Tick += delegate { StartCheck(true); };
            _timer.Start();
            NetworkChange.NetworkAddressChanged += OnNetwork;
            SystemEvents.SessionEnding += OnSessionEnding;
        }

        string EnsureAutoStart()
        {
            if (AppFlags.DryRun || !Admin.Elevated) return null;
            bool first = !SettingsStore.Current.AutoStartConfigured;
            if (!first && !SettingsStore.Current.AutoStartDirty) return null;
            try
            {
                bool want = first || SettingsStore.Current.AutoStart;
                if (want) AutoStart.Install();
                else AutoStart.Remove();
                SettingsStore.Current.AutoStart = want;
                SettingsStore.Current.AutoStartConfigured = true;
                SettingsStore.Current.AutoStartDirty = false;
                SettingsStore.Save();
                return null;
            }
            catch (Exception ex)
            {
                Log.Write("автозапуск не включился: " + ex.Message);
                return Lang.F("err.autostart", ex.Message);
            }
        }

        void OnNetwork(object sender, EventArgs e)
        {
            if (_closing || _host.IsDisposed) return;
            try
            {
                _host.BeginInvoke((MethodInvoker)delegate { BumpSoon(); });
            }
            catch (InvalidOperationException)
            {
            }
        }

        void BumpSoon()
        {
            if (_closing) return;
            if (_followNetUntil <= DateTime.UtcNow)
            {
                // Включение VPN шлёт несколько событий и поднимает маршрут не сразу.
                Log.Write("сеть изменилась, проверка через 5 с");
                _followNetUntil = DateTime.UtcNow.AddSeconds(25);
            }
            uint best;
            if (DnsGuard.TryBestIndex(out best)) _dnsRoute = best;
            bool tunnel = DnsGuard.RouteIsTunnel();
            if (tunnel != _dnsOnTunnel)
            {
                if (tunnel && SettingsStore.Current.DnsMode == 2)
                {
                    _dnsOnTunnel = true;
                    _dnsReleaseHome = false;
                    _dnsSnooze = false;
                    _dnsSoon = true;
                    if (!_dnsBusy)
                    {
                        _dnsTimer.Stop();
                        _dnsTimer.Interval = 6000;
                        _dnsTimer.Start();
                    }
                }
                else if (!tunnel && DnsGuard.IsHolding())
                {
                    _dnsReleaseHome = true;
                    _dnsTimer.Stop();
                    _dnsTimer.Interval = 6000;
                    _dnsTimer.Start();
                }
                else _dnsOnTunnel = tunnel;
            }
            if (_busy)
            {
                _runAgain = true;
                _delayNext = 5000;
                return;
            }
            _timer.Stop();
            _timer.Interval = 5000;
            _timer.Start();
        }

        void AskCheck()
        {
            _reportNext = true;
            StartCheck(false);
        }

        void StartCheck(bool allowCache)
        {
            if (_closing) return;
            _allowCacheNext = allowCache;
            _generation++;
            if (_busy)
            {
                _runAgain = true;
                return;
            }
            BeginBackground();
        }

        void BeginBackground()
        {
            _busy = true;
            _timer.Stop();
            int gen = _generation;
            bool allowCache = _allowCacheNext;
            bool report = _reportNext;
            _reportNext = false;
            Dictionary<string, string> pins = SettingsStore.CopyPins();
            GeoHit previous = _known;

            Task.Factory.StartNew(new Func<GeoHit>(delegate
            {
                return Locator.Resolve(allowCache, pins, previous);
            })).ContinueWith(delegate(Task<GeoHit> task)
            {
                string fault = null;
                if (task.IsFaulted) fault = task.Exception.GetBaseException().Message;
                if (_closing || _host.IsDisposed || !_host.IsHandleCreated) return;
                try
                {
                    _host.BeginInvoke((MethodInvoker)delegate
                    {
                        try
                        {
                            if (!_closing && gen == _generation)
                            {
                                if (fault != null)
                                {
                                    Fail(fault);
                                    if (report) Balloon(fault);
                                }
                                else if (!task.IsCanceled && task.Result != null) ApplyResult(task.Result, report);
                            }
                        }
                        catch (Exception ex)
                        {
                            Fail(ex.Message);
                            if (report) Balloon(ex.Message);
                        }
                        finally
                        {
                            _busy = false;
                            ScheduleNext();
                        }
                    });
                }
                catch (InvalidOperationException)
                {
                }
            });
        }

        void ScheduleNext()
        {
            if (_closing) return;
            if (_runAgain)
            {
                _runAgain = false;
                int delay = _delayNext;
                _delayNext = 0;
                if (delay > 0)
                {
                    _timer.Interval = delay;
                    _timer.Start();
                    return;
                }
                BeginBackground();
                return;
            }
            int ms = 10000;
            if (_checkAgainSoon) ms = 2000;
            else if (DateTime.UtcNow < _followNetUntil) ms = 5000;
            _checkAgainSoon = false;
            _timer.Interval = ms;
            _timer.Start();
        }

        void ApplyResult(GeoHit hit, bool report)
        {
            if (!string.IsNullOrEmpty(hit.Error))
            {
                bool otherIp = !string.IsNullOrEmpty(hit.Ip)
                    && (_known == null || !string.Equals(_known.Ip, hit.Ip, StringComparison.OrdinalIgnoreCase));
                if (otherIp)
                {
                    var stub = new GeoHit();
                    stub.Ip = hit.Ip;
                    _known = stub;
                }
                Fail(hit.Error);
                if (report) Balloon(hit.Error);
                return;
            }

            if (hit.FromPin && !WinTz.Known(hit.WindowsId))
            {
                SettingsStore.Current.Pins.Remove(hit.Ip);
                SettingsStore.Save();
                _known = null;
                _checkAgainSoon = true;
                Log.Write("сбросил неизвестное закрепление " + hit.WindowsId);
                return;
            }

            bool ipChanged = _known == null || !string.Equals(_known.Ip, hit.Ip, StringComparison.OrdinalIgnoreCase);
            if (ipChanged) _followNetUntil = DateTime.MinValue;
            _known = hit;
            _error = null;
            _lastLoggedError = null;
            if (ipChanged && !hit.FromCache)
                Log.Write("IP " + hit.Ip + " " + Program.JoinPlace(hit.City, hit.Country) + " " + hit.Iana + " -> " + hit.WindowsId + " (" + hit.Source + ")");

            UpdateUi();
            if (SettingsStore.Current.Paused)
            {
                if (report) Balloon(Lang.F("balloon.paused", PlaceText(hit)));
                return;
            }

            string current = WinTz.CurrentId();
            bool same = string.Equals(current, hit.WindowsId, StringComparison.OrdinalIgnoreCase);
            if (AppFlags.DryRun)
            {
                if (!Welcome(!same, hit) && report)
                    Balloon(Lang.F("balloon.dry", PlaceText(hit)));
                return;
            }
            if (!Admin.Elevated)
            {
                _error = Lang.T("err.noadmin");
                UpdateUi();
                if (report) Balloon(Lang.F("balloon.noadmin.ip", PlaceText(hit)));
                return;
            }
            if (same)
            {
                if (!Welcome(false, hit) && report)
                    Balloon(Lang.F("balloon.same", PlaceText(hit)));
                return;
            }

            if (!AppFlags.DryRun
                && string.Equals(_lastFailedId, hit.WindowsId, StringComparison.OrdinalIgnoreCase)
                && DateTime.UtcNow - _lastFailUtc < TimeSpan.FromMinutes(2))
            {
                if (report) Balloon(_error ?? Lang.T("balloon.fail"));
                return;
            }

            try
            {
                WinTz.Apply(hit.WindowsId);
                _lastFailedId = null;
                if (!AppFlags.DryRun)
                    Log.Write("пояс " + current + " -> " + hit.WindowsId);
                UpdateUi();
                if (!Welcome(true, hit) && report)
                    Balloon(Lang.F("balloon.set", PlaceText(hit)));
            }
            catch (Exception ex)
            {
                _lastFailedId = hit.WindowsId;
                _lastFailUtc = DateTime.UtcNow;
                _error = ex.Message;
                Log.Write("не поставил " + hit.WindowsId + ": " + ex.Message);
                UpdateUi();
                if (report) Balloon(ex.Message);
            }
        }

        bool Welcome(bool changed, GeoHit hit)
        {
            string place = PlaceText(hit);
            if (!SettingsStore.Current.SeenWelcome)
            {
                SettingsStore.Current.SeenWelcome = true;
                SettingsStore.Save();
                string home = WinTz.Display(SettingsStore.Current.HomeTimeZoneId);
                string tail = Lang.F("welcome.home", home);
                if (!AppFlags.DryRun && SettingsStore.Current.AutoStart)
                    tail += " " + Lang.T("welcome.auto");
                if (AppFlags.DryRun)
                    Balloon(Lang.F("balloon.dry.welcome", place, tail));
                else if (changed)
                    Balloon(Lang.F("balloon.set.welcome", place, tail));
                else
                    Balloon(Lang.F("balloon.same.welcome", tail));
                _lastNotifiedId = hit.WindowsId;
                return true;
            }

            if (!changed) return false;
            if (string.Equals(_lastNotifiedId, hit.WindowsId, StringComparison.OrdinalIgnoreCase))
                return false;
            _lastNotifiedId = hit.WindowsId;
            if (AppFlags.DryRun) Balloon(Lang.F("balloon.dry.place", place));
            else Balloon(Lang.F("balloon.place", place));
            return true;
        }

        void Fail(string message)
        {
            _error = message;
            if (message != _lastLoggedError)
            {
                _lastLoggedError = message;
                Log.Write(message);
            }
            UpdateUi();
        }

        void UpdateUi()
        {
            string currentId = WinTz.CurrentId();
            TimeSpan offset = TimeZoneInfo.Local.GetUtcOffset(DateTime.Now);
            string label = Program.FormatOffset(offset);
            string desired = _known == null ? null : _known.WindowsId;
            bool known = !string.IsNullOrEmpty(desired);
            bool match = known && string.Equals(currentId, desired, StringComparison.OrdinalIgnoreCase);
            Color color = Badge.Idle;
            if (_error != null && !known) color = Badge.Error;
            else if (match) color = Badge.Home;
            else if (known) color = Badge.Away;
            try { _icon.Icon = Badge.Get(color, _error != null && !known ? "!" : label); }
            catch { _icon.Icon = SystemIcons.Application; }
            _icon.Text = Tip(BuildTip(label, match, known));
            RefreshMenu();
        }

        string BuildTip(string label, bool match, bool known)
        {
            string city = _known == null ? "" : (_known.City ?? "");
            string text = label;
            if (!string.IsNullOrEmpty(city)) text += " " + city;
            if (_known != null && !string.IsNullOrEmpty(_known.Ip)) text += " " + _known.Ip;
            if (SettingsStore.Current.Paused) text = Lang.F("tip.pause", text);
            if (known && !match) text += Lang.T("tip.mismatch");
            if (_error != null) text = _error;
            return text;
        }

        void RefreshMenu()
        {
            string city = _known == null ? null : Program.JoinPlace(_known.City, _known.Country);
            string place;
            if (_error != null) place = _error;
            else if (_known != null && _known.FromPin && string.IsNullOrEmpty(city)) place = Lang.T("status.pinned");
            else if (!string.IsNullOrEmpty(city)) place = city;
            else place = Lang.T("status.checking");

            string ip;
            if (_known != null && !string.IsNullOrEmpty(_known.Ip))
            {
                ip = _known.Ip;
                if (!string.IsNullOrEmpty(_known.Iana)) ip += " · " + _known.Iana;
                else if (_known.FromPin) ip += " · " + Lang.T("status.pinned");
            }
            else ip = Lang.T("status.noip");

            string current = WinTz.Display(WinTz.CurrentId());
            string desired = _known == null ? null : _known.WindowsId;
            bool match = desired != null && string.Equals(WinTz.CurrentId(), desired, StringComparison.OrdinalIgnoreCase);
            string zone = desired != null && !match
                ? Lang.F("status.now", current, WinTz.Display(desired))
                : current;

            _check.Text = Lang.T("menu.check");
            _pause.Text = SettingsStore.Current.Paused ? Lang.T("menu.resume") : Lang.T("menu.pause");
            _home.Text = Lang.T("menu.home");
            _unpin.Text = Lang.T("menu.unpin");
            _elevate.Text = Lang.T("menu.elevate");
            _copy.Text = Lang.T("menu.copy");
            _dnsClose.Text = Lang.T("menu.dns");
            _settings.Text = Lang.T("menu.settings");
            _about.Text = Lang.T("menu.about");
            _exit.Text = Lang.T("menu.exit");
            _home.Enabled = !string.IsNullOrEmpty(SettingsStore.Current.HomeTimeZoneId);
            bool hasIp = _known != null && !string.IsNullOrEmpty(_known.Ip);
            _copy.Enabled = hasIp;
            bool pinned = hasIp && SettingsStore.Current.Pins != null && SettingsStore.Current.Pins.ContainsKey(_known.Ip);
            _unpin.Visible = pinned;
            _elevate.Visible = !Admin.Elevated && !AppFlags.DryRun;
            _dnsClose.Visible = _dnsReport != null && _dnsReport.Leak && !SettingsStore.Current.DnsClosed;

            int cap = 0;
            foreach (ToolStripItem item in _icon.ContextMenuStrip.Items)
            {
                if (item is ToolStripLabel || item is ToolStripSeparator || !item.Available) continue;
                cap = Math.Max(cap, TextWidth(item.Text, item.Font));
            }
            if (cap < 80) cap = 80;
            SetStatus(_place, place, cap);
            SetStatus(_ipLine, ip, cap);
            SetStatus(_zoneLine, zone, cap);
            _place.ForeColor = _error != null ? Color.DarkRed : SystemColors.ControlText;
            _ipLine.ForeColor = SystemColors.GrayText;
            _zoneLine.ForeColor = SystemColors.GrayText;
        }

        static void SetStatus(ToolStripLabel label, string text, int maxWidth)
        {
            string shown = Fit(text, label.Font, maxWidth);
            label.Text = shown;
            label.ToolTipText = shown == text ? "" : text;
        }

        static string Fit(string text, Font font, int maxWidth)
        {
            if (string.IsNullOrEmpty(text)) return "";
            if (TextWidth(text, font) <= maxWidth) return text;
            const string ell = "…";
            int lo = 0;
            int hi = text.Length;
            while (lo < hi)
            {
                int mid = (lo + hi + 1) / 2;
                string cut = text.Substring(0, mid).TrimEnd() + ell;
                if (TextWidth(cut, font) <= maxWidth) lo = mid;
                else hi = mid - 1;
            }
            if (lo <= 0) return ell;
            return text.Substring(0, lo).TrimEnd() + ell;
        }

        static int TextWidth(string text, Font font)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            var flags = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine;
            return TextRenderer.MeasureText(text, font, new Size(int.MaxValue, int.MaxValue), flags).Width;
        }

        static string Tip(string text)
        {
            if (string.IsNullOrEmpty(text)) return AppInfo.Product;
            text = text.Replace("\r", " ").Replace("\n", " ");
            if (text.Length > 63) return text.Substring(0, 63);
            return text;
        }

        static string PlaceText(GeoHit hit)
        {
            if (hit != null && hit.FromPin) return WinTz.Display(hit.WindowsId);
            string place = Program.JoinPlace(hit.City, hit.Country);
            if (!string.IsNullOrEmpty(place)) return place;
            return WinTz.Display(hit.WindowsId);
        }

        void TogglePause()
        {
            SettingsStore.Current.Paused = !SettingsStore.Current.Paused;
            SettingsStore.Save();
            Log.Write(SettingsStore.Current.Paused ? "пауза" : "автосмена включена");
            UpdateUi();
            if (!SettingsStore.Current.Paused) StartCheck(true);
        }

        void GoHome()
        {
            string home = SettingsStore.Current.HomeTimeZoneId;
            if (string.IsNullOrEmpty(home)) return;
            _generation++;
            SettingsStore.Current.Paused = true;
            SettingsStore.Save();
            try
            {
                if (!string.Equals(WinTz.CurrentId(), home, StringComparison.OrdinalIgnoreCase))
                    WinTz.Apply(home);
                _lastNotifiedId = home;
                _error = null;
                UpdateUi();
                Balloon(Lang.F("balloon.home.pause", Lang.T("menu.resume")));
            }
            catch (Exception ex)
            {
                _error = ex.Message;
                Log.Write(ex.Message);
                UpdateUi();
                Balloon(ex.Message);
            }
        }

        void RememberHome()
        {
            SettingsStore.Current.HomeTimeZoneId = WinTz.CurrentId();
            SettingsStore.Save();
            Log.Write("новый домашний пояс: " + SettingsStore.Current.HomeTimeZoneId);
            Balloon(Lang.F("balloon.home", WinTz.Display(SettingsStore.Current.HomeTimeZoneId)));
            UpdateUi();
        }

        void Unpin()
        {
            if (_known == null || string.IsNullOrEmpty(_known.Ip)) return;
            if (SettingsStore.Current.Pins != null) SettingsStore.Current.Pins.Remove(_known.Ip);
            SettingsStore.Save();
            _known = null;
            Log.Write("закрепление снято");
            StartCheck(false);
        }

        void OpenAbout()
        {
            using (var dialog = new AboutDialog())
            {
                dialog.Icon = WindowIcon();
                _front = dialog;
                try { dialog.ShowDialog(_host); }
                finally { _front = null; }
            }
        }

        void OpenSettings()
        {
            using (var dialog = new SettingsDialog(
                delegate { RememberHome(); },
                delegate(int mode) { RunManualDns(mode); }))
            {
                dialog.Icon = WindowIcon();
                _front = dialog;
                try
                {
                    if (dialog.ShowDialog(_host) != DialogResult.OK) return;
                    ApplySettings(dialog);
                }
                finally { _front = null; }
            }
        }

        void ApplyChrome()
        {
            Lang.Use(SettingsStore.Current.Lang);
            if (_icon != null && _icon.ContextMenuStrip != null)
            {
                _icon.ContextMenuStrip.Font = Lang.UiFont;
                _icon.ContextMenuStrip.RightToLeft = RightToLeft.No;
            }
            if (_place != null)
            {
                var bold = new Font(Lang.UiFont, FontStyle.Bold);
                Font old = _place.Font;
                _place.Font = bold;
                if (old != null && !object.ReferenceEquals(old, Lang.UiFont)) old.Dispose();
            }
            if (_icon != null && _icon.ContextMenuStrip != null) RefreshMenu();
        }

        void ApplySettings(SettingsDialog dialog)
        {
            string nextLang = dialog.LanguageCode ?? "";
            bool langChanged = !string.Equals(nextLang, SettingsStore.Current.Lang ?? "", StringComparison.OrdinalIgnoreCase);
            SettingsStore.Current.Lang = nextLang;
            ApplyChrome();
            SettingsStore.Current.RestoreHomeOnExit = dialog.RestoreHome;
            SettingsStore.Current.CheckTimeoutSec = dialog.TimeoutSec;
            int mode = dialog.DnsMode;
            int prev = SettingsStore.Current.DnsMode;
            bool wantAuto = dialog.AutoStart;
            bool autoChanged = wantAuto != SettingsStore.Current.AutoStart || !SettingsStore.Current.AutoStartConfigured;
            SettingsStore.Current.DnsMode = mode;
            SettingsStore.SyncDnsFlags(SettingsStore.Current);

            bool revert = mode == 0 && prev != 0 && (prev == 2 || DnsGuard.IsHolding());
            if (revert && !AppFlags.DryRun && !Admin.Elevated && DnsGuard.RestoreNeedsAdmin())
            {
                SettingsStore.Current.DnsRevertRequested = true;
                if (autoChanged)
                {
                    SettingsStore.Current.AutoStart = wantAuto;
                    SettingsStore.Current.AutoStartConfigured = true;
                    SettingsStore.Current.AutoStartDirty = true;
                }
                SettingsStore.Save();
                Handoff();
                return;
            }
            if (revert)
            {
                try
                {
                    string msg = DnsGuard.Revert();
                    _dnsReport = null;
                    _dnsTimer.Stop();
                    Balloon(msg);
                }
                catch (Exception ex)
                {
                    Log.Write(ex.Message);
                    Balloon(ex.Message);
                }
            }
            else if (mode != 2)
            {
                _dnsTimer.Stop();
                if (prev == 2) Log.Write("слежение за утечкой DNS выключено");
            }

            SettingsStore.Current.DnsMode = mode;
            SettingsStore.SyncDnsFlags(SettingsStore.Current);
            SettingsStore.Save();
            if (langChanged && !AppFlags.DryRun && Admin.Elevated && SettingsStore.Current.AutoStart)
            {
                try { AutoStart.Install(); }
                catch (Exception ex) { Log.Write(ex.Message); }
            }
            if (mode == 2 && prev != 2) ArmWatch();
            ApplyAutoStart(wantAuto, autoChanged);
            UpdateUi();
        }

        void ApplyAutoStart(bool want, bool changed)
        {
            if (!changed) return;
            if (AppFlags.DryRun)
            {
                SettingsStore.Current.AutoStart = want;
                SettingsStore.Current.AutoStartConfigured = true;
                SettingsStore.Current.AutoStartDirty = false;
                SettingsStore.Save();
                return;
            }
            if (!Admin.Elevated)
            {
                SettingsStore.Current.AutoStart = want;
                SettingsStore.Current.AutoStartConfigured = true;
                SettingsStore.Current.AutoStartDirty = true;
                SettingsStore.Save();
                Handoff();
                return;
            }
            try
            {
                if (want) AutoStart.Install();
                else AutoStart.Remove();
                SettingsStore.Current.AutoStart = want;
                SettingsStore.Current.AutoStartConfigured = true;
                SettingsStore.Current.AutoStartDirty = false;
                SettingsStore.Save();
                Log.Write(want ? "автозапуск включён" : "автозапуск выключен");
            }
            catch (Exception ex)
            {
                Log.Write(ex.Message);
                Balloon(ex.Message);
            }
        }

        void ArmWatch()
        {
            _dnsSnooze = false;
            _dnsAnnounce = true;
            _dnsOnTunnel = DnsGuard.RouteIsTunnel();
            Log.Write("слежение за утечкой DNS включено");
            if (!_dnsOnTunnel) return;
            _dnsTimer.Stop();
            _dnsTimer.Interval = 1500;
            _dnsTimer.Start();
        }

        void RunManualDns(int mode)
        {
            if (mode != 1 && mode != 2) return;
            _dnsAct = mode;
            _dnsSnooze = false;
            _dnsAnnounce = true;
            _dnsTimer.Stop();
            StartDnsCheck();
        }

        static Icon WindowIcon()
        {
            try
            {
                Icon associated = Icon.ExtractAssociatedIcon(Admin.AssemblyPath());
                if (associated != null) return associated;
            }
            catch
            {
            }
            return (Icon)Badge.Get(Badge.Home, "+3").Clone();
        }

        void Handoff()
        {
            Program.ReleaseMutex();
            if (!Admin.TryElevate())
            {
                if (!Program.ReacquireMutex()) Balloon(Lang.T("balloon.other"));
                else Balloon(Lang.T("balloon.noadmin"));
                return;
            }
            Exit(false);
        }

        void CopyIp()
        {
            if (_known == null || string.IsNullOrEmpty(_known.Ip)) return;
            try
            {
                Clipboard.SetText(_known.Ip);
                Balloon(Lang.F("balloon.copy", _known.Ip));
            }
            catch (Exception ex)
            {
                Balloon(Lang.F("balloon.copyfail", ex.Message));
            }
        }

        string FinishDnsRequest()
        {
            if (AppFlags.DryRun || !Admin.Elevated) return null;
            try
            {
                if (SettingsStore.Current.DnsRevertRequested) return DnsGuard.Revert();
                if (SettingsStore.Current.DnsCloseRequested) return DnsGuard.Apply();
            }
            catch (Exception ex)
            {
                Log.Write(ex.Message);
                return ex.Message;
            }
            return null;
        }

        static string JoinNotes(string first, string second)
        {
            if (string.IsNullOrEmpty(first)) return second;
            if (string.IsNullOrEmpty(second)) return first;
            return first + " " + second;
        }

        void OnDnsTick()
        {
            _dnsTimer.Stop();
            if (_dnsReleaseHome)
            {
                _dnsReleaseHome = false;
                if (DnsGuard.RouteIsTunnel())
                {
                    _dnsOnTunnel = true;
                    if (!DnsGuard.IsHolding())
                    {
                        _dnsSnooze = false;
                        StartDnsCheck();
                    }
                    return;
                }
                _dnsGeneration++;
                _dnsSnooze = true;
                _dnsOnTunnel = false;
                if (DnsGuard.IsHolding()) ReleaseForHome();
                return;
            }
            StartDnsCheck();
        }

        void StartDnsCheck()
        {
            bool once = _dnsStartupLeft;
            bool manual = _dnsAct != 0;
            bool follow = _dnsVerify;
            if (_closing || (SettingsStore.Current.DnsMode != 2 && !once && !manual && !follow)) return;
            if (once) _dnsStartupLeft = false;
            if (_dnsBusy)
            {
                _dnsAgain = true;
                if (once) _dnsStartupLeft = true;
                return;
            }
            _dnsBusy = true;
            UpdateUi();
            int gen = _dnsGeneration;
            bool announce = _dnsAnnounce;
            _dnsAnnounce = false;
            Task.Factory.StartNew(new Func<DnsReport>(delegate { return DnsGuard.Check(); }))
                .ContinueWith(delegate(Task<DnsReport> task)
                {
                    DnsReport report = new DnsReport();
                    if (task.IsFaulted) report.Summary = report.Error = task.Exception.GetBaseException().Message;
                    else if (task.Result != null) report = task.Result;
                    if (_closing || _host.IsDisposed || !_host.IsHandleCreated) return;
                    try
                    {
                        _host.BeginInvoke((MethodInvoker)delegate { FinishDns(gen, announce, report); });
                    }
                    catch (InvalidOperationException)
                    {
                    }
                });
        }

        void FinishDns(int gen, bool announce, DnsReport report)
        {
            _dnsBusy = false;
            if (_closing) return;
            if (gen != _dnsGeneration)
            {
                _dnsAgain = false;
                return;
            }
            HandleDns(announce, report);
            PumpDns();
        }

        void PumpDns()
        {
            if (_closing) return;
            if (_dnsAgain && (SettingsStore.Current.DnsMode == 2 || _dnsAct != 0 || _dnsStartupLeft || _dnsVerify))
            {
                _dnsAgain = false;
                StartDnsCheck();
                return;
            }
            _dnsAgain = false;
            if (SettingsStore.Current.DnsMode != 2 || !_dnsSoon) return;
            _dnsSoon = false;
            _dnsTimer.Interval = 6000;
            _dnsTimer.Start();
        }

        void HandleDns(bool announce, DnsReport report)
        {
            if (report == null) return;
            string prev = _dnsReport == null ? null : _dnsReport.Summary;
            _dnsReport = report;
            if (prev != report.Summary) Log.Write("dns: " + report.Summary);
            UpdateUi();
            if (_dnsVerify)
            {
                _dnsVerify = false;
                _dnsSnooze = true;
                if (!string.IsNullOrEmpty(report.Error))
                    Balloon(Lang.F("balloon.dns.verifyfail", report.Error));
                else if (!report.Leak)
                    Balloon(Lang.T("balloon.dns.fixed"));
                else
                    Balloon(Lang.F("balloon.dns.still", report.Summary));
                return;
            }
            int act = _dnsAct;
            _dnsAct = 0;
            bool auto = act == 2 || (act == 0 && SettingsStore.Current.DnsMode == 2);
            bool interested = act != 0 || SettingsStore.Current.DnsMode != 0;
            if (report.Leak && interested && !_dnsSnooze && !_dnsDialog && !SettingsStore.Current.DnsClosed)
            {
                if (auto && DnsGuard.RouteIsTunnel())
                {
                    CommitClose();
                    return;
                }
                OfferClose();
                return;
            }
            if (auto && DnsGuard.RouteIsTunnel()
                && SettingsStore.Current.DnsClosed
                && string.IsNullOrEmpty(SettingsStore.Current.DnsAdapterBackup)
                && !_dnsSnooze && !_dnsDialog)
            {
                CommitClose();
                return;
            }
            if (announce && !report.Leak && !string.IsNullOrEmpty(report.Summary))
                Balloon(report.Summary);
        }

        void OfferClose()
        {
            if (_dnsDialog || _closing || _dnsReport == null || !_dnsReport.Leak) return;
            _dnsDialog = true;
            try
            {
                using (var dialog = new DnsCloseDialog(_dnsReport.Where))
                {
                    Form owner = _front != null && _front.Visible ? _front : _host;
                    if (dialog.ShowDialog(owner) != DialogResult.OK)
                    {
                        _dnsSnooze = true;
                        Log.Write("закрытие утечки DNS отложено");
                        return;
                    }
                }
                CommitClose();
            }
            finally
            {
                _dnsDialog = false;
            }
        }

        void CommitClose()
        {
            if (!AppFlags.DryRun && !Admin.Elevated)
            {
                SettingsStore.Current.DnsCloseRequested = true;
                SettingsStore.Save();
                Handoff();
                return;
            }
            try
            {
                string applied = DnsGuard.Apply();
                _dnsSnooze = true;
                UpdateUi();
                if (applied.IndexOf(Lang.T("dns.restart.reboot"), StringComparison.Ordinal) >= 0)
                {
                    Balloon(applied);
                    return;
                }
                _dnsVerify = true;
                StartDnsCheck();
            }
            catch (Exception ex)
            {
                _dnsSnooze = true;
                Log.Write(ex.Message);
                Balloon(ex.Message);
            }
        }

        void ReleaseForHome()
        {
            if (!DnsGuard.IsHolding()) return;
            if (!AppFlags.DryRun && !Admin.Elevated)
            {
                Log.Write("домашняя сеть: нет прав вернуть DNS");
                Balloon(Lang.T("balloon.dns.norestore"));
                return;
            }
            try
            {
                string msg = DnsGuard.Release();
                _dnsReport = null;
                Balloon(msg);
                UpdateUi();
            }
            catch (Exception ex)
            {
                Log.Write(ex.Message);
                Balloon(ex.Message);
            }
        }

        void Balloon(string text)
        {
            if (string.IsNullOrEmpty(text) || _icon == null) return;
            if (text.Length > 220) text = text.Substring(0, 220);
            _icon.ShowBalloonTip(6000, AppInfo.Product, text, ToolTipIcon.Info);
        }

        void OnSessionEnding(object sender, SessionEndingEventArgs e)
        {
            RestoreHome();
        }

        void RestoreHome()
        {
            if (_homeRestored || !SettingsStore.Current.RestoreHomeOnExit) return;
            _homeRestored = true;
            string home = SettingsStore.Current.HomeTimeZoneId;
            if (string.IsNullOrEmpty(home)) return;
            try
            {
                if (string.Equals(WinTz.CurrentId(), home, StringComparison.OrdinalIgnoreCase)) return;
                WinTz.Apply(home);
                Log.Write("выход: пояс -> " + home);
            }
            catch (Exception ex)
            {
                Log.Write("выход, домашний пояс не поставлен: " + ex.Message);
            }
        }

        void Exit(bool restoreHome = true)
        {
            if (_closing) return;
            _closing = true;
            _generation++;
            if (restoreHome) RestoreHome();
            try { NetworkChange.NetworkAddressChanged -= OnNetwork; } catch { }
            try { SystemEvents.SessionEnding -= OnSessionEnding; } catch { }
            if (_timer != null) _timer.Stop();
            if (_dnsTimer != null) _dnsTimer.Stop();
            if (_icon != null)
            {
                _icon.Visible = false;
                _icon.Dispose();
            }
            ExitThread();
        }
    }

    sealed class MenuLook : ToolStripProfessionalRenderer
    {
        static readonly Color Info = Color.FromArgb(245, 245, 245);

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (e.Item is ToolStripLabel)
            {
                using (var brush = new SolidBrush(Info))
                    e.Graphics.FillRectangle(brush, new Rectangle(Point.Empty, e.Item.Size));
                return;
            }
            base.OnRenderMenuItemBackground(e);
        }
    }

    static class DialogShell
    {
        public const int Width = 440;

        public static void Prepare(Form form, string title)
        {
            form.Text = title;
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.MaximizeBox = false;
            form.MinimizeBox = false;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.TopMost = true;
            form.ShowInTaskbar = true;
            form.Font = Lang.UiFont;
            form.RightToLeft = RightToLeft.No;
            form.RightToLeftLayout = false;
            form.AutoScaleMode = AutoScaleMode.Font;
            form.AutoSize = true;
            form.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            form.Padding = new Padding(24);
            form.KeyPreview = true;
        }

        public static FlowLayoutPanel Column()
        {
            var body = new FlowLayoutPanel();
            body.FlowDirection = FlowDirection.TopDown;
            body.WrapContents = false;
            body.AutoSize = true;
            body.Margin = new Padding(0);
            body.MinimumSize = new Size(Width, 0);
            return body;
        }

        public static Label TextLine(string text, Color color)
        {
            var label = new Label();
            label.AutoSize = true;
            label.MinimumSize = new Size(Width, 0);
            label.MaximumSize = new Size(Width, 0);
            label.Margin = new Padding(0, 0, 0, 8);
            label.Text = text;
            label.ForeColor = color;
            return label;
        }

        public static FlowLayoutPanel ButtonRow(Button button)
        {
            var row = new FlowLayoutPanel();
            row.FlowDirection = FlowDirection.RightToLeft;
            row.WrapContents = false;
            row.MinimumSize = new Size(Width, 40);
            row.Width = Width;
            row.Height = 40;
            row.Margin = new Padding(0, 8, 0, 0);
            row.Controls.Add(button);
            return row;
        }

        public static Control Fill(Control child)
        {
            int height = child.Height < 20 ? 23 : child.Height;
            child.Dock = DockStyle.Fill;
            child.Margin = new Padding(0);
            var host = new Panel();
            host.Size = new Size(Width, height);
            host.MinimumSize = host.Size;
            host.Margin = new Padding(0, 0, 0, 8);
            host.Controls.Add(child);
            return host;
        }
    }

    sealed class ZoneDialog : Form
    {
        readonly ComboBox _combo;
        readonly CheckBox _pin;
        public string SelectedId;

        public ZoneDialog(string ip, string currentId)
        {
            DialogShell.Prepare(this, Lang.T("zone.title"));

            var ipLine = DialogShell.TextLine("IP " + ip, SystemColors.ControlText);

            _combo = new ComboBox();
            _combo.DropDownStyle = ComboBoxStyle.DropDownList;
            _combo.Width = DialogShell.Width;
            var zones = new List<TimeZoneInfo>(TimeZoneInfo.GetSystemTimeZones());
            zones.Sort(delegate(TimeZoneInfo a, TimeZoneInfo b)
            {
                int cmp = a.BaseUtcOffset.CompareTo(b.BaseUtcOffset);
                if (cmp != 0) return cmp;
                return string.Compare(a.DisplayName, b.DisplayName, StringComparison.CurrentCulture);
            });
            int select = 0;
            int drop = DialogShell.Width;
            for (int i = 0; i < zones.Count; i++)
            {
                var item = new ZoneItem(zones[i]);
                _combo.Items.Add(item);
                Size size = TextRenderer.MeasureText(item.ToString(), Font);
                if (size.Width + 28 > drop) drop = size.Width + 28;
                if (string.Equals(zones[i].Id, currentId, StringComparison.OrdinalIgnoreCase)) select = i;
            }
            _combo.DropDownWidth = Math.Min(drop, 760);
            if (_combo.Items.Count > 0) _combo.SelectedIndex = select;

            _pin = new CheckBox();
            _pin.Text = Lang.T("zone.pin");
            _pin.Checked = true;
            _pin.AutoSize = true;
            _pin.Margin = new Padding(0, 4, 0, 4);

            var hint = DialogShell.TextLine(Lang.T("zone.hint"), SystemColors.GrayText);

            var ok = new Button();
            ok.Text = Lang.T("zone.apply");
            ok.DialogResult = DialogResult.OK;
            ok.AutoSize = true;
            ok.MinimumSize = new Size(120, 32);
            AcceptButton = ok;

            var body = DialogShell.Column();
            body.Controls.Add(ipLine);
            body.Controls.Add(DialogShell.Fill(_combo));
            body.Controls.Add(_pin);
            body.Controls.Add(hint);
            body.Controls.Add(DialogShell.ButtonRow(ok));
            Controls.Add(body);
        }

        public bool Pin { get { return _pin.Checked; } }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (DialogResult == DialogResult.OK)
            {
                var item = _combo.SelectedItem as ZoneItem;
                if (item == null)
                {
                    e.Cancel = true;
                    return;
                }
                SelectedId = item.Zone.Id;
            }
            base.OnFormClosing(e);
        }

        sealed class ZoneItem
        {
            public readonly TimeZoneInfo Zone;
            public ZoneItem(TimeZoneInfo zone) { Zone = zone; }
            public override string ToString() { return Zone.DisplayName; }
        }
    }

    sealed class AboutDialog : Form
    {
        readonly Font _bold;

        public AboutDialog()
        {
            const int PadX = 40;
            const int PadTop = 32;
            const int PadBottom = 36;
            DialogShell.Prepare(this, Lang.T("about.title"));
            AutoSize = false;
            AutoScaleMode = AutoScaleMode.None;
            Padding = new Padding(0);
            _bold = new Font(Font.FontFamily, Font.Size + 3f, FontStyle.Bold, Font.Unit);

            var name = new Label();
            name.AutoSize = false;
            name.Font = _bold;
            name.Text = AppInfo.Product;

            var version = new Label();
            version.AutoSize = false;
            version.Font = Font;
            version.Text = Lang.F("about.version", AppInfo.Version);

            var who = new Label();
            who.AutoSize = false;
            who.Font = Font;
            who.Text = Lang.F("about.author", AppInfo.Publisher);

            var link = new LinkLabel();
            link.AutoSize = false;
            link.Font = Font;
            link.Text = AppInfo.SiteUrl;
            link.LinkClicked += delegate
            {
                try
                {
                    var start = new ProcessStartInfo();
                    start.FileName = AppInfo.SiteUrl;
                    start.UseShellExecute = true;
                    Process.Start(start);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };

            int inner = 0;
            inner = Math.Max(inner, TextRenderer.MeasureText(name.Text, _bold).Width);
            inner = Math.Max(inner, TextRenderer.MeasureText(version.Text, Font).Width);
            inner = Math.Max(inner, TextRenderer.MeasureText(who.Text, Font).Width);
            inner = Math.Max(inner, TextRenderer.MeasureText(link.Text, Font).Width);
            inner += 12;

            int y = PadTop;
            int nameH = Math.Max(22, TextRenderer.MeasureText(name.Text, _bold).Height);
            name.Bounds = new Rectangle(PadX, y, inner, nameH);
            y = name.Bottom + 18;
            int versionH = Math.Max(18, TextRenderer.MeasureText(version.Text, Font).Height);
            version.Bounds = new Rectangle(PadX, y, inner, versionH);
            y = version.Bottom + 12;
            int whoH = Math.Max(18, TextRenderer.MeasureText(who.Text, Font).Height);
            who.Bounds = new Rectangle(PadX, y, inner, whoH);
            y = who.Bottom + 16;
            int linkH = Math.Max(18, TextRenderer.MeasureText(link.Text, Font).Height);
            link.Bounds = new Rectangle(PadX, y, inner, linkH);
            y = link.Bottom + PadBottom;

            Controls.Add(name);
            Controls.Add(version);
            Controls.Add(who);
            Controls.Add(link);
            ClientSize = new Size(PadX + inner + PadX, y);
            MinimumSize = Size;
            name.Bounds = new Rectangle(PadX, name.Top, inner, nameH);
            version.Bounds = new Rectangle(PadX, version.Top, inner, versionH);
            who.Bounds = new Rectangle(PadX, who.Top, inner, whoH);
            link.Bounds = new Rectangle(PadX, link.Top, inner, linkH);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                Close();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _bold != null) _bold.Dispose();
            base.Dispose(disposing);
        }
    }

    sealed class SettingsDialog : Form
    {
        readonly CheckBox _restore;
        readonly CheckBox _auto;
        readonly RadioButton _t5;
        readonly RadioButton _t10;
        readonly RadioButton _t20;
        readonly RadioButton _off;
        readonly RadioButton _once;
        readonly RadioButton _watch;
        readonly Button _checkNow;
        readonly Label _homeLine;
        readonly ComboBox _lang;
        readonly Action _onRemember;
        readonly Action<int> _onCheck;

        public SettingsDialog(Action onRemember, Action<int> onCheck)
        {
            const int PadX = 40;
            const int PadTop = 32;
            const int PadBottom = 28;
            const int Gap = 36;
            _onRemember = onRemember;
            _onCheck = onCheck;
            DialogShell.Prepare(this, Lang.T("set.title"));
            AutoSize = false;
            AutoScaleMode = AutoScaleMode.None;
            Padding = new Padding(0);
            SuspendLayout();

            _restore = new CheckBox();
            _restore.AutoSize = true;
            _restore.Margin = new Padding(0, 0, 0, 6);
            _restore.Text = Lang.T("set.restore");
            _restore.Checked = SettingsStore.Current.RestoreHomeOnExit;

            _homeLine = Note(HomeLine(), SystemColors.GrayText, 800);
            _homeLine.Margin = new Padding(22, 0, 0, 8);

            var remember = new Button();
            remember.AutoSize = true;
            remember.MinimumSize = new Size(160, 30);
            remember.Margin = new Padding(22, 0, 0, 16);
            remember.Text = Lang.T("set.remember");

            _auto = new CheckBox();
            _auto.AutoSize = true;
            _auto.Margin = new Padding(0, 4, 0, 8);
            _auto.Text = Lang.T("set.autostart");
            _auto.Checked = SettingsStore.Current.AutoStart;

            _t5 = TimeoutRadio(Lang.T("set.sec5"), SettingsStore.Current.CheckTimeoutSec == 5 || (SettingsStore.Current.CheckTimeoutSec != 10 && SettingsStore.Current.CheckTimeoutSec != 20));
            _t10 = TimeoutRadio(Lang.T("set.sec10"), SettingsStore.Current.CheckTimeoutSec == 10);
            _t20 = TimeoutRadio(Lang.T("set.sec20"), SettingsStore.Current.CheckTimeoutSec == 20);
            var timeouts = new FlowLayoutPanel();
            timeouts.FlowDirection = FlowDirection.LeftToRight;
            timeouts.AutoSize = true;
            timeouts.WrapContents = false;
            timeouts.Margin = new Padding(0, 0, 0, 4);
            timeouts.Controls.Add(_t5);
            timeouts.Controls.Add(_t10);
            timeouts.Controls.Add(_t20);

            int mode = SettingsStore.Current.DnsMode;
            _off = ModeRadio(Lang.T("set.dns.off"), mode != 1 && mode != 2);
            _once = ModeRadio(Lang.T("set.dns.once"), mode == 1);
            _watch = ModeRadio(Lang.T("set.dns.watch"), mode == 2);
            _off.CheckedChanged += delegate { SyncCheck(); };
            _once.CheckedChanged += delegate { SyncCheck(); };
            _watch.CheckedChanged += delegate { SyncCheck(); };

            var onceHint = Note(Lang.T("set.dns.oncehint"), SystemColors.GrayText, 800);
            onceHint.Margin = new Padding(22, 0, 0, 6);
            var watchHint = Note(Lang.T("set.dns.watchhint"), SystemColors.GrayText, 800);
            watchHint.Margin = new Padding(22, 0, 0, 10);

            _checkNow = new Button();
            _checkNow.AutoSize = true;
            _checkNow.MinimumSize = new Size(150, 30);
            _checkNow.Margin = new Padding(0, 4, 0, 0);
            _checkNow.Text = Lang.T("menu.check");
            _checkNow.Click += delegate
            {
                if (_onCheck != null) _onCheck(DnsMode);
            };
            SyncCheck();

            var path = new TextBox();
            path.ReadOnly = true;
            path.BorderStyle = BorderStyle.FixedSingle;
            path.Margin = new Padding(0);
            path.Text = AppPaths.Dir();

            var open = new Button();
            open.AutoSize = true;
            open.MinimumSize = new Size(130, 30);
            open.Margin = new Padding(0, 2, 0, 0);
            open.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            open.Text = Lang.T("set.open");
            open.Click += delegate { OpenFolder(); };

            var save = new Button();
            save.Text = Lang.T("set.save");
            save.DialogResult = DialogResult.OK;
            save.AutoSize = true;
            save.MinimumSize = new Size(150, 34);
            save.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            AcceptButton = save;

            var waitTitle = Note(Lang.T("set.wait"), SystemColors.ControlText, 800);
            var pollHint = Note(Lang.T("set.poll"), SystemColors.GrayText, 800);
            var dnsTitle = Note(Lang.T("set.dns"), SystemColors.ControlText, 800);
            var filesTitle = Note(Lang.T("set.files"), SystemColors.ControlText, 800);
            var langLabel = Note(Lang.T("set.lang"), SystemColors.ControlText, 800);
            _lang = new ComboBox();
            _lang.DropDownStyle = ComboBoxStyle.DropDownList;
            _lang.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            int drop = 180;
            string storedLang = SettingsStore.Current.Lang ?? "";
            int selectedLang = 0;
            for (int i = 0; i < Lang.ChoiceCodes.Length; i++)
            {
                string label = Lang.ChoiceLabel(Lang.ChoiceCodes[i]);
                _lang.Items.Add(label);
                int itemW = TextRenderer.MeasureText(label, Font).Width + 32;
                if (itemW > drop) drop = itemW;
                if (string.Equals(Lang.ChoiceCodes[i], storedLang, StringComparison.OrdinalIgnoreCase)) selectedLang = i;
            }
            _lang.DropDownWidth = drop;
            if (_lang.Items.Count > 0) _lang.SelectedIndex = selectedLang;
            var leftRule = Hairline(10);

            SameFont(_restore, _homeLine, remember, _auto, timeouts, _t5, _t10, _t20, waitTitle, pollHint);
            SameFont(dnsTitle, _off, _once, onceHint, _watch, watchHint, _checkNow, filesTitle, langLabel, _lang, path, open, save);

            int leftW = 0;
            leftW = Wider(leftW, _restore);
            leftW = Wider(leftW, _auto);
            leftW = Wider(leftW, remember);
            leftW = Wider(leftW, timeouts);
            leftW = Wider(leftW, waitTitle);
            leftW = Wider(leftW, pollHint);
            leftW += 16;

            int rightW = 0;
            rightW = Wider(rightW, dnsTitle);
            rightW = Wider(rightW, _off);
            rightW = Wider(rightW, _once);
            rightW = Wider(rightW, _watch);
            rightW = Wider(rightW, _checkNow);
            rightW += 16;

            _homeLine.MaximumSize = new Size(Math.Max(40, leftW - _homeLine.Margin.Horizontal), 0);
            onceHint.MaximumSize = new Size(Math.Max(40, rightW - onceHint.Margin.Horizontal), 0);
            watchHint.MaximumSize = new Size(Math.Max(40, rightW - watchHint.Margin.Horizontal), 0);
            leftRule.Width = leftW;
            leftRule.MinimumSize = new Size(leftW, 1);
            leftRule.MaximumSize = new Size(leftW, 1);

            var left = new Panel();
            left.Font = Font;
            var right = new Panel();
            right.Font = Font;
            var rule = Hairline(10);
            Controls.Add(left);
            Controls.Add(right);
            Controls.Add(rule);
            Controls.Add(langLabel);
            Controls.Add(_lang);
            Controls.Add(filesTitle);
            Controls.Add(path);
            Controls.Add(open);
            Controls.Add(save);

            int yLeft = 0;
            yLeft = Place(left, _restore, yLeft, leftW);
            yLeft = Place(left, _homeLine, yLeft, leftW);
            yLeft = Place(left, remember, yLeft, leftW);
            yLeft = Place(left, leftRule, yLeft, leftW);
            yLeft = Place(left, _auto, yLeft, leftW);
            yLeft = Place(left, waitTitle, yLeft, leftW);
            yLeft = Place(left, timeouts, yLeft, leftW);
            yLeft = Place(left, pollHint, yLeft, leftW);
            left.Bounds = new Rectangle(PadX, PadTop, leftW, yLeft);

            int yRight = 0;
            yRight = Place(right, dnsTitle, yRight, rightW);
            yRight = Place(right, _off, yRight, rightW);
            yRight = Place(right, _once, yRight, rightW);
            yRight = Place(right, onceHint, yRight, rightW);
            yRight = Place(right, _watch, yRight, rightW);
            yRight = Place(right, watchHint, yRight, rightW);
            yRight = Place(right, _checkNow, yRight, rightW);
            right.Bounds = new Rectangle(PadX + leftW + Gap, PadTop, rightW, yRight);

            int innerW = leftW + Gap + rightW;
            int y = PadTop + Math.Max(left.Height, right.Height) + 8;
            rule.Margin = new Padding(0);
            rule.MinimumSize = new Size(innerW, 1);
            rule.MaximumSize = new Size(innerW, 1);
            rule.Bounds = new Rectangle(PadX, y, innerW, 1);
            y = rule.Bottom + 16;

            int langTextW = TextRenderer.MeasureText(langLabel.Text, Font).Width + 8;
            int langLabelH = Math.Max(18, TextRenderer.MeasureText(langLabel.Text, Font).Height);
            int langBoxH = Math.Max(24, _lang.GetPreferredSize(Size.Empty).Height);
            int langRowH = Math.Max(langLabelH, langBoxH);
            langLabel.AutoSize = false;
            var langLabelAt = new Rectangle(PadX, y + Math.Max(0, (langRowH - langLabelH) / 2), langTextW, langLabelH);
            var langBoxAt = new Rectangle(PadX + langTextW + 8, y + Math.Max(0, (langRowH - langBoxH) / 2), Math.Max(160, innerW - langTextW - 8), langBoxH);
            langLabel.Bounds = langLabelAt;
            _lang.Bounds = langBoxAt;
            y = langBoxAt.Bottom + 16;

            int filesH = Math.Max(16, filesTitle.GetPreferredSize(new Size(innerW, int.MaxValue)).Height);
            filesTitle.AutoSize = false;
            filesTitle.Bounds = new Rectangle(PadX, y, innerW, filesH);
            y = filesTitle.Bottom + 8;

            int openW = Math.Max(open.MinimumSize.Width, open.GetPreferredSize(Size.Empty).Width);
            int openH = Math.Max(open.MinimumSize.Height, open.GetPreferredSize(Size.Empty).Height);
            int pathH = Math.Max(23, path.PreferredHeight);
            int rowH = Math.Max(openH, pathH);
            open.AutoSize = false;
            open.Bounds = new Rectangle(PadX + innerW - openW, y + (rowH - openH) / 2, openW, openH);
            path.Bounds = new Rectangle(PadX, y + (rowH - pathH) / 2, Math.Max(80, open.Left - PadX - 10), pathH);
            y += rowH + 22;

            int saveW = Math.Max(save.MinimumSize.Width, save.GetPreferredSize(Size.Empty).Width);
            int saveH = Math.Max(save.MinimumSize.Height, save.GetPreferredSize(Size.Empty).Height);
            save.AutoSize = false;
            var saveAt = new Rectangle(PadX + Math.Max(0, (innerW - saveW) / 2), y, saveW, saveH);
            save.Bounds = saveAt;
            y = saveAt.Bottom + PadBottom;

            var leftAt = left.Bounds;
            var rightAt = right.Bounds;
            var ruleAt = rule.Bounds;
            var langLabelAtSaved = langLabel.Bounds;
            var langBoxAtSaved = _lang.Bounds;
            var filesAt = filesTitle.Bounds;
            var pathAt = path.Bounds;
            var openAt = open.Bounds;
            ResumeLayout(false);
            ClientSize = new Size(PadX + innerW + PadX, y);
            MinimumSize = Size;
            left.Bounds = leftAt;
            right.Bounds = rightAt;
            rule.Bounds = ruleAt;
            langLabel.Bounds = langLabelAtSaved;
            _lang.Bounds = langBoxAtSaved;
            filesTitle.Bounds = filesAt;
            path.Bounds = pathAt;
            open.Bounds = openAt;
            save.Bounds = saveAt;

            remember.Click += delegate
            {
                if (_onRemember != null) _onRemember();
                _homeLine.Text = HomeLine();
                GrowHome(left, right);
            };
        }

        void GrowHome(Panel left, Panel right)
        {
            int oldSpan = Math.Max(left.Bottom, right.Bottom);
            int limit = _homeLine.Bottom;
            int height = TextRenderer.MeasureText(
                _homeLine.Text,
                _homeLine.Font,
                new Size(_homeLine.Width, int.MaxValue),
                TextFormatFlags.WordBreak | TextFormatFlags.NoPadding).Height;
            if (height < 16) height = 16;
            int dy = height - _homeLine.Height;
            if (dy == 0) return;
            _homeLine.Height = height;
            foreach (Control child in left.Controls)
            {
                if (child != _homeLine && child.Top >= limit)
                    child.Top += dy;
            }
            int content = 0;
            foreach (Control child in left.Controls)
                if (child.Bottom > content) content = child.Bottom;
            left.Height = content;
            int shift = Math.Max(left.Bottom, right.Bottom) - oldSpan;
            if (shift == 0) return;
            foreach (Control child in Controls)
            {
                if (child == left || child == right || child.Top < oldSpan) continue;
                child.Top += shift;
            }
            ClientSize = new Size(ClientSize.Width, ClientSize.Height + shift);
            MinimumSize = Size;
        }

        public bool RestoreHome { get { return _restore.Checked; } }
        public bool AutoStart { get { return _auto.Checked; } }
        public int TimeoutSec { get { return _t20.Checked ? 20 : (_t10.Checked ? 10 : 5); } }
        public int DnsMode { get { return _watch.Checked ? 2 : (_once.Checked ? 1 : 0); } }
        public string LanguageCode
        {
            get
            {
                if (_lang == null || _lang.SelectedIndex <= 0) return "";
                return Lang.ChoiceCodes[_lang.SelectedIndex];
            }
        }

        static string HomeLine()
        {
            string id = SettingsStore.Current.HomeTimeZoneId;
            if (string.IsNullOrEmpty(id)) return Lang.T("set.home.none");
            return Lang.F("set.home", WinTz.Display(id));
        }

        void SameFont(params Control[] controls)
        {
            foreach (Control control in controls)
            {
                control.Font = Font;
                foreach (Control child in control.Controls)
                    child.Font = Font;
            }
        }

        static int Wider(int width, Control control)
        {
            int w = control is FlowLayoutPanel ? FlowWidth((FlowLayoutPanel)control) : Pref(control);
            return Math.Max(width, w);
        }

        static int Pref(Control control)
        {
            Size size = control.GetPreferredSize(Size.Empty);
            int w = size.Width;
            if (control.MinimumSize.Width > w) w = control.MinimumSize.Width;
            return w + control.Margin.Horizontal;
        }

        static int FlowWidth(FlowLayoutPanel panel)
        {
            int w = panel.Padding.Horizontal + panel.Margin.Horizontal;
            foreach (Control child in panel.Controls)
                w += Pref(child);
            return w;
        }

        static int Place(Control parent, Control child, int y, int width)
        {
            if (child.Parent != parent) parent.Controls.Add(child);
            int left = child.Margin.Left;
            int top = y + child.Margin.Top;
            int inner = width - child.Margin.Horizontal;
            if (inner < 20) inner = 20;

            if (child is Panel && child.Height > 0 && child.Height <= 2 && !(child is FlowLayoutPanel))
            {
                child.Bounds = new Rectangle(0, top, width, child.Height);
                return child.Bottom + child.Margin.Bottom;
            }

            Size pref;
            if (child is FlowLayoutPanel)
            {
                var flow = (FlowLayoutPanel)child;
                int flowW = 0;
                int flowH = 0;
                foreach (Control item in flow.Controls)
                {
                    Size itemSize = item.GetPreferredSize(Size.Empty);
                    int itemW = Math.Max(itemSize.Width, item.MinimumSize.Width);
                    int itemH = Math.Max(itemSize.Height, item.MinimumSize.Height);
                    flowW += itemW + item.Margin.Horizontal;
                    int row = itemH + item.Margin.Vertical;
                    if (row > flowH) flowH = row;
                }
                flowW += flow.Padding.Horizontal;
                flowH += flow.Padding.Vertical;
                if (flowH < 24) flowH = 24;
                flow.WrapContents = false;
                flow.AutoSize = false;
                pref = new Size(flowW, flowH);
            }
            else if (child is Label)
                pref = child.GetPreferredSize(new Size(inner, int.MaxValue));
            else
                pref = child.GetPreferredSize(Size.Empty);

            int w = Math.Max(pref.Width, child.MinimumSize.Width);
            int h = Math.Max(pref.Height, child.MinimumSize.Height);
            if (child is Label)
            {
                w = inner;
                if (h < 16) h = 16;
            }
            else if (child is CheckBox || child is RadioButton) w += 6;
            if (h < 1) h = 1;
            child.AutoSize = false;
            child.Bounds = new Rectangle(left, top, w, h);
            return child.Bottom + child.Margin.Bottom;
        }

        static Label Note(string text, Color color, int width)
        {
            var label = new Label();
            label.AutoSize = true;
            label.MaximumSize = new Size(width, 0);
            label.Margin = new Padding(0, 2, 0, 8);
            label.Text = text;
            label.ForeColor = color;
            return label;
        }

        static Panel Hairline(int width)
        {
            var line = new Panel();
            line.Height = 1;
            line.Width = width;
            line.MinimumSize = new Size(width, 1);
            line.MaximumSize = new Size(width, 1);
            line.BackColor = SystemColors.ControlDark;
            line.Margin = new Padding(0, 8, 0, 12);
            return line;
        }

        static RadioButton TimeoutRadio(string text, bool on)
        {
            var radio = new RadioButton();
            radio.AutoSize = true;
            radio.Margin = new Padding(0, 0, 16, 0);
            radio.Text = text;
            radio.Checked = on;
            return radio;
        }

        static RadioButton ModeRadio(string text, bool on)
        {
            var radio = new RadioButton();
            radio.AutoSize = true;
            radio.Margin = new Padding(0, 2, 0, 2);
            radio.Text = text;
            radio.Checked = on;
            return radio;
        }

        void SyncCheck()
        {
            _checkNow.Enabled = DnsMode != 0;
        }

        static void OpenFolder()
        {
            try
            {
                var start = new ProcessStartInfo();
                start.FileName = "explorer.exe";
                start.Arguments = "\"" + AppPaths.Dir() + "\"";
                start.UseShellExecute = false;
                Process.Start(start);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, AppInfo.Product, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }

    sealed class DnsCloseDialog : Form
    {
        public DnsCloseDialog(string where)
        {
            Text = Lang.T("dns.title");
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            TopMost = true;
            ShowInTaskbar = true;
            Font = Lang.UiFont;
            RightToLeft = RightToLeft.No;
            RightToLeftLayout = false;
            AutoScaleMode = AutoScaleMode.Font;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Padding = new Padding(24);

            string who = string.IsNullOrEmpty(where) ? Lang.T("dns.router") : where;
            var label = new Label();
            label.AutoSize = true;
            label.MaximumSize = new Size(460, 0);
            label.Margin = new Padding(0, 0, 0, 12);
            label.Text = Lang.F("dns.body", who);

            var cancel = new Button();
            cancel.Text = Lang.T("dns.later");
            cancel.AutoSize = true;
            cancel.MinimumSize = new Size(130, 34);
            cancel.Margin = new Padding(8, 0, 0, 0);
            cancel.DialogResult = DialogResult.Cancel;
            var ok = new Button();
            ok.Text = Lang.T("dns.fix");
            ok.AutoSize = true;
            ok.MinimumSize = new Size(140, 34);
            ok.Margin = new Padding(8, 0, 0, 0);
            ok.DialogResult = DialogResult.OK;

            var row = new FlowLayoutPanel();
            row.FlowDirection = FlowDirection.RightToLeft;
            row.WrapContents = false;
            row.AutoSize = true;
            row.Margin = new Padding(0);
            row.Controls.Add(ok);
            row.Controls.Add(cancel);

            var body = new FlowLayoutPanel();
            body.FlowDirection = FlowDirection.TopDown;
            body.WrapContents = false;
            body.AutoSize = true;
            body.Dock = DockStyle.Fill;
            body.Controls.Add(label);
            body.Controls.Add(row);

            AcceptButton = ok;
            CancelButton = cancel;
            Controls.Add(body);
        }
    }

    static class Badge
    {
        public static readonly Color Home = Color.FromArgb(46, 125, 50);
        public static readonly Color Away = Color.FromArgb(239, 108, 0);
        public static readonly Color Idle = Color.FromArgb(97, 97, 97);
        public static readonly Color Error = Color.FromArgb(198, 40, 40);

        static readonly Dictionary<string, Icon> Cache = new Dictionary<string, Icon>();

        public static Icon Get(Color color, string label)
        {
            int size = IconSize();
            string key = size.ToString(System.Globalization.CultureInfo.InvariantCulture)
                + "|" + color.ToArgb().ToString(System.Globalization.CultureInfo.InvariantCulture)
                + "|" + label;
            Icon icon;
            if (Cache.TryGetValue(key, out icon)) return icon;
            using (Bitmap bmp = Draw(size, color, label))
            {
                // HICON живёт до конца процесса: уникальных значков мало.
                icon = Icon.FromHandle(bmp.GetHicon());
            }
            Cache[key] = icon;
            return icon;
        }

        public static void SavePng(string path, Color color, string label)
        {
            using (Bitmap bmp = Draw(64, color, label))
                bmp.Save(path, ImageFormat.Png);
        }

        public static void SaveIco(string path)
        {
            int[] sizes = new int[] { 16, 24, 32, 48, 64, 256 };
            var pngs = new byte[sizes.Length][];
            for (int i = 0; i < sizes.Length; i++)
            {
                using (Bitmap bmp = Draw(sizes[i], Home, "+3"))
                using (var stream = new MemoryStream())
                {
                    bmp.Save(stream, ImageFormat.Png);
                    pngs[i] = stream.ToArray();
                }
            }
            using (var file = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (var writer = new BinaryWriter(file))
            {
                writer.Write((ushort)0);
                writer.Write((ushort)1);
                writer.Write((ushort)sizes.Length);
                int offset = 6 + (16 * sizes.Length);
                for (int i = 0; i < sizes.Length; i++)
                {
                    int size = sizes[i] >= 256 ? 0 : sizes[i];
                    writer.Write((byte)size);
                    writer.Write((byte)size);
                    writer.Write((byte)0);
                    writer.Write((byte)0);
                    writer.Write((ushort)1);
                    writer.Write((ushort)32);
                    writer.Write(pngs[i].Length);
                    writer.Write(offset);
                    offset += pngs[i].Length;
                }
                for (int i = 0; i < pngs.Length; i++) writer.Write(pngs[i]);
            }
        }

        static int IconSize()
        {
            int sm = GetSystemMetrics(49);
            if (sm < 32) return 32;
            if (sm > 64) return 64;
            return sm;
        }

        static Bitmap Draw(int size, Color color, string label)
        {
            var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                g.Clear(Color.Transparent);
                var rect = new Rectangle(1, 1, size - 3, size - 3);
                using (var brush = new SolidBrush(color))
                    g.FillEllipse(brush, rect);
                float fontPx = size * 0.46f;
                if (label != null && label.Length >= 5) fontPx = size * 0.28f;
                else if (label != null && label.Length == 4) fontPx = size * 0.32f;
                else if (label != null && label.Length == 3) fontPx = size * 0.38f;
                using (var font = new Font("Segoe UI", fontPx, FontStyle.Bold, GraphicsUnit.Pixel))
                using (var format = new StringFormat())
                {
                    format.Alignment = StringAlignment.Center;
                    format.LineAlignment = StringAlignment.Center;
                    g.DrawString(label ?? "", font, Brushes.White, new RectangleF(0, 0, size, size - 1), format);
                }
            }
            return bmp;
        }

        [DllImport("user32.dll")]
        static extern int GetSystemMetrics(int nIndex);
    }
}
