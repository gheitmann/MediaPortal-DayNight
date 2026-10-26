param(
    [string]$MediaPortalDirectory='C:\Program Files\Team MediaPortal\MediaPortal',
    [ValidateSet('x86','x64')][string]$Architecture='x64',
    [string]$OutputDirectory=(Join-Path $PSScriptRoot 'tests/output')
)
$ErrorActionPreference='Stop'
$root=$PSScriptRoot
$out=[IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Path $out -Force | Out-Null
$exe=Join-Path $out ('DayNightProbe.'+$Architecture+'.exe')
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$compileArgs=@('/nologo','/target:exe',('/platform:'+$Architecture),('/out:'+$exe),'/r:System.dll','/r:System.Core.dll','/r:System.Xml.dll','/r:System.Drawing.dll','/r:System.Windows.Forms.dll',('/r:'+(Join-Path $MediaPortalDirectory 'Core.dll')),('/r:'+(Join-Path $root 'dist/DayNight.Backgrounds.dll')),(Join-Path $PSScriptRoot 'tests/NativeProbe.cs'))
& $compiler @compileArgs
if($LASTEXITCODE -ne 0){throw 'Day/Night probe compilation failed.'}
& $exe $MediaPortalDirectory $root (Join-Path $out 'scratch')
if($LASTEXITCODE -ne 0){throw 'Day/Night probe failed.'}
