# 🗺️ ROADMAP - IndigoLab Cluster

**Piano di sviluppo e stato progetto**

Versione corrente: **2.3.0**  
Ultimo aggiornamento: **2026-01-02**

---

## ✅ STATO ATTUALE (v2.3.0)

### **🎯 Core Cluster**
- ✅ **Orchestrator** (porta 5001) - Load balancing + intelligent AI routing
- ✅ **Worker01/02** (porte 5002/5003) - Task execution standard
- ✅ **IndigoAiWorker01** (porta 5005) - AI-powered task executor
- ✅ **Monitor** (porta 5004) - Health check cluster
- ✅ **CursorMonitorAgent** (porta 5006) - File monitoring + autonomous loop

### **🖥️ Control Center UI**
- ✅ **Natural Language Console** - Input linguaggio naturale + Timeline operativa
- ✅ **Tema Console** - Palette BLU SCURO + CIANO BRILLANTE (WCAG AAA)
- ✅ **Avvio Automatico Cluster** - Zero configurazione manuale
- ✅ **Log Real-Time** - Visualizzazione stdout/stderr tutti gli agenti
- ✅ **ClusterProcessManager** - Gestione processi in background (niente PowerShell)
- ✅ **Diagnostica Avanzata** - Stati agenti (NotStarted/Starting/Running/Crashed)

### **🔒 Sicurezza & UX**
- ✅ **Preview Mode** - Anteprima obbligatoria prima di creare file
- ✅ **Folder Exists Protection** - Protezione sovrascrittura cartelle esistenti
- ✅ **ForceOverwrite Flag** - Conferma esplicita richiesta per sovrascritture
- ✅ **Nome Alternativo Suggerito** - MyNewSolution_1, _2, _3, ...
- ✅ **Doppia Conferma Sovrascrittura** - MessageBox + conferma UI

### **📊 Logging & Diagnostica**
- ✅ **LogService** - Buffer centralizzato thread-safe
- ✅ **Log Levels** - Info, Warning, Error
- ✅ **Log Selezionabili** - TextBox con selezione e copia (Ctrl+C)
- ✅ **Filtro Agenti** - System, Orchestrator, AI Worker
- ✅ **Watchdog Timers** - Rileva agenti che non producono output (5s)

### **🔗 Integrazioni**
- ✅ **CursorBridge** - Scrittura file in `.cursor/ai-requests/`
- ✅ **FILE ALWAYS MODE** - Ogni task AI genera file .md
- ✅ **Payload camelCase** - Parsing corretto userRequest/targetPath/forceOverwrite

### **📝 Documentazione**
- ✅ `ARCHITECTURE.md` - Vista architettura completa
- ✅ `WORKFLOW_CLUSTER.md` - Flussi operativi dettagliati
- ✅ `WORKER_AI.md` - Documentazione IndigoAiWorker01
- ✅ `UI_CONSOLE.md` - Design system e componenti UI
- ✅ `CHANGELOG.md` - Tracciamento versioni
- ✅ `README.md` - Overview e getting started

---

## 🚧 PROSSIMI STEP (Priorità Alta)

### **🎨 STEP 1: Rifinitura UI Console (Leggibilità Finale)**
**Stato**: 🟡 IN PROGRESS  
**Priorità**: 🔥🔥🔥 ALTA  
**Tempo stimato**: 2-3 ore

**Obiettivo:**
Completare la trasformazione UI console, garantendo leggibilità perfetta in TUTTE le schermate.

**Task dettagliati:**
1. ✅ ~~Creare `Themes/ConsoleTheme.xaml` con palette BLU + CIANO~~ (FATTO)
2. ✅ ~~Applicare tema a NaturalLanguageWindow.xaml~~ (FATTO)
3. ⏳ Applicare tema a **DashboardPage.xaml**
4. ⏳ Applicare tema a **ClusterLogsView.xaml** (verificare)
5. ⏳ Applicare tema a **tutti i Dialog** (Preview, FolderExists, Input, Explain)
6. ⏳ Rimuovere TUTTI i colori hardcoded residui
7. ⏳ Verificare contrasto su TUTTE le schermate
8. ⏳ Test finale leggibilità con utente reale

**Motivazione:**
L'UI è il punto di contatto principale con l'utente. Una UI illeggibile compromette l'intera esperienza, indipendentemente da quanto il backend sia robusto.

**Impatto:**
- ✅ UX: Leggibilità ottimale, zero affaticamento visivo
- ✅ Accessibilità: WCAG AAA ovunque
- ✅ Brand: Estetica professionale e tecnica coerente

**File da modificare:**
- `ControlCenter.UI/Views/DashboardPage.xaml`
- `ControlCenter.UI/Views/PreviewDialog.xaml`
- `ControlCenter.UI/Views/FolderExistsDialog.xaml`
- `ControlCenter.UI/Views/InputDialog.xaml`
- `ControlCenter.UI/Views/ExplainDialog.xaml`
- `ControlCenter.UI/MainWindow.xaml` (verificare pulsanti menu)

---

### **🗂️ STEP 2: Flusso Completo Folder Exists (UI + Backend)**
**Stato**: 🟡 PARZIALE (backend FATTO, UI mancano handler completi)  
**Priorità**: 🔥🔥 MEDIA-ALTA  
**Tempo stimato**: 3-4 ore

**Obiettivo:**
Completare il flusso end-to-end per gestione cartelle esistenti, con tutte le 3 opzioni funzionanti.

**Task dettagliati:**
1. ✅ ~~Worker AI rileva cartella esistente e suggerisce alternativa~~ (FATTO)
2. ✅ ~~Worker AI blocca creazione se forceOverwrite=false~~ (FATTO)
3. ✅ ~~FolderExistsDialog.xaml creato~~ (FATTO)
4. ✅ ~~Opzione "Sovrascrivi" con doppia conferma~~ (FATTO)
5. ⏳ **Opzione "Usa nome diverso"** - Implementazione COMPLETA in NaturalLanguageViewModel
6. ⏳ Re-dispatch con nuovo nome (aggiornare solutionName nel payload)
7. ⏳ Test scenario "Nome diverso" end-to-end
8. ⏳ Log diagnostici completi per ogni scenario

**Motivazione:**
Prevenire data loss è fondamentale. L'opzione "Usa nome diverso" è la più user-friendly (non richiede conferma pericolosa come "Sovrascrivi").

**Impatto:**
- ✅ Sicurezza: Zero data loss accidentale
- ✅ UX: Flusso fluido senza blocchi
- ✅ Debugging: Log chiari per ogni scenario

**File da completare:**
- `ControlCenter.UI/ViewModels/NaturalLanguageViewModel.cs` (metodo HandleFolderExistsConflictAsync)
- Test manuale completo con 3 scenari

---

### **🔢 STEP 3: Versioning Automatico Soluzioni**
**Stato**: ⚪ TODO  
**Priorità**: 🔥 MEDIA  
**Tempo stimato**: 4-6 ore

**Obiettivo:**
Implementare sistema di versioning automatico per evitare conflitti e tracciare iterazioni.

**Proposta:**
```
Invece di:
  MyNewSolution/
  MyNewSolution_1/
  MyNewSolution_2/

Usa:
  MyNewSolution/
    v001/  ← Prima versione
    v002/  ← Seconda versione (dopo modifica)
    v003/  ← Terza versione
    .current → symlink o file che punta a v003
```

**Task dettagliati:**
1. Worker AI crea sottocartelle versionate (v001, v002, ...)
2. Worker AI mantiene file `.current` con numero versione attiva
3. UI mostra "Versione attuale: v003" nella Timeline
4. Comando "Rollback a v002" per ripristinare versione precedente
5. Cleanup automatico versioni vecchie (mantieni ultime 5)

**Motivazione:**
Permette iterazioni rapide senza perdere versioni precedenti. L'utente può sperimentare liberamente sapendo di poter tornare indietro.

**Impatto:**
- ✅ UX: Iterazioni senza paura
- ✅ Sicurezza: Rollback immediato
- ✅ Debugging: Confronto versioni

**File da creare/modificare:**
- `IndigoAiWorker01/VersionManager.cs` (nuovo)
- `IndigoAiWorker01/Program.cs` (integrazione versioning)
- `ControlCenter.UI/ViewModels/NaturalLanguageViewModel.cs` (UI versioning)

---

### **📤 STEP 4: Export Log e Diagnostica**
**Stato**: ⚪ TODO  
**Priorità**: 🟡 BASSA  
**Tempo stimato**: 2-3 ore

**Obiettivo:**
Permettere export completo log e diagnostica per debugging offline o condivisione.

**Funzionalità:**
1. Pulsante "💾 Esporta Log" in ClusterLogsView
2. Format output:
   ```
   IndigoLab Cluster - Export Log
   Data: 2026-01-02 14:30:45
   
   === ORCHESTRATOR ===
   [09:14:29.123] [INFO] === Agent.Orchestrator avviato ===
   [09:14:30.001] [INFO] === AI ROUTING ATTIVATO ===
   ...
   
   === INDIGOAIWORKER01 ===
   [09:14:31.100] [INFO] Task AI ricevuto: Task='cursor-prompt'
   ...
   
   === DIAGNOSTICA ===
   Orchestrator: Running (uptime 15m 32s)
   IndigoAiWorker01: Running (uptime 15m 28s)
   ```
3. Salvataggio in `{targetPath}/IndigoLab_Log_{timestamp}.txt`
4. MessageBox con percorso file salvato

**Motivazione:**
Debugging complesso richiede analisi offline. Export log permette condivisione con altri developer o analisi post-mortem.

**Impatto:**
- ✅ Debugging: Analisi offline
- ✅ Collaborazione: Condivisione log
- ✅ Audit: Tracciabilità operazioni

**File da modificare:**
- `ControlCenter.UI/Services/LogService.cs` (metodo ExportLogs)
- `ControlCenter.UI/Views/ClusterLogsView.xaml` (pulsante Export)
- `ControlCenter.UI/Views/ClusterLogsView.xaml.cs` (handler Export)

---

### **💬 STEP 5: Explain Mode (UI Narrativa)**
**Stato**: ⚪ TODO  
**Priorità**: 🟡 MEDIA-BASSA  
**Tempo stimato**: 6-8 ore

**Obiettivo:**
Permettere all'utente di chiedere "Perché?" su qualsiasi step della Timeline, ricevendo spiegazione narrativa e tecnica.

**Funzionalità:**
1. Pulsante "❓ Perché?" accanto a ogni step Timeline
2. Click → Dispatch task `explain-step` con contesto
3. Worker AI genera spiegazione:
   - Narrativa (perché sta facendo questo)
   - Tecnica (quali file/API/processi coinvolti)
   - Dipendenze (cosa ha portato a questo step)
   - Impatto (cosa succede se confermi/annulli)
   - Alternative (altre opzioni possibili)
4. UI mostra ExplainDialog con spiegazione formattata

**Esempio:**
```
User click "❓ Perché?" su step "🔍 Anteprima generata"

ExplainDialog mostra:
┌──────────────────────────────────────────────┐
│ 💬 Spiegazione: Anteprima generata           │
├──────────────────────────────────────────────┤
│ PERCHÉ?                                       │
│ Il sistema ha generato un'anteprima per      │
│ permetterti di vedere COSA verrà creato      │
│ PRIMA di modificare il file system.          │
│                                               │
│ TECNICO:                                      │
│ - Worker AI ha analizzato la tua richiesta   │
│ - Ha determinato struttura soluzione         │
│ - Ha generato file PREVIEW.md                │
│ - NON ha ancora creato file reali            │
│                                               │
│ IMPATTO:                                      │
│ - Se confermi → crea file sul disco          │
│ - Se annulli → nessuna modifica              │
│                                               │
│ ALTERNATIVE:                                  │
│ - Modifica manualmente PREVIEW.md            │
│ - Cambia percorso destinazione                │
│                                               │
│ [OK, capito]                                 │
└──────────────────────────────────────────────┘
```

**Motivazione:**
Trasparenza completa. L'utente deve sempre capire PERCHÉ il cluster fa qualcosa, non solo COSA fa.

**Impatto:**
- ✅ UX: Comprensione profonda flussi
- ✅ Onboarding: Riduce learning curve
- ✅ Trust: Utente si fida del sistema

**File da creare/modificare:**
- `IndigoAiWorker01/Program.cs` (task `explain-step`)
- `ControlCenter.UI/Views/ExplainDialog.xaml` (già esiste, verificare stile console)
- `ControlCenter.UI/ViewModels/NaturalLanguageViewModel.cs` (handler explain)
- `ControlCenter.UI/Views/NaturalLanguageWindow.xaml` (pulsante "❓" su Timeline)

---

## 📋 BACKLOG (Non Pianificato)

### **🔄 Iterazioni e Miglioramenti**
- ⚪ **Rollback Automatico** - Ripristina versione precedente soluzione
- ⚪ **Template System** - Template predefiniti (WebAPI, WPF, Console, ...)
- ⚪ **Git Integration** - `git init` automatico + primo commit
- ⚪ **Dependency Installer** - NuGet packages automatici
- ⚪ **Solution Analyzer** - Analizza soluzione esistente e suggerisci miglioramenti

### **🤖 AI Enhancements**
- ⚪ **Multi-Model Support** - Integrazione GPT-4, Claude, Gemini
- ⚪ **Context Window Expansion** - Gestione contesto > 100K tokens
- ⚪ **Semantic Code Search** - Ricerca semantica nel codice generato
- ⚪ **Auto-Refactoring** - Refactoring automatico basato su best practices

### **🌐 Collaborazione**
- ⚪ **Multi-User Support** - Più utenti sullo stesso cluster
- ⚪ **Remote Cluster** - Cluster su server remoto
- ⚪ **Shared Solutions** - Soluzioni condivise tra utenti
- ⚪ **Activity Log Export** - Export timeline per condivisione

---

## 🎯 VISIONE FUTURA (3-6 mesi)

### **IndigoLab diventa:**

**1. Orchestratore Intelligente**
- Non solo genera codice, ma **progetta architetture complete**
- Suggerisce pattern, librerie, best practices
- Rileva code smells e propone fix automatici

**2. Assistente Conversazionale**
- Dialog naturale: "Perché hai scelto questo pattern?"
- Explain mode su ogni decisione
- Apprendimento dalle preferenze utente

**3. Sistema di Versionamento Intelligente**
- Versioning automatico con tag semantici
- Diff visuale tra versioni
- Rollback one-click
- Branch/merge soluzioni

**4. Hub Integrazioni**
- GitHub/GitLab integration (push automatico)
- CI/CD pipeline generation
- Docker containerization automatica
- Azure/AWS deployment one-click

**5. Knowledge Base**
- Il cluster "ricorda" soluzioni passate
- Suggerisce soluzioni simili già create
- Riutilizzo componenti tra progetti
- Pattern library condivisa

---

## 📊 METRICHE DI SUCCESSO

### **v2.3.0 (Attuale)**
- ✅ **Avvio cluster**: < 5 secondi
- ✅ **Tempo generazione preview**: < 200ms
- ✅ **Tempo creazione soluzione media**: < 1 secondo
- ✅ **Uptime agenti**: > 99%
- ✅ **Leggibilità UI**: Contrasto WCAG AAA (8:1)
- ✅ **Zero finestre PowerShell**: 100%

### **Target v3.0 (Futuro)**
- 🎯 **Tempo end-to-end** (input → soluzione creata): < 3 secondi
- 🎯 **Accuratezza generazione**: > 95%
- 🎯 **User satisfaction**: > 90%
- 🎯 **Zero data loss**: 100%
- 🎯 **Documentazione coverage**: 100%

---

## 🔄 CICLO SVILUPPO

### **Workflow Standard per Nuove Feature**

```
1. USER REQUEST
   ↓
2. VERIFICA DOCUMENTAZIONE ESISTENTE
   - Leggi ARCHITECTURE.md, WORKFLOW_CLUSTER.md
   - Verifica non ci siano conflitti
   ↓
3. PROPONI AGGIORNAMENTI DOCUMENTAZIONE
   - Quali file toccare
   - Quali sezioni aggiornare
   ↓
4. IMPLEMENTA CODICE
   - Backend first
   - UI dopo
   - Test continuo
   ↓
5. AGGIORNA DOCUMENTAZIONE
   - CHANGELOG.md (cosa è cambiato)
   - ROADMAP.md (marca DONE)
   - File specifici (ARCHITECTURE, WORKER_AI, etc.)
   ↓
6. COMMIT + PUSH
   - Commit message chiaro
   - Tag versione se release
```

---

## 📅 TIMELINE RELEASES

| Versione | Data | Milestone | Stato |
|----------|------|-----------|-------|
| **v2.0.0** | 2025-12-15 | Cluster base + CursorMonitorAgent | ✅ RILASCIATO |
| **v2.1.0** | 2025-12-28 | Preview Mode + Folder Protection | ✅ RILASCIATO |
| **v2.2.0** | 2026-01-01 | Avvio automatico + Log integrati | ✅ RILASCIATO |
| **v2.3.0** | 2026-01-02 | Tema Console + Fix parsing JSON | ✅ RILASCIATO |
| **v2.4.0** | 2026-01-10 | Explain Mode + UI refinements | 🎯 PIANIFICATO |
| **v2.5.0** | 2026-01-20 | Versioning automatico | 🎯 PIANIFICATO |
| **v3.0.0** | 2026-02-01 | Multi-model AI + Template system | 🔮 FUTURO |

---

## 🚀 COME CONTRIBUIRE

### **Priorità Attuali**

Se vuoi contribuire, inizia da:

1. **UI Console Refinements** (STEP 1) - Impatto immediato su UX
2. **Folder Exists Flow Completo** (STEP 2) - Sicurezza critica
3. **Documentazione** - Sempre ben accetta!

### **Come Proporre Feature**

1. Apri issue su GitHub (o documento interno)
2. Descrivi:
   - Problema che risolve
   - Utenti che beneficiano
   - Complessità stimata
   - File coinvolti
3. Aspetta feedback prima di implementare
4. Implementa con test
5. Aggiorna documentazione
6. Apri PR con descrizione chiara

---

## 📝 NOTE FINALI

**IndigoLab è:**
- ✅ Un sistema **documentato** (non solo codice)
- ✅ Un progetto **narrativo** (UI spiega cosa fa)
- ✅ Un cluster **sicuro** (conferme esplicite, protezione data loss)
- ✅ Un'esperienza **trasparente** (log, timeline, diagnostica)

**Ogni modifica deve:**
- ✅ Rispettare i principi di design (console mode, leggibilità)
- ✅ Aggiornare la documentazione pertinente
- ✅ Essere testata end-to-end
- ✅ Essere loggata con dettaglio appropriato

---

**Versione documento:** 2.3.0  
**Ultimo aggiornamento:** 2026-01-02  
**Autore:** IndigoLab Team  
**Prossima revisione:** Dopo STEP 1 completato
