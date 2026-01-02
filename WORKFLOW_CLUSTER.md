# 🔄 WORKFLOW CLUSTER - IndigoLab

**Flusso dettagliato delle operazioni del cluster IndigoLab**

Versione: **2.3.0**  
Ultimo aggiornamento: **2026-01-02**

---

## 🎬 SCENARIO 1: Avvio Control Center

### **Step-by-Step**

```
1. UTENTE ESEGUE COMANDO
   ┌────────────────────────────────────────────┐
   │ Terminal:                                   │
   │ > cd ControlCenter.UI                      │
   │ > dotnet run                               │
   └────────────────────────────────────────────┘
                    ↓
2. APP.XAML.CS - STARTUP
   ┌────────────────────────────────────────────┐
   │ protected override void OnStartup()        │
   │ {                                           │
   │   // 1. Registra servizi                   │
   │   LogService = new LogService();           │
   │   ClusterProcessManager = new(...);        │
   │                                             │
   │   // 2. Avvia finestra principale          │
   │   var window = new NaturalLanguageWindow(); │
   │   window.Show();                            │
   │                                             │
   │   // 3. Avvia cluster (ritardo 1s)         │
   │   Task.Run(async () => {                   │
   │     await Task.Delay(1000);                │
   │     await ClusterProcessManager            │
   │       .StartAllAgents();                   │
   │   });                                       │
   │ }                                           │
   └────────────────────────────────────────────┘
                    ↓
3. CLUSTERPRO

CESSMANAGER - START AGENTS
   ┌────────────────────────────────────────────┐
   │ Per ogni agente (Orchestrator, Workers):   │
   │                                             │
   │ 1. Crea ProcessStartInfo                   │
   │    FileName = "dotnet"                     │
   │    Arguments = "run --project {path}"      │
   │    UseShellExecute = false                 │
   │    CreateNoWindow = true                   │
   │    RedirectStandardOutput = true           │
   │    RedirectStandardError = true            │
   │                                             │
   │ 2. Hook event handlers                     │
   │    process.OutputDataReceived +=           │
   │      (s, e) => LogService.AppendLog(...)   │
   │    process.ErrorDataReceived +=            │
   │      (s, e) => LogService.AppendLog(...)   │
   │    process.Exited +=                       │
   │      (s, e) => SetStatus(Crashed)          │
   │                                             │
   │ 3. Avvia processo                          │
   │    process.Start()                         │
   │    process.BeginOutputReadLine()           │
   │    process.BeginErrorReadLine()            │
   │                                             │
   │ 4. Avvia watchdog timer (5s)               │
   │    Se nessun output → log warning          │
   └────────────────────────────────────────────┘
                    ↓
4. AGENTI IN BACKGROUND
   ┌────────────────────────────────────────────┐
   │ [Orchestrator - porta 5001]                │
   │ === Agent.Orchestrator avviato ===         │
   │ Porta: 5001                                │
   │ Versione: 2.2.0                            │
   │ Intelligent AI routing configurato         │
   │ In ascolto su http://localhost:5001        │
   │                                             │
   │ [IndigoAiWorker01 - porta 5005]           │
   │ === IndigoAiWorker01 avviato ===          │
   │ Porta: 5005                                │
   │ Tipo: AI-Powered Worker                    │
   │ CursorBridge pronto                        │
   │ In ascolto su http://localhost:5005        │
   └────────────────────────────────────────────┘
                    ↓
5. UI PRONTA
   ┌────────────────────────────────────────────┐
   │ Natural Language Console                    │
   │ ┌────────────────────────────────────────┐ │
   │ │ Cosa vuoi che faccia il cluster?       │ │
   │ │ [input box]                            │ │
   │ │ [🚀 Esegui]                            │ │
   │ └────────────────────────────────────────┘ │
   │                                             │
   │ ⚙️ Stato Orchestrator                      │
   │ Stato: ✅ Online su porta 5001            │
   │ Risposta: 12ms                             │
   │                                             │
   │ 📊 Timeline Operativa                      │
   │ (vuota - in attesa richieste)              │
   └────────────────────────────────────────────┘
```

**Tempo totale avvio**: ~3-5 secondi

---

## 💬 SCENARIO 2: Richiesta "Crea una soluzione per gestire palette di colori"

### **Fase 1: Input Utente**

```
UTENTE SCRIVE:
┌────────────────────────────────────────────────────────┐
│ Cosa vuoi che faccia il cluster?                       │
│ ┌────────────────────────────────────────────────────┐ │
│ │ crea una soluzione per gestire palette di colori   │ │
│ │ di una applicazione                                │ │
│ │                                                     │ │
│ └────────────────────────────────────────────────────┘ │
│                                                         │
│ [🚀 Esegui] ← CLICK                                   │
└────────────────────────────────────────────────────────┘
```

**Timeline aggiorna:**
```
09:14:28  📝 Input ricevuto
          Comando: crea una soluzione per gestire...
```

---

### **Fase 2: Dispatch a Orchestrator**

```csharp
// NaturalLanguageViewModel.cs
await BootstrapperClient.DispatchTaskAsync(
    agentName: "Orchestrator",
    task: "cursor-prompt",
    payload: userInput,
    targetPath: ConfigService.DefaultSolutionPath // "C:/Users/.../INBOX"
);
```

**HTTP Request:**
```http
POST http://localhost:5001/dispatch
Content-Type: application/json

{
  "Task": "cursor-prompt",
  "Payload": {
    "userRequest": "crea una soluzione per gestire palette di colori...",
    "targetPath": "C:/Users/filip/OneDrive/00_INBOX"
  }
}
```

**Timeline aggiorna:**
```
09:14:29  🎯 Invio a Orchestrator
          Instradamento verso cluster IndigoLab
```

**Log (System):**
```
[09:14:29.123] [INFO] Dispatch task 'cursor-prompt' to Orchestrator
[09:14:29.125] [INFO] Target path: C:/Users/filip/OneDrive/00_INBOX
```

---

### **Fase 3: Intelligent AI Routing**

```csharp
// Orchestrator/Program.cs - POST /dispatch

// 1. Analisi task
bool isAiTask = IsAiTask("cursor-prompt", payload);

// Verifica:
// ✓ Task name contiene "ai"? → NO
// ✓ Payload con verbi creativi ("crea")? → SÌ
// ✓ Linguaggio naturale? → SÌ
// → CLASSIFICATO COME AI TASK

// 2. Routing
string workerUrl = "http://localhost:5005"; // IndigoAiWorker01

// 3. Dispatch
await httpClient.PostAsJsonAsync(
    $"{workerUrl}/execute",
    new { Task = "cursor-prompt", Payload = request.Payload }
);
```

**Timeline aggiorna:**
```
09:14:30  ⚡ Analisi linguaggio naturale
          Classificazione automatica come AI Task
```

**Log (Orchestrator):**
```
[09:14:30.001] [INFO] === AI ROUTING ATTIVATO ===
[09:14:30.002] [INFO] Analisi task: 'cursor-prompt'
[09:14:30.003] [INFO] ✓ Payload contiene verbi creativi
[09:14:30.004] [INFO] ✓ Linguaggio naturale rilevato
[09:14:30.005] [INFO] >>> Task classificato come AI
[09:14:30.006] [INFO] >>> Instradato a Worker AI (IndigoAiWorker01)
```

---

### **Fase 4: Worker AI - Genera PREVIEW**

```csharp
// IndigoAiWorker01/Program.cs - POST /execute

// 1. Parse payload
string userRequest = payloadObj.GetProperty("userRequest").GetString();
string targetPath = payloadObj.GetProperty("targetPath").GetString();

// 2. Determina nome soluzione
string solutionName = "ColorPaletteManager"; // Estratto da userRequest

// 3. ⚠️ PROTEZIONE: Verifica se cartella esiste
string fullPath = Path.Combine(targetPath, solutionName);
if (Directory.Exists(fullPath))
{
    // Suggerisci nome alternativo
    int counter = 1;
    string altName = $"{solutionName}_{counter}";
    while (Directory.Exists(Path.Combine(targetPath, altName)))
    {
        counter++;
        altName = $"{solutionName}_{counter}";
    }
    
    return Results.Ok(new {
        Success = true,
        Status = "folder-exists",
        ExistingPath = fullPath,
        SuggestedAlternativeName = altName
    });
}

// 4. Genera PREVIEW (file .md)
string previewMd = GenerateSolutionPreview(userRequest, targetPath);

// 5. Scrivi file PREVIEW
string previewPath = Path.Combine(targetPath, $"{solutionName}_PREVIEW.md");
File.WriteAllText(previewPath, previewMd);

// 6. Ritorna successo
return Results.Ok(new {
    Success = true,
    Status = "preview-generated",
    PreviewPath = previewPath
});
```

**Timeline aggiorna:**
```
09:14:31  🔨 Generazione anteprima
          Preparazione preview delle modifiche da applicare
09:14:32  📄 Percorso selezionato
          Percorso: C:/Users/filip/OneDrive/00_INBOX
09:14:33  💾 Percorso salvato in configurazione
09:14:34  🔍 Anteprima generata
          Preview delle modifiche pronta
```

**Log (AI Worker):**
```
[09:14:31.100] [INFO] Task AI ricevuto: Task='cursor-prompt'
[09:14:31.105] [INFO] [06:14:31.105] Esecuzione: CreateNewSolution - PREVIEW MODE
[09:14:31.110] [INFO] [06:14:31.110] UserRequest: 'crea una soluzione...'
[09:14:31.115] [INFO] [06:14:31.115] TargetPath: 'C:/Users/filip/OneDrive/00_INBOX'
[09:14:32.200] [INFO] [06:14:32.200] PREVIEW generata: ColorPaletteManager_PREVIEW.md
[09:14:32.205] [INFO] [06:14:32.205] Preview salvata in: C:/.../INBOX/ColorPaletteManager_PREVIEW.md
```

**File generato:**
```markdown
# 📋 PREVIEW - Soluzione ColorPaletteManager

## 📁 Struttura Prevista

```
ColorPaletteManager/
  ColorPaletteManager.sln
  ColorPaletteManager/
    ColorPaletteManager.csproj
    Program.cs
    Models/
      ColorPalette.cs
      Color.cs
    Services/
      PaletteManager.cs
```

## 📄 File che verranno creati

- `ColorPaletteManager.sln` - File soluzione
- `ColorPaletteManager.csproj` - Progetto console .NET 8
- `Program.cs` - Entry point
- `Models/ColorPalette.cs` - Modello palette
- `Models/Color.cs` - Modello colore
- `Services/PaletteManager.cs` - Logica gestione palette

⏸️ **In attesa di conferma utente**
```

---

### **Fase 5: UI Mostra Preview - Conferma Richiesta**

```
MODALE PREVIEW DIALOG
┌────────────────────────────────────────────────────────┐
│ 🔍 Anteprima modifiche                                 │
├────────────────────────────────────────────────────────┤
│                                                         │
│ 📁 File che verranno creati:                           │
│ - ColorPaletteManager.sln                              │
│ - ColorPaletteManager/ColorPaletteManager.csproj       │
│ - ColorPaletteManager/Program.cs                       │
│ - ColorPaletteManager/Models/ColorPalette.cs           │
│ - ColorPaletteManager/Models/Color.cs                  │
│ - ColorPaletteManager/Services/PaletteManager.cs       │
│                                                         │
│ 🗂️ Cartelle che verranno create:                       │
│ - ColorPaletteManager/                                 │
│ - ColorPaletteManager/Models/                          │
│ - ColorPaletteManager/Services/                        │
│                                                         │
│ 🧱 Struttura finale prevista:                          │
│ ├─ ColorPaletteManager.sln                            │
│ └─ ColorPaletteManager/                               │
│    ├─ ColorPaletteManager.csproj                      │
│    ├─ Program.cs                                       │
│    ├─ Models/                                          │
│    │  ├─ ColorPalette.cs                              │
│    │  └─ Color.cs                                      │
│    └─ Services/                                        │
│       └─ PaletteManager.cs                            │
│                                                         │
│ [Procedi] [Annulla] [Mostra dettagli tecnici]         │
└────────────────────────────────────────────────────────┘
```

**Timeline aggiorna:**
```
09:14:35  ⏸️ Conferma PREVIEW richiesta
          Verifica le modifiche prima di procedere
```

**UTENTE CLICK [Procedi]**

---

### **Fase 6: Conferma → Execute Solution Creation**

```csharp
// NaturalLanguageViewModel.cs - HandlePreviewConfirmationAsync()

await BootstrapperClient.DispatchTaskAsync(
    agentName: "Orchestrator",
    task: "execute-solution-creation",
    payload: userRequest,
    targetPath: targetPath,
    forceOverwrite: false  // ⚠️ Protezione attiva
);
```

**HTTP Request:**
```http
POST http://localhost:5001/dispatch
Content-Type: application/json

{
  "Task": "execute-solution-creation",
  "Payload": {
    "userRequest": "crea una soluzione per gestire palette di colori...",
    "targetPath": "C:/Users/filip/OneDrive/00_INBOX",
    "forceOverwrite": false
  }
}
```

**Timeline aggiorna:**
```
09:14:36  ▶️ Esecuzione confermata
          Inizio creazione fisica dei file
```

---

### **Fase 7: Worker AI - Creazione REALE**

```csharp
// IndigoAiWorker01/Program.cs - POST /execute (execute-solution-creation)

// 1. Parse payload
string userRequest = payloadObj.GetProperty("userRequest").GetString();
string targetPath = payloadObj.GetProperty("targetPath").GetString();
bool forceOverwrite = payloadObj.GetProperty("forceOverwrite").GetBoolean();

// 2. Determina nome soluzione
string solutionName = "ColorPaletteManager";
string fullPath = Path.Combine(targetPath, solutionName);

// 3. ⚠️ PROTEZIONE: Verifica sovrascrittura
if (Directory.Exists(fullPath) && !forceOverwrite)
{
    // BLOCCA creazione
    return Results.Ok(new {
        Success = false,
        Status = "blocked",
        Reason = "folder-exists-no-confirmation",
        ExistingPath = fullPath
    });
}

// 4. Se forceOverwrite=true, elimina cartella esistente
if (Directory.Exists(fullPath) && forceOverwrite)
{
    Directory.Delete(fullPath, recursive: true);
}

// 5. Crea struttura soluzione
Directory.CreateDirectory(fullPath);
Directory.CreateDirectory(Path.Combine(fullPath, "ColorPaletteManager"));
Directory.CreateDirectory(Path.Combine(fullPath, "ColorPaletteManager", "Models"));
Directory.CreateDirectory(Path.Combine(fullPath, "ColorPaletteManager", "Services"));

// 6. Crea file
File.WriteAllText(
    Path.Combine(fullPath, "ColorPaletteManager.sln"),
    GenerateSolutionFileContent()
);
File.WriteAllText(
    Path.Combine(fullPath, "ColorPaletteManager", "ColorPaletteManager.csproj"),
    GenerateCsprojContent()
);
File.WriteAllText(
    Path.Combine(fullPath, "ColorPaletteManager", "Program.cs"),
    GenerateProgramCsContent()
);
// ... altri file

// 7. Ritorna successo
return Results.Ok(new {
    Success = true,
    Message = "Soluzione creata con successo",
    SolutionPath = fullPath
});
```

**Timeline aggiorna:**
```
09:14:37  🚀 Creazione in corso
          Scrittura file e cartelle sul disco
09:14:38  ✅ Operazione completata
          Soluzione creata in C:/.../INBOX/ColorPaletteManager
```

**Log (AI Worker):**
```
[09:14:37.001] [INFO] [06:14:37.001] Esecuzione: ExecuteSolutionCreation - REAL MODE
[09:14:37.005] [INFO] [06:14:37.005] UserRequest: 'crea una soluzione...'
[09:14:37.010] [INFO] [06:14:37.010] TargetPath: 'C:/Users/filip/OneDrive/00_INBOX'
[09:14:37.015] [INFO] [06:14:37.015] ForceOverwrite: false
[09:14:37.050] [INFO] [06:14:37.050] ✓ Cartella creata: ColorPaletteManager
[09:14:37.100] [INFO] [06:14:37.100] ✓ File creato: ColorPaletteManager.sln
[09:14:37.150] [INFO] [06:14:37.150] ✓ File creato: Program.cs
[09:14:37.200] [INFO] [06:14:37.200] ✓ File creato: Models/ColorPalette.cs
[09:14:37.250] [INFO] [06:14:37.250] ✓ File creato: Models/Color.cs
[09:14:37.300] [INFO] [06:14:37.300] ✓ File creato: Services/PaletteManager.cs
[09:14:38.000] [INFO] [06:14:38.000] ✅ Soluzione completata: C:/.../INBOX/ColorPaletteManager
```

---

### **Fase 8: Risultato Finale**

```
FINESTRA PRINCIPALE
┌────────────────────────────────────────────────────────┐
│ 📊 Timeline Operativa                                  │
├────────────────────────────────────────────────────────┤
│                                                         │
│ 09:14:28  📝 Input ricevuto                            │
│ 09:14:29  🎯 Invio a Orchestrator                      │
│ 09:14:30  ⚡ Analisi linguaggio naturale               │
│ 09:14:31  🔨 Generazione anteprima                     │
│ 09:14:34  🔍 Anteprima generata                        │
│ 09:14:35  ⏸️ Conferma PREVIEW richiesta               │
│ 09:14:36  ▶️ Esecuzione confermata                     │
│ 09:14:37  🚀 Creazione in corso                        │
│ 09:14:38  ✅ Operazione completata                     │
│           Soluzione creata in                          │
│           C:/.../INBOX/ColorPaletteManager             │
│                                                         │
│ [🗑️ Pulisci Timeline]                                 │
└────────────────────────────────────────────────────────┘
```

**MESSAGGIO SUCCESS:**
```
⚡ Operazione completata con successo!
Soluzione creata in:
C:/Users/filip/OneDrive/00_INBOX/ColorPaletteManager
```

**Tempo totale**: ~10 secondi  
**File creati**: 6  
**Cartelle create**: 3

---

## 📊 TASK AI PRINCIPALI

### **1. cursor-prompt**
**Tipo**: AI Task  
**Descrizione**: Riceve input in linguaggio naturale e genera una prima analisi

**Input:**
```json
{
  "Task": "cursor-prompt",
  "Payload": {
    "userRequest": "crea una dashboard WPF...",
    "targetPath": "C:/Projects"
  }
}
```

**Output:**
```json
{
  "Success": true,
  "OptimizedPrompt": "Creazione dashboard WPF con...",
  "CursorFilePath": ".cursor/ai-requests/cursor-prompt-20260102-143052.md",
  "CursorFileWritten": true
}
```

---

### **2. create-new-solution (PREVIEW MODE)**
**Tipo**: AI Task  
**Descrizione**: Genera preview della soluzione senza creare file reali

**Input:**
```json
{
  "Task": "create-new-solution",
  "Payload": {
    "userRequest": "crea una soluzione...",
    "targetPath": "C:/Projects"
  }
}
```

**Output (Normale):**
```json
{
  "Success": true,
  "Status": "preview-generated",
  "PreviewPath": "C:/Projects/MySolution_PREVIEW.md"
}
```

**Output (Cartella esiste):**
```json
{
  "Success": true,
  "Status": "folder-exists",
  "Message": "La cartella di destinazione esiste già",
  "ExistingPath": "C:/Projects/MySolution",
  "SuggestedAlternativeName": "MySolution_1",
  "TargetPath": "C:/Projects"
}
```

---

### **3. execute-solution-creation (CREAZIONE REALE)**
**Tipo**: AI Task  
**Descrizione**: Crea realmente file e cartelle dopo conferma utente

**Input:**
```json
{
  "Task": "execute-solution-creation",
  "Payload": {
    "userRequest": "crea una soluzione...",
    "targetPath": "C:/Projects",
    "forceOverwrite": false
  }
}
```

**Output (Successo):**
```json
{
  "Success": true,
  "Message": "Soluzione creata con successo",
  "SolutionPath": "C:/Projects/MySolution"
}
```

**Output (Bloccato - cartella esiste):**
```json
{
  "Success": false,
  "Status": "blocked",
  "Reason": "folder-exists-no-confirmation",
  "ExistingPath": "C:/Projects/MySolution"
}
```

---

## 📝 LOGGING EVENTI

### **Log per Agente**

**System:**
```
[09:14:29.123] [INFO] Dispatch task 'cursor-prompt' to Orchestrator
[09:14:29.125] [INFO] Target path: C:/Users/filip/OneDrive/00_INBOX
[09:14:38.500] [INFO] Task completed successfully
```

**Orchestrator:**
```
[09:14:30.001] [INFO] === AI ROUTING ATTIVATO ===
[09:14:30.002] [INFO] Analisi task: 'cursor-prompt'
[09:14:30.005] [INFO] >>> Task classificato come AI
[09:14:30.006] [INFO] >>> Instradato a Worker AI (IndigoAiWorker01)
```

**AI Worker:**
```
[09:14:31.100] [INFO] Task AI ricevuto: Task='cursor-prompt'
[09:14:31.105] [INFO] [06:14:31.105] Esecuzione: CreateNewSolution - PREVIEW MODE
[09:14:32.200] [INFO] [06:14:32.200] PREVIEW generata: ColorPaletteManager_PREVIEW.md
[09:14:37.001] [INFO] [06:14:37.001] Esecuzione: ExecuteSolutionCreation - REAL MODE
[09:14:38.000] [INFO] [06:14:38.000] ✅ Soluzione completata
```

---

**Versione documento:** 2.3.0  
**Ultimo aggiornamento:** 2026-01-02  
**Autore:** IndigoLab Team
