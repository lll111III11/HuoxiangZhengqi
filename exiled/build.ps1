# 藿香正气水 EXILED build script (UTF-8 BOM required for PS 5.1)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$out  = Join-Path (Split-Path $root -Parent) 'HuoxiangZhengqi.Exiled.dll'

$csc = @(Get-ChildItem "$env:ProgramFiles\dotnet\sdk\*\Roslyn\bincore\csc.dll" -ErrorAction SilentlyContinue)
if ($csc.Count -eq 0) { throw "csc.dll not found" }
$cscPath = $csc[0].FullName

$managedCandidates = @(
  'D:\SteamLibrary\steamapps\common\SCP Secret Laboratory Dedicated Server\SCPSL_Data\Managed',
  'C:\Program Files (x86)\Steam\steamapps\common\SCP Secret Laboratory Dedicated Server\SCPSL_Data\Managed',
  'C:\SteamLibrary\steamapps\common\SCP Secret Laboratory Dedicated Server\SCPSL_Data\Managed'
)
$managed = $null
foreach ($p in $managedCandidates) { if (Test-Path (Join-Path $p 'LabApi.dll')) { $managed = $p; break } }
if (-not $managed) { throw "game Managed folder not found" }

$exiledApiCandidates = @(
  "$env:APPDATA\SCP Secret Laboratory\LabAPI\dependencies\global",
  "$env:APPDATA\SCP Secret Laboratory\PluginAPI\plugins\global\dependencies"
)
$exiledApi = $null
foreach ($p in $exiledApiCandidates) { if (Test-Path (Join-Path $p 'Exiled.API.dll')) { $exiledApi = $p; break } }
if (-not $exiledApi) { throw "Exiled.API.dll not found" }

$exiledPlugins = "$env:APPDATA\EXILED\Plugins"
if (-not (Test-Path (Join-Path $exiledPlugins 'Exiled.Events.dll'))) { throw "Exiled.Events.dll not found in $exiledPlugins" }

$refs = @(
  (Join-Path $managed 'mscorlib.dll'),
  (Join-Path $managed 'netstandard.dll'),
  (Join-Path $managed 'System.dll'),
  (Join-Path $managed 'System.Core.dll'),
  (Join-Path $managed 'Assembly-CSharp.dll'),
  (Join-Path $managed 'UnityEngine.CoreModule.dll'),
  (Join-Path $managed 'UnityEngine.PhysicsModule.dll'),
  (Join-Path $managed 'CommandSystem.Core.dll'),
  (Join-Path $managed 'Mirror.dll'),
  (Join-Path $managed 'NorthwoodLib.dll'),
  (Join-Path $managed 'LabApi.dll'),
  (Join-Path $managed '0Harmony.dll'),
  (Join-Path $exiledApi 'Exiled.API.dll'),
  (Join-Path $exiledPlugins 'Exiled.Events.dll')
)

$cscArgs = @($cscPath,'-noconfig','-nostdlib+','-target:library','-warn:4','-nowarn:CS0169,CS0219,CS0414',
  ("-out:" + $out), ("-lib:" + $managed), ("-lib:" + $exiledApi), ("-lib:" + $exiledPlugins))
foreach ($r in $refs) { $cscArgs += ("-r:" + $r) }
$cscArgs += (Join-Path $root 'HuoxiangPlugin.Exiled.cs')

& dotnet @cscArgs
if ($LASTEXITCODE -ne 0) { throw "compile failed (exit $LASTEXITCODE)" }
Write-Host ("BUILD OK -> {0} ({1} bytes)" -f $out, (Get-Item $out).Length)