param([switch]$NoRestore, [string]$TypeScriptCompiler)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$projectRoot = Split-Path -Parent $PSScriptRoot
$generatorProject = Join-Path $projectRoot 'SqlModelGenerator.csproj'
$fixturesProject = Join-Path $PSScriptRoot 'GenerateFixtures/GenerateFixtures.csproj'
$smokeProject = Join-Path $PSScriptRoot 'GeneratedSmoke/GeneratedSmoke.csproj'
$outputDirectory = Join-Path $PSScriptRoot 'GeneratedSmoke/Generated'
function Invoke-DotNet {
    param([string[]]$Arguments)
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) { throw "dotnet failed with exit code $LASTEXITCODE" }
}
if (-not $NoRestore) {
    Invoke-DotNet -Arguments @('restore', $fixturesProject)
    Invoke-DotNet -Arguments @('restore', $smokeProject)
}
Invoke-DotNet -Arguments @('build', $generatorProject, '--no-restore', '-c', 'Release', '-warnaserror')
Invoke-DotNet -Arguments @('run', '--project', $fixturesProject, '--no-restore', '-c', 'Release', '--', $outputDirectory)
Invoke-DotNet -Arguments @('run', '--project', $smokeProject, '--no-restore', '-c', 'Release')
$typeScriptCheck = Join-Path $PSScriptRoot 'TypeScriptSmoke/check.cjs'
if ($TypeScriptCompiler) { & node $typeScriptCheck $TypeScriptCompiler }
else { & node $typeScriptCheck }
if ($LASTEXITCODE -ne 0) { throw "TypeScript checks failed with exit code $LASTEXITCODE" }
Write-Output 'All local regression checks passed. No database was contacted.' 
