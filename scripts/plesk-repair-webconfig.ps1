# web.config tamamen yeniden yazar — POST 405 / yanlis yapi icin
param(
    [string]$SiteRoot = "C:\Plesk Vhosts\ledajans.com\geldim.ledajans.com"
)

$ErrorActionPreference = "Stop"
$configPath = Join-Path $SiteRoot "web.config"
$backupPath = Join-Path $SiteRoot "web.config.backup-$(Get-Date -Format 'yyyyMMdd-HHmmss')"

if (-not (Test-Path $configPath)) {
    Write-Host "web.config bulunamadi: $configPath" -ForegroundColor Red
    exit 1
}

Copy-Item $configPath $backupPath -Force
Write-Host "Yedek: $backupPath" -ForegroundColor DarkGray

# Mevcut ortam degiskenlerini koru
$envVars = @{}
try {
    [xml]$old = Get-Content $configPath
    $nodes = $old.SelectNodes("//environmentVariable")
    foreach ($n in $nodes) {
        $name = $n.GetAttribute("name")
        $value = $n.GetAttribute("value")
        if ($name) { $envVars[$name] = $value }
    }
} catch {
    Write-Host "Eski env okunamadi, varsayilanlar kullanilacak." -ForegroundColor Yellow
}

if (-not $envVars.ContainsKey("ASPNETCORE_ENVIRONMENT")) {
    $envVars["ASPNETCORE_ENVIRONMENT"] = "Production"
}

$envXml = ""
foreach ($kv in $envVars.GetEnumerator() | Sort-Object Name) {
    $val = [System.Security.SecurityElement]::Escape($kv.Value)
    $envXml += "          <environmentVariable name=`"$($kv.Key)`" value=`"$val`" />`r`n"
}

$content = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <modules>
        <remove name="WebDAVModule" />
      </modules>
      <handlers>
        <remove name="WebDAV" />
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet"
                  arguments=".\Ledajans.Server.dll"
                  stdoutLogEnabled="true"
                  stdoutLogFile=".\logs\stdout"
                  hostingModel="OutOfProcess">
        <environmentVariables>
$envXml        </environmentVariables>
      </aspNetCore>
    </system.webServer>
  </location>
</configuration>
"@

Set-Content -Path $configPath -Value $content -Encoding UTF8
Write-Host "web.config yenilendi (path=* verb=*)" -ForegroundColor Green

$pool = "geldim.ledajans.com(domain)(4.0)(pool)"
$appcmd = "$env:windir\system32\inetsrv\appcmd.exe"
if (Test-Path $appcmd) {
    & $appcmd recycle apppool "/apppool.name:$pool"
    Write-Host "App pool yenilendi: $pool" -ForegroundColor Green
}

Write-Host "Tamam" -ForegroundColor Cyan
