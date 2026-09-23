<#
.SYNOPSIS
Ordena os usings do topo dos arquivos .cs por tamanho (do menor pro maior), desempatando em ordem alfabética.

.DESCRIPTION
Cada bloco contínuo de usings é ordenado separadamente, então linhas em branco entre grupos são preservadas.
Sem argumentos, processa Back/ e Tests/. Aceita arquivos ou pastas.

.EXAMPLE
.\Scripts\sort-usings.ps1
.\Scripts\sort-usings.ps1 Tests\Base\KeycloakFactory.cs
.\Scripts\sort-usings.ps1 Back\Features\Identity
#>
param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$Paths
)

$repo = Split-Path -Parent $PSScriptRoot
if (-not $Paths) { $Paths = @((Join-Path $repo 'Back'), (Join-Path $repo 'Tests')) }

$files = foreach ($p in $Paths) {
    $item = Get-Item $p -ErrorAction Stop
    if ($item.PSIsContainer) {
        Get-ChildItem $item.FullName -Recurse -Filter *.cs -File |
            Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' }
    }
    else { $item }
}

$usingLine = '^\s*(global\s+)?using\s+(static\s+)?[\w.]+(\s*=\s*[\w.<>, ]+)?\s*;\s*$'
$headerLine = '^\s*($|//|#)'
$changed = 0

foreach ($file in $files) {
    $bytes = [IO.File]::ReadAllBytes($file.FullName)
    $hasBom = $bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF
    $text = [Text.Encoding]::UTF8.GetString($bytes)
    if ($hasBom) { $text = $text.Substring(1) }

    $eol = if ($text.Contains("`r`n")) { "`r`n" } else { "`n" }
    $lines = $text -split '\r?\n'

    $end = 0
    while ($end -lt $lines.Count -and ($lines[$end] -match $usingLine -or $lines[$end] -match $headerLine)) { $end++ }

    $i = 0
    while ($i -lt $end) {
        if ($lines[$i] -notmatch $usingLine) { $i++; continue }
        $start = $i
        while ($i -lt $end -and $lines[$i] -match $usingLine) { $i++ }

        $block = $lines[$start..($i - 1)]
        $sorted = [string[]]($block | Sort-Object @{ Expression = { $_.Trim().Length } }, @{ Expression = { $_.Trim() } })
        for ($j = 0; $j -lt $sorted.Count; $j++) { $lines[$start + $j] = $sorted[$j] }
    }

    $newText = $lines -join $eol
    if ($newText -ne $text) {
        [IO.File]::WriteAllText($file.FullName, $newText, (New-Object Text.UTF8Encoding($hasBom)))
        Write-Host "-> $($file.FullName.Substring($repo.Length + 1))"
        $changed++
    }
}

Write-Host "$changed arquivo(s) alterado(s)."
