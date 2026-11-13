$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

function Ensure-Utf8Bom([string]$path) {
    $bytes = [IO.File]::ReadAllBytes($path)
    if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) { return }
    $utf8 = New-Object System.Text.UTF8Encoding $false
    $text = [IO.File]::ReadAllText($path, $utf8)
    $bom = New-Object System.Text.UTF8Encoding $true
    [IO.File]::WriteAllText($path, $text, $bom)
}

$settings = New-Object System.Xml.XmlReaderSettings
$settings.DtdProcessing = [System.Xml.DtdProcessing]::Ignore
$reader = [System.Xml.XmlReader]::Create((Join-Path $root "data\windowsZones.xml"), $settings)
$xml = New-Object System.Xml.XmlDocument
$xml.Load($reader)
$reader.Close()

$map = New-Object "System.Collections.Generic.Dictionary[string,string]"
foreach ($node in $xml.SelectNodes("//mapZone")) {
    $win = $node.GetAttribute("other")
    $terr = $node.GetAttribute("territory")
    $types = $node.GetAttribute("type").Split([char[]]@(' '), [StringSplitOptions]::RemoveEmptyEntries)
    foreach ($iana in $types) {
        if ($terr -eq "001" -or -not $map.ContainsKey($iana)) {
            $map[$iana] = $win
        }
    }
}

$aliases = [ordered]@{
    "Europe/Kyiv" = "Europe/Kiev"
    "Asia/Calcutta" = "Asia/Kolkata"
    "Asia/Saigon" = "Asia/Ho_Chi_Minh"
    "Asia/Rangoon" = "Asia/Yangon"
    "Asia/Katmandu" = "Asia/Kathmandu"
    "Asia/Ulan_Bator" = "Asia/Ulaanbaatar"
    "America/Indianapolis" = "America/Indiana/Indianapolis"
    "America/Buenos_Aires" = "America/Argentina/Buenos_Aires"
    "America/Cordoba" = "America/Argentina/Cordoba"
    "America/Godthab" = "America/Nuuk"
    "America/Louisville" = "America/Kentucky/Louisville"
    "Atlantic/Faeroe" = "Atlantic/Faroe"
    "Pacific/Ponape" = "Pacific/Pohnpei"
    "Pacific/Truk" = "Pacific/Chuuk"
    "Africa/Asmera" = "Africa/Asmara"
    "Asia/Chongqing" = "Asia/Shanghai"
}
foreach ($name in $aliases.Keys) {
    $target = $aliases[$name]
    if (-not $map.ContainsKey($name) -and $map.ContainsKey($target)) {
        $map[$name] = $map[$target]
    }
}

$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine("using System;")
[void]$sb.AppendLine("using System.Collections.Generic;")
[void]$sb.AppendLine()
[void]$sb.AppendLine("namespace IpTzTray")
[void]$sb.AppendLine("{")
[void]$sb.AppendLine("    // Соответствия IANA -> Windows из Unicode CLDR windowsZones (territory 001 важнее остальных).")
[void]$sb.AppendLine("    static class ZoneMap")
[void]$sb.AppendLine("    {")
[void]$sb.AppendLine("        static readonly Dictionary<string, string> Map = new Dictionary<string, string>(StringComparer.Ordinal)")
[void]$sb.AppendLine("        {")
foreach ($key in ($map.Keys | Sort-Object)) {
    $iana = $key.Replace("\", "\\").Replace("`"", "\`"")
    $win = $map[$key].Replace("\", "\\").Replace("`"", "\`"")
    [void]$sb.AppendLine("            { `"$iana`", `"$win`" },")
}
[void]$sb.AppendLine("        };")
[void]$sb.AppendLine()
[void]$sb.AppendLine("        public static bool TryGet(string iana, out string windowsId)")
[void]$sb.AppendLine("        {")
[void]$sb.AppendLine("            windowsId = null;")
[void]$sb.AppendLine("            if (string.IsNullOrEmpty(iana)) return false;")
[void]$sb.AppendLine("            return Map.TryGetValue(iana, out windowsId);")
[void]$sb.AppendLine("        }")
[void]$sb.AppendLine("    }")
[void]$sb.AppendLine("}")

$zonePath = Join-Path $root "src\ZoneMap.cs"
$utf8bom = New-Object System.Text.UTF8Encoding $true
[IO.File]::WriteAllText($zonePath, $sb.ToString(), $utf8bom)
Write-Host ("zones " + $map.Count)

foreach ($src in @("src\Program.cs", "src\Locator.cs", "src\TrayApp.cs", "src\DnsGuard.cs", "src\Lang.cs")) {
    Ensure-Utf8Bom (Join-Path $root $src)
}

New-Item -ItemType Directory -Force -Path (Join-Path $root "publish") | Out-Null
$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
& $csc /nologo /target:winexe /optimize+ /platform:x64 /codepage:65001 /utf8output `
    /win32manifest:app.manifest `
    /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Web.Extensions.dll `
    /out:publish\IpTzTray.exe `
    src\Program.cs src\Locator.cs src\TrayApp.cs src\ZoneMap.cs src\DnsGuard.cs src\Lang.cs
if ($LASTEXITCODE -ne 0) { throw "csc failed: $LASTEXITCODE" }

& (Join-Path $root "publish\IpTzTray.exe") --icon (Join-Path $root "publish\app.ico")
if ($LASTEXITCODE -ne 0) { throw "icon failed: $LASTEXITCODE" }

& $csc /nologo /target:winexe /optimize+ /platform:x64 /codepage:65001 /utf8output `
    /win32manifest:app.manifest `
    /win32icon:publish\app.ico `
    /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Web.Extensions.dll `
    /out:publish\IpTzTray.exe `
    src\Program.cs src\Locator.cs src\TrayApp.cs src\ZoneMap.cs src\DnsGuard.cs src\Lang.cs
if ($LASTEXITCODE -ne 0) { throw "csc icon failed: $LASTEXITCODE" }
Write-Host "built publish\IpTzTray.exe"
