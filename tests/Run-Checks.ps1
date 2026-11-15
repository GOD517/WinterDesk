$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
& (Join-Path $root 'build.ps1') | Out-Null
$source = Join-Path $root 'src'
$work = Join-Path $root 'work/checks'
New-Item -ItemType Directory -Force -Path $work | Out-Null
$native = Get-Content (Join-Path $source 'Native.cs') -Raw -Encoding UTF8
$native = $native.Replace('internal sealed class Sprite : IDisposable {', 'internal sealed class Sprite : IDisposable { internal static int Allocated,Released;')
$native = $native.Replace('old=Native.SelectObject(DC,bitmap);}', 'old=Native.SelectObject(DC,bitmap);Allocated++;}')
$native = $native.Replace('Native.DeleteObject(bitmap);Native.DeleteDC(DC);DC=IntPtr.Zero;}', 'Native.DeleteObject(bitmap);Native.DeleteDC(DC);DC=IntPtr.Zero;Released++;}')
$nativePath = Join-Path $work 'NativeInstrumented.cs'
Set-Content $nativePath $native -Encoding UTF8
$checks = Get-Content (Join-Path $PSScriptRoot 'SceneChecks.cs') -Raw -Encoding UTF8
$checksPath = Join-Path $work 'SceneChecks.cs'
Set-Content $checksPath ($checks.Replace('Linux managed sprite adapter','Windows native DIB with allocation counters')) -Encoding UTF8
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
if (!(Test-Path $compiler)) { $compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework/v4.0.30319/csc.exe' }
$resources = @()
foreach ($pair in @(@('drawn-animation.zip','DrawnPack'),@('decor-watercolor.png','DecorSheet'),@('penguin.ico','PenguinIcon'))) {
 $resources += '/resource:' + (Join-Path $source ('assets/' + $pair[0])) + ',WinterDesk.' + $pair[1]
}
$sources = @(Get-ChildItem $source -Filter '*.cs' | Where-Object Name -ne 'Native.cs' | ForEach-Object FullName) + @($nativePath, $checksPath)
$exe = Join-Path $work 'SceneChecks.exe'
& $compiler /nologo /unsafe /optimize+ /codepage:65001 /main:WinterDesk.RevisionChecks "/out:$exe" /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.IO.Compression.dll $resources $sources
if ($LASTEXITCODE -ne 0) { throw 'Scene check compilation failed' }
Push-Location $work
try {
 & $exe
 if ($LASTEXITCODE -ne 0) { throw 'Scene checks failed' }
} finally { Pop-Location }
& (Join-Path $root 'build.ps1') | Out-Null
$rsa = New-Object System.Security.Cryptography.RSACryptoServiceProvider 2048
$rsa.PersistKeyInCsp = $false
try {
 [IO.File]::WriteAllText((Join-Path $work 'test-public.xml'),$rsa.ToXmlString($false))
 [IO.File]::WriteAllText((Join-Path $work 'test-private.xml'),$rsa.ToXmlString($true))
} finally { $rsa.Dispose() }
$updateExe = Join-Path $work 'UpdateChecks.exe'
& $compiler /nologo /codepage:65001 /main:WinterDesk.UpdateChecks "/out:$updateExe" /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll ('/resource:' + (Join-Path $work 'test-public.xml') + ',WinterDesk.UpdatePublicKey') (Join-Path $source 'Updates.cs') (Join-Path $source 'RuntimeLog.cs') (Join-Path $source 'AssemblyInfo.cs') (Join-Path $PSScriptRoot 'UpdateChecks.cs')
if ($LASTEXITCODE -ne 0) { throw 'Update check compilation failed' }
& $updateExe (Join-Path $work 'test-private.xml') (Join-Path $root 'dist/WinterDesk.exe')
if ($LASTEXITCODE -ne 0) { throw 'Update checks failed' }
