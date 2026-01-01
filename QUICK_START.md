# ⚡ Quick Start - IndigoLab Cluster

Guida rapida per avviare e testare il cluster in 5 minuti.

---

## 🚀 AVVIO RAPIDO

### Prerequisiti

- ✅ .NET 8 SDK installato
- ✅ Windows 10/11
- ✅ PowerShell

---

## 📦 STEP 1: Avvia Tutti gli Agenti (6 terminali)

### Opzione A: Avvio Manuale

Apri **6 terminali PowerShell** e esegui:

```powershell
# Terminal 1 - Orchestrator (5001)
cd c:\Users\filip\OneDrive\Documents\02_AREAS\FNA_Coding\VISUAL_STUDIO\INDIGOLAB\INDIGO_BOOTHSTRAPPER\Agent.Orchestrator
dotnet run

# Terminal 2 - Worker01 (5002)
cd c:\Users\filip\OneDrive\Documents\02_AREAS\FNA_Coding\VISUAL_STUDIO\INDIGOLAB\INDIGO_BOOTHSTRAPPER\Agent.Worker01
dotnet run

# Terminal 3 - Worker02 (5003)
cd c:\Users\filip\OneDrive\Documents\02_AREAS\FNA_Coding\VISUAL_STUDIO\INDIGOLAB\INDIGO_BOOTHSTRAPPER\Agent.Worker02
dotnet run

# Terminal 4 - Monitor (5004)
cd c:\Users\filip\OneDrive\Documents\02_AREAS\FNA_Coding\VISUAL_STUDIO\INDIGOLAB\INDIGO_BOOTHSTRAPPER\Agent.Monitor
dotnet run

# Terminal 5 - IndigoAiWorker01 (5005)
cd c:\Users\filip\OneDrive\Documents\02_AREAS\FNA_Coding\VISUAL_STUDIO\INDIGOLAB\INDIGO_BOOTHSTRAPPER\IndigoAiWorker01
dotnet run

# Terminal 6 - CursorMonitorAgent (5006)
cd c:\Users\filip\OneDrive\Documents\02_AREAS\FNA_Coding\VISUAL_STUDIO\INDIGOLAB\INDIGO_BOOTHSTRAPPER\CursorMonitorAgent
dotnet run
```

**Attendi 10-15 secondi** per il completo avvio.

---

### Opzione B: Script PowerShell (consigliato)

Crea file `start-cluster.ps1`:

```powershell
# start-cluster.ps1
$basePath = "c:\Users\filip\OneDrive\Documents\02_AREAS\FNA_Coding\VISUAL_STUDIO\INDIGOLAB\INDIGO_BOOTHSTRAPPER"

Write-Output "=== Avvio IndigoLab Cluster ==="

# Avvia agenti in terminali separati (minimizzati)
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$basePath\Agent.Orchestrator'; dotnet run" -WindowStyle Minimized
Start-Sleep -Seconds 2

Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$basePath\Agent.Worker01'; dotnet run" -WindowStyle Minimized
Start-Sleep -Seconds 2

Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$basePath\Agent.Worker02'; dotnet run" -WindowStyle Minimized
Start-Sleep -Seconds 2

Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$basePath\Agent.Monitor'; dotnet run" -WindowStyle Minimized
Start-Sleep -Seconds 2

Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$basePath\IndigoAiWorker01'; dotnet run" -WindowStyle Minimized
Start-Sleep -Seconds 2

Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$basePath\CursorMonitorAgent'; dotnet run" -WindowStyle Minimized

Write-Output "Cluster avviato! Attendi 10 secondi..."
Start-Sleep -Seconds 10

Write-Output "Verifica cluster..."
.\verify-cluster.ps1
```

Esegui:
```powershell
.\start-cluster.ps1
```

---

## ✅ STEP 2: Verifica Cluster

Crea file `verify-cluster.ps1`:

```powershell
# verify-cluster.ps1
Write-Output "=== VERIFICA CLUSTER INDIGOLAB ==="
Write-Output ""

$agents = @(
    @{Port=5001; Name="Orchestrator"},
    @{Port=5002; Name="Worker01"},
    @{Port=5003; Name="Worker02"},
    @{Port=5004; Name="Monitor"},
    @{Port=5005; Name="IndigoAiWorker01"},
    @{Port=5006; Name="CursorMonitorAgent"}
)

foreach ($agent in $agents) {
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:$($agent.Port)/health" -UseBasicParsing -TimeoutSec 2
        Write-Output "[$($agent.Port)] ✅ $($agent.Name) - ONLINE"
    } catch {
        Write-Output "[$($agent.Port)] ❌ $($agent.Name) - OFFLINE"
    }
}

Write-Output ""
Write-Output "Cluster Status:"
try {
    $status = Invoke-WebRequest -Uri "http://localhost:5004/cluster/status" -UseBasicParsing | ConvertFrom-Json
    Write-Output "  Agenti monitorati: $($status.Agents.Count)"
    Write-Output "  Tutti operativi: $($status.Success)"
} catch {
    Write-Output "  ❌ Monitor non raggiungibile"
}
```

Esegui:
```powershell
.\verify-cluster.ps1
```

**Output atteso:**
```
[5001] ✅ Orchestrator - ONLINE
[5002] ✅ Worker01 - ONLINE
[5003] ✅ Worker02 - ONLINE
[5004] ✅ Monitor - ONLINE
[5005] ✅ IndigoAiWorker01 - ONLINE
[5006] ✅ CursorMonitorAgent - ONLINE

Cluster Status:
  Agenti monitorati: 4
  Tutti operativi: True
```

---

## 🧪 STEP 3: Test Rapido

### Test 1: Dispatch Task Standard

```powershell
$body = @{
    Task = "task-test"
    Payload = "Hello from quick start"
} | ConvertTo-Json

Invoke-WebRequest -Uri "http://localhost:5001/dispatch" `
    -Method POST `
    -Body $body `
    -ContentType "application/json" `
    -UseBasicParsing
```

**Risultato**: Task dispatched a Worker01 o Worker02

---

### Test 2: Dispatch Task AI (optimize-prompt)

```powershell
$body = @{
    Task = "optimize-prompt"
    Payload = "Crea dashboard WPF per monitoraggio cluster con grafici real-time"
} | ConvertTo-Json

$response = Invoke-WebRequest -Uri "http://localhost:5001/dispatch" `
    -Method POST `
    -Body $body `
    -ContentType "application/json" `
    -UseBasicParsing

$data = $response.Content | ConvertFrom-Json

Write-Output "Success: $($data.Success)"
Write-Output "Worker: $($data.Worker)"
Write-Output "WorkerType: $($data.WorkerType)"
Write-Output "IsAiTask: $($data.IsAiTask)"
```

**Risultato**: 
- Task dispatched a IndigoAiWorker01
- File generato in CursorBridge/
- CursorMonitorAgent rileva file

---

### Test 3: Verifica FileSystemWatcher

```powershell
# Crea file test con errore compilazione
$testContent = @"
# Test FileSystemWatcher

Questo file testa il monitoraggio automatico.

Error CS1234: Missing semicolon at line 10
Build failed with 1 error
"@

$testPath = "c:\Users\filip\OneDrive\Documents\02_AREAS\FNA_Coding\VISUAL_STUDIO\INDIGOLAB\INDIGO_BOOTHSTRAPPER\IndigoAiWorker01\bin\Debug\net8.0\CursorBridge\test-quick-start.md"

Set-Content -Path $testPath -Value $testContent

Write-Output "File creato: test-quick-start.md"
Write-Output "Attendi 2 secondi per rilevamento..."
Start-Sleep -Seconds 2

# Verifica log CursorMonitorAgent
$logs = Invoke-WebRequest -Uri "http://localhost:5006/logs" -UseBasicParsing | ConvertFrom-Json

Write-Output ""
Write-Output "Ultimi 5 log CursorMonitorAgent:"
$logs.Logs | Select-Object -Last 5 | ForEach-Object {
    Write-Output "  [$($_.Level)] $($_.Message)"
}
```

**Risultato**: 
- File rilevato
- Task suggerito: `fix-compilation-errors`

---

### Test 4: Verifica Dialogo Utente

```powershell
# Crea domanda
$questionBody = @{
    Question = "Vuoi correggere l'errore di compilazione?"
    Context = "Rilevato error CS1234 in test-quick-start.md"
    Options = @("fix-compilation-errors", "ignore", "ask-later")
} | ConvertTo-Json

$questionResp = Invoke-WebRequest -Uri "http://localhost:5006/ask-user/create" `
    -Method POST `
    -Body $questionBody `
    -ContentType "application/json" `
    -UseBasicParsing

$question = ($questionResp.Content | ConvertFrom-Json).Question

Write-Output "Domanda creata:"
Write-Output "  ID: $($question.Id)"
Write-Output "  Question: $($question.Question)"
Write-Output "  Options: $($question.Options -join ', ')"

# Simula risposta utente
$answerBody = @{
    QuestionId = $question.Id
    Answer = "fix-compilation-errors"
} | ConvertTo-Json

$answerResp = Invoke-WebRequest -Uri "http://localhost:5006/ask-user/answer" `
    -Method POST `
    -Body $answerBody `
    -ContentType "application/json" `
    -UseBasicParsing

$answer = $answerResp.Content | ConvertFrom-Json

Write-Output ""
Write-Output "Risposta registrata:"
Write-Output "  Success: $($answer.Success)"
Write-Output "  Message: $($answer.Message)"
```

**Risultato**: 
- Domanda creata e risposta registrata

---

## 🖥️ STEP 4: Avvia Control Center UI

```powershell
# Terminal 7 (nuovo)
cd c:\Users\filip\OneDrive\Documents\02_AREAS\FNA_Coding\VISUAL_STUDIO\INDIGOLAB\INDIGO_BOOTHSTRAPPER\ControlCenter.UI
dotnet run
```

**Nell'UI:**
1. ✅ Dashboard mostra 6 agenti (ora con CursorMonitorAgent)
2. ✅ Click su "agent-orchestrator" → Agent Details
3. ✅ Dispatch task AI:
   - Task Name: `optimize-prompt`
   - Payload: `Crea sistema di notifiche WPF real-time`
4. ✅ Verifica pannello "AI Task Result" appare
5. ✅ Click "Mostra Log" → Verifica log
6. ✅ Abilita "Auto-refresh" → Log si aggiornano ogni 5s

---

## 🔄 STEP 5: Test Ciclo Autonomo

### Scenario Completo

1. **Dispatch task iniziale** (da Control Center UI o PowerShell):
   ```powershell
   POST http://localhost:5001/dispatch
   {
     "Task": "optimize-prompt",
     "Payload": "Crea sistema di notifiche"
   }
   ```

2. **Monitora eventi**:
   ```powershell
   # Verifica log IndigoAiWorker01
   curl http://localhost:5005/logs
   
   # Verifica log CursorMonitorAgent
   curl http://localhost:5006/logs
   ```

3. **Verifica file generato**:
   ```powershell
   # Lista file CursorBridge
   curl http://localhost:5005/cursor/bridge-files
   ```

4. **Verifica rilevamento CursorMonitorAgent**:
   ```powershell
   # Log CursorMonitorAgent dovrebbe mostrare:
   # - "File creato: ai-output-optimize-prompt-xxx.md"
   # - "Task suggerito: ..." (se applicabile)
   ```

5. **Ciclo continua autonomamente** ♾️

---

## 🛑 STEP 6: Arresta Cluster

### Opzione A: Manuale
Chiudi tutti i terminali PowerShell (CTRL+C in ogni terminale)

### Opzione B: Script

Crea file `stop-cluster.ps1`:

```powershell
# stop-cluster.ps1
Write-Output "=== Arresto IndigoLab Cluster ==="

# Trova e termina processi .NET sulle porte 5001-5006
@(5001, 5002, 5003, 5004, 5005, 5006) | ForEach-Object {
    $port = $_
    $connections = netstat -ano | Select-String ":$port"
    
    foreach ($conn in $connections) {
        $line = $conn -replace '\s+', ' '
        $parts = $line -split ' '
        $processId = $parts[-1]
        
        try {
            Stop-Process -Id $processId -Force -ErrorAction SilentlyContinue
            Write-Output "[$port] Processo $processId terminato"
        } catch {
            # Ignora errori
        }
    }
}

Write-Output "Cluster arrestato!"
```

Esegui:
```powershell
.\stop-cluster.ps1
```

---

## 📊 VERIFICA FINALE

### Checklist

- [ ] Tutti i 6 agenti online
- [ ] Monitor restituisce 4 agenti
- [ ] Task standard funziona (round-robin)
- [ ] Task AI funziona (IndigoAiWorker01)
- [ ] File generato in CursorBridge
- [ ] CursorMonitorAgent rileva file
- [ ] Dialogo utente funziona
- [ ] Control Center UI funziona
- [ ] AI Task Result Panel appare
- [ ] Log Viewer funziona

---

## 🎯 PROSSIMI PASSI

Dopo aver verificato il funzionamento base:

1. **Leggi documentazione completa**: `README.md`
2. **Esplora guide specifiche**:
   - `CURSOR_MONITOR_AGENT_GUIDE.md` - Agente autonomo
   - `FILE_ALWAYS_MODE_GUIDE.md` - FILE ALWAYS MODE
   - `AI_TASK_RESULT_PANEL_GUIDE.md` - UI risultati AI
3. **Testa scenari avanzati**:
   - Multi-cursor support
   - Auto-dispatch task
   - Pattern recognition
4. **Integra con progetti reali**

---

## 🐛 TROUBLESHOOTING RAPIDO

### Agente non si avvia
```powershell
# Verifica porta occupata
netstat -ano | findstr ":<PORT>"

# Termina processo
Stop-Process -Id <PID> -Force
```

### File non rilevato da CursorMonitorAgent
```powershell
# Verifica path monitorato
curl http://localhost:5006/monitored-instances

# Verifica log
curl http://localhost:5006/logs
```

### Control Center UI non mostra agenti
```powershell
# Verifica tutti gli agenti online
.\verify-cluster.ps1
```

---

## 📞 RISORSE

- **Swagger API**: http://localhost:500X/swagger (X = 1-6)
- **Documentazione**: `README.md`
- **Changelog**: `CHANGELOG.md`
- **Guide dettagliate**: `*_GUIDE.md`

---

## 🎉 CONCLUSIONE

**Congratulazioni!** 🎊

Hai avviato con successo IndigoLab Cluster v2.0 - un sistema AI autonomo end-to-end!

Il cluster può ora:
- ✅ Eseguire task AI
- ✅ Generare file automaticamente
- ✅ Monitorare eventi real-time
- ✅ Dialogare con utente
- ✅ **Completare cicli di sviluppo autonomamente** ♾️

---

**⚡ QUICK START COMPLETATO - BUON SVILUPPO CON INDIGOLAB!** 🚀✨

---

*Quick Start Guide - IndigoLab Cluster v2.0*  
*Tempo stimato: 5-10 minuti*  
*Difficoltà: ⭐⭐☆☆☆*
