# Cluster IndigoLab - Miglioramenti e Logging

Guida completa ai miglioramenti implementati nel cluster IndigoLab.

## Modifiche Implementate

### A) INDIGOAIWORKER01 AGGIUNTO AL MONITOR ✅

**File modificato**: `Agent.Monitor/Program.cs`

**Prima:**
```csharp
var clusterAgents = new[]
{
    new { Name = "orchestrator", Url = "http://localhost:5001" },
    new { Name = "worker01", Url = "http://localhost:5002" },
    new { Name = "worker02", Url = "http://localhost:5003" }
};
```

**Dopo:**
```csharp
var clusterAgents = new[]
{
    new { Name = "orchestrator", Url = "http://localhost:5001" },
    new { Name = "worker01", Url = "http://localhost:5002" },
    new { Name = "worker02", Url = "http://localhost:5003" },
    new { Name = "indigoaiworker01", Url = "http://localhost:5005" }  // ⭐ NEW
};
```

**Test:**
```bash
GET http://localhost:5004/cluster/status

Response:
{
  "Success": true,
  "Agents": [
    "orchestrator",
    "worker01",
    "worker02",
    "indigoaiworker01"  ⭐
  ]
}
```

---

### B) ENDPOINT /logs AGGIUNTO A TUTTI GLI AGENTI ✅

#### LogBuffer.cs (Nuovo)

Creato in ogni agente:
- `Agent.Orchestrator/LogBuffer.cs`
- `Agent.Worker01/LogBuffer.cs`
- `Agent.Worker02/LogBuffer.cs`
- `IndigoAiWorker01/LogBuffer.cs`

**Funzionalità:**
- Buffer thread-safe con `lock()`
- Mantiene gli ultimi 50 eventi
- Metodi: `Add(message, level)`, `GetAll()`, `Clear()`

**Esempio:**
```csharp
public class LogBuffer
{
    private readonly List<LogEntry> _logs = new();
    private readonly object _lock = new();
    private readonly int _maxEntries;

    public LogBuffer(int maxEntries = 50)
    {
        _maxEntries = maxEntries;
    }

    public void Add(string message, string level = "INFO")
    {
        lock (_lock)
        {
            _logs.Add(new LogEntry
            {
                Timestamp = DateTime.UtcNow,
                Level = level,
                Message = message
            });

            if (_logs.Count > _maxEntries)
            {
                _logs.RemoveAt(0);
            }
        }
    }

    public List<LogEntry> GetAll()
    {
        lock (_lock)
        {
            return _logs.ToList();
        }
    }
}

public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = "INFO";
    public string Message { get; set; } = "";
}
```

#### Endpoint GET /logs

Aggiunto in tutti gli agenti:

**Esempio (Agent.Orchestrator):**
```csharp
app.MapGet("/logs", (LogBuffer logBuffer) =>
{
    logger.LogInformation("Log richiesti");
    return Results.Ok(new
    {
        Success = true,
        Count = logBuffer.GetAll().Count,
        Logs = logBuffer.GetAll()
    });
})
.WithName("GetLogs")
.WithOpenApi();
```

**Response JSON:**
```json
{
  "Success": true,
  "Count": 3,
  "Logs": [
    {
      "Timestamp": "2026-01-01T12:32:45.123Z",
      "Level": "INFO",
      "Message": "Task ricevuto: optimize-prompt"
    },
    {
      "Timestamp": "2026-01-01T12:32:45.150Z",
      "Level": "INFO",
      "Message": "Instradato a AI-Worker: http://localhost:5005"
    },
    {
      "Timestamp": "2026-01-01T12:32:46.200Z",
      "Level": "INFO",
      "Message": "Task 'optimize-prompt' completato con successo da AI-Worker"
    }
  ]
}
```

---

### C) LOGGING EVENTI NEI MICROSERVIZI ✅

#### Agent.Orchestrator

**Eventi loggati:**
```csharp
// POST /dispatch
logBuffer.Add($"Task ricevuto: {request.Task}");
logBuffer.Add($"Instradato a {workerType}: {workerUrl}");
logBuffer.Add($"Task '{request.Task}' completato con successo da {workerType}");
logBuffer.Add($"Errore da worker: {response.StatusCode}", "ERROR");
```

#### Agent.Worker01 / Agent.Worker02

**Eventi loggati:**
```csharp
// POST /execute
logBuffer.Add($"Task ricevuto: {request.Task}");
logBuffer.Add($"Esecuzione task '{request.Task}' in corso...");
logBuffer.Add($"Task '{request.Task}' completato con successo");
```

#### IndigoAiWorker01

**Eventi loggati:**
```csharp
// POST /execute
logBuffer.Add($"Task AI ricevuto: {request.Task}");

// Case optimize-prompt
logBuffer.Add($"Ottimizzazione prompt in corso...");
logBuffer.Add($"Prompt ottimizzato generato e salvato: {fileName}");
logBuffer.Add($"Errore scrittura prompt: {ex.Message}", "ERROR");

// Default
logBuffer.Add($"Task non riconosciuto: {request.Task}", "WARN");
logBuffer.Add($"Task AI '{request.Task}' completato con successo");
```

---

## Test Completo

### 1. Verifica Cluster Attivo

```bash
netstat -ano | findstr "500[1-5]"
```

**Output:**
```
TCP    127.0.0.1:5001    LISTENING    32236  # Orchestrator
TCP    127.0.0.1:5002    LISTENING    25384  # Worker01
TCP    127.0.0.1:5003    LISTENING    40836  # Worker02
TCP    127.0.0.1:5004    LISTENING    41628  # Monitor
TCP    127.0.0.1:5005    LISTENING    26080  # IndigoAiWorker01
```

### 2. Test Monitor con IndigoAiWorker01

```bash
curl http://localhost:5004/cluster/status
```

**Response:**
```json
{
  "Success": true,
  "Timestamp": "2026-01-01T12:30:00Z",
  "Agents": [
    {
      "Name": "orchestrator",
      "Url": "http://localhost:5001",
      "Status": { "Agent": "orchestrator", "Uptime": "00:05:12", "Version": "1.0.0" }
    },
    {
      "Name": "worker01",
      "Url": "http://localhost:5002",
      "Status": { "Agent": "worker01", "Uptime": "00:05:10", "Version": "1.0.0" }
    },
    {
      "Name": "worker02",
      "Url": "http://localhost:5003",
      "Status": { "Agent": "worker02", "Uptime": "00:05:08", "Version": "1.0.0" }
    },
    {
      "Name": "indigoaiworker01",
      "Url": "http://localhost:5005",
      "Status": {
        "Agent": "IndigoAiWorker01",
        "Type": "AI-Worker",
        "Uptime": "00:05:05",
        "Version": "1.0.0-AI",
        "Capabilities": [
          "generate-code",
          "refactor-code",
          "explain-code",
          "create-component",
          "fix-snippet",
          "cursor-prompt",
          "optimize-prompt"
        ]
      }
    }
  ]
}
```

### 3. Test Endpoint /logs

```bash
# Orchestrator
curl http://localhost:5001/logs

# Worker01
curl http://localhost:5002/logs

# Worker02
curl http://localhost:5003/logs

# IndigoAiWorker01
curl http://localhost:5005/logs
```

### 4. Test Completo: Dispatch + Logs

**Step 1: Dispatch Task**
```bash
curl -X POST http://localhost:5001/dispatch \
  -H "Content-Type: application/json" \
  -d '{
    "Task": "optimize-prompt",
    "Payload": "Crea pagina WPF metriche cluster real-time con LiveCharts2, MVVM, stile IndigoLab"
  }'
```

**Response:**
```json
{
  "Success": true,
  "Message": "Task dispatched to AI-Worker",
  "Worker": "http://localhost:5005",
  "WorkerType": "AI-Worker",
  "IsAiTask": true,
  "WorkerResult": {
    "Success": true,
    "Message": "AI Task executed",
    "Result": "# TASK: UI: Crea pagina WPF metriche cluster...",
    "ExecutedTask": "optimize-prompt",
    "Timestamp": "2026-01-01T12:32:50.123Z",
    "CursorFileWritten": true,
    "CursorFilePath": "C:\\...\\CursorBridge\\optimized-2026-01-01-123250.md",
    "OptimizedPrompt": "# TASK: UI: Crea pagina WPF metriche cluster..."
  }
}
```

**Step 2: Verifica Logs Orchestrator**
```bash
curl http://localhost:5001/logs
```

**Response:**
```json
{
  "Success": true,
  "Count": 3,
  "Logs": [
    {
      "Timestamp": "2026-01-01T12:32:45.123Z",
      "Level": "INFO",
      "Message": "Task ricevuto: optimize-prompt"
    },
    {
      "Timestamp": "2026-01-01T12:32:45.150Z",
      "Level": "INFO",
      "Message": "Instradato a AI-Worker: http://localhost:5005"
    },
    {
      "Timestamp": "2026-01-01T12:32:50.200Z",
      "Level": "INFO",
      "Message": "Task 'optimize-prompt' completato con successo da AI-Worker"
    }
  ]
}
```

**Step 3: Verifica Logs IndigoAiWorker01**
```bash
curl http://localhost:5005/logs
```

**Response:**
```json
{
  "Success": true,
  "Count": 4,
  "Logs": [
    {
      "Timestamp": "2026-01-01T12:32:45.160Z",
      "Level": "INFO",
      "Message": "Task AI ricevuto: optimize-prompt"
    },
    {
      "Timestamp": "2026-01-01T12:32:45.180Z",
      "Level": "INFO",
      "Message": "Ottimizzazione prompt in corso..."
    },
    {
      "Timestamp": "2026-01-01T12:32:50.100Z",
      "Level": "INFO",
      "Message": "Prompt ottimizzato generato e salvato: optimized-2026-01-01-123250.md"
    },
    {
      "Timestamp": "2026-01-01T12:32:50.150Z",
      "Level": "INFO",
      "Message": "Task AI 'optimize-prompt' completato con successo"
    }
  ]
}
```

---

## Architettura Logging

```
┌─────────────────────────────────────────────────────────────┐
│                         CLUSTER                              │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  ┌─────────────────┐                                        │
│  │  Orchestrator   │◄────── POST /dispatch                  │
│  │   (Port 5001)   │                                        │
│  │                 │  LogBuffer:                            │
│  │  GET /logs  ✅  │  1. "Task ricevuto: optimize-prompt"  │
│  │                 │  2. "Instradato a AI-Worker"           │
│  │                 │  3. "Task completato"                  │
│  └────────┬────────┘                                        │
│           │                                                  │
│           ├────────► Worker01 (5002) - GET /logs ✅         │
│           │          LogBuffer: task execution logs         │
│           │                                                  │
│           ├────────► Worker02 (5003) - GET /logs ✅         │
│           │          LogBuffer: task execution logs         │
│           │                                                  │
│           └────────► IndigoAiWorker01 (5005) - GET /logs ✅ │
│                      LogBuffer:                             │
│                      1. "Task AI ricevuto"                  │
│                      2. "Ottimizzazione in corso"           │
│                      3. "Prompt salvato"                    │
│                      4. "Task completato"                   │
│                                                               │
│  ┌─────────────────┐                                        │
│  │     Monitor     │◄────── GET /cluster/status             │
│  │   (Port 5004)   │                                        │
│  │                 │  Agents:                               │
│  │  Agents: 4 ✅   │  - orchestrator                        │
│  │                 │  - worker01                            │
│  │                 │  - worker02                            │
│  │                 │  - indigoaiworker01 ⭐ NEW            │
│  └─────────────────┘                                        │
│                                                               │
└─────────────────────────────────────────────────────────────┘
```

---

## Livelli di Log

| Level | Uso | Esempio |
|-------|-----|---------|
| **INFO** | Eventi normali | "Task ricevuto", "Task completato" |
| **WARN** | Situazioni anomale ma gestibili | "Task non riconosciuto" |
| **ERROR** | Errori che impediscono il completamento | "Errore da worker", "Errore scrittura file" |

---

## Buffer Configuration

| Parametro | Valore | Descrizione |
|-----------|--------|-------------|
| `maxEntries` | 50 | Numero massimo di log mantenuti |
| Thread-Safe | ✅ | Usa `lock()` per concorrenza |
| Auto-cleanup | ✅ | Rimuove automaticamente i log più vecchi |
| Retention | In-memory | I log vengono persi al riavvio |

---

## Endpoint Summary

| Agente | Endpoint | Descrizione | Status |
|--------|----------|-------------|--------|
| **Orchestrator** | `GET /logs` | Attività routing e dispatch | ✅ |
| **Worker01** | `GET /logs` | Attività esecuzione task | ✅ |
| **Worker02** | `GET /logs` | Attività esecuzione task | ✅ |
| **IndigoAiWorker01** | `GET /logs` | Attività AI task e PromptOptimizer | ✅ |
| **Monitor** | `GET /cluster/status` | Stato cluster (include IndigoAiWorker01) | ✅ |

---

## Prossimi Passi

### D) CONTROL CENTER UI - FEEDBACK LIVE

**Modifiche necessarie:**

1. **AgentDetailsPage.xaml** - Aggiungere sezioni:
   ```xml
   <!-- Feedback Task Inviato -->
   <Border Background="LightBlue" Visibility="{Binding IsTaskDispatched}">
       <TextBlock Text="Task inviato all'Orchestrator ✓" />
   </Border>

   <!-- Worker Selezionato -->
   <Border Background="LightGreen" Visibility="{Binding IsWorkerSelected}">
       <StackPanel>
           <TextBlock Text="{Binding SelectedWorkerType}" />
           <TextBlock Text="{Binding SelectedWorkerUrl}" />
       </StackPanel>
   </Border>

   <!-- File Generati -->
   <Border Background="LightYellow" Visibility="{Binding HasCursorFile}">
       <StackPanel>
           <TextBlock Text="File generato:" />
           <TextBlock Text="{Binding CursorFilePath}" />
           <Button Content="Apri cartella" Command="{Binding OpenFolderCommand}" />
       </StackPanel>
   </Border>

   <!-- Attività Recenti -->
   <Button Content="Mostra attività recenti" Command="{Binding ShowLogsCommand}" />
   <ItemsControl ItemsSource="{Binding Logs}">
       <ItemsControl.ItemTemplate>
           <DataTemplate>
               <StackPanel Orientation="Horizontal">
                   <TextBlock Text="{Binding Timestamp, StringFormat='HH:mm:ss'}" />
                   <TextBlock Text="{Binding Level}" Foreground="{Binding LevelColor}" />
                   <TextBlock Text="{Binding Message}" />
               </StackPanel>
           </DataTemplate>
       </ItemsControl.ItemTemplate>
   </ItemsControl>
   ```

2. **AgentDetailsViewModel.cs** - Aggiungere proprietà:
   ```csharp
   [ObservableProperty]
   private bool _isTaskDispatched;

   [ObservableProperty]
   private bool _isWorkerSelected;

   [ObservableProperty]
   private string _selectedWorkerType = "";

   [ObservableProperty]
   private string _selectedWorkerUrl = "";

   [ObservableProperty]
   private bool _hasCursorFile;

   [ObservableProperty]
   private string _cursorFilePath = "";

   public ObservableCollection<LogEntry> Logs { get; } = new();

   [RelayCommand]
   private async Task ShowLogs()
   {
       var logs = await _agentService.GetLogsAsync(SelectedAgent.Url);
       Logs.Clear();
       foreach (var log in logs)
       {
           Logs.Add(log);
       }
   }

   [RelayCommand]
   private void OpenFolder()
   {
       var folder = Path.GetDirectoryName(CursorFilePath);
       Process.Start("explorer.exe", folder);
   }
   ```

3. **DashboardViewModel.cs** - Modifiche per IndigoAiWorker01:
   ```csharp
   private async Task RefreshClusterAsync()
   {
       // ... existing code ...
       
       foreach (var agentInfo in clusterStatus.Agents)
       {
           var agent = new AgentStatusItem
           {
               Name = agentInfo.Name,
               Url = agentInfo.Url,
               // ... existing properties ...
               
               // NEW: Icona diversa per AI Worker
               Icon = agentInfo.Name == "indigoaiworker01" 
                   ? "🧠" 
                   : "⚙️"
           };
           Agents.Add(agent);
       }
   }
   ```

---

## Benefici

1. ✅ **Monitoraggio Completo**: Tutti gli agenti inclusi nel Monitor
2. ✅ **Tracciabilità**: Ogni evento viene loggato con timestamp
3. ✅ **Debugging Facilitato**: Logs consultabili via API
4. ✅ **Thread-Safe**: LogBuffer gestisce concorrenza
5. ✅ **Performance**: Buffer in-memory, veloce
6. ✅ **Scalabilità**: Facile aggiungere nuovi agenti
7. ✅ **Standardizzazione**: Stesso formato log per tutti

---

## Troubleshooting

### Problema: Logs vuoti

**Causa**: Nessun task eseguito dopo l'avvio  
**Soluzione**: Esegui un task e poi richiama `/logs`

### Problema: Monitor non vede IndigoAiWorker01

**Causa**: Monitor non riavviato dopo modifica  
**Soluzione**: Riavvia Agent.Monitor

### Problema: Logs non persistiti

**Causa**: Buffer in-memory, non su disco  
**Soluzione**: I logs vengono persi al riavvio (comportamento intenzionale)

---

## Conclusione

Il cluster IndigoLab è ora completo con:
- ✅ IndigoAiWorker01 monitorato
- ✅ Logging completo su tutti gli agenti
- ✅ Endpoint /logs per attività recenti
- ✅ Tracciabilità end-to-end

**Prossimo step**: Implementare UI feedback live nel Control Center!

---

**Cluster IndigoLab** - Logging & Monitoring v1.0
