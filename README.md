**English** · [中文](README.zh.md) · [हिन्दी](README.hi.md) · [Español](README.es.md) · [العربية](README.ar.md) · [Français](README.fr.md) · [বাংলা](README.bn.md) · [Português](README.pt.md) · [Русский](README.ru.md) · [Bahasa Indonesia](README.id.md)

# IPTzTray

**Windows time zone and DNS that follow your public IP, including when a VPN is on.**

Name: IP + Tz (time zone) + Tray (Windows notification area).

## In short

IPTzTray sets the Windows time zone from your public IP — the address your traffic actually goes out through. Turn on a VPN in another country, and the time zone switches to the exit country. Come back to your home IP, and the time zone is home again. Optionally, the program also closes the DNS leak around the tunnel, which lets your provider see site names while the VPN is on.

Only the time zone changes; the clock itself (UTC time) is not moved. The screen shows a different local time, and websites, programs and services show the same time.

This way the program removes two visible signs of a VPN: local time that does not match the IP, and DNS queries that bypass the tunnel. Other signs, such as a datacenter IP or the system language, it does not touch.

The program sees the IP of the whole system. A proxy set only in the browser goes unnoticed.

Free for any use. Terms for forks are in “License and name”.

## Why

- Websites, programs and services show home time even though traffic already goes out from another country.
- The VPN is off, but the time zone stayed foreign.
- Windows sets the time zone by the nearest Wi‑Fi network, not by the IP your traffic goes through.
- With the VPN on, Windows keeps asking the router's DNS. The query bypasses the tunnel, and the provider sees site names.

Time zone adjustment solves the first three; DNS leak protection solves the fourth.

- It may also help against account blocks by Claude (Anthropic) and other vendors of AI agent-development tools.

## Getting started

Requires Windows 10 or 11, 64-bit. Nothing to install: the program is one file, [IpTzTray.exe](https://github.com/kubsite1/iptztray/releases/latest/download/IpTzTray.exe), up to 250 KB. All versions are on the [Releases](https://github.com/kubsite1/iptztray/releases) page.

1. Download and run `IpTzTray.exe`.
2. Allow administrator rights: without them Windows does not let a program change the time zone.
3. An icon appears in the tray; from then on the time zone adjusts by itself.

On first launch the program remembers the current time zone as home and turns on its own autostart at Windows sign-in. On every launch with administrator rights it turns off automatic time zone setting in Windows (the `tzautoupdate` service); otherwise Windows sets the time zone by the nearest Wi‑Fi and its value differs from the program's.

If you refuse the rights, the icon still appears, but the time zone will not change. The menu keeps “Run as administrator”.

When you exit the program and when the Windows session ends (sign-out or shutdown), the home time zone comes back, even if auto-switch is paused. This can be turned off in the settings.

## Icon

The icon shows the offset of the current Windows time zone: `+3`, `-5`, `+5:30`.

<p>
<img src="screenshots/icon-match.png" width="56" alt="Green">
<img src="screenshots/icon-mismatch.png" width="56" alt="Orange">
<img src="screenshots/icon-wait.png" width="56" alt="Grey">
<img src="screenshots/icon-error.png" width="56" alt="Red">
</p>

- **Green** — the Windows time zone matches the IP time zone.
- **Orange** — does not match: the VPN is already on but the time zone is still the old one, or auto-switch is paused.
- **Grey** — the IP has not been checked yet.
- **Red** — could not learn the IP or the time zone. The time zone does not change.

Double-click the icon to check now. A second copy of the program does not start: the first one stays in the tray.

## DNS leak

Checking is off by default. Turn it on in the settings.

The program checks whether Windows asks DNS not only through the tunnel but also on another active adapter, most often the router. Virtual adapters, Bluetooth and TAP are left out. The check is one query for a random `.invalid` name sent to that DNS. If it answers while parallel DNS lookup is on in Windows, site names can leave past the VPN.

![DNS leak window](screenshots/dns.png)

When a leak is found, the program shows this window. Closing it and “Not now” change nothing. “Fix”, for as long as the VPN is on:

- turns off parallel DNS lookup in Windows;
- remembers the side adapter's DNS and puts the tunnel DNS in its place;
- blocks outgoing queries to the old DNS and to well-known public DNS servers, including DNS-over-TLS and DNS-over-HTTPS;
- sends all names to the tunnel DNS.

On the home network everything goes back as it was.

Modes:

- **Do not check** — removes protection that is already on and restores the previous settings.
- **Once at startup** — asks only if there is a leak at that moment. A VPN turned on later is not checked automatically.
- **Watch and fix by itself** — fixes without asking every time the VPN comes on, and restores the settings at home.

For this feature the VPN counts as on when the main route goes through a tunnel adapter, WireGuard, Amnezia or TAP. Time zone switching does not depend on this: it looks only at the public IP. If the VPN adapter looks like none of these, the time zone still switches, but the program may not fix DNS by itself.

After a fix the program restarts the DNS service. If the internet drops, restart the computer. Sometimes the change takes effect only after a restart.

A third-party leak test may still show the VPN's own DNS, for example Cloudflare in the exit country. That is the tunnel's DNS, not your home router.

## Reference

### Menu

![Menu](screenshots/menu.png)

Three grey lines at the top: place, IP with the IANA time zone name, current Windows time zone. If the current time zone does not match the IP time zone, the third line shows both. A long line is cut; the full text is in the tooltip.

- **Check now** — an extra IP check right away.
- **Pause auto-switch** — checks continue, the time zone does not change. “Resume auto-switch” lifts the pause and adjusts the time zone at once.
- **Restore home time zone** — sets the remembered home time zone and turns on the pause; otherwise the next IP check would override it at once.
- **Remove pin** — shown only if the current IP has a pin (see “Settings”).
- **Fix DNS leak** — shown only if a leak has already been found.
- **Run as administrator** — shown only without administrator rights.
- **Copy IP** — copies the public IP to the clipboard.
- **Settings**, **About**, **Exit**.

### Settings

![Settings](screenshots/settings.png)

- **Restore the home time zone on exit** — on by default.
- **Remember the current time zone** — makes the current time zone home right away.
- **Start at Windows sign-in** — a Task Scheduler task `IpTzTray` with administrator rights, without asking for rights again. Unchecking removes the task.
- **How long to wait for the service** — 5, 10 or 20 seconds, 5 by default. This is the limit for each step: separately for the IP check and for the time zone request.
- **DNS leak** — the three modes from “DNS leak”.
- **Language** — the Windows language by default. If it is not in the list, English. “Save” applies the language at once, without restarting.

Interface languages: English, 中文, हिन्दी, Español, العربية, Français, বাংলা, Português, Русский, Bahasa Indonesia. Time zone names come from Windows, so they are in the system language.

At the bottom of the window: the path to the `%AppData%\IpTzTray` folder and the “Open” button. The folder holds `settings.json` and the log `log.txt`. The log is written in Russian. When the file grows, the older part is kept in `log.txt.old`.

**Pin** — your own time zone for a specific IP. While it exists, the program sets that time zone for this IP and does not ask the geolocation service. This version has no window for pinning. You can add a pin to `settings.json` while the program is closed, in the `Pins` field:

```json
"Pins": { "203.0.113.10": "Eastern Standard Time" }
```

The time zone is given by its Windows name. `tzutil /l` prints the list of names.

### Command line

- `IpTzTray.exe --check` — prints the IP and time zone, changes nothing.
- `IpTzTray.exe --dns` — prints whether a DNS leak is visible, changes nothing.
- `IpTzTray.exe --dry` — the tray in preview mode: time zone and DNS do not change, settings go to a temporary folder.
- `IpTzTray.exe --map Europe/Amsterdam` — prints the Windows time zone for an IANA name.
- `IpTzTray.exe --selftest` — checks the time zone map, response parsing and settings saving; does not change the system.

## How it works

- **Public IP** is asked from three services at once: Cloudflare (`1.1.1.1`), ipinfo.io and ip-api.com. The address named by at least two is taken. A single service's answer is accepted only if the others are silent. If the answers differ, the check is repeated. Private addresses are discarded.
- **Frequency**: while the IP stays the same, about every 10 seconds. After a network change the first check comes in 5 seconds, then every 5 seconds for about 25 seconds, then every 10 seconds again.
- **Time zone** is asked in turn from ip-api.com, ipwho.is, ipinfo.io and geojs.io, only when the IP has changed. The IANA time zone name is mapped to a Windows time zone with the Unicode CLDR table and set with `tzutil`.
- **Home time zone** — the Windows time zone at first launch, before the first adjustment. If a foreign time zone was set then, that one is remembered. You can fix it with “Remember the current time zone”.
- **What goes out**: the IP lookup services see your public IP on every check. The geolocation service gets the IP only when the address changes.

## Limitations

- A proxy set only in the browser goes unnoticed: the program sees the IP of the whole system.
- The VPN datacenter's city in the geolocation database sometimes does not match the right time zone. The time zone the service returned is set.
- Without administrator rights, neither the time zone nor DNS changes.

## Build

C#, Windows Forms, the .NET Framework that ships with Windows, x64. Only the `csc` compiler is needed, and it comes with Windows. Visual Studio is not needed.

```
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

Output: `publish\IpTzTray.exe` and the icon `publish\app.ico`.

- `src` — source code.
- `data/windowsZones.xml` — Unicode CLDR time zone table.
- `build.ps1` — build script.
- `app.manifest` — Windows manifest.

The program was written and built with Grok 4.7.

## License and name

Free for any use, including commercial.

Forks and modified builds have two conditions: keep the authorship and do not use the name IPTzTray. Full text in [LICENSE](LICENSE).

Author: hello@kubsite.ru  
Program page: https://github.com/kubsite1/iptztray

Thank you for the support — the messenger aggregator [wamm.chat](https://wamm.chat).
