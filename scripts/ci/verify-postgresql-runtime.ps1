param([Parameter(Mandatory=$true)][string]$PublishPath)
$ErrorActionPreference='Stop'
$root=(Resolve-Path -LiteralPath $PublishPath).Path
$deps=Get-Content -LiteralPath (Join-Path $root 'ConvenienceStore.Web.deps.json') -Raw | ConvertFrom-Json
$packages=@($deps.libraries.PSObject.Properties.Name)
if($packages | Where-Object {$_ -match '(?i)mysql|pomelo'}) {throw 'MySQL dependency found in release runtime.'}
if(-not ($packages | Where-Object {$_ -like 'Npgsql.EntityFrameworkCore.PostgreSQL/*'})) {throw 'Missing Npgsql EF provider.'}
$versions=@($packages | Where-Object {$_ -match '^Microsoft.EntityFrameworkCore(?:\.Relational|\.Abstractions)?/'} | ForEach-Object {($_ -split '/')[1]} | Select-Object -Unique)
if($versions.Count -ne 1 -or $versions[0] -notlike '10.*') {throw 'EF Core runtime packages must share one .NET 10 version.'}
$secrets=@(Get-ChildItem -LiteralPath $root -Recurse -File | Where-Object {$_.Name -like '.env*' -or $_.Name -eq 'appsettings.Development.json'})
if($secrets.Count) {throw 'Development configuration found in publish output.'}
Write-Output "PASS: Npgsql only, EF Core $($versions[0]), no development secrets/configuration in publish output."
