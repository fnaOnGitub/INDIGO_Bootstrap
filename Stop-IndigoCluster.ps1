# ========================================
# 🛑 IndigoLab Cluster Stopper
# ========================================

Write-Host ""
Write-Host "================================================" -ForegroundColor Red
Write-Host " 🛑 ARRESTO CLUSTER INDIGOLAB" -ForegroundColor Red
Write-Host "================================================" -ForegroundColor Red
Write-Host ""

Write-Host "🔍 Ricerca processi attivi..." -ForegroundColor Yellow

$processNames = @(
    "Agent.Orchestrator",
    "IndigoAiWorker01", 
    "ControlCenter.UI",
    "ControlCenter"
)

$foundProcesses = 0

foreach ($procName in $processNames) {
    $processes = Get-Process | Where-Object {$_.ProcessName -like "*$procName*"}
    
    if ($processes) {
        foreach ($proc in $processes) {
            Write-Host "  🛑 Arresto: $($proc.ProcessName) (PID: $($proc.Id))" -ForegroundColor Yellow
            Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
            $foundProcesses++
        }
    }
}

Start-Sleep -Seconds 2

Write-Host ""
if ($foundProcesses -gt 0) {
    Write-Host "✅ Arrestati $foundProcesses processi" -ForegroundColor Green
} else {
    Write-Host "ℹ️  Nessun processo attivo trovato" -ForegroundColor Gray
}

Write-Host ""
Write-Host "================================================" -ForegroundColor Green
Write-Host " ✅ CLUSTER ARRESTATO" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green
Write-Host ""
