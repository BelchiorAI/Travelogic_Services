# Stops Supplier Hub's API, database and file storage on Windows.
# Run stop.cmd (keeps your data) or "stop.cmd --reset" (also deletes all data).
param([switch]$Reset)
$ErrorActionPreference = 'Stop'
Set-Location (Join-Path (Split-Path -Parent $PSScriptRoot) 'backend')

if ($Reset -or $args -contains '--reset') {
    docker compose down -v
    Write-Host 'Stopped, and all data deleted. The next start.cmd begins with the sample suppliers again.'
}
else {
    docker compose down
    Write-Host 'Stopped. Your data is kept; run start.cmd to start again.'
}
