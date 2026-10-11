#!/usr/bin/env pwsh
<#
.SYNOPSIS
Creates (or completes) the property-test project for a .NET project whose interfaces have ContractCheck contracts.

.DESCRIPTION
Creates <Project>.PropertyTests next to the project under test: an xUnit v3 test project with FsCheck.Xunit.v3 that
references the project under test, ContractCheck and ContractCheck.FsCheck. Adds it to the solution and builds it.

Idempotent: re-running only adds what is missing. Existing files, package versions and references are never changed.

.PARAMETER ProjectUnderTest
Path to the .csproj whose contracts the property tests verify.

.PARAMETER Solution
Solution (.sln/.slnx) to add the test project to. Default: the solution in the nearest ancestor folder of the project.

.PARAMETER ContractCheckSource
auto    – project references when ContractCheck.csproj and ContractCheck.FsCheck.csproj are in the repository, else NuGet.
project – project references; fails if the projects cannot be found.
package – NuGet packages ContractCheck and ContractCheck.FsCheck, version -ContractCheckVersion.

.PARAMETER GrantInternalsAccess
Adds <InternalsVisibleTo Include="<test project>" /> to the project under test, so properties can use internal types.

.EXAMPLE
pwsh .claude/skills/dbc-property-tests/scripts/init-property-tests.ps1 -ProjectUnderTest src/Shop/Shop.csproj
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$ProjectUnderTest,
    [string]$Solution,
    [string]$Name,
    [string]$Directory,
    [ValidateSet('auto', 'project', 'package')][string]$ContractCheckSource = 'auto',
    [string]$ContractCheckVersion = '0.1.0',
    [string]$XunitVersion = '3.2.2',
    [string]$XunitRunnerVersion = '3.1.5',
    [string]$TestSdkVersion = '17.14.1',
    [string]$FsCheckXunitVersion = '3.3.4',
    [switch]$GrantInternalsAccess,
    [switch]$SkipBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$report = [System.Collections.Generic.List[string]]::new()
function Note([string]$status, [string]$item) { $report.Add(('{0,-8} {1}' -f $status, $item)) }

function Invoke-Dotnet {
    $output = & dotnet @args 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($args -join ' ') failed:`n$($output -join "`n")"
    }
    return $output
}

function Get-RelativePath([string]$from, [string]$to) { [System.IO.Path]::GetRelativePath($from, $to) -replace '\\', '/' }

# --- Inputs -----------------------------------------------------------------------------------------------------
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw 'The .NET SDK (dotnet) is not on PATH.' }

$projectPath = (Resolve-Path -LiteralPath $ProjectUnderTest).Path
if ([System.IO.Path]::GetExtension($projectPath) -ne '.csproj') { throw "$ProjectUnderTest is not a .csproj file." }
$projectDir = Split-Path -Parent $projectPath
$projectName = [System.IO.Path]::GetFileNameWithoutExtension($projectPath)

$testName = if ($Name) { $Name } else { "$projectName.PropertyTests" }
$testDir = if ($Directory) { [System.IO.Path]::GetFullPath($Directory) } else { Join-Path (Split-Path -Parent $projectDir) $testName }
$testProject = Join-Path $testDir "$testName.csproj"
# C# namespace for generated files: the project name with every segment made a valid identifier.
$testNamespace = ($testName.Split('.') | ForEach-Object { $segment = $_ -replace '[^\w]', '_'; if ($segment -match '^\d') { "_$segment" } else { $segment } }) -join '.'

$repoRoot = (& git -C $projectDir rev-parse --show-toplevel 2>$null)
if ($LASTEXITCODE -ne 0 -or -not $repoRoot) { $repoRoot = Split-Path -Parent $projectDir }
$repoRoot = [System.IO.Path]::GetFullPath($repoRoot)

if (-not $Solution) {
    for ($dir = $projectDir; $dir; $dir = Split-Path -Parent $dir) {
        $candidates = @(Get-ChildItem -LiteralPath $dir -File | Where-Object Extension -in '.sln', '.slnx')
        if ($candidates.Count -eq 1) { $Solution = $candidates[0].FullName; break }
        if ($candidates.Count -gt 1) { throw "Several solutions in ${dir}: pass -Solution." }
        if ($dir -eq $repoRoot) { break }
    }
}

# --- Target framework: the project's, or its newest when it multi-targets -----------------------------------------
$properties = (Invoke-Dotnet msbuild $projectPath '-getProperty:TargetFramework' '-getProperty:TargetFrameworks') -join "`n" | ConvertFrom-Json
$framework = $properties.Properties.TargetFramework
if (-not $framework) {
    $framework = $properties.Properties.TargetFrameworks.Split(';', [System.StringSplitOptions]::RemoveEmptyEntries) |
        Sort-Object { if ($_ -match '^net(\d+)\.(\d+)') { [version]"$($Matches[1]).$($Matches[2])" } else { [version]'0.0' } } -Descending |
        Select-Object -First 1
}
if (-not $framework) { throw "Could not determine the target framework of $projectName." }
if ($framework -notmatch '^net\d+\.\d+') {
    # netstandard / .NET Framework libraries: test on the installed SDK's runtime (ContractCheck needs net8.0+).
    $sdkMajor = ((Invoke-Dotnet --version) -join '').Trim().Split('.')[0]
    Note 'note' "$projectName targets $framework; the property tests target net$sdkMajor.0"
    $framework = "net$sdkMajor.0"
}

# --- ContractCheck: project references or packages ---------------------------------------------------------------
function Find-Project([string]$fileName) {
    $tracked = @(& git -C $repoRoot ls-files "*$fileName" 2>$null | Where-Object { (Split-Path -Leaf $_) -eq $fileName })
    if ($tracked.Count -gt 0) { return (Join-Path $repoRoot $tracked[0]) }
    $found = Get-ChildItem -LiteralPath $repoRoot -Recurse -Filter $fileName -File -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -notmatch '[\\/](bin|obj|node_modules)[\\/]' } | Select-Object -First 1
    if ($found) { return $found.FullName }
    return $null
}

$contractCheck = $null
$contractCheckFsCheck = $null
if ($ContractCheckSource -ne 'package') {
    $contractCheck = Find-Project 'ContractCheck.csproj'
    $contractCheckFsCheck = Find-Project 'ContractCheck.FsCheck.csproj'
    if (-not ($contractCheck -and $contractCheckFsCheck)) {
        if ($ContractCheckSource -eq 'project') { throw 'ContractCheck.csproj / ContractCheck.FsCheck.csproj not found in the repository.' }
        $contractCheck = $contractCheckFsCheck = $null
    }
}

# --- Project files -------------------------------------------------------------------------------------------------
New-Item -ItemType Directory -Force -Path $testDir | Out-Null

if (Test-Path -LiteralPath $testProject) {
    Note 'exists' (Get-RelativePath $repoRoot $testProject)
} else {
    @"
<Project Sdk="Microsoft.NET.Sdk">

  <!-- FsCheck property tests that use the ContractCheck contracts of $projectName as their oracle.
       Created by .claude/skills/dbc-property-tests/scripts/init-property-tests.ps1. -->
  <PropertyGroup>
    <TargetFramework>$framework</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsContractPropertyTestProject>true</IsContractPropertyTestProject>
  </PropertyGroup>

  <ItemGroup>
    <Using Include="Xunit" />
    <Using Include="FsCheck" />
    <Using Include="FsCheck.Fluent" />
    <Using Include="FsCheck.Xunit" />
    <Using Include="ContractCheck" />
    <Using Include="ContractCheck.FsCheck" />
  </ItemGroup>

</Project>
"@ | Set-Content -LiteralPath $testProject -Encoding utf8NoBOM
    Note 'created' (Get-RelativePath $repoRoot $testProject)
}

$readme = Join-Path $testDir 'README.md'
if (-not (Test-Path -LiteralPath $readme)) {
    @"
# $testName

Property-based tests for **$projectName**: FsCheck generates inputs, ``Dbc.Make`` proxies check the ContractCheck
contracts, and ``DbcPropertyTest`` turns the outcome into a property (entry-check failures discard the case;
postcondition and invariant failures fail it).

- One ``*Properties.cs`` file per contract interface; custom generators go in ``Generators.cs``.
- Written and maintained by the ``dbc-property-tests`` Claude skill (``.claude/skills/dbc-property-tests``).
- Run: ``dotnet test $(Get-RelativePath $repoRoot $testProject)``
"@ | Set-Content -LiteralPath $readme -Encoding utf8NoBOM
    Note 'created' (Get-RelativePath $repoRoot $readme)
} else {
    Note 'exists' (Get-RelativePath $repoRoot $readme)
}

$generators = Join-Path $testDir 'Generators.cs'
if (-not (Test-Path -LiteralPath $generators)) {
    @"
namespace $testNamespace;

/// <summary>
/// Custom FsCheck generators for the property tests. Register them per test with
/// [Property(Arbitrary = [typeof(Generators)])]; each public static method returning Arbitrary&lt;T&gt; supplies T.
/// </summary>
public static class Generators
{
}
"@ | Set-Content -LiteralPath $generators -Encoding utf8NoBOM
    Note 'created' (Get-RelativePath $repoRoot $generators)
} else {
    Note 'exists' (Get-RelativePath $repoRoot $generators)
}

# --- Packages and references (only the missing ones) ----------------------------------------------------------------
[xml]$xml = Get-Content -LiteralPath $testProject -Raw
$packages = @($xml.SelectNodes('//PackageReference') | ForEach-Object { $_.GetAttribute('Include') })
$references = @($xml.SelectNodes('//ProjectReference') | ForEach-Object { [System.IO.Path]::GetFileName(($_.GetAttribute('Include') -replace '\\', '/')) })

$wanted = [ordered]@{
    'Microsoft.NET.Test.Sdk'    = $TestSdkVersion
    'xunit.v3'                  = $XunitVersion
    'xunit.runner.visualstudio' = $XunitRunnerVersion
    'FsCheck.Xunit.v3'          = $FsCheckXunitVersion
}
if (-not $contractCheck) {
    $wanted['ContractCheck'] = $ContractCheckVersion
    $wanted['ContractCheck.FsCheck'] = $ContractCheckVersion
}
foreach ($package in $wanted.Keys) {
    if ($packages -contains $package) { Note 'exists' "package $package"; continue }
    Invoke-Dotnet add $testProject package $package --version $wanted[$package] --no-restore | Out-Null
    Note 'added' "package $package $($wanted[$package])"
}

foreach ($reference in @($projectPath, $contractCheck, $contractCheckFsCheck) | Where-Object { $_ }) {
    $file = [System.IO.Path]::GetFileName($reference)
    if ($references -contains $file) { Note 'exists' "reference $file"; continue }
    Invoke-Dotnet add $testProject reference $reference | Out-Null
    Note 'added' "reference $file"
}

# --- Optional: let the tests see internal types of the project under test ------------------------------------------
if ($GrantInternalsAccess) {
    $source = Get-Content -LiteralPath $projectPath -Raw
    if ($source -match "<InternalsVisibleTo\s+Include=`"$([regex]::Escape($testName))`"") {
        Note 'exists' "InternalsVisibleTo $testName in $projectName"
    } else {
        $entry = "  <ItemGroup>`n    <InternalsVisibleTo Include=`"$testName`" />`n  </ItemGroup>`n`n</Project>"
        $source = [regex]::Replace($source, '\s*</Project>\s*$', "`n`n$entry`n")
        Set-Content -LiteralPath $projectPath -Value $source -Encoding utf8NoBOM -NoNewline
        Note 'added' "InternalsVisibleTo $testName in $projectName"
    }
}

# --- Solution ------------------------------------------------------------------------------------------------------
if ($Solution) {
    $listed = (Invoke-Dotnet sln $Solution list) -join "`n"
    if ($listed -match [regex]::Escape("$testName.csproj")) {
        Note 'exists' "$testName in $(Split-Path -Leaf $Solution)"
    } else {
        Invoke-Dotnet sln $Solution add $testProject | Out-Null
        Note 'added' "$testName to $(Split-Path -Leaf $Solution)"
    }
} else {
    Note 'skipped' 'no solution found; pass -Solution to add the project to one'
}

# --- Build ---------------------------------------------------------------------------------------------------------
if (-not $SkipBuild) {
    Invoke-Dotnet build $testProject --nologo -v quiet | Out-Null
    Note 'built' (Get-RelativePath $repoRoot $testProject)
}

Write-Output "Property-test project for ${projectName}: $(Get-RelativePath $repoRoot $testProject) ($framework, ContractCheck from $(if ($contractCheck) { 'project references' } else { "NuGet $ContractCheckVersion" }))"
$report | ForEach-Object { Write-Output "  $_" }
