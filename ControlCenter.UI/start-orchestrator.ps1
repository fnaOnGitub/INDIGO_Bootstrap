# start-orchestrator.ps1
# Script per avviare automaticamente l'Orchestrator

Write-Output "=== AVVIO ORCHESTRATOR AUTOMATICO ==="
Write-Output ""

# Trova la cartella Agent.Orchestrator
$currentDir = Get-Location
$orchestratorPath = $null

# Prova percorsi relativi
$paths = @(
    "..\Agent.Orchestrator",
    "..\..\Agent.Orchestrator",
    "..\..\..\Agent.Orchestrator"
)

foreach ($path in $paths) {
    $fullPath = Join-Path $currentDir $path
    if (Test-Path $fullPath) {
        $orchestratorPath = Resolve-Path $fullPath
        break
    }
}

if ($null -eq $orchestratorPath) {
    Write-Output "❌ Cartella Agent.Orchestrator non trovata"
    Write-Output "Percorso corrente: $currentDir"
    exit 1
}

Write-Output "✓ Cartella trovata: $orchestratorPath"
Write-Output ""

# Verifica se l'Orchestrator è già in esecuzione
$existingProcess = netstat -ano | Select-String ":5001" | Select-Object -First 1

if ($existingProcess) {
    Write-Output "⚠️ Orchestrator già in esecuzione su porta 5001"
    $line = $existingProcess -replace '\s+', ' '
    $parts = $line -split ' '
    $processId = $parts[-1]
    Write-Output "PID: $processId"
    
    $response = Read-Host "Vuoi terminarlo e riavviarlo? (s/n)"
    if ($response -eq 's') {
        Stop-Process -Id $processId -Force
        Write-Output "✓ Processo terminato"
        Start-Sleep -Seconds 2
    } else {
        Write-Output "✓ Orchestrator già attivo"
        exit 0
    }
}

# Avvia l'Orchestrator
Write-Output "Avvio Orchestrator..."
Set-Location $orchestratorPath

try {
    # Avvia dotnet run in una nuova finestra minimizzata
    $process = Start-Process -FilePath "dotnet" `
                             -ArgumentList "run" `
                             -WorkingDirectory $orchestratorPath `
                             -WindowStyle Minimized `
                             -PassThru
    
    Write-Output "✓ Processo avviato (PID: $($process.Id))"
    Write-Output "Attendi 5 secondi per l'avvio completo..."
    Start-Sleep -Seconds 5
    
    # Verifica se risponde
    try {
        $health = Invoke-WebRequest -Uri "http://localhost:5001/health" -UseBasicParsing -TimeoutSec 2
        Write-Output "✅ Orchestrator ONLINE su porta 5001"
        Write-Output ""
        Write-Output "=== ORCHESTRATOR AVVIATO CON SUCCESSO ==="
        exit 0
    } catch {
        Write-Output "⚠️ Orchestrator avviato ma non risponde ancora"
        Write-Output "Potrebbe necessitare di più tempo (attendi 10 secondi)"
        exit 0
    }
}
catch {
    Write-Output "❌ Errore durante l'avvio: $($_.Exception.Message)"
    exit 1
}
