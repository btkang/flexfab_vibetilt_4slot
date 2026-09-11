param(
    [Parameter(Mandatory = $true, ValueFromRemainingArguments = $true)]
    [string[]]$Paths
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

foreach ($rawPath in $Paths) {
    $resolved = Resolve-Path -LiteralPath $rawPath
    $fullPath = $resolved.Path

    $content = [System.IO.File]::ReadAllText($fullPath)
    $normalized = $content -replace "`r?`n", "`r`n"

    [System.IO.File]::WriteAllText(
        $fullPath,
        $normalized,
        [System.Text.UTF8Encoding]::new($false)
    )

    Write-Output "normalized: $fullPath"
}
