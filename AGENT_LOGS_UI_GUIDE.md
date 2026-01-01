# Agent Logs UI - Guida Completa

Documentazione delle modifiche per visualizzare i log degli agenti nel Control Center.

## Modifiche Implementate

### A) AgentDetailWindow.xaml ✅

**Sezione aggiunta dopo "Dispatch Task":**

```xml
<!-- Log Agente -->
<Border Style="{StaticResource CardBorderStyle}" Margin="0,0,0,15">
    <StackPanel>
        <Grid Margin="0,0,0,10">
            <TextBlock Text="Log dell'Agente" Style="{StaticResource SubtitleTextStyle}"/>
            <StackPanel Orientation="Horizontal" HorizontalAlignment="Right">
                <Button Content="🔄 Aggiorna Log"
                       Click="RefreshLogs_Click"
                       IsEnabled="{Binding IsLoadingLogs, Converter={StaticResource InverseBoolConverter}}"/>
                
                <Button x:Name="ToggleLogsButton"
                       Content="📋 Mostra Log"
                       Click="ToggleLogs_Click"/>
            </StackPanel>
        </Grid>
        
        <!-- Sezione Log (nascosta di default) -->
        <StackPanel x:Name="LogsSection" Visibility="Collapsed">
            <!-- Loading indicator -->
            <TextBlock Text="Caricamento log..."
                      Visibility="{Binding IsLoadingLogs, Converter={StaticResource BoolToVisibilityConverter}}"/>
            
            <!-- TextBox per i log -->
            <TextBox x:Name="LogsTextBox"
                    Text="{Binding LogsText, Mode=OneWay}"
                    IsReadOnly="True"
                    Height="250"
                    FontFamily="Consolas"
                    FontSize="11"/>
            
            <!-- Info log count -->
            <TextBlock>
                <Run Text="Totale eventi:"/>
                <Run Text="{Binding LogCount, Mode=OneWay}"/>
                <Run Text=" | Auto-refresh: "/>
                <Run Text="{Binding IsAutoRefreshEnabled, Mode=OneWay}"/>
            </TextBlock>
            
            <!-- Toggle auto-refresh -->
            <CheckBox Content="Abilita auto-refresh (ogni 5 secondi)"
                     IsChecked="{Binding IsAutoRefreshEnabled}"/>
        </StackPanel>
    </StackPanel>
</Border>
```

**Caratteristiche UI:**
- ✅ Pulsante "Mostra Log" / "Nascondi Log"
- ✅ Pulsante "Aggiorna Log"
- ✅ TextBox readonly con font monospace
- ✅ Scrollbar verticale e orizzontale
- ✅ Indicatore di caricamento
- ✅ Contatore eventi
- ✅ Checkbox per auto-refresh

---

### B) AgentDetailWindow.xaml.cs ✅

**Modifiche al code-behind:**

```csharp
using System.Windows;
using System.Windows.Threading;
using ControlCenter.UI.Models;
using ControlCenter.UI.ViewModels;

public partial class AgentDetailWindow : Window
{
    private readonly AgentDetailViewModel _viewModel;
    private readonly DispatcherTimer _logTimer;
    private bool _isLogsVisible = false;

    public AgentDetailWindow(AgentInfoViewModel agent)
    {
        InitializeComponent();
        
        _viewModel = new AgentDetailViewModel(agent);
        DataContext = _viewModel;

        // Setup auto-refresh timer
        _logTimer = new DispatcherTimer();
        _logTimer.Interval = TimeSpan.FromSeconds(5);
        _logTimer.Tick += async (s, e) => await LoadLogsAsync();

        // Subscribe to auto-refresh changes
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(_viewModel.IsAutoRefreshEnabled))
            {
                if (_viewModel.IsAutoRefreshEnabled && _isLogsVisible)
                {
                    _logTimer.Start();
                }
                else
                {
                    _logTimer.Stop();
                }
            }
        };
    }

    private async void ToggleLogs_Click(object sender, RoutedEventArgs e)
    {
        _isLogsVisible = !_isLogsVisible;

        if (_isLogsVisible)
        {
            LogsSection.Visibility = Visibility.Visible;
            ToggleLogsButton.Content = "📋 Nascondi Log";
            await LoadLogsAsync();
            
            if (_viewModel.IsAutoRefreshEnabled)
            {
                _logTimer.Start();
            }
        }
        else
        {
            LogsSection.Visibility = Visibility.Collapsed;
            ToggleLogsButton.Content = "📋 Mostra Log";
            _logTimer.Stop();
        }
    }

    private async void RefreshLogs_Click(object sender, RoutedEventArgs e)
    {
        await LoadLogsAsync();
    }

    private async Task LoadLogsAsync()
    {
        await _viewModel.LoadLogsAsync();
    }

    protected override void OnClosed(EventArgs e)
    {
        _logTimer.Stop();
        base.OnClosed(e);
    }
}
```

**Caratteristiche:**
- ✅ DispatcherTimer per auto-refresh ogni 5 secondi
- ✅ Toggle visibilità sezione log
- ✅ Caricamento log on-demand
- ✅ Pulizia timer alla chiusura finestra
- ✅ Subscribe a PropertyChanged per auto-refresh

---

### C) AgentDetailViewModel.cs ✅

**Nuove proprietà:**

```csharp
// Log Properties
[ObservableProperty]
private bool _isLoadingLogs;

[ObservableProperty]
private string _logsText = "Nessun log disponibile. Clicca 'Aggiorna Log' per caricare.";

[ObservableProperty]
private int _logCount;

[ObservableProperty]
private bool _isAutoRefreshEnabled;
```

**Nuovo metodo LoadLogsAsync():**

```csharp
public async Task LoadLogsAsync()
{
    Debug.WriteLine($"[AgentDetailViewModel] Caricamento log per agente: {Agent.Name}");

    IsLoadingLogs = true;

    try
    {
        // Costruisce l'URL dell'agente
        var agentUrl = $"http://localhost:{Agent.Port}";

        // Chiama l'endpoint /logs
        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(10);

        var response = await httpClient.GetAsync($"{agentUrl}/logs");

        if (response.IsSuccessStatusCode)
        {
            var logsResponse = await response.Content.ReadFromJsonAsync<LogsResponse>();

            if (logsResponse != null && logsResponse.Success && logsResponse.Logs != null && logsResponse.Logs.Count > 0)
            {
                LogCount = logsResponse.Count;

                // Formatta i log in una stringa multilinea
                var logsLines = logsResponse.Logs.Select(log =>
                {
                    var timestamp = log.Timestamp.ToLocalTime().ToString("HH:mm:ss");
                    var level = log.Level.PadRight(5);
                    return $"[{timestamp}] [{level}] {log.Message}";
                });

                LogsText = string.Join("\n", logsLines);
            }
            else
            {
                LogCount = 0;
                LogsText = "Nessun log disponibile per questo agente.";
            }
        }
        else
        {
            LogCount = 0;
            LogsText = $"Errore nel caricamento dei log: HTTP {response.StatusCode}";
        }
    }
    catch (Exception ex)
    {
        LogCount = 0;
        LogsText = $"Errore nel caricamento dei log:\n{ex.Message}";
    }
    finally
    {
        IsLoadingLogs = false;
    }
}
```

**Nuovi modelli:**

```csharp
public class LogsResponse
{
    public bool Success { get; set; }
    public int Count { get; set; }
    public List<LogEntry> Logs { get; set; } = new();
}

public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = "INFO";
    public string Message { get; set; } = "";
}
```

**Caratteristiche:**
- ✅ Caricamento log da `GET http://localhost:{port}/logs`
- ✅ Parsing JSON response
- ✅ Formattazione log con timestamp, level, message
- ✅ Gestione errori HTTP e eccezioni
- ✅ Timeout 10 secondi
- ✅ Conversione timestamp UTC → LocalTime

---

## Formato Log

### Input (JSON da /logs):

```json
{
  "Success": true,
  "Count": 4,
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
    },
    {
      "Timestamp": "2026-01-01T12:33:10.500Z",
      "Level": "ERROR",
      "Message": "Errore da worker: 500"
    }
  ]
}
```

### Output (TextBox):

```
[12:32:45] [INFO ] Task ricevuto: optimize-prompt
[12:32:45] [INFO ] Instradato a AI-Worker: http://localhost:5005
[12:32:50] [INFO ] Task 'optimize-prompt' completato con successo da AI-Worker
[12:33:10] [ERROR] Errore da worker: 500
```

---

## Workflow Utente

### 1. Aprire Agent Details

**Da Dashboard o Agents Page:**
- Click su un agente (es. "Orchestrator", "IndigoAiWorker01")
- Si apre la finestra "Dettagli Agente"

### 2. Mostrare i Log

**Click su "📋 Mostra Log":**
- La sezione log diventa visibile
- Viene caricato automaticamente il primo set di log
- Il pulsante cambia in "📋 Nascondi Log"

### 3. Aggiornare i Log

**Click su "🔄 Aggiorna Log":**
- Ricarica i log più recenti dall'agente
- Mostra "Caricamento log..." durante il fetch

### 4. Auto-refresh

**Checkbox "Abilita auto-refresh (ogni 5 secondi)":**
- Se abilitato, i log si aggiornano automaticamente ogni 5 secondi
- Utile per monitoraggio real-time

### 5. Nascondere i Log

**Click su "📋 Nascondi Log":**
- La sezione log viene nascosta
- L'auto-refresh viene fermato

---

## Test Completo

### Test 1: Visualizzazione Log Orchestrator

**Passi:**
1. Avvia il cluster (tutti gli agenti online)
2. Apri Control Center UI
3. Vai su "Agents" → Click su "agent-orchestrator"
4. Click su "📋 Mostra Log"

**Risultato atteso:**
- Sezione log visibile
- Se non ci sono log: "Nessun log disponibile per questo agente."
- Se ci sono log: Lista formattata con timestamp, level, message

### Test 2: Dispatch Task + Visualizzazione Log

**Passi:**
1. In Agent Details (Orchestrator):
2. Dispatch un task:
   - Task Name: `optimize-prompt`
   - Payload: `Crea pagina WPF dashboard`
3. Attendi completamento dispatch
4. Click su "🔄 Aggiorna Log"

**Risultato atteso:**
```
[HH:mm:ss] [INFO ] Task ricevuto: optimize-prompt
[HH:mm:ss] [INFO ] Instradato a AI-Worker: http://localhost:5005
[HH:mm:ss] [INFO ] Task 'optimize-prompt' completato con successo da AI-Worker
```

### Test 3: Auto-refresh

**Passi:**
1. In Agent Details (IndigoAiWorker01):
2. Click su "📋 Mostra Log"
3. Abilita checkbox "Abilita auto-refresh (ogni 5 secondi)"
4. Dispatch un nuovo task dall'Orchestrator
5. Osserva i log di IndigoAiWorker01

**Risultato atteso:**
- I log si aggiornano automaticamente ogni 5 secondi
- Nuovi log appaiono senza click manuale su "Aggiorna Log"

### Test 4: Errori

**Test 4.1: Agente Offline**

**Passi:**
1. Ferma Worker01: `Stop-Process -Name "dotnet" (PID worker01)`
2. Apri Agent Details (Worker01)
3. Click su "📋 Mostra Log"

**Risultato atteso:**
```
Errore nel caricamento dei log:
No connection could be made because the target machine actively refused it.
```

**Test 4.2: Endpoint /logs non esistente**

**Passi:**
1. Crea un agente mock senza endpoint /logs
2. Prova a caricare i log

**Risultato atteso:**
```
Errore nel caricamento dei log: HTTP 404
```

---

## Architettura

```
┌─────────────────────────────────────────────────────────┐
│                  Control Center UI                       │
├─────────────────────────────────────────────────────────┤
│                                                           │
│  ┌───────────────────────────────────────────────┐      │
│  │       AgentDetailWindow (View)                 │      │
│  │                                                 │      │
│  │  [📋 Mostra Log] [🔄 Aggiorna Log]            │      │
│  │                                                 │      │
│  │  ┌─────────────────────────────────────────┐  │      │
│  │  │  LogsSection (Collapsed by default)     │  │      │
│  │  │                                          │  │      │
│  │  │  [Caricamento log...] ← IsLoadingLogs   │  │      │
│  │  │                                          │  │      │
│  │  │  ┌────────────────────────────────────┐ │  │      │
│  │  │  │ [HH:mm:ss] [INFO ] Message 1       │ │  │      │
│  │  │  │ [HH:mm:ss] [INFO ] Message 2       │ │  │      │
│  │  │  │ [HH:mm:ss] [ERROR] Message 3       │ │  │      │
│  │  │  └────────────────────────────────────┘ │  │      │
│  │  │                                          │  │      │
│  │  │  Totale eventi: 3 | Auto-refresh: True  │  │      │
│  │  │                                          │  │      │
│  │  │  ☑ Abilita auto-refresh (ogni 5 sec)   │  │      │
│  │  └─────────────────────────────────────────┘  │      │
│  └───────────────────────────────────────────────┘      │
│                          │                                │
│                          ▼                                │
│  ┌───────────────────────────────────────────────┐      │
│  │    AgentDetailViewModel (ViewModel)           │      │
│  │                                                 │      │
│  │  Properties:                                   │      │
│  │  - IsLoadingLogs                               │      │
│  │  - LogsText                                    │      │
│  │  - LogCount                                    │      │
│  │  - IsAutoRefreshEnabled                        │      │
│  │                                                 │      │
│  │  Methods:                                      │      │
│  │  - LoadLogsAsync()                             │      │
│  │    └─ GET http://localhost:{port}/logs        │      │
│  └───────────────────────────────────────────────┘      │
│                          │                                │
│                          ▼                                │
│  ┌───────────────────────────────────────────────┐      │
│  │       DispatcherTimer (Auto-refresh)          │      │
│  │                                                 │      │
│  │  Interval: 5 seconds                           │      │
│  │  Tick: LoadLogsAsync()                         │      │
│  │  Start: When IsAutoRefreshEnabled = true      │      │
│  │  Stop: When IsAutoRefreshEnabled = false      │      │
│  └───────────────────────────────────────────────┘      │
│                                                           │
└─────────────────────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────┐
│                    Agenti (Backend)                      │
├─────────────────────────────────────────────────────────┤
│                                                           │
│  GET http://localhost:5001/logs  (Orchestrator)         │
│  GET http://localhost:5002/logs  (Worker01)             │
│  GET http://localhost:5003/logs  (Worker02)             │
│  GET http://localhost:5005/logs  (IndigoAiWorker01)     │
│                                                           │
│  Response:                                               │
│  {                                                       │
│    "Success": true,                                      │
│    "Count": 3,                                           │
│    "Logs": [                                             │
│      {                                                   │
│        "Timestamp": "2026-01-01T12:32:45.123Z",         │
│        "Level": "INFO",                                  │
│        "Message": "Task ricevuto: optimize-prompt"      │
│      }                                                   │
│    ]                                                     │
│  }                                                       │
│                                                           │
└─────────────────────────────────────────────────────────┘
```

---

## File Modificati

| File | Modifiche | Descrizione |
|------|-----------|-------------|
| `AgentDetailWindow.xaml` | **Modificato** | Aggiunta sezione log UI |
| `AgentDetailWindow.xaml.cs` | **Modificato** | Metodi per toggle, refresh, auto-refresh |
| `AgentDetailViewModel.cs` | **Modificato** | Proprietà log + LoadLogsAsync() |

**Totale**: 3 file modificati

---

## Caratteristiche

| Feature | Status | Descrizione |
|---------|--------|-------------|
| **Mostra/Nascondi Log** | ✅ | Toggle visibilità sezione |
| **Carica Log** | ✅ | GET /logs dall'agente |
| **Aggiorna Log** | ✅ | Refresh manuale |
| **Auto-refresh** | ✅ | Ogni 5 secondi |
| **Formato Log** | ✅ | [HH:mm:ss] [LEVEL] Message |
| **Contatore Eventi** | ✅ | Mostra numero log |
| **Loading Indicator** | ✅ | Feedback durante caricamento |
| **Gestione Errori** | ✅ | HTTP errors e exceptions |
| **Font Monospace** | ✅ | Consolas per leggibilità |
| **Scrollbar** | ✅ | Verticale e orizzontale |
| **Cleanup Timer** | ✅ | Stop al chiudi finestra |

---

## Benefici

1. ✅ **Monitoring Real-time**: Visualizza log agenti in tempo reale
2. ✅ **Debugging**: Traccia eventi e errori direttamente dalla UI
3. ✅ **Auto-refresh**: Aggiornamento automatico ogni 5 secondi
4. ✅ **User-Friendly**: Toggle mostra/nascondi, refresh manuale
5. ✅ **Formato Chiaro**: Timestamp, livello, messaggio
6. ✅ **Performance**: Caricamento on-demand, timer gestito
7. ✅ **Robustezza**: Gestione errori e timeout

---

## Troubleshooting

### Problema: "Nessun log disponibile"

**Causa**: L'agente non ha ancora ricevuto task  
**Soluzione**: Dispatch un task e ricarica i log

### Problema: "Errore nel caricamento dei log: HTTP 404"

**Causa**: Endpoint /logs non implementato  
**Soluzione**: Verifica che l'agente abbia endpoint GET /logs

### Problema: Log non si aggiornano con auto-refresh

**Causa**: Checkbox non abilitata o sezione log nascosta  
**Soluzione**: 
1. Mostra i log ("📋 Mostra Log")
2. Abilita "☑ Abilita auto-refresh"

### Problema: "Errore: No connection could be made"

**Causa**: Agente offline  
**Soluzione**: Riavvia l'agente con `dotnet run`

---

## Prossimi Miglioramenti

### 1. Filtro per Level

```xml
<ComboBox SelectedItem="{Binding LogLevelFilter}">
    <ComboBoxItem Content="Tutti"/>
    <ComboBoxItem Content="INFO"/>
    <ComboBoxItem Content="WARN"/>
    <ComboBoxItem Content="ERROR"/>
</ComboBox>
```

### 2. Ricerca nel Log

```xml
<TextBox PlaceholderText="Cerca nei log..." 
         Text="{Binding SearchText, UpdateSourceTrigger=PropertyChanged}"/>
```

### 3. Export Log

```csharp
[RelayCommand]
private void ExportLogs()
{
    var dialog = new SaveFileDialog();
    dialog.Filter = "Text Files (*.txt)|*.txt";
    if (dialog.ShowDialog() == true)
    {
        File.WriteAllText(dialog.FileName, LogsText);
    }
}
```

### 4. Clear Log Buffer

```csharp
[RelayCommand]
private async Task ClearLogsAsync()
{
    // Chiama endpoint POST /logs/clear
    await httpClient.PostAsync($"{agentUrl}/logs/clear", null);
    await LoadLogsAsync();
}
```

---

## Conclusione

La funzionalità di visualizzazione log nel Control Center UI è stata **implementata con successo**! 🎉

**Caratteristiche principali:**
- ✅ Visualizzazione log real-time
- ✅ Auto-refresh opzionale
- ✅ Formato chiaro e leggibile
- ✅ Gestione errori robusta

**L'applicazione è pronta per il testing!** 🚀

---

**Control Center UI** - Agent Logs Feature v1.0
