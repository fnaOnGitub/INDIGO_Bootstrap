# 🏗️ ARCHITECTURE - IndigoLab Cluster

**Architettura completa del sistema IndigoLab Control Center + Cluster AI**

Versione: **2.3.0**  
Ultimo aggiornamento: **2026-01-02**

---

## 📊 VISTA AD ALTO LIVELLO

```
┌──────────────────────────────────────────────────────────────────┐
│                    INDIGOLAB ECOSYSTEM                           │
├──────────────────────────────────────────────────────────────────┤
│                                                                   │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │         CONTROL CENTER (WPF .NET 8)                        │ │
│  │  ┌──────────────────────────────────────────────────────┐  │ │
│  │  │  Natural Language Console                            │  │ │
│  │  │  - Input linguaggio naturale                         │  │ │
│  │  │  - Timeline operativa real-time                      │  │ │
│  │  │  - Log cluster integrati                             │  │ │
│  │  │  - Preview/Confirm flow                              │  │ │
│  │  └──────────────────────────────────────────────────────┘  │ │
│  │                                                              │ │
│  │  Services:                                                   │ │
│  │  - ClusterProcessManager  → Gestione processi agenti        │ │
│  │  - LogService             → Buffer log centralizzato        │ │
│  │  - BootstrapperClient     → Comunicazione con Orchestrator  │ │
│  │  - ConfigService          → Gestione configurazioni         │ │
│  └──────────────────────────────────────────────────────────────┘ │
│                               ↓                                   │
│                     HTTP POST /dispatch                           │
│                               ↓                                   │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │   ORCHESTRATOR (ASP.NET Core Minimal API - Porta 5001)    │ │
│  │  ┌──────────────────────────────────────────────────────┐  │ │
│  │  │  Intelligent AI Routing                              │  │ │
│  │  │  - Analisi task name (contiene "ai"?)               │  │ │
│  │  │  - Analisi payload (verbi creativi?)                │  │ │
│  │  │  - Rilevamento linguaggio naturale                  │  │ │
│  │  └──────────────────────────────────────────────────────┘  │ │
│  │                                                              │ │
│  │  Routing:                                                    │ │
│  │  - AI Task      → IndigoAiWorker01 (5005)                  │ │
│  │  - Standard Task → Worker01/02 (round-robin)               │ │
│  └──────────────────────────────────────────────────────────────┘ │
│                               ↓                                   │
│                     HTTP POST /execute                            │
│                               ↓                                   │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │   INDIGOAIWORKER01 (ASP.NET Core - Porta 5005)           │ │
│  │  ┌──────────────────────────────────────────────────────┐  │ │
│  │  │  AI Task Executor                                    │  │ │
│  │  │  - cursor-prompt                                     │  │ │
│  │  │  - create-new-solution (PREVIEW MODE)                │  │ │
│  │  │  - execute-solution-creation (CREAZIONE REALE)       │  │ │
│  │  └──────────────────────────────────────────────────────┘  │ │
│  │                                                              │ │
│  │  Components:                                                 │ │
│  │  - PromptOptimizer  → Analisi semantica prompt              │ │
│  │  - CursorBridge     → Integrazione Cursor .cursor/ai-reqs  │ │
│  │  - AiEngine         → Logica AI (futuro)                    │ │
│  └──────────────────────────────────────────────────────────────┘ │
│                               ↓                                   │
│                   File System Integration                         │
│                               ↓                                   │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │   CURSOR INTEGRATION                                        │ │
│  │  - .cursor/ai-requests/*.md  (FILE ALWAYS MODE)            │ │
│  │  - Soluzioni generate su disco                              │ │
│  │  - Preview *.md prima della conferma                        │ │
│  └──────────────────────────────────────────────────────────────┘ │
│                                                                   │
└──────────────────────────────────────────────────────────────────┘
```

---

## 🔄 FLUSSO RICHIESTA LINGUAGGIO NATURALE

### **Step-by-Step Flow**

```
1. UTENTE SCRIVE INPUT
   ┌────────────────────────────────────────────────────┐
   │ Natural Language Console                            │
   │ Input: "crea una soluzione per gestire colori"     │
   │ [🚀 Esegui]                                        │
   └────────────────────────────────────────────────────┘
                         ↓
2. DISPATCH TO ORCHESTRATOR
   ┌────────────────────────────────────────────────────┐
   │ BootstrapperClient.DispatchTaskAsync()             │
   │ POST http://localhost:5001/dispatch                │
   │ {                                                   │
   │   "Task": "cursor-prompt",                         │
   │   "Payload": {                                      │
   │     "userRequest": "crea una soluzione...",        │
   │     "targetPath": "C:/Users/.../INBOX"             │
   │   }                                                 │
   │ }                                                   │
   └────────────────────────────────────────────────────┘
                         ↓
3. INTELLIGENT AI ROUTING
   ┌────────────────────────────────────────────────────┐
   │ Orchestrator analizza:                              │
   │ ✓ Task name contiene "ai"? → NO                    │
   │ ✓ Payload con verbi creativi? → SÌ ("crea")       │
   │ ✓ Linguaggio naturale? → SÌ                        │
   │ → CLASSIFICATO COME AI TASK                        │
   │ → INSTRADATO A: IndigoAiWorker01 (5005)           │
   └────────────────────────────────────────────────────┘
                         ↓
4. WORKER AI - GENERA PREVIEW
   ┌────────────────────────────────────────────────────┐
   │ IndigoAiWorker01 riceve task                       │
   │ Task: "create-new-solution" (auto-generato)        │
   │                                                     │
   │ ⚠️ PREVIEW MODE ATTIVO                             │
   │ 1. Verifica se targetPath già esiste               │
   │ 2. Se esiste → restituisce "folder-exists"         │
   │ 3. Altrimenti → genera file PREVIEW.md             │
   │                                                     │
   │ File generato:                                      │
   │ {targetPath}/ColorManagement_PREVIEW.md            │
   │ - Struttura soluzione                              │
   │ - File che verranno creati                         │
   │ - Cartelle previste                                │
   │ - Nota: "In attesa conferma utente"                │
   └────────────────────────────────────────────────────┘
                         ↓
5. UI MOSTRA PREVIEW
   ┌────────────────────────────────────────────────────┐
   │ Timeline Operativa - Nuovo step:                    │
   │ 🔍 Anteprima generata                              │
   │                                                     │
   │ [MODALE PreviewDialog]                             │
   │ 📁 File da creare:                                 │
   │ - ColorManagement.sln                              │
   │ - ColorManagement/Program.cs                       │
   │ - ...                                              │
   │                                                     │
   │ [Procedi] [Annulla]                                │
   └────────────────────────────────────────────────────┘
                         ↓
6. UTENTE CONFERMA
   ┌────────────────────────────────────────────────────┐
   │ User click [Procedi]                               │
   │                                                     │
   │ Control Center invia:                              │
   │ POST http://localhost:5001/dispatch                │
   │ {                                                   │
   │   "Task": "execute-solution-creation",             │
   │   "Payload": {                                      │
   │     "userRequest": "crea...",                      │
   │     "targetPath": "C:/.../INBOX",                  │
   │     "forceOverwrite": false                        │
   │   }                                                 │
   │ }                                                   │
   └────────────────────────────────────────────────────┘
                         ↓
7. CREAZIONE REALE
   ┌────────────────────────────────────────────────────┐
   │ IndigoAiWorker01 - EXECUTE MODE                    │
   │                                                     │
   │ ⚠️ PROTEZIONE SOVRASCRITTURA:                      │
   │ 1. Verifica se cartella esiste                     │
   │ 2. Se esiste E forceOverwrite=false → BLOCCA       │
   │ 3. Se forceOverwrite=true → Elimina + Ricrea       │
   │ 4. Altrimenti → Crea nuova soluzione               │
   │                                                     │
   │ Azioni:                                             │
   │ - Directory.CreateDirectory(targetPath/ColorMgmt)  │
   │ - File.WriteAllText(*.sln)                         │
   │ - File.WriteAllText(Program.cs)                    │
   │ - ...                                              │
   │                                                     │
   │ Risultato:                                          │
   │ ✅ Soluzione creata in C:/.../INBOX/ColorMgmt      │
   └────────────────────────────────────────────────────┘
                         ↓
8. UI MOSTRA RISULTATO
   ┌────────────────────────────────────────────────────┐
   │ Timeline Operativa - Ultimo step:                   │
   │ ✅ Operazione completata                           │
   │ Soluzione creata in C:/.../INBOX/ColorMgmt         │
   │                                                     │
   │ [Pulisci Timeline]                                 │
   └────────────────────────────────────────────────────┘
```

---

## 🔌 PORTE E COMUNICAZIONE

| Componente | Porta | Protocollo | Endpoint Principali |
|------------|-------|------------|-------------------|
| **Control Center UI** | N/A | Client HTTP | POST /dispatch → 5001 |
| **Orchestrator** | 5001 | HTTP | POST /dispatch<br>GET /health<br>GET /status<br>GET /logs |
| **Worker01** | 5002 | HTTP | POST /execute<br>GET /health<br>GET /logs |
| **Worker02** | 5003 | HTTP | POST /execute<br>GET /health<br>GET /logs |
| **Monitor** | 5004 | HTTP | GET /cluster/health<br>GET /cluster/status |
| **IndigoAiWorker01** | 5005 | HTTP | POST /execute<br>GET /health<br>GET /logs |
| **CursorMonitorAgent** | 5006 | HTTP | GET /ask-user<br>POST /ask-user/answer |

### **Comunicazione Inter-Service**

```
Control Center UI
       ↓ (HTTP POST /dispatch)
   Orchestrator (5001)
       ↓ (HTTP POST /execute)
   IndigoAiWorker01 (5005)
       ↓ (File System Write)
   .cursor/ai-requests/*.md
```

---

## 🧩 COMPONENTI CHIAVE

### **1. Control Center UI**
- **Tecnologia**: WPF .NET 8, MVVM, CommunityToolkit.Mvvm
- **Responsabilità**:
  - Interfaccia utente principale
  - Avvio/gestione processi agenti (ClusterProcessManager)
  - Visualizzazione log real-time (LogService)
  - Preview/Confirm flow
  - Protezione sovrascrittura cartelle

### **2. Orchestrator**
- **Tecnologia**: ASP.NET Core Minimal API .NET 8
- **Responsabilità**:
  - Routing intelligente AI vs Standard
  - Load balancing round-robin
  - Logging centralizzato
  - Dispatch task ai worker appropriati

### **3. IndigoAiWorker01**
- **Tecnologia**: ASP.NET Core Minimal API .NET 8
- **Responsabilità**:
  - Esecuzione task AI
  - Generazione preview (*.md)
  - Creazione reale soluzioni
  - Integrazione Cursor (CursorBridge)
  - Protezione sovrascrittura (forceOverwrite)

### **4. CursorBridge**
- **Tecnologia**: Modulo C# interno a IndigoAiWorker01
- **Responsabilità**:
  - Scrittura file in `.cursor/ai-requests/`
  - FILE ALWAYS MODE (tutti i task generano file)
  - Creazione directory se non esistente
  - Timestamp automatici nei nomi file

---

## 🗂️ RELAZIONE CON CURSOR

### **FILE ALWAYS MODE**
IndigoLab implementa il principio **FILE ALWAYS MODE**: ogni task AI genera SEMPRE un file `.md` leggibile.

**Directory standard:**
```
ProjectRoot/
  .cursor/
    ai-requests/
      cursor-prompt-20260102-143052.md
      solution-preview-20260102-143118.md
      solution-created-20260102-143201.md
```

**Vantaggi:**
- ✅ Tracciabilità completa (ogni azione ha un file associato)
- ✅ Rollback facile (leggi file precedente)
- ✅ Debugging semplice (apri file e vedi cosa è stato generato)
- ✅ Compatibilità Cursor (i file sono nel path monitorato)

### **Integrazione Cursor AI**
- I file generati sono **immediatamente visibili** a Cursor
- Cursor può **leggere i file** e usarli come contesto
- L'utente può **modificare manualmente** i file prima della conferma
- Il sistema **rispetta sempre** i file esistenti (protezione sovrascrittura)

---

## 🔒 PROTEZIONE DATI

### **Protezione Sovrascrittura Cartelle**

**Flusso:**
1. Worker AI verifica se `{targetPath}/{SolutionName}` esiste
2. Se esiste E `forceOverwrite=false` → restituisce `"folder-exists"`
3. Control Center UI mostra dialog con opzioni:
   - **Sovrascrivi** → Re-dispatch con `forceOverwrite=true`
   - **Usa nome diverso** → Input nuovo nome + Re-dispatch
   - **Annulla** → Interrompe operazione
4. Worker AI procede solo se cartella NON esiste O se `forceOverwrite=true`

**Benefici:**
- ✅ Zero data loss accidentale
- ✅ Conferma esplicita richiesta
- ✅ Nomi alternativi suggeriti (MyNewSolution_1, _2, ...)
- ✅ UX narrativa e chiara

---

## 📊 LOGGING E DIAGNOSTICA

### **Log Service Architecture**

```
ClusterProcessManager
       ↓ (cattura stdout/stderr)
   LogService (buffer centralizzato)
       ↓ (evento LogUpdated)
   UI Components
       ├─ ClusterLogsView (vista dedicata)
       └─ NaturalLanguageWindow (pannello integrato)
```

**Livelli Log:**
- `Info` → Operazioni normali (CIANO)
- `Warning` → Situazioni anomale non bloccanti (GIALLO)
- `Error` → Errori bloccanti (ROSSO)

**Diagnostica Agenti:**
- `NotStarted` → Agente non ancora avviato
- `Starting` → Avvio in corso (< 5s)
- `Running` → Agente operativo (riceve/invia log)
- `Crashed` → Agente terminato inaspettatamente

---

## 🎨 DESIGN PRINCIPLES

### **Console Mode UI**
- Palette BLU SCURO + CIANO BRILLANTE
- Font tecnici: Inter (UI) + Cascadia Code (log)
- Contrasto WCAG AAA (8:1)
- Zero decorazioni inutili (cerchi, gradienti)
- Layout pulito e leggibile

### **Narrative UX**
- Ogni azione ha un messaggio esplicativo
- Timeline operativa mostra tutti gli step
- Preview obbligatoria prima della modifica
- Conferme esplicite richieste
- Messaggi di errore chiari e actionable

---

## 🚀 NEXT STEPS ARCHITETTURALI

1. **Explain Mode** → Spiegazione narrativa prima dell'esecuzione
2. **Versioning Automatico** → Solution_001, Solution_002, ...
3. **Rollback Automatico** → Ripristino stato precedente
4. **Multi-Tenant** → Supporto progetti multipli simultanei
5. **AI Model Integration** → Integrazione modelli AI esterni

---

**Versione documento:** 2.3.0  
**Ultimo aggiornamento:** 2026-01-02  
**Autore:** IndigoLab Team
