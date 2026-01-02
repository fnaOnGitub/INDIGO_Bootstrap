# IndigoLab Cluster Stopper

Write-Host ""
Write-Host "================================================" -ForegroundColor Red
Write-Host " ARRESTO CLUSTER INDIGOLAB" -ForegroundColor Red
Write-Host "================================================" -ForegroundColor Red
Write-Host ""

Write-Host "Ricerca processi..." -ForegroundColor Yellow

$found = 0

Get-Process | Where-Object {
    $_.ProcessName -like "*Orchestrator*" -or 
    $_.ProcessName -like "*IndigoAi*" -or 
    $_.ProcessName -like "*ControlCenter*"
} | ForEach-Object {
    Write-Host "  Arresto: $($_.ProcessName) (PID: $($_.Id))" -ForegroundColor Yellow
    Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue
    $found++
}

Start-Sleep -Seconds 2

Write-Host ""
if ($found -gt 0) {
    Write-Host "OK Arrestati $found processi" -ForegroundColor Green
} else {
    Write-Host "Nessun processo attivo" -ForegroundColor Gray
}

Write-Host ""
Write-Host "================================================" -ForegroundColor Green
Write-Host " OK CLUSTER ARRESTATO" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green
Write-Host ""
