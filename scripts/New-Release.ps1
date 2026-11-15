param(
 [Parameter(Mandatory=$true)][string]$PrivateKeyPath,
 [string]$ExecutablePath = (Join-Path $PSScriptRoot '../dist/WinterDesk.exe')
)
$ErrorActionPreference = 'Stop'
$exe = Get-Item -LiteralPath $ExecutablePath
$version = [Reflection.AssemblyName]::GetAssemblyName($exe.FullName).Version
$url = 'https://github.com/GOD517/WinterDesk/releases/download/v' + $version.ToString(3) + '/WinterDesk.exe'
$hash = (Get-FileHash -LiteralPath $exe.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
$payload = $version.ToString(4) + "`n" + $url + "`n" + $exe.Length + "`n" + $hash + "`n"
$rsa = New-Object System.Security.Cryptography.RSACryptoServiceProvider
$rsa.PersistKeyInCsp = $false
try {
 $rsa.FromXmlString([IO.File]::ReadAllText((Resolve-Path -LiteralPath $PrivateKeyPath)))
 $signature = [Convert]::ToBase64String($rsa.SignData([Text.Encoding]::UTF8.GetBytes($payload), 'SHA256'))
 [IO.File]::WriteAllText((Join-Path $exe.DirectoryName 'update.txt'), $payload + $signature + "`n", (New-Object Text.UTF8Encoding $false))
 [IO.File]::WriteAllText((Join-Path $exe.DirectoryName 'SHA256SUMS.txt'), $hash + '  WinterDesk.exe' + "`n", (New-Object Text.UTF8Encoding $false))
} finally { $rsa.Dispose() }
Write-Output ('Signed release v' + $version.ToString(3))
