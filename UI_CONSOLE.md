# 🖥️ UI CONSOLE - IndigoLab Natural Language Console

**Documentazione completa dell'interfaccia utente Control Center**

Versione: **2.3.0**  
Ultimo aggiornamento: **2026-01-02**  
Tecnologia: **WPF .NET 8 + MVVM**

---

## 🎨 CONSOLE MODE - Design System

### **Palette Colori**

**Tema: BLU SCURO + CIANO BRILLANTE**

| Nome Risorsa | Valore Hex | Uso | Contrasto |
|--------------|------------|-----|-----------|
| `ConsoleBackground` | `#0A1628` | Sfondo principale | - |
| `ConsolePanel` | `#0F2942` | Pannelli e card | - |
| `ConsolePanelAlt` | `#1A3A5C` | Pannelli alternativi | - |
| `ConsoleBorder` | `#2C5282` | Bordi elementi | - |
| **ConsoleText** | **`#00E5FF`** | **Testo primario (CIANO BRILLANTE)** | **8.2:1 ✅ AAA** |
| **ConsoleTextSecondary** | **`#80F2FF`** | **Testo secondario (CIANO CHIARO)** | **6.8:1 ✅ AA** |
| `ConsoleTextDisabled` | `#4DD0E1` | Testo disabilitato | - |
| `AccentCyan` | `#06B6D4` | Accenti e pulsanti | 7.5:1 ✅ AAA |
| `AccentOrange` | `#F97316` | Azioni primarie | 5.2:1 ✅ AA |

**Log Levels:**
| Livello | Colore | Hex |
|---------|--------|-----|
| Info | Ciano chiaro | `#A5F3FC` |
| Debug | Verde chiaro | `#6EE7B7` |
| Warning | Giallo | `#FBBF24` |
| Error | Rosso | `#F87171` |

**Stati:**
| Stato | Colore | Hex |
|-------|--------|-----|
| Running | Verde | `#10B981` |
| Starting | Giallo | `#FBBF24` |
| Crashed | Rosso | `#EF4444` |
| Stopped | Grigio | `#6B7280` |

### **Font System**

| Tipo | Font | Dimensione | Peso | Uso |
|------|------|------------|------|-----|
| **UI Principale** | `Inter, Segoe UI` | 13-20pt | Regular/SemiBold | Tutta la UI |
| **Log Monospaziato** | `Cascadia Code, JetBrains Mono, Consolas` | 12pt | Regular | Solo pannelli log |
| **Titoli** | `Inter` | 18-20pt | SemiBold | Titoli sezioni |
| **Label** | `Inter` | 12-13pt | Regular | Sottotitoli e label |

### **Principi Design**

1. ✅ **Zero decorazioni inutili** (niente cerchi colorati, gradienti, ombre)
2. ✅ **Contrasto massimo** (WCAG AAA ovunque possibile)
3. ✅ **Layout pulito** (2 colonne: Input + Timeline)
4. ✅ **Feedback visivo immediato** (Timeline operativa real-time)
5. ✅ **Console style** (estetica terminale tecnico moderno)

---

## 🖼️ STRUTTURA UI

### **Natural Language Console - Layout Principale**

```
┌──────────────────────────────────────────────────────────────────┐
│ 🧬 IndigoLab Natural Language Console              [─][□][×]    │ ← Header BLU
│ Scrivi in linguaggio naturale e lascia che il cluster...         │
│                                                                    │
│ ⚙️ Orchestrator: ✅ Online su porta 5001          [📊][📁]      │
├──────────────────────────────────────────────────────────────────┤
│ PANNELLO SINISTRO (BLU)    │  PANNELLO DESTRO (BLU)             │
│                             │                                      │
│ Cosa vuoi che faccia il     │  📊 Timeline Operativa              │
│ cluster? (CIANO)            │  ──────────────────────────────    │
│ ┌─────────────────────────┐ │  09:14:28  📝 Input ricevuto       │
│ │ [input box]             │ │            Comando: crea una...    │
│ │ CIANO su trasparente    │ │                                     │
│ │                         │ │  09:14:29  🎯 Invio Orchestrator   │
│ │                         │ │                                     │
│ └─────────────────────────┘ │  09:14:30  ⚡ Analisi linguaggio   │
│                             │                                      │
│     [🚀 Esegui]            │  09:14:31  🔨 Generazione preview   │
│     (outline ARANCIONE)     │                                     │
│                             │  09:14:34  🔍 Anteprima generata   │
│ ⚙️ Stato Orchestrator       │                                     │
│ ┌─────────────────────────┐ │  09:14:35  ⏸️ Conferma PREVIEW    │
│ │ Stato: ✅ Online 5001   │ │                                     │
│ │ Porta: 5001             │ │  [🗑️ Pulisci]                     │
│ │ Risposta: 12ms          │ │                                     │
│ └─────────────────────────┘ │                                     │
│                             │                                      │
│ 📊 Log Cluster              │                                     │
│ ┌─────────────────────────┐ │                                     │
│ │ [System][Orchestr][AI]  │ │                                     │
│ │ [09:14:29.123] [INFO]   │ │                                     │
│ │ === CONTROL_CENTER ===  │ │                                     │
│ │ ...                     │ │                                     │
│ └─────────────────────────┘ │                                     │
└──────────────────────────────────────────────────────────────────┘
Background: BLU NOTTE (#0A1628)
```

### **Componenti Principali**

#### **1. Input Box**
```xml
<TextBox Text="{Binding UserInput}"
         Foreground="{StaticResource ConsoleTextBrush}"
         Background="Transparent"
         CaretBrush="{StaticResource AccentCyanBrush}"
         FontSize="16"
         MinHeight="150"
         MaxHeight="300"/>
```

**Caratteristiche:**
- Testo CIANO BRILLANTE su trasparente
- Cursore CIANO
- Font Inter 16pt
- Auto-resize (150-300px)
- TextWrapping abilitato

#### **2. Timeline Operativa**
```xml
<ItemsControl ItemsSource="{Binding TimelineSteps}">
    <ItemsControl.ItemTemplate>
        <DataTemplate>
            <Border Style="{StaticResource TimelineStepStyle}">
                <Grid>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition Width="Auto"/>
                    </Grid.ColumnDefinitions>
                    
                    <StackPanel Grid.Column="0">
                        <TextBlock Text="{Binding Title}"
                                   Foreground="{StaticResource ConsoleTextBrush}"/>
                        <TextBlock Text="{Binding Description}"
                                   Foreground="{StaticResource ConsoleTextSecondaryBrush}"/>
                    </StackPanel>
                    
                    <TextBlock Grid.Column="1"
                               Text="{Binding FormattedTime}"
                               Foreground="{StaticResource ConsoleTextSecondaryBrush}"/>
                </Grid>
            </Border>
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>
```

**Caratteristiche:**
- Background trasparente (nessun sfondo dietro eventi)
- Bordo sottile CIANO quando attivo
- 2 colonne: Contenuto + Timestamp
- Zero cerchi decorativi
- Layout pulito e leggibile

#### **3. Log Panel Integrato**
```xml
<Expander Header="📊 Log Cluster (Tempo Reale)">
    <Border>
        <StackPanel>
            <!-- Filtri agente -->
            <StackPanel Orientation="Horizontal">
                <Button Content="System" Click="SelectLogAgent_Click"/>
                <Button Content="Orchestrator" Click="SelectLogAgent_Click"/>
                <Button Content="AI Worker" Click="SelectLogAgent_Click"/>
            </StackPanel>
            
            <!-- Log display -->
            <TextBox x:Name="LogTextBox"
                     Style="{StaticResource ConsoleLogTextBox}"
                     MinHeight="300"/>
        </StackPanel>
    </Border>
</Expander>
```

**Caratteristiche:**
- Font monospaziato (Cascadia Code)
- Sfondo `#121212` (BLU NOTTE)
- Testo CIANO
- Selezionabile e copiabile (Ctrl+C)
- Auto-scroll su nuovi log
- Filtro per agente

#### **4. Pulsante Esegui**
```xml
<Button Content="🚀 Esegui"
        Style="{StaticResource ConsoleButtonOrange}"
        Command="{Binding DispatchTaskCommand}"/>
```

**Caratteristiche:**
- Outline ARANCIONE (`#F97316`)
- Background trasparente
- Hover: background ARANCIONE pieno
- Font Inter 18pt SemiBold
- Padding 32x16

---

## 🔄 COMPORTAMENTI UI

### **1. Preview Flow**

```
User input → [Esegui]
         ↓
Timeline: "🔨 Generazione anteprima"
         ↓
Worker AI genera PREVIEW.md
         ↓
Timeline: "🔍 Anteprima generata"
         ↓
MODALE PreviewDialog.xaml
┌────────────────────────────────────┐
│ 🔍 Anteprima modifiche             │
│                                     │
│ 📁 File da creare: 6               │
│ 🗂️ Cartelle da creare: 3           │
│ 🧱 Struttura finale: [dettaglio]   │
│                                     │
│ [Procedi] [Annulla]                │
└────────────────────────────────────┘
         ↓
User click [Procedi]
         ↓
Timeline: "▶️ Esecuzione confermata"
         ↓
Worker AI crea REALMENTE
         ↓
Timeline: "✅ Operazione completata"
```

### **2. Folder Exists Flow**

```
Preview MODE rileva cartella esistente
         ↓
Worker AI restituisce status="folder-exists"
         ↓
MODALE FolderExistsDialog.xaml
┌────────────────────────────────────┐
│ ⚠️ Cartella già esistente          │
│                                     │
│ La cartella "MyNewSolution"        │
│ esiste già in:                     │
│ C:/.../INBOX/MyNewSolution         │
│                                     │
│ Come vuoi procedere?               │
│                                     │
│ [🔥 Sovrascrivi]                   │
│ [✏️ Usa nome diverso]              │
│ [❌ Annulla]                       │
└────────────────────────────────────┘
         ↓
User sceglie opzione
         ↓
┌───────────────┬─────────────────┬───────────────┐
│ Sovrascrivi   │ Nome diverso    │ Annulla       │
├───────────────┼─────────────────┼───────────────┤
│ Conferma      │ InputDialog     │ Stop workflow │
│ doppia        │ per nuovo nome  │ Timeline:     │
│ MessageBox    │                 │ "❌ Annullato"│
│ ↓             │ ↓               │               │
│ Re-dispatch   │ Re-dispatch     │               │
│ forceOver-    │ con nuovo       │               │
│ write=true    │ solutionName    │               │
└───────────────┴─────────────────┴───────────────┘
```

### **3. Logging Real-Time**

```
LogService cattura output da agenti
         ↓
Evento LogUpdated triggerato
         ↓
UI aggiorna LogTextBox.Text
         ↓
Auto-scroll a fine testo
         ↓
User può selezionare e copiare (Ctrl+C)
```

---

## 📐 STILI CONSOLE THEME

### **Stili Disponibili** (Themes/ConsoleTheme.xaml)

#### **Pannelli**
```xml
<!-- Pannello console standard -->
<Style x:Key="ConsolePanelStyle" TargetType="Border">
    <Setter Property="Background" Value="{StaticResource ConsolePanelBrush}"/>
    <Setter Property="BorderBrush" Value="{StaticResource ConsoleBorderBrush}"/>
    <Setter Property="BorderThickness" Value="1"/>
    <Setter Property="CornerRadius" Value="4"/>
    <Setter Property="Padding" Value="12"/>
</Style>

<!-- Pannello trasparente per testo -->
<Style x:Key="TransparentPanelStyle" TargetType="Border">
    <Setter Property="Background" Value="Transparent"/>
    <Setter Property="BorderThickness" Value="0"/>
</Style>
```

#### **Pulsanti**
```xml
<!-- Pulsante outline cyan -->
<Style x:Key="ConsoleButton" TargetType="Button">
    <Setter Property="Background" Value="Transparent"/>
    <Setter Property="BorderBrush" Value="{StaticResource AccentCyanBrush}"/>
    <Setter Property="Foreground" Value="{StaticResource ConsoleTextBrush}"/>
    <Setter Property="FontFamily" Value="Inter, Segoe UI"/>
    <Setter Property="Padding" Value="8,4"/>
</Style>

<!-- Pulsante outline arancione -->
<Style x:Key="ConsoleButtonOrange" TargetType="Button">
    <Setter Property="BorderBrush" Value="{StaticResource AccentOrangeBrush}"/>
    <!-- Hover: background arancione pieno -->
</Style>
```

#### **TextBox**
```xml
<!-- Input generale -->
<Style x:Key="ConsoleTextBox" TargetType="TextBox">
    <Setter Property="Background" Value="Transparent"/>
    <Setter Property="Foreground" Value="{StaticResource ConsoleTextBrush}"/>
    <Setter Property="FontFamily" Value="Inter, Segoe UI"/>
    <Setter Property="CaretBrush" Value="{StaticResource AccentCyanBrush}"/>
</Style>

<!-- Log monospaziato -->
<Style x:Key="ConsoleLogTextBox" TargetType="TextBox">
    <Setter Property="Background" Value="#121212"/>
    <Setter Property="Foreground" Value="#E5E7EB"/>
    <Setter Property="FontFamily" Value="Cascadia Code, JetBrains Mono, Consolas"/>
    <Setter Property="FontSize" Value="12"/>
    <Setter Property="IsReadOnly" Value="True"/>
    <Setter Property="TextWrapping" Value="Wrap"/>
</Style>
```

#### **TextBlock**
```xml
<!-- Titolo -->
<Style x:Key="ConsoleTitleText" TargetType="TextBlock">
    <Setter Property="Foreground" Value="{StaticResource ConsoleTextBrush}"/>
    <Setter Property="FontFamily" Value="Inter, Segoe UI"/>
    <Setter Property="FontSize" Value="18"/>
    <Setter Property="FontWeight" Value="SemiBold"/>
</Style>

<!-- Label -->
<Style x:Key="ConsoleLabelText" TargetType="TextBlock">
    <Setter Property="Foreground" Value="{StaticResource ConsoleTextSecondaryBrush}"/>
    <Setter Property="FontFamily" Value="Inter, Segoe UI"/>
    <Setter Property="FontSize" Value="13"/>
</Style>

<!-- Monospace -->
<Style x:Key="ConsoleMonoText" TargetType="TextBlock">
    <Setter Property="FontFamily" Value="Cascadia Code, JetBrains Mono, Consolas"/>
    <Setter Property="FontSize" Value="12"/>
</Style>
```

---

## 🧩 COMPONENTI UI

### **1. Natural Language Input**

**File:** `Views/NaturalLanguageWindow.xaml`  
**ViewModel:** `ViewModels/NaturalLanguageViewModel.cs`

**Responsabilità:**
- Input utente in linguaggio naturale
- Dispatch task a Orchestrator
- Gestione Timeline operativa
- Visualizzazione log integrati
- Preview/Confirm flow

**Binding Principali:**
```csharp
// Input
public string UserInput { get; set; }

// Timeline
public ObservableCollection<TimelineStep> TimelineSteps { get; }

// Stato
public bool IsOrchestratorOnline { get; set; }
public string CurrentStepTitle { get; set; }
public string CurrentStepDescription { get; set; }

// Log
public string SelectedLogAgent { get; set; } = "System";
```

**Comandi:**
```csharp
[RelayCommand]
private async Task DispatchTaskAsync()
{
    // 1. Aggiunge step a Timeline
    AddTimelineStep("📝 Input ricevuto", $"Comando: {UserInput}");
    
    // 2. Dispatch a Orchestrator
    var response = await _client.DispatchTaskAsync(
        "Orchestrator",
        "cursor-prompt",
        UserInput,
        targetPath: _config.DefaultSolutionPath
    );
    
    // 3. Gestisce risposta
    if (response?.Status == "folder-exists")
    {
        await HandleFolderExistsConflictAsync(response);
    }
    else if (response?.Status == "preview-generated")
    {
        await HandlePreviewConfirmationAsync(response);
    }
}
```

---

### **2. Timeline Operativa**

**Modello:** `Models/TimelineStep.cs`

```csharp
public class TimelineStep
{
    public string Title { get; set; }
    public string Description { get; set; }
    public DateTime Timestamp { get; set; }
    public TimelineStepType Type { get; set; }
    public bool IsActive { get; set; }
    
    public string FormattedTime => Timestamp.ToString("HH:mm:ss");
}

public enum TimelineStepType
{
    Input,      // 📝 Input ricevuto
    Routing,    // 🎯 Invio a Orchestrator
    Processing, // ⚡ Analisi/Elaborazione
    Output,     // 📄 Risultato
    Dialog,     // 💬 Richiesta conferma
    Success,    // ✅ Operazione completata
    Error,      // ❌ Errore
    Info        // ℹ️ Informazione
}
```

**Colori Timeline:**
- Input → CIANO (`#06B6D4`)
- Processing → ARANCIONE (`#F97316`)
- Success → VERDE (`#10B981`)
- Error → ROSSO (`#F87171`)

---

### **3. Preview Dialog**

**File:** `Views/PreviewDialog.xaml`  
**Code-behind:** `Views/PreviewDialog.xaml.cs`

**Struttura:**
```xml
<Window Background="{StaticResource ConsolePanelBrush}">
    <StackPanel>
        <TextBlock Text="🔍 Anteprima modifiche"
                   Style="{StaticResource ConsoleTitleText}"/>
        
        <!-- Sezioni -->
        <TextBlock Text="📁 File che verranno creati"/>
        <TextBlock Text="🗂️ Cartelle che verranno create"/>
        <TextBlock Text="🧱 Struttura finale prevista"/>
        
        <!-- Azioni -->
        <StackPanel Orientation="Horizontal">
            <Button Content="Procedi" Style="{StaticResource ConsoleButtonOrange}"/>
            <Button Content="Annulla" Style="{StaticResource ConsoleButton}"/>
        </StackPanel>
    </StackPanel>
</Window>
```

**Comportamento:**
```csharp
private void BtnProceed_Click(object sender, RoutedEventArgs e)
{
    UserAction = PreviewAction.Proceed;
    DialogResult = true;
    Close();
}

private void BtnCancel_Click(object sender, RoutedEventArgs e)
{
    UserAction = PreviewAction.Cancel;
    DialogResult = false;
    Close();
}
```

---

### **4. Folder Exists Dialog**

**File:** `Views/FolderExistsDialog.xaml`  
**Code-behind:** `Views/FolderExistsDialog.xaml.cs`

**Proprietà:**
```csharp
public enum FolderExistsAction
{
    None,
    Overwrite,
    UseDifferentName,
    Cancel
}

public FolderExistsAction UserAction { get; private set; }
public string? NewSolutionName { get; private set; }
```

**Comportamento:**
```csharp
// Sovrascrivi → Doppia conferma
private void BtnOverwrite_Click(object sender, RoutedEventArgs e)
{
    var result = MessageBox.Show(
        "⚠️ ATTENZIONE: Questa azione eliminerà TUTTI i file...",
        "Conferma sovrascrittura",
        MessageBoxButton.YesNo,
        MessageBoxImage.Warning
    );
    
    if (result == MessageBoxResult.Yes)
    {
        UserAction = FolderExistsAction.Overwrite;
        DialogResult = true;
        Close();
    }
}

// Usa nome diverso → Input dialog
private void BtnDifferentName_Click(object sender, RoutedEventArgs e)
{
    var inputDialog = new InputDialog
    {
        Title = "Nuovo nome soluzione",
        Label = "Inserisci il nuovo nome:",
        DefaultValue = SuggestedAlternativeName
    };
    
    if (inputDialog.ShowDialog() == true)
    {
        NewSolutionName = inputDialog.UserInput;
        UserAction = FolderExistsAction.UseDifferentName;
        DialogResult = true;
        Close();
    }
}
```

---

## 🎯 NARRATIVE UX PRINCIPLES

### **1. Ogni Azione È Visibile**
- Niente operazioni "nascoste"
- Timeline mostra TUTTI gli step
- Log sempre accessibili
- Timestamp su ogni evento

### **2. Conferma Prima di Modifiche Permanenti**
- Preview OBBLIGATORIA per creazione soluzioni
- Doppia conferma per sovrascrittura
- Opzione "Annulla" sempre presente

### **3. Messaggi Chiari e Actionable**
```
❌ MALE:  "Errore"
✅ BENE:  "❌ CREAZIONE BLOCCATA: La cartella esiste già e forceOverwrite=false"

❌ MALE:  "Success"
✅ BENE:  "✅ Operazione completata - Soluzione creata in C:/.../MyNewSolution"
```

### **4. Protezione Data Loss**
- Zero sovrascritture accidentali
- Suggerimento nomi alternativi (MyNewSolution_1, _2, ...)
- Doppia conferma per eliminazioni
- Validazione input sempre presente

---

## 📊 ACCESSIBILITY & UX

### **Contrasto Colori (WCAG)**

| Combinazione | Contrasto | Livello | Rating |
|--------------|-----------|---------|--------|
| CIANO BRILLANTE su BLU SCURO | 8.2:1 | AAA | ⭐⭐⭐ |
| CIANO CHIARO su BLU SCURO | 6.8:1 | AA | ⭐⭐ |
| ARANCIONE su BLU SCURO | 5.2:1 | AA | ⭐⭐ |

### **Leggibilità**

✅ **Font Size Minimo**: 12pt (log) - 13pt (UI) - 16pt (input)  
✅ **Line Height**: 1.5 (testo corpo) - 1.2 (titoli)  
✅ **Padding**: Minimo 8px (pulsanti) - 12px (pannelli)  
✅ **Selezione Testo**: Abilitata ovunque (tranne header)  
✅ **Copy/Paste**: Ctrl+C funziona su log

### **Responsive Behavior**

- Input box: auto-resize 150-300px
- Log panel: collapsible (Expander)
- Timeline: auto-scroll su nuovi eventi
- Finestra: ridimensionabile (min 800x600)

---

## 🛠️ SERVIZI UI

### **LogService**

**File:** `Services/LogService.cs`

```csharp
public class LogService
{
    private ConcurrentDictionary<string, List<LogEntry>> _logs;
    public event EventHandler<string>? LogUpdated;
    
    public void AppendLog(string agentName, string message, LogLevel level = LogLevel.Info)
    {
        var entry = new LogEntry
        {
            Timestamp = DateTime.Now,
            Level = level,
            Message = message
        };
        
        _logs.GetOrAdd(agentName, new List<LogEntry>()).Add(entry);
        
        LogUpdated?.Invoke(this, agentName);
    }
    
    public List<LogEntry> GetLogs(string agentName)
    {
        return _logs.TryGetValue(agentName, out var logs) ? logs : new List<LogEntry>();
    }
}
```

### **ClusterProcessManager**

**File:** `Services/ClusterProcessManager.cs`

```csharp
public class ClusterProcessManager
{
    public enum AgentStatus { NotStarted, Starting, Running, Crashed }
    
    public class AgentDiagnostics
    {
        public AgentStatus Status { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? LastOutputTime { get; set; }
        public int OutputLinesReceived { get; set; }
        public int ErrorLinesReceived { get; set; }
        public bool ReceivedOutputAfterStart { get; set; }
        public string? LastError { get; set; }
        public int? ExitCode { get; set; }
    }
    
    public async Task StartAgent(string agentName)
    {
        // 1. Crea ProcessStartInfo
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{projectPath}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        
        // 2. Hook event handlers
        process.OutputDataReceived += (s, e) =>
        {
            _logService.AppendLog(agentName, e.Data, LogLevel.Info);
            UpdateDiagnostics(agentName, outputLine: true);
        };
        
        process.ErrorDataReceived += (s, e) =>
        {
            _logService.AppendLog(agentName, $"[ERR] {e.Data}", LogLevel.Error);
            UpdateDiagnostics(agentName, errorLine: true);
        };
        
        process.Exited += (s, e) =>
        {
            SetAgentStatus(agentName, AgentStatus.Crashed);
            _logService.AppendLog(agentName, "[FATAL] Processo terminato", LogLevel.Error);
        };
        
        // 3. Avvia
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        
        // 4. Watchdog timer (5s)
        StartOutputWatchdog(agentName);
    }
}
```

---

## 🎨 BEST PRACTICES UI

### **DO ✅**

1. **Usa sempre stili da ConsoleTheme.xaml**
   ```xml
   <Button Style="{StaticResource ConsoleButton}"/>
   <TextBlock Style="{StaticResource ConsoleTitleText}"/>
   ```

2. **Applica font corretti**
   - UI → Inter
   - Log → Cascadia Code

3. **Mantieni contrasto alto**
   - Testo CIANO su BLU SCURO
   - Mai testo scuro su scuro

4. **Background trasparente per testo**
   ```xml
   <TextBox Background="Transparent"
            Foreground="{StaticResource ConsoleTextBrush}"/>
   ```

5. **Timeline pulita**
   - Niente cerchi decorativi
   - Solo testo + timestamp
   - Border sottile quando attivo

### **DON'T ❌**

1. ❌ **MAI usare colori hardcoded**
   ```xml
   <!-- SBAGLIATO -->
   <TextBlock Foreground="#333"/>
   
   <!-- CORRETTO -->
   <TextBlock Foreground="{StaticResource ConsoleTextBrush}"/>
   ```

2. ❌ **MAI usare VIOLA**
   - `#4B0082`, `#7C3AED`, `#8B5CF6`, `#A78BFA` sono VIETATI

3. ❌ **MAI sfondo scuro dietro testo diretto**
   ```xml
   <!-- SBAGLIATO (illeggibile) -->
   <Border Background="#111827">
       <TextBlock Text="..." Foreground="#333"/>
   </Border>
   
   <!-- CORRETTO -->
   <Border Background="{StaticResource ConsolePanelBrush}">
       <TextBlock Text="..." Foreground="{StaticResource ConsoleTextBrush}"/>
   </Border>
   ```

4. ❌ **MAI decorazioni inutili**
   - Niente cerchi colorati
   - Niente gradienti
   - Niente ombre pesanti

---

## 🧪 UI TESTING CHECKLIST

### **Test Visivi**

- [ ] **Leggibilità Input Box**: Testo CIANO su BLU leggibile
- [ ] **Leggibilità Timeline**: Eventi CIANO su BLU trasparente leggibili
- [ ] **Leggibilità Log**: Font mono, scroll funzionante, selezionabile
- [ ] **Contrasto**: Nessun testo con contrasto < 4.5:1
- [ ] **Font**: Inter ovunque (UI), Cascadia Code solo log
- [ ] **Colori**: Zero viola, zero grigio-su-grigio

### **Test Funzionali**

- [ ] **Input multilinea**: TextWrapping funziona
- [ ] **Copia log**: Ctrl+C funziona
- [ ] **Auto-scroll log**: Scroll automatico su nuovi log
- [ ] **Preview dialog**: Mostra file/cartelle previsti
- [ ] **Folder exists**: Opzioni Sovrascrivi/Nome diverso/Annulla
- [ ] **Timeline clear**: Pulsante "Pulisci" funziona

---

## 🚀 FUTURE UI ENHANCEMENTS

1. **Explain Mode** → Pulsante "❓ Perché?" su ogni step
2. **Dark/Light Toggle** → Switch tra tema scuro e chiaro
3. **Font Size Slider** → Regolazione dimensione font log
4. **Export Logs** → Salva log su file .txt
5. **Timeline Filter** → Filtra eventi per tipo (solo errori, solo successi, etc.)
6. **Keyboard Shortcuts** → Ctrl+Enter per Esegui, Ctrl+L per pulire timeline

---

**Versione documento:** 2.3.0  
**Ultimo aggiornamento:** 2026-01-02  
**Autore:** IndigoLab Team
