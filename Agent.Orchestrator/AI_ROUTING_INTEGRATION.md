# AI Routing Integration - Agent.Orchestrator

Documentazione dell'integrazione di routing intelligente per task AI in Agent.Orchestrator.

## Modifiche Implementate

### Agent.Orchestrator/Program.cs

#### 1. Configurazione Workers (righe 50-102)

**Prima:**
```csharp
var workers = new[] { "http://localhost:5002", "http://localhost:5003" };
```

**Dopo:**
```csharp
// Workers standard (task generici)
var workers = new[] { "http://localhost:5002", "http://localhost:5003" };

// Workers AI (task AI)
var aiWorkers = new[] { "http://localhost:5005" };
```

#### 2. Metodo IsAiTask() (nuovo)

```csharp
// Verifica se un task è di tipo AI
bool IsAiTask(string task)
{
    var aiTaskTypes = new[]
    {
        "generate-code",
        "refactor-code",
        "explain-code",
        "create-component",
        "fix-snippet",
        "cursor-prompt"
    };
    
    return aiTaskTypes.Contains(task.ToLowerInvariant());
}
```

#### 3. Metodo GetWorkerForTask() (nuovo)

```csharp
// Routing intelligente: task AI → IndigoAiWorker01, task standard → Worker01/02
string GetWorkerForTask(string task)
{
    if (IsAiTask(task))
    {
        logger.LogInformation("Task AI riconosciuto: {Task} → IndigoAiWorker01", task);
        return aiWorkers[0];
    }
    else
    {
        var workerUrl = GetNextWorkerUrl();
        logger.LogInformation("Task standard: {Task} → Worker operativo (round-robin)", task);
        return workerUrl;
    }
}
```

#### 4. Endpoint POST /dispatch (modificato)

**Prima:**
```csharp
var workerUrl = GetNextWorkerUrl();
log.LogInformation("Task inoltrato a worker: {WorkerUrl}", workerUrl);
```

**Dopo:**
```csharp
// Routing intelligente
var workerUrl = GetWorkerForTask(request.Task);
var workerType = IsAiTask(request.Task) ? "AI-Worker" : "Standard-Worker";
log.LogInformation("Task inoltrato a {WorkerType}: {WorkerUrl}", workerType, workerUrl);
```

#### 5. Risposta JSON (arricchita)

**Prima:**
```json
{
  "Success": true,
  "Message": "Task dispatched to worker",
  "Worker": "http://localhost:5005",
  "WorkerResult": {...}
}
```

**Dopo:**
```json
{
  "Success": true,
  "Message": "Task dispatched to AI-Worker",
  "Worker": "http://localhost:5005",
  "WorkerType": "AI-Worker",
  "IsAiTask": true,
  "WorkerResult": {...}
}
```

---

## Architettura Routing

```
POST /dispatch
   ↓
GetWorkerForTask(task)
   ↓
IsAiTask(task)?
   ├─ YES → aiWorkers[0] (IndigoAiWorker01 - 5005)
   └─ NO  → GetNextWorkerUrl() (Worker01/02 - round-robin)
```

### Task AI (→ IndigoAiWorker01)
- `generate-code`
- `refactor-code`
- `explain-code`
- `create-component`
- `fix-snippet`
- `cursor-prompt`

### Task Standard (→ Worker01/02)
- Tutti gli altri task (round-robin)

---

## Test Eseguiti

### Test 1: Task AI (generate-code)

**Request:**
```bash
curl -X POST http://localhost:5001/dispatch \
  -H "Content-Type: application/json" \
  -d '{"Task":"generate-code","Payload":"Crea classe Invoice"}'
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
    "Result": "// Codice generato da IndigoAiWorker01...",
    "ExecutedTask": "generate-code",
    "Timestamp": "2026-01-01T11:23:45.150Z"
  }
}
```

✅ **Risultato**: Task AI inoltrato a IndigoAiWorker01 (porta 5005)

---

### Test 2: Task Standard (normal-task)

**Request:**
```bash
curl -X POST http://localhost:5001/dispatch \
  -H "Content-Type: application/json" \
  -d '{"Task":"normal-task","Payload":"Task standard"}'
```

**Response:**
```json
{
  "Success": true,
  "Message": "Task dispatched to Standard-Worker",
  "Worker": "http://localhost:5002",
  "WorkerType": "Standard-Worker",
  "IsAiTask": false,
  "WorkerResult": {
    "Success": true,
    "Message": "Task executed",
    "Result": "Task 'normal-task' completato con successo...",
    "ExecutedTask": "normal-task",
    "Timestamp": "2026-01-01T11:23:46.127Z"
  }
}
```

✅ **Risultato**: Task standard inoltrato a Worker01 (porta 5002) tramite round-robin

---

### Test 3: Task AI (refactor-code)

**Request:**
```bash
curl -X POST http://localhost:5001/dispatch \
  -H "Content-Type: application/json" \
  -d '{"Task":"refactor-code","Payload":"Refactor snippet"}'
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
    "Result": "// Codice refactorato da IndigoAiWorker01...",
    "ExecutedTask": "refactor-code",
    "Timestamp": "2026-01-01T11:23:47.050Z"
  }
}
```

✅ **Risultato**: Task AI inoltrato a IndigoAiWorker01 (porta 5005)

---

### Test 4: Task AI (cursor-prompt)

**Request:**
```bash
curl -X POST http://localhost:5001/dispatch \
  -H "Content-Type: application/json" \
  -d '{"Task":"cursor-prompt","Payload":"Crea pagina WPF"}'
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
    "Result": "Prompt scritto per Cursor: C:\\...\\cursor-prompt-2026-01-01-112347.md",
    "ExecutedTask": "cursor-prompt",
    "Timestamp": "2026-01-01T11:23:47.780Z"
  }
}
```

✅ **Risultato**: Task AI inoltrato a IndigoAiWorker01 e file scritto per Cursor

---

## Log Orchestrator

```
info: Program[0]
      Load balancing configurato:
info: Program[0]
        - Workers standard: 2
info: Program[0]
          • http://localhost:5002
info: Program[0]
          • http://localhost:5003
info: Program[0]
        - Workers AI: 1
info: Program[0]
          • http://localhost:5005 (AI-Powered)
```

**Durante dispatch:**
```
info: Program[0]
      Dispatch ricevuto: Task='generate-code', Payload length=19
info: Program[0]
      Task AI riconosciuto: generate-code → IndigoAiWorker01
info: Program[0]
      Task inoltrato a AI-Worker: http://localhost:5005
info: Program[0]
      AI-Worker http://localhost:5005 ha completato il task con successo
```

---

## Vantaggi

1. ✅ **Routing Automatico**: Nessuna configurazione manuale per ogni task
2. ✅ **Scalabilità**: Facile aggiungere più AI workers
3. ✅ **Backwards Compatible**: Task esistenti continuano a funzionare
4. ✅ **Trasparenza**: WorkerType e IsAiTask nella risposta
5. ✅ **Logging Dettagliato**: Trace completo del routing
6. ✅ **Mantenibilità**: Lista task AI centralizzata

---

## Cluster Completo

| Porta | Agente | Tipo | Routing |
|-------|--------|------|---------|
| 5001 | Orchestrator | Dispatcher | - |
| 5002 | Worker01 | Standard | Round-robin |
| 5003 | Worker02 | Standard | Round-robin |
| **5005** | **IndigoAiWorker01** | **AI** | **Task AI** |

---

## Utilizzo da Control Center UI

### Dispatch Task AI

1. Apri Control Center UI
2. Vai su **Agents** → Seleziona **agent-orchestrator**
3. Nella sezione **Dispatch Task**:
   - **Task Name**: `generate-code`
   - **Payload**: `Crea classe Product con Id, Name, Price`
4. Clicca **Dispatch Task**

**Flusso:**
```
UI → Orchestrator (5001)
   → Riconosce "generate-code" come AI task
   → Inoltra a IndigoAiWorker01 (5005)
   → AiEngine elabora
   → CursorBridge scrive file
   → Risposta aggregata a UI
```

---

## Future Enhancements

### 1. Load Balancing AI Workers

```csharp
// Multipli AI workers con round-robin
var aiWorkers = new[] { 
    "http://localhost:5005",  // IndigoAiWorker01
    "http://localhost:5006",  // IndigoAiWorker02
    "http://localhost:5007"   // IndigoAiWorker03
};

string GetNextAiWorkerUrl()
{
    lock (aiWorkerLock)
    {
        var url = aiWorkers[aiWorkerIndex];
        aiWorkerIndex = (aiWorkerIndex + 1) % aiWorkers.Length;
        return url;
    }
}
```

### 2. Task Priority Queue

```csharp
public enum TaskPriority { Low, Normal, High, Critical }

string GetWorkerForTask(string task, TaskPriority priority)
{
    if (IsAiTask(task))
    {
        // High priority → dedicated AI worker
        if (priority >= TaskPriority.High)
            return aiWorkersHigh[0];
        else
            return GetNextAiWorkerUrl();
    }
    // ...
}
```

### 3. Worker Health Check

```csharp
async Task<bool> IsWorkerHealthy(string workerUrl)
{
    try
    {
        var response = await httpClient.GetAsync($"{workerUrl}/health");
        return response.IsSuccessStatusCode;
    }
    catch { return false; }
}

string GetWorkerForTask(string task)
{
    var worker = DetermineWorker(task);
    
    if (!await IsWorkerHealthy(worker))
    {
        logger.LogWarning("Worker {Worker} non disponibile, fallback", worker);
        return GetFallbackWorker(task);
    }
    
    return worker;
}
```

---

## Troubleshooting

### Problema: Task AI va a Worker standard

**Causa**: Nome task non riconosciuto o case-sensitive  
**Soluzione**: Verifica che il nome task sia esattamente uno di:
- `generate-code`
- `refactor-code`
- `explain-code`
- `create-component`
- `fix-snippet`
- `cursor-prompt`

**Fix**: Il metodo `IsAiTask()` usa `.ToLowerInvariant()`, quindi è case-insensitive.

### Problema: IndigoAiWorker01 non raggiungibile

**Causa**: Worker AI non avviato  
**Soluzione**:
```bash
cd IndigoAiWorker01
dotnet run
# Verifica: curl http://localhost:5005/health
```

### Problema: Orchestrator non riconosce nuovi task AI

**Causa**: Lista task AI non aggiornata  
**Soluzione**: Aggiungi il nuovo task in `IsAiTask()`:
```csharp
var aiTaskTypes = new[]
{
    "generate-code",
    // ... altri task ...
    "new-ai-task"  // ← aggiungi qui
};
```

---

## Conclusione

L'integrazione di routing intelligente nell'Orchestrator permette di distinguere automaticamente tra task AI e task standard, inoltrandoli ai worker appropriati senza configurazione manuale. Questo approccio garantisce scalabilità, mantenibilità e trasparenza nel cluster IndigoLab.

---

**Agent.Orchestrator** - AI Routing Integration v1.0
