<#
.SYNOPSIS
Roda o Stryker (só unit tests) e publica o relatório em https://zaqueucavalcante.github.io/estud/mutation.

.EXAMPLE
.\Scripts\mutation-tests.ps1
.\Scripts\mutation-tests.ps1 -NoPublish
.\Scripts\mutation-tests.ps1 -NoPublish -m "**/Extensions/**/*.cs"
#>
param(
    [switch]$NoPublish,
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$StrykerArgs
)

$ErrorActionPreference = 'Stop'

$repo = Split-Path -Parent $PSScriptRoot
$tests = Join-Path $repo 'Tests'
$output = Join-Path $tests 'StrykerOutput'
$icon = Join-Path $repo '.github\assets\stryker.svg'

Push-Location $repo
try {
    dotnet tool restore
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    $startedAt = Get-Date
    Push-Location $tests
    try {
        dotnet stryker --output $output @StrykerArgs
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    } finally {
        Pop-Location
    }

    $html = Get-ChildItem $output -Recurse -Filter 'mutation-report.html' |
        Where-Object { $_.LastWriteTime -ge $startedAt } |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1
    if (-not $html) {
        Write-Error "Nenhum mutation-report.html gerado em $output."
        exit 1
    }
    $reportDir = $html.DirectoryName

    # Regex em vez de ConvertFrom-Json: o relatório embute o código-fonte e passa do limite do Windows PowerShell 5.1.
    $json = Get-Content (Join-Path $reportDir 'mutation-report.json') -Raw
    $statuses = [regex]::Matches($json, '"status"\s*:\s*"(\w+)"') | ForEach-Object { $_.Groups[1].Value }
    $detected = @($statuses | Where-Object { $_ -in 'Killed', 'Timeout' }).Count
    $undetected = @($statuses | Where-Object { $_ -in 'Survived', 'NoCoverage' }).Count
    $score = if ($detected + $undetected -eq 0) { 0 } else { [math]::Round($detected * 100 / ($detected + $undetected), 2) }

    $thresholds = (Get-Content (Join-Path $tests 'stryker-config.json') -Raw | ConvertFrom-Json).'stryker-config'.thresholds
    $color = if ($score -ge $thresholds.high) { 'brightgreen' } elseif ($score -ge $thresholds.low) { 'orange' } else { 'red' }

    Write-Host "Mutation score: $score% ($detected detectados, $undetected não detectados)"
    Write-Host "Relatório: $($html.FullName)"

    if ($NoPublish) { exit 0 }

    $sha = git rev-parse --short HEAD
    if (git status --porcelain) { $sha = "$sha-dirty" }

    $pages = Join-Path ([IO.Path]::GetTempPath()) "estud-gh-pages-$([guid]::NewGuid().ToString('N'))"
    git fetch origin gh-pages
    git worktree add --detach $pages origin/gh-pages
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    try {
        $target = Join-Path $pages 'mutation'
        if (Test-Path $target) { Remove-Item $target -Recurse -Force }
        Copy-Item $reportDir $target -Recurse

        Move-Item (Join-Path $target 'mutation-report.html') (Join-Path $target 'index.html')
        Remove-Item (Join-Path $target 'mutation-report.json')
        Copy-Item $icon (Join-Path $target 'favicon.svg')

        $index = Join-Path $target 'index.html'
        $content = [IO.File]::ReadAllText($index)
        $content = ([regex]'<head>').Replace($content, '<head><link rel="icon" type="image/svg+xml" href="favicon.svg">', 1)
        [IO.File]::WriteAllText($index, $content)

        $logo = [Convert]::ToBase64String([IO.File]::ReadAllBytes($icon))
        $badgeUrl = "https://img.shields.io/badge/Mutation_Score-$score%25-$color" +
            "?logo=$([uri]::EscapeDataString("data:image/svg+xml;base64,$logo"))"
        Invoke-WebRequest $badgeUrl -OutFile (Join-Path $target 'badge.svg') -UseBasicParsing

        git -C $pages add --all mutation
        git -C $pages commit -m "deploy: mutation report for $sha ($score%)"
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

        # O ci.cd.yml também publica no gh-pages (coverage); se ele empurrou no meio tempo, rebaseia e tenta de novo.
        git -C $pages push origin HEAD:gh-pages
        if ($LASTEXITCODE -ne 0) {
            git -C $pages pull --rebase origin gh-pages
            git -C $pages push origin HEAD:gh-pages
            if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
        }

        Write-Host 'Publicado em https://zaqueucavalcante.github.io/estud/mutation'
    } finally {
        git worktree remove --force $pages
    }
} finally {
    Pop-Location
}
