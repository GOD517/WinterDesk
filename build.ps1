param([string]$OutputDirectory = (Join-Path $PSScriptRoot 'dist'))
$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot 'src'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
if (!(Test-Path -LiteralPath $compiler)) { $compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework/v4.0.30319/csc.exe' }
if (!(Test-Path -LiteralPath $compiler)) { throw 'The .NET Framework C# compiler was not found.' }
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$resources = @()
foreach ($pair in @(@('drawn-animation.zip','DrawnPack'),@('decor-watercolor.png','DecorSheet'),@('penguin.ico','PenguinIcon'))) {
 $resources += '/resource:' + (Join-Path $source ('assets/' + $pair[0])) + ',WinterDesk.' + $pair[1]
}
$resources += '/resource:' + (Join-Path $source 'release-public.xml') + ',WinterDesk.UpdatePublicKey'
$sources = @(Get-ChildItem -LiteralPath $source -Filter '*.cs' | ForEach-Object FullName)
& $compiler /nologo /target:winexe /optimize+ /unsafe /platform:anycpu /codepage:65001 "/win32icon:$source/assets/penguin.ico" "/win32manifest:$source/app.manifest" "/out:$OutputDirectory/WinterDesk.exe" $resources /reference:System.IO.Compression.dll /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll $sources
if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
Write-Output (Join-Path $OutputDirectory 'WinterDesk.exe')
