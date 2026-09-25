# Starts Supplier Hub on Windows: the API, database and file storage in Docker, then the web app.
# Run start.cmd (double-click) rather than this file directly; stop the backend later with stop.cmd.
$ErrorActionPreference = 'Stop'
Set-Location (Split-Path -Parent $PSScriptRoot)

$ApiUrl = 'http://localhost:5000'
$GeminiEndpoint = 'https://generativelanguage.googleapis.com/v1beta/openai/'

function Step($text) { Write-Host "`n==> $text" -ForegroundColor Cyan }
function Fail($text) {
    Write-Host "`nError: $text" -ForegroundColor Red
    exit 1
}

# 1. Prerequisites, with plain instructions when something is missing.
Step 'Checking prerequisites'
if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    Fail 'Docker is not installed. Install Docker Desktop: https://www.docker.com/products/docker-desktop/'
}
# Through cmd: Windows PowerShell would turn Docker's error output into a crash instead of this message.
cmd /c "docker info >nul 2>&1"
if ($LASTEXITCODE -ne 0) {
    Fail 'Docker is installed but not running. Start Docker Desktop, wait until it says it is running, then try again.'
}
if (-not (Get-Command node -ErrorAction SilentlyContinue)) {
    Fail 'Node.js is not installed. Install version 20 or newer from https://nodejs.org/'
}
$nodeMajor = [int](node -p "process.versions.node.split('.')[0]")
if ($nodeMajor -lt 20) {
    Fail "Node.js 20 or newer is needed (you have $(node -v)). Update it from https://nodejs.org/"
}
Write-Host "Docker and Node.js $(node -v) found."

# 2. Optional AI import: asked once, remembered in backend\.env (git-ignored).
if (-not (Test-Path 'backend\.env')) {
    Step 'AI import (optional)'
    Write-Host 'AI import reads a pasted rate sheet or email and fills in the supplier form.'
    Write-Host 'Paste a Google Gemini API key to switch it on, or just press Enter to skip.'
    $secure = Read-Host 'Gemini API key' -AsSecureString
    $key = [System.Net.NetworkCredential]::new('', $secure).Password
    if ($key) {
        $lines = @('AI_ENABLED=true', 'AI_MODEL=gemini-3.5-flash', "AI_API_KEY=$key", "AI_ENDPOINT=$GeminiEndpoint")
        Write-Host 'AI import switched on (saved in backend\.env).'
    }
    else {
        $lines = @('# AI import is off. To switch it on, set these and run start.cmd again:', 'AI_ENABLED=false',
            'AI_MODEL=gemini-3.5-flash', 'AI_API_KEY=', "AI_ENDPOINT=$GeminiEndpoint")
        Write-Host 'Skipped. The app works without it; add a key to backend\.env later if you like.'
    }
    # No byte-order mark: docker compose reads this file.
    [System.IO.File]::WriteAllLines((Join-Path (Get-Location) 'backend\.env'), $lines, [System.Text.UTF8Encoding]::new($false))
}

# 3. Backend: API + SQL Server + S3 store.
Step 'Starting the API, database and file storage'
Write-Host 'The first run downloads about 1 GB and builds the API, which can take several minutes.'
Push-Location backend
docker compose up -d --build
$composeExit = $LASTEXITCODE
Pop-Location
if ($composeExit -ne 0) { Fail 'Docker could not start the services. See the messages above.' }

Step 'Waiting for the API to be ready'
$ready = $false
for ($i = 0; $i -lt 90; $i++) {
    try {
        Invoke-WebRequest "$ApiUrl/health/ready" -UseBasicParsing -TimeoutSec 5 | Out-Null
        $ready = $true
        break
    }
    catch {
        Write-Host -NoNewline '.'
        Start-Sleep -Seconds 2
    }
}
Write-Host ''
if (-not $ready) {
    Fail "The API didn't become ready in 3 minutes. See what happened with: cd backend; docker compose logs api"
}
Write-Host "API ready at $ApiUrl (docs: $ApiUrl/scalar/v1)."

# 4. Web app.
Set-Location frontend
if (-not (Test-Path '.env.local')) { Copy-Item '.env.example' '.env.local' }
if (-not (Test-Path 'node_modules')) {
    Step "Installing the web app's packages (first run only)"
    if (Get-Command bun -ErrorAction SilentlyContinue) { bun install --frozen-lockfile } else { npm install --no-audit --no-fund }
    if ($LASTEXITCODE -ne 0) { Fail 'Installing packages failed. See the messages above.' }
}

Step 'Starting the web app'
Write-Host 'Your browser opens automatically. Close this window or press Ctrl+C to stop the web app;'
Write-Host 'the API keeps running until you run stop.cmd.'
# The same command as "npm run dev"; called directly because Windows PowerShell can drop npm's "--" separator.
npx vite dev --open
