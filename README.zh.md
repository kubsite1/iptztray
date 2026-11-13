[English](README.md) · **中文** · [हिन्दी](README.hi.md) · [Español](README.es.md) · [العربية](README.ar.md) · [Français](README.fr.md) · [বাংলা](README.bn.md) · [Português](README.pt.md) · [Русский](README.ru.md) · [Bahasa Indonesia](README.id.md)

# IPTzTray

**按公网 IP 设置 Windows 时区和 DNS，开着 VPN 时也一样。**

名称：IP + Tz（time zone，时区）+ Tray（Windows 托盘）。

## 简介

IPTzTray 按公网 IP 设置 Windows 时区。公网 IP 就是流量实际出去的地址。打开另一个国家的 VPN，时区就切换到出口国家；回到家里的 IP，时区又变回家庭时区。程序还可以按需修复绕过隧道的 DNS 泄漏：开着 VPN 时，这种泄漏会让运营商看到网站名称。

只改时区，时钟本身（UTC 时间）不调。屏幕上显示另一个本地时间，网站、程序和服务也显示同样的时间。

这样程序去掉了 VPN 的两个明显痕迹：本地时间与 IP 不符，DNS 查询绕过隧道。其他痕迹，例如数据中心 IP 或系统语言，程序不处理。

程序看到的是整个系统的 IP。只在浏览器里设置的代理，它察觉不到。

任何用途都免费。分支（fork）的条件见“许可和名称”。

## 解决什么问题

- 流量已经从另一个国家出去，网站、程序和服务却还显示家里的时间。
- VPN 关了，时区却还是外国的。
- Windows 按附近的 Wi‑Fi 网络设置时区，而不是按流量经过的 IP。
- 开着 VPN 时，Windows 仍然向路由器的 DNS 查询。请求绕过隧道，运营商能看到网站名称。

前三条靠调整时区解决，第四条靠 DNS 泄漏保护解决。

- 这或许还有助于避免 Claude（Anthropic）以及其他智能体开发（AI）厂商封禁账号。

## 开始使用

需要 Windows 10 或 11，64 位。无需安装：程序就是一个文件 [IpTzTray.exe](https://github.com/kubsite1/iptztray/releases/latest/download/IpTzTray.exe)，不超过 250 KB。所有版本见 [Releases](https://github.com/kubsite1/iptztray/releases) 页面。

1. 下载并运行 `IpTzTray.exe`。
2. 允许管理员权限：没有它，Windows 不让程序更改时区。
3. 托盘里出现图标，之后时区会自动调整。

第一次运行时，程序把当前时区记为家庭时区，并开启登录 Windows 时自动启动。每次以管理员权限运行时，程序都会关闭 Windows 的自动设置时区（`tzautoupdate` 服务）；否则 Windows 会按附近的 Wi‑Fi 设置时区，结果与程序不一致。

如果拒绝权限，图标仍会出现，但时区不会改变。菜单里会保留“以管理员身份运行”。

退出程序以及 Windows 会话结束（注销或关机）时，会恢复家庭时区，即使自动切换已暂停。可以在设置里关闭。

## 图标

图标上显示当前 Windows 时区的偏移：`+3`、`-5`、`+5:30`。

<p>
<img src="screenshots/icon-match.png" width="56" alt="绿色">
<img src="screenshots/icon-mismatch.png" width="56" alt="橙色">
<img src="screenshots/icon-wait.png" width="56" alt="灰色">
<img src="screenshots/icon-error.png" width="56" alt="红色">
</p>

- **绿色** — Windows 时区与 IP 的时区一致。
- **橙色** — 不一致：VPN 已经打开但时区还是旧的，或者自动切换已暂停。
- **灰色** — 还没有检查 IP。
- **红色** — 未能获知 IP 或时区。时区不变。

双击图标即立即检查。程序的第二个副本不会启动：托盘里保留第一个。

## DNS 泄漏

默认不检查，可在设置里开启。

程序检查 Windows 是否不仅通过隧道查询 DNS，还向另一个已启用的适配器查询，最常见的是路由器。虚拟适配器、蓝牙和 TAP 不参与检查。检查方式：向这个 DNS 发送一次随机 `.invalid` 名称的查询。如果 Windows 开着并行 DNS 查询时它有响应，网站名称就可能绕过 VPN 发出去。

![DNS 泄漏窗口](screenshots/dns.png)

发现泄漏时，程序会显示这个窗口。关闭窗口和“以后再说”不会改任何东西。在 VPN 开着期间，“修复”会：

- 关闭 Windows 的并行 DNS 查询；
- 记住旁路适配器的 DNS，并换成隧道的 DNS；
- 阻止发往旧 DNS 和常见公共 DNS 的出站请求，包括 DNS-over-TLS 和 DNS-over-HTTPS；
- 把所有名称查询都交给隧道的 DNS。

回到家庭网络后，一切恢复原样。

模式：

- **不检查** — 撤销已开启的保护，恢复原来的设置。
- **启动时一次** — 只在那一刻有泄漏时才询问。之后才打开的 VPN 不会自动检查。
- **自行监视并修复** — 每次打开 VPN 时不询问、直接修复，回到家里时恢复设置。

对这个功能来说，主路由经过隧道适配器、WireGuard、Amnezia 或 TAP 时，才算 VPN 已打开。切换时区不受此影响：它只看公网 IP。如果 VPN 适配器不像其中任何一种，时区照样会切换，但程序可能不会自动修复 DNS。

修复后程序会重启 DNS 服务。如果断网了，请重启电脑。有时更改要重启后才生效。

第三方泄漏测试里可能仍会显示 VPN 自己的 DNS，例如出口国家的 Cloudflare。那是隧道的 DNS，不是家里的路由器。

## 参考

### 菜单

![菜单](screenshots/menu.png)

顶部三行灰字：地点、IP 和 IANA 时区名、当前 Windows 时区。如果当前时区与 IP 的时区不一致，第三行会同时显示两者。过长的行会被截断，完整文字在悬停提示里。

- **立即检查** — 马上额外检查一次 IP。
- **暂停自动切换** — 检查照常进行，时区不变。“继续自动切换”取消暂停并立即调整时区。
- **恢复家庭时区** — 设置记住的家庭时区并开启暂停，否则下一次检查 IP 会马上把它改掉。
- **取消固定** — 只有当前 IP 有固定设置时才显示（见“设置”）。
- **修复 DNS 泄漏** — 只有已经发现泄漏时才显示。
- **以管理员身份运行** — 只有没有管理员权限时才显示。
- **复制 IP** — 把公网 IP 复制到剪贴板。
- **设置**、**关于**、**退出**。

### 设置

![设置](screenshots/settings.png)

- **退出时恢复家庭时区** — 默认开启。
- **记住当前时区** — 立即把当前时区设为家庭时区。
- **登录 Windows 时启动** — 任务计划程序中名为 `IpTzTray` 的任务，带管理员权限，不会再次请求权限。取消勾选会删除该任务。
- **等待服务回复多久** — 5、10 或 20 秒，默认 5 秒。这是每一步的上限：检查 IP 和查询时区分别计算。
- **DNS 泄漏** — 即“DNS 泄漏”一节中的三种模式。
- **语言** — 默认跟随 Windows 语言；列表里没有时用英语。“保存”立即应用语言，无需重启。

界面语言：English、中文、हिन्दी、Español、العربية、Français、বাংলা、Português、Русский、Bahasa Indonesia。时区名称由 Windows 提供，所以显示为系统语言。

窗口底部是 `%AppData%\IpTzTray` 文件夹路径和“打开”按钮。文件夹里有 `settings.json` 和日志 `log.txt`。日志用俄语书写。文件变大后，旧的部分保存到 `log.txt.old`。

**固定** — 为某个 IP 指定自己的时区。存在固定设置时，程序对这个 IP 使用指定的时区，不再询问地理定位服务。本版本没有设置固定的窗口。可以在程序关闭时，把它写进 `settings.json` 的 `Pins` 字段：

```json
"Pins": { "203.0.113.10": "Eastern Standard Time" }
```

时区用 Windows 名称填写。`tzutil /l` 会列出所有名称。

### 命令行

- `IpTzTray.exe --check` — 输出 IP 和时区，不做任何更改。
- `IpTzTray.exe --dns` — 输出是否发现 DNS 泄漏，不做任何更改。
- `IpTzTray.exe --dry` — 以预览模式运行托盘：不改时区和 DNS，设置写入临时文件夹。
- `IpTzTray.exe --map Europe/Amsterdam` — 输出 IANA 名称对应的 Windows 时区。
- `IpTzTray.exe --selftest` — 检查时区对照表、响应解析和设置保存，不改系统。

## 工作原理

- **公网 IP** 同时向三个服务查询：Cloudflare（`1.1.1.1`）、ipinfo.io 和 ip-api.com。采用至少两个服务给出的地址。只有其他服务都没回应时，才接受单个服务的结果。结果不一致就重新检查。私有地址会被丢弃。
- **频率**：IP 不变时，大约每 10 秒检查一次。网络变化后，5 秒后第一次检查，之后约 25 秒内每 5 秒一次，然后恢复每 10 秒一次。
- **时区** 只在 IP 改变时，依次向 ip-api.com、ipwho.is、ipinfo.io 和 geojs.io 查询。IANA 时区名按 Unicode CLDR 对照表换成 Windows 时区，并用 `tzutil` 设置。
- **家庭时区** — 第一次运行、尚未调整前的 Windows 时区。如果当时已经是外国时区，记住的就是它。可以用“记住当前时区”改正。
- **会发出去什么**：每次检查时，查询 IP 的服务都会看到你的公网 IP。只有地址变化时，IP 才会发给地理定位服务。

## 限制

- 只在浏览器里设置的代理，程序察觉不到：它看到的是整个系统的 IP。
- 地理定位数据库里 VPN 数据中心的城市，有时与应有的时区不符。程序设置的是服务返回的时区。
- 没有管理员权限时，时区和 DNS 都不会改变。

## 构建

C#、Windows Forms、Windows 自带的 .NET Framework，x64。只需要 `csc` 编译器，它随 Windows 提供。不需要 Visual Studio。

```
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

结果：`publish\IpTzTray.exe` 和图标 `publish\app.ico`。

- `src` — 源代码。
- `data/windowsZones.xml` — Unicode CLDR 时区对照表。
- `build.ps1` — 构建脚本。
- `app.manifest` — Windows 清单。

程序借助 Grok 4.7 编写和构建。

## 许可和名称

任何用途都免费，包括商业用途。

分支和修改后的版本须遵守两个条件：不删除作者信息，不使用 IPTzTray 这个名称。完整条款见 [LICENSE](LICENSE)。

作者：hello@kubsite.ru  
程序主页：https://github.com/kubsite1/iptztray

感谢即时通讯聚合平台 [wamm.chat](https://wamm.chat) 的支持。
