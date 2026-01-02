# IndigoLab Cluster Launcher - Console Minimizzate (AFFIDABILE)

Write-Host ""
Write-Host "================================================" -ForegroundColor Cyan
Write-Host " AVVIO CLUSTER INDIGOLAB" -ForegroundColor Cyan  
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# Chiudi processi esistenti
Write-Host "Chiusura processi..." -ForegroundColor Yellow
Get-Process | Where-Object {
    $_.ProcessName -like "*Orchestrator*" -or 
    $_.ProcessName -like "*IndigoAi*" -or 
    $_.ProcessName -like "*ControlCenter*"
} | Stop-Process -Force -ErrorAction SilentlyContinue

Start-Sleep -Seconds 2
Write-Host "OK" -ForegroundColor Green
Write-Host ""

# Percorsi
$basePath = "c:\Users\filip\OneDrive\Documents\02_AREAS\FNA_Coding\VISUAL_STUDIO\INDIGOLAB\INDIGO_BOOTHSTRAPPER"

Write-Host "Avvio componenti..." -ForegroundColor Cyan
Write-Host ""

# 1. Orchestrator (Minimizzato)
Write-Host "  1. Orchestrator (porta 5001)" -ForegroundColor Magenta
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$basePath\Agent.Orchestrator'; Write-Host 'ORCHESTRATOR - Porta 5001' -ForegroundColor Magenta; dotnet run" -WindowStyle Minimized
Start-Sleep -Seconds 3

# 2. AI Worker (Minimizzato)  
Write-Host "  2. AI Worker (porta 5005)" -ForegroundColor Cyan
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$basePath\IndigoAiWorker01'; Write-Host 'AI WORKER - Porta 5005' -ForegroundColor Cyan; dotnet run" -WindowStyle Minimized
Start-Sleep -Seconds 4

# 3. Control Center UI (Minimizzato)
Write-Host "  3. Control Center UI" -ForegroundColor Green
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$basePath\ControlCenter.UI'; Write-Host 'CONTROL CENTER UI' -ForegroundColor Green; dotnet run" -WindowStyle Minimized
Start-Sleep -Seconds 2

Write-Host ""
Write-Host "================================================" -ForegroundColor Green
Write-Host " OK CLUSTER AVVIATO!" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green
Write-Host ""
Write-Host "3 console PowerShell minimizzate nella barra" -ForegroundColor White
Write-Host "   - Orchestrator (porta 5001)" -ForegroundColor Magenta
Write-Host "   - AI Worker (porta 5005)" -ForegroundColor Cyan
Write-Host "   - Control Center UI" -ForegroundColor Green
Write-Host ""
Write-Host "Attendi 10-15 secondi per il caricamento" -ForegroundColor Yellow
Write-Host ""
Write-Host "Per fermare: esegui StopCluster.ps1" -ForegroundColor Gray
Write-Host ""
