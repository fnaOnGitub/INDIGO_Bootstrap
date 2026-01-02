# 🤖 WORKER AI - IndigoAiWorker01

**Documentazione completa del Worker AI - IndigoAiWorker01**

Versione: **2.3.0**  
Ultimo aggiornamento: **2026-01-02**  
Porta: **5005**

---

## 📊 OVERVIEW

**IndigoAiWorker01** è il worker specializzato per task AI del cluster IndigoLab. È responsabile di:

- ✅ Esecuzione task AI (linguaggio naturale, generazione codice)
- ✅ Generazione preview soluzioni (PREVIEW MODE)
- ✅ Creazione reale soluzioni (EXECUTE MODE)
- ✅ Protezione sovrascrittura cartelle esistenti
- ✅ Integrazione Cursor tramite CursorBridge
- ✅ FILE ALWAYS MODE (ogni task genera file .md)

---

## 🔧 TASK SUPPORTATI

### **1. cursor-prompt**
**Descrizione**: Riceve input in linguaggio naturale e genera analisi iniziale

**Payload:**
```json
{
  "userRequest": "crea una soluzione per gestire X",
  "targetPath": "C:/Users/filip/OneDrive/00_INBOX"
}
```

**Comportamento:**
1. Analizza `userRequest`
2. Genera file `.cursor/ai-requests/cursor-prompt-{timestamp}.md`
3. Restituisce prompt ottimizzato
4. Genera automaticamente task `create-new-solution`

**Output:**
```json
{
  "Success": true,
  "OptimizedPrompt": "...",
  "CursorFilePath": ".cursor/ai-requests/cursor-prompt-20260102-143052.md",
  "CursorFileWritten": true
}
```

---

### **2. create-new-solution (PREVIEW MODE)**
**Descrizione**: Genera preview della soluzione SENZA creare file reali

**Payload:**
```json
{
  "userRequest": "crea una soluzione per gestire X",
  "targetPath": "C:/Users/filip/OneDrive/00_INBOX"
}
```

**Comportamento:**

#### **A) Verifica Cartella Esistente**
```csharp
// 1. Determina nome soluzione
string solutionName = "MyNewSolution"; // Estratto da userRequest

// 2. Costruisce percorso completo
string fullTargetPath = Path.Combine(targetPath, solutionName);

// 3. ⚠️ PROTEZIONE: Verifica se esiste
if (Directory.Exists(fullTargetPath))
{
    // Suggerisci nome alternativo
    int counter = 1;
    string alternativeName = $"{solutionName}_{counter}";
    while (Directory.Exists(Path.Combine(targetPath, alternativeName)))
    {
        counter++;
        alternativeName = $"{solutionName}_{counter}";
    }
    
    // Restituisci status speciale "folder-exists"
    return Results.Ok(new
    {
        Success = true,
        Status = "folder-exists",
        Message = "La cartella di destinazione esiste già",
        ExistingPath = fullTargetPath,
        SuggestedAlternativeName = alternativeName,
        TargetPath = targetPath,
        UserRequest = userRequest,
        Timestamp = DateTime.UtcNow
    });
}
```

#### **B) Genera Preview**
```csharp
// 4. Se cartella NON esiste, genera preview
string previewMd = GenerateSolutionPreview(userRequest, targetPath);

// 5. Salva preview su disco
string previewPath = Path.Combine(targetPath, $"{solutionName}_PREVIEW.md");
File.WriteAllText(previewPath, previewMd);

// 6. Restituisci successo
return Results.Ok(new
{
    Success = true,
    Status = "preview-generated",
    PreviewPath = previewPath,
    SolutionName = solutionName
});
```

**Output (Normale):**
```json
{
  "Success": true,
  "Status": "preview-generated",
  "PreviewPath": "C:/.../INBOX/MyNewSolution_PREVIEW.md",
  "SolutionName": "MyNewSolution"
}
```

**Output (Cartella esiste):**
```json
{
  "Success": true,
  "Status": "folder-exists",
  "Message": "La cartella di destinazione esiste già",
  "ExistingPath": "C:/.../INBOX/MyNewSolution",
  "SuggestedAlternativeName": "MyNewSolution_1",
  "TargetPath": "C:/.../INBOX",
  "UserRequest": "crea una soluzione..."
}
```

---

### **3. execute-solution-creation (CREAZIONE REALE)**
**Descrizione**: Crea REALMENTE file e cartelle dopo conferma utente

**Payload:**
```json
{
  "userRequest": "crea una soluzione per gestire X",
  "targetPath": "C:/Users/filip/OneDrive/00_INBOX",
  "forceOverwrite": false
}
```

**Comportamento:**

#### **A) Parse Payload (camelCase!)**
```csharp
// ⚠️ ATTENZIONE: Il payload usa camelCase!
string userRequest = "";
string? targetPath = null;
bool forceOverwrite = false;

if (payloadObj.ValueKind == JsonValueKind.Object)
{
    // Usa "userRequest" (camelCase), NON "UserRequest"
    if (payloadObj.TryGetProperty("userRequest", out var ur))
        userRequest = ur.GetString() ?? "";
    
    // Usa "targetPath" (camelCase), NON "TargetPath"
    if (payloadObj.TryGetProperty("targetPath", out var tp))
        targetPath = tp.GetString();
    
    // Usa "forceOverwrite" (camelCase), NON "ForceOverwrite"
    if (payloadObj.TryGetProperty("forceOverwrite", out var fo))
        forceOverwrite = fo.GetBoolean();
}

// Log diagnostico
log.LogInformation("[DEBUG] Estratto userRequest='{UserRequest}'", userRequest);
log.LogInformation("[DEBUG] Estratto targetPath='{TargetPath}'", targetPath);
log.LogInformation("[DEBUG] Estratto forceOverwrite={ForceOverwrite}", forceOverwrite);
```

#### **B) Protezione Sovrascrittura**
```csharp
// 1. Verifica se cartella esiste
if (Directory.Exists(fullTargetPath) && !forceOverwrite)
{
    // BLOCCA creazione
    log.LogError("❌ CREAZIONE BLOCCATA: cartella esiste e forceOverwrite=false");
    
    return Results.Ok(new {
        Success = false,
        Status = "blocked",
        Reason = "folder-exists-no-confirmation",
        ExistingPath = fullTargetPath,
        Message = "La cartella esiste già. Serve conferma esplicita (forceOverwrite=true)."
    });
}

// 2. Se forceOverwrite=true E cartella esiste → ELIMINA
if (Directory.Exists(fullTargetPath) && forceOverwrite)
{
    log.LogWarning("⚠️ SOVRASCRITTURA CONFERMATA: Eliminazione cartella {Path}", fullTargetPath);
    
    try
    {
        Directory.Delete(fullTargetPath, recursive: true);
        log.LogInformation("✓ Cartella esistente eliminata");
    }
    catch (Exception ex)
    {
        log.LogError("❌ Errore durante eliminazione: {Error}", ex.Message);
        return Results.Ok(new { Success = false, Message = ex.Message });
    }
}
```

#### **C) Creazione Soluzione**
```csharp
// 3. Crea directory
Directory.CreateDirectory(fullTargetPath);
Directory.CreateDirectory(Path.Combine(fullTargetPath, solutionName));
Directory.CreateDirectory(Path.Combine(fullTargetPath, solutionName, "Models"));
// ...

// 4. Crea file
File.WriteAllText(Path.Combine(fullTargetPath, $"{solutionName}.sln"), solutionContent);
File.WriteAllText(Path.Combine(fullTargetPath, solutionName, "Program.cs"), programContent);
// ...

// 5. Log successo
log.LogInformation("✅ Soluzione completata: {Path}", fullTargetPath);

// 6. Restituisci risultato
return Results.Ok(new {
    Success = true,
    Message = "Soluzione creata con successo",
    SolutionPath = fullTargetPath,
    FilesCreated = filesCreated.Count,
    FoldersCreated = foldersCreated.Count
});
```

**Output (Successo):**
```json
{
  "Success": true,
  "Message": "Soluzione creata con successo",
  "SolutionPath": "C:/.../INBOX/MyNewSolution",
  "FilesCreated": 6,
  "FoldersCreated": 3
}
```

**Output (Bloccato):**
```json
{
  "Success": false,
  "Status": "blocked",
  "Reason": "folder-exists-no-confirmation",
  "ExistingPath": "C:/.../INBOX/MyNewSolution"
}
```

---

## 🔐 PAYLOAD PARSING

### **⚠️ ATTENZIONE: camelCase vs PascalCase**

Il Control Center invia payload in **camelCase**:
```json
{
  "userRequest": "...",
  "targetPath": "...",
  "forceOverwrite": false
}
```

Il Worker AI deve usare **camelCase** nel parsing:
```csharp
// ✅ CORRETTO
payloadObj.TryGetProperty("userRequest", out var ur)
payloadObj.TryGetProperty("targetPath", out var tp)
payloadObj.TryGetProperty("forceOverwrite", out var fo)

// ❌ SBAGLIATO (non funziona!)
payloadObj.TryGetProperty("UserRequest", out var ur)  // NON TROVA
payloadObj.TryGetProperty("TargetPath", out var tp)   // NON TROVA
```

### **Log Diagnostici Chiave**

```csharp
// Sempre loggare i valori estratti dal payload
log.LogInformation("[DEBUG] Estratto userRequest='{Value}'", userRequest);
log.LogInformation("[DEBUG] Estratto targetPath='{Value}'", targetPath);
log.LogInformation("[DEBUG] Estratto forceOverwrite={Value}", forceOverwrite);

// Se targetPath è vuoto, blocca subito
if (string.IsNullOrWhiteSpace(targetPath))
{
    log.LogError("❌ TargetPath mancante nel payload");
    throw new ArgumentException("TargetPath mancante nel payload...");
}
```

---

## 🗂️ CURSOR BRIDGE

### **Responsabilità**
CursorBridge è il modulo che gestisce l'integrazione con Cursor IDE.

**Metodi principali:**
```csharp
// 1. Inizializza directory
public static void EnsureCursorDirectory(string basePath)
{
    var cursorPath = Path.Combine(basePath, ".cursor", "ai-requests");
    Directory.CreateDirectory(cursorPath);
}

// 2. Scrive file request
public static string WriteAiRequest(string basePath, string content, string prefix = "cursor-prompt")
{
    var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
    var fileName = $"{prefix}-{timestamp}.md";
    var fullPath = Path.Combine(basePath, ".cursor", "ai-requests", fileName);
    
    File.WriteAllText(fullPath, content);
    
    return fullPath;
}
```

**Directory standard:**
```
ProjectRoot/
  .cursor/
    ai-requests/
      cursor-prompt-20260102-143052.md
      solution-preview-20260102-143118.md
      solution-created-20260102-143201.md
```

---

## 🛡️ GESTIONE CARTELLA ESISTENTE

### **Flusso Completo**

```
1. PREVIEW MODE
   ┌──────────────────────────────────────────┐
   │ Directory.Exists(fullPath)?              │
   │ ├─ SÌ → status="folder-exists"           │
   │ │       + SuggestedAlternativeName       │
   │ └─ NO → genera PREVIEW.md                │
   └──────────────────────────────────────────┘
                    ↓
2. UI MOSTRA OPZIONI
   ┌──────────────────────────────────────────┐
   │ ⚠️ Cartella già esistente                │
   │                                           │
   │ La cartella "MyNewSolution" esiste già.   │
   │ Come vuoi procedere?                      │
   │                                           │
   │ [🔥 Sovrascrivi]                          │
   │ [✏️ Usa nome diverso] → MyNewSolution_1  │
   │ [❌ Annulla]                              │
   └──────────────────────────────────────────┘
                    ↓
3. UTENTE SCEGLIE "Sovrascrivi"
   ┌──────────────────────────────────────────┐
   │ Re-dispatch con forceOverwrite=true       │
   └──────────────────────────────────────────┘
                    ↓
4. EXECUTE MODE
   ┌──────────────────────────────────────────┐
   │ Directory.Exists(fullPath)?              │
   │ ├─ SÌ E forceOverwrite=false → BLOCCA    │
   │ ├─ SÌ E forceOverwrite=true → ELIMINA    │
   │ └─ NO → CREA NORMALMENTE                 │
   └──────────────────────────────────────────┘
```

### **Codice Protezione**

**create-new-solution:**
```csharp
// ⚠️ PROTEZIONE: Verifica esistenza cartella
if (!string.IsNullOrWhiteSpace(targetPath))
{
    string solutionName = "MyNewSolution";
    string fullTargetPath = Path.Combine(targetPath, solutionName);
    
    if (Directory.Exists(fullTargetPath))
    {
        log.LogWarning("⚠️ CARTELLA GIÀ ESISTENTE: {Path}", fullTargetPath);
        
        // Suggerisci alternativa
        int counter = 1;
        string alternativeName = $"{solutionName}_{counter}";
        while (Directory.Exists(Path.Combine(targetPath, alternativeName)))
        {
            counter++;
            alternativeName = $"{solutionName}_{counter}";
        }
        
        log.LogInformation("Nome alternativo suggerito: {Alt}", alternativeName);
        
        // Restituisci status speciale
        return Results.Ok(new
        {
            Success = true,
            Status = "folder-exists",
            Message = "La cartella di destinazione esiste già",
            ExistingPath = fullTargetPath,
            SuggestedAlternativeName = alternativeName,
            TargetPath = targetPath,
            UserRequest = userRequest,
            Timestamp = DateTime.UtcNow
        });
    }
}

// Continua con generazione preview normale...
```

**execute-solution-creation:**
```csharp
// ⚠️ PROTEZIONE: Blocca se cartella esiste senza conferma
if (!string.IsNullOrWhiteSpace(execTargetPath))
{
    string solutionName = "MyNewSolution";
    string fullTargetPath = Path.Combine(execTargetPath, solutionName);
    
    // CASO 1: Cartella esiste MA forceOverwrite=false → BLOCCA
    if (Directory.Exists(fullTargetPath) && !forceOverwrite)
    {
        log.LogError("❌ CREAZIONE BLOCCATA: cartella esiste e forceOverwrite=false");
        
        return Results.Ok(new {
            Success = false,
            Status = "blocked",
            Reason = "folder-exists-no-confirmation",
            ExistingPath = fullTargetPath,
            Message = "La cartella esiste già e forceOverwrite=false"
        });
    }
    
    // CASO 2: Cartella esiste E forceOverwrite=true → ELIMINA
    if (Directory.Exists(fullTargetPath) && forceOverwrite)
    {
        log.LogWarning("⚠️ SOVRASCRITTURA CONFERMATA: Eliminazione {Path}", fullTargetPath);
        
        try
        {
            Directory.Delete(fullTargetPath, recursive: true);
            log.LogInformation("✓ Cartella esistente eliminata con successo");
        }
        catch (Exception ex)
        {
            log.LogError("❌ Errore durante eliminazione: {Error}", ex.Message);
            return Results.Ok(new {
                Success = false,
                Status = "delete-failed",
                Message = $"Impossibile eliminare cartella: {ex.Message}"
            });
        }
    }
}

// Continua con creazione normale...
```

---

## 📝 LOG DIAGNOSTICI

### **Livelli e Formati**

**Info:**
```
[09:14:31.100] [INFO] Task AI ricevuto: Task='cursor-prompt', Payload length=47
[09:14:31.105] [INFO] [06:14:31.105] Esecuzione: CreateNewSolution - PREVIEW MODE
[09:14:31.110] [INFO] [06:14:31.110] UserRequest: 'crea una soluzione...'
[09:14:31.115] [INFO] [06:14:31.115] TargetPath: 'C:/Users/filip/OneDrive/00_INBOX'
```

**Warning:**
```
[09:14:32.000] [WARN] ⚠️ CARTELLA GIÀ ESISTENTE: C:/.../MyNewSolution
[09:14:32.005] [WARN] Nome alternativo suggerito: MyNewSolution_1
[09:14:37.500] [WARN] ⚠️ SOVRASCRITTURA CONFERMATA: Eliminazione cartella
```

**Error:**
```
[09:14:40.000] [ERROR] ❌ CREAZIONE BLOCCATA: cartella esiste e forceOverwrite=false
[09:14:40.010] [ERROR] ❌ TargetPath mancante nel payload
[09:14:40.020] [ERROR] ❌ Errore durante eliminazione cartella: Access denied
```

### **Debug Log Pattern**

**Sempre loggare:**
1. Task ricevuto + lunghezza payload
2. Valori estratti dal payload (userRequest, targetPath, forceOverwrite)
3. Percorso completo calcolato
4. Risultato verifica esistenza cartella
5. Azioni eseguite (create, delete, skip)
6. Risultato finale

---

## 🔗 INTEGRAZIONE CONTROL CENTER

### **Control Center → Worker AI**

**Flow:**
```
1. User input in NaturalLanguageWindow
         ↓
2. NaturalLanguageViewModel.DispatchTaskAsync()
         ↓
3. BootstrapperClient.DispatchTaskAsync()
         ↓
4. HTTP POST http://localhost:5001/dispatch
         ↓
5. Orchestrator routing → http://localhost:5005/execute
         ↓
6. IndigoAiWorker01 esegue task
         ↓
7. Risposta JSON → Control Center
         ↓
8. UI aggiorna Timeline + mostra risultato
```

### **Gestione Status Speciali**

**Control Center deve gestire:**

1. `status="preview-generated"` → Mostra PreviewDialog
2. `status="folder-exists"` → Mostra FolderExistsDialog
3. `status="blocked"` → Mostra messaggio errore
4. `status="delete-failed"` → Mostra errore + suggerisci retry

---

## 🧪 TEST SCENARIOS

### **Test 1: Creazione Normale (cartella NON esiste)**
```
Input:
  userRequest: "crea soluzione X"
  targetPath: "C:/Temp"
  forceOverwrite: false

Expected:
  1. Preview generata
  2. User conferma
  3. Soluzione creata in C:/Temp/X
  4. Success=true
```

### **Test 2: Cartella Esiste - Annulla**
```
Input:
  userRequest: "crea soluzione X"
  targetPath: "C:/Temp" (già contiene "X/")
  forceOverwrite: false

Expected:
  1. Preview rileva folder-exists
  2. UI mostra FolderExistsDialog
  3. User click [Annulla]
  4. Operazione interrotta, nessuna modifica
```

### **Test 3: Cartella Esiste - Sovrascrivi**
```
Input:
  userRequest: "crea soluzione X"
  targetPath: "C:/Temp" (già contiene "X/")
  forceOverwrite: false → RE-DISPATCH con true

Expected:
  1. Preview rileva folder-exists
  2. UI mostra FolderExistsDialog
  3. User click [Sovrascrivi] + conferma doppia
  4. Re-dispatch con forceOverwrite=true
  5. Worker elimina cartella esistente
  6. Worker crea nuova soluzione
  7. Success=true
```

### **Test 4: Cartella Esiste - Nome Alternativo**
```
Input:
  userRequest: "crea soluzione X"
  targetPath: "C:/Temp" (già contiene "X/")
  suggestedName: "X_1"

Expected:
  1. Preview rileva folder-exists
  2. UI mostra FolderExistsDialog con suggeri

mento "X_1"
  3. User click [Usa nome diverso]
  4. UI aggiorna solutionName → "X_1"
  5. Re-dispatch con nuovo nome
  6. Worker crea soluzione in C:/Temp/X_1
  7. Success=true
```

---

## 📊 METRICHE PERFORMANCE

| Operazione | Tempo Medio | Note |
|------------|-------------|------|
| Parsing payload | < 1ms | Sempre istantaneo |
| Verifica cartella esistente | < 5ms | Directory.Exists() |
| Generazione preview | 50-200ms | Dipende da complessità |
| Eliminazione cartella | 10-500ms | Dipende da dimensione |
| Creazione soluzione | 100-1000ms | 6-20 file |
| Scrittura file .md | 5-20ms | Per file |

---

## 🚀 FUTURE ENHANCEMENTS

1. **Template System** → Template predefiniti per tipi soluzione comuni
2. **Git Integration** → `git init` automatico nelle soluzioni
3. **Dependency Management** → NuGet packages automatici
4. **Smart Naming** → Naming conventions basate su analisi semantica
5. **Rollback Automatico** → Ripristino cartella se creazione fallisce

---

**Versione documento:** 2.3.0  
**Ultimo aggiornamento:** 2026-01-02  
**Autore:** IndigoLab Team
