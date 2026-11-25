[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidatePattern('^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(-[0-9A-Za-z]+(?:[.-][0-9A-Za-z]+)*)?$')]
    [string]$Version,
    [switch]$SkipConsumerTests
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
Push-Location $repoRoot
try {
    # Versioned output prevents old packages from entering a new release artifact.
    $feed = Join-Path $repoRoot "artifacts/packages/$Version"
    New-Item -ItemType Directory -Force -Path $feed | Out-Null
    $properties = @("-p:Version=$Version", '-p:ContinuousIntegrationBuild=true', '-p:UseSharedCompilation=false')

    dotnet restore EdiMorph.slnx @properties
    if ($LASTEXITCODE -ne 0) { throw 'Solution restore failed.' }
    dotnet build EdiMorph.slnx -c Release --no-restore @properties -m:1
    if ($LASTEXITCODE -ne 0) { throw 'Solution build failed.' }
    dotnet test EdiMorph.slnx -c Release --no-build --no-restore @properties `
        --logger trx --results-directory artifacts/test-results -m:1
    if ($LASTEXITCODE -ne 0) { throw 'Unit tests failed.' }
    dotnet pack EdiMorph.slnx -c Release --no-build --no-restore @properties -o $feed
    if ($LASTEXITCODE -ne 0) { throw 'NuGet packing failed.' }

    $packageIds = @('EdiMorph.Abstractions', 'EdiMorph.Specifications', 'EdiMorph.Specifications.X12', 'EdiMorph.X12')
    $packages = @(Get-ChildItem -LiteralPath $feed -Filter '*.nupkg')
    $expected = @($packageIds | ForEach-Object { "$_.${Version}.nupkg" })
    if ($packages.Count -ne $expected.Count -or @(Compare-Object $expected $packages.Name).Count -gt 0) {
        throw 'The candidate must contain exactly the four product packages.'
    }
    $packages | Sort-Object Name | ForEach-Object {
        "{0}  {1}" -f (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant(), $_.Name
    } | Set-Content -LiteralPath (Join-Path $feed 'SHA256SUMS') -Encoding ascii

    if (!$SkipConsumerTests) {
        & (Join-Path $PSScriptRoot 'Test-Packages.ps1') -Version $Version -FeedPath $feed
    }
}
finally {
    Pop-Location
}
