$ErrorActionPreference = 'Stop'

Write-Host '[phase0] Building solution...'
dotnet build ModeManager.sln | Out-Host

Write-Host '[phase0] Validating language files...'
$langFiles = @(
    'lang/en.json',
    'lang/pt-BR.json',
    'lang/es.json',
    'lang/ru.json'
)

foreach ($file in $langFiles) {
    Get-Content $file -Raw | ConvertFrom-Json | Out-Null
    Write-Host ("[phase0] OK: {0}" -f $file)
}

Write-Host '[phase0] Static baseline validation completed.'
