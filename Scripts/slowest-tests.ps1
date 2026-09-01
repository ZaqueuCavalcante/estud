<#
.SYNOPSIS
Roda os testes e escreve em tests.txt a lista dos que rodaram, do mais demorado pro mais rápido.

.EXAMPLE
.\Scripts\slowest-tests.ps1
.\Scripts\slowest-tests.ps1 -Out lentos.txt
.\Scripts\slowest-tests.ps1 --filter "FullyQualifiedName~IntegrationTests"
#>
param(
    [string]$Out,
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$TestArgs
)

$repo = Split-Path -Parent $PSScriptRoot
if (-not $Out) { $Out = Join-Path $repo 'tests.txt' }
$trxDir = Join-Path $repo 'Tests\TestResults'
$trx = Join-Path $trxDir 'slowest-tests.trx'

Push-Location $repo
try {
    Remove-Item $trx -ErrorAction SilentlyContinue
    dotnet test --report-trx --report-trx-filename slowest-tests.trx --results-directory $trxDir @TestArgs
    $status = $LASTEXITCODE

    if (-not (Test-Path $trx)) {
        Write-Error "Nenhum relatório gerado em $trx (o build falhou?)."
        exit 1
    }

    $results = ([xml](Get-Content $trx)).TestRun.Results.UnitTestResult | ForEach-Object {
        [pscustomobject]@{
            Seconds = [TimeSpan]::Parse($_.duration).TotalSeconds
            Name    = $_.testName
            Outcome = $_.outcome
        }
    } | Sort-Object Seconds -Descending

    $total = ($results | Measure-Object Seconds -Sum).Sum
    $failed = @($results | Where-Object { $_.Outcome -ne 'Passed' -and $_.Outcome -ne 'NotExecuted' }).Count

    $lines = @("$($results.Count) testes em $('{0:N1}' -f $total)s ($failed falhando)", '')
    $lines += $results | ForEach-Object {
        '{0,9:N3}s  {1,-11} {2}' -f $_.Seconds, $_.Outcome, $_.Name
    }
    $lines | Set-Content $Out -Encoding UTF8

    Write-Host "-> $Out"
    exit $status
}
finally {
    Pop-Location
}
