# 藿香正气水 build script (UTF-8 BOM required for PS 5.1 Chinese literals)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$out  = Join-Path $root 'HuoxiangZhengqi.dll'

$csc = @(Get-ChildItem "$env:ProgramFiles\dotnet\sdk\*\Roslyn\bincore\csc.dll" -ErrorAction SilentlyContinue)
if ($csc.Count -eq 0) { throw "csc.dll not found" }
$cscPath = $csc[0].FullName

$managedCandidates = @(
  'D:\SteamLibrary\steamapps\common\SCP Secret Laboratory Dedicated Server\SCPSL_Data\Managed',
  'C:\Program Files (x86)\Steam\steamapps\common\SCP Secret Laboratory Dedicated Server\SCPSL_Data\Managed',
  'C:\SteamLibrary\steamapps\common\SCP Secret Laboratory Dedicated Server\SCPSL_Data\Managed'
)
$managed = $null
foreach ($p in $managedCandidates) {
  if (Test-Path (Join-Path $p 'LabApi.dll')) { $managed = $p; break }
}
if (-not $managed) { throw "Managed folder with LabApi.dll not found" }

$refs = @('mscorlib','netstandard','System','System.Core','Assembly-CSharp','LabApi',
          'UnityEngine.CoreModule','CommandSystem.Core','0Harmony','NorthwoodLib','Mirror')

$cscArgs = @($cscPath,'-noconfig','-nostdlib+','-target:library','-warn:4','-nowarn:CS0169,CS0219,CS0414',"-out:$out","-lib:$managed")
foreach ($r in $refs) { $cscArgs += "-r:$r.dll" }
$cscArgs += (Join-Path $root 'HuoxiangPlugin.cs')

& dotnet @cscArgs
if ($LASTEXITCODE -ne 0) { throw "compile failed (exit $LASTEXITCODE)" }
Write-Host ("BUILD OK -> {0} ({1} bytes)" -f $out, (Get-Item $out).Length)