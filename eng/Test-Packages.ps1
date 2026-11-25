[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Version,
    [Parameter(Mandatory)][string]$FeedPath
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$feed = (Resolve-Path -LiteralPath $FeedPath).Path
$testArtifacts = Join-Path $repoRoot 'artifacts/package-tests'
New-Item -ItemType Directory -Force -Path $testArtifacts | Out-Null
$configPath = Join-Path $testArtifacts 'NuGet.Config'
$escapedFeed = [System.Security.SecurityElement]::Escape($feed)
@"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="candidate" value="$escapedFeed" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <packageSourceMapping>
    <clear />
    <packageSource key="candidate"><package pattern="EdiMorph.*" /></packageSource>
    <packageSource key="nuget.org"><package pattern="*" /></packageSource>
  </packageSourceMapping>
</configuration>
"@ | Set-Content -LiteralPath $configPath -Encoding utf8

Push-Location $repoRoot
try {
    $project = 'tests/Tidat.Consumer.Tests/Tidat.Consumer.Tests.csproj'
    # A separate cache prevents a previously restored public package from masking a broken candidate.
    $cache = Join-Path $testArtifacts "packages/$Version"
    dotnet restore $project --configfile $configPath --packages $cache "-p:EdiMorphPackageVersion=$Version"
    if ($LASTEXITCODE -ne 0) { throw 'Package-consumer restore failed.' }

    dotnet test $project -c Release --no-restore "-p:EdiMorphPackageVersion=$Version" `
        -p:UseSharedCompilation=false --logger 'trx;LogFileName=package-consumer.trx' `
        --results-directory (Join-Path $testArtifacts 'results')
    if ($LASTEXITCODE -ne 0) { throw 'Package-consumer tests failed.' }

    dotnet run --project samples/Tidat.Consumer/Tidat.Consumer.csproj -c Release --no-build --no-restore `
        "-p:EdiMorphPackageVersion=$Version"
    if ($LASTEXITCODE -ne 0) { throw 'Package-consumer example failed.' }
}
finally {
    Pop-Location
}
