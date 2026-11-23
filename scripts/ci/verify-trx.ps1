param(
    [Parameter(Mandatory = $true)]
    [string]$Path
)

$ErrorActionPreference = 'Stop'
if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
    throw "Missing test result file: $Path"
}

[xml]$result = Get-Content -LiteralPath $Path -Raw
$counters = $result.TestRun.ResultSummary.Counters
foreach ($name in @('total', 'executed', 'passed', 'failed', 'notExecuted')) {
    if (-not $counters -or -not $counters.HasAttribute($name)) {
        throw "Invalid TRX: missing counter $name"
    }
}

$total = [int]$counters.total
$executed = [int]$counters.executed
$passed = [int]$counters.passed
$failed = [int]$counters.failed
$skipped = [int]$counters.notExecuted
if ($total -le 0 -or $executed -ne $total -or $passed -ne $total -or $failed -ne 0 -or $skipped -ne 0) {
    throw "Tests incomplete: total=$total executed=$executed passed=$passed failed=$failed skipped=$skipped"
}

Write-Output "Verified: $passed/$total tests passed; no failures or skipped tests."
