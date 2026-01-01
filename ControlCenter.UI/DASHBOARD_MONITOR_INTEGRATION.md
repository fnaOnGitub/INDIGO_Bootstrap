# Dashboard - Monitor Integration

Integrazione della Dashboard del Control Center con Agent.Monitor per visualizzare lo stato reale del cluster.

## Modifiche Implementate

### 1. Nuovi File Creati

#### **Models/AgentStatusItem.cs**
Modello per rappresentare lo stato di un agente nella Dashboard:
```csharp
- string Name          // Nome agente (es. "orchestrator")
- string Url           // URL agente (es. "http://localhost:5001")
- string Status        // Status (es. "orchestrator")
- string Uptime        // Uptime (es. "00:15:30")
- string LastTask      // Ultimo task eseguito
- bool IsHealthy       // Indica se l'agente è online
- string Version       // Versione agente
```

#### **Services/MonitorService.cs**
Servizio per comunicare con Agent.Monitor (porta 5004):
```csharp
- GetClusterStatusAsync()  → GET http://localhost:5004/cluster/status
- GetClusterHealthAsync()  → GET http://localhost:5004/cluster/health
- CheckMonitorHealthAsync() → GET http://localhost:5004/health
```

**Modelli di risposta:**
- `ClusterStatusResponse`: Success, Timestamp, List<AgentStatusInfo>
- `AgentStatusInfo`: Name, Url, AgentStatusData
- `AgentStatusData`: Agent, Uptime, Version, LastCommand, LastTask

#### **Converters/ZeroToVisibilityConverter.cs**
Converter per mostrare un messaggio quando non ci sono agenti:
- 0 → Visibility.Visible
- > 0 → Visibility.Collapsed

### 2. File Modificati

#### **ViewModels/DashboardViewModel.cs**

**Nuove proprietà:**
```csharp
- ObservableCollection<AgentStatusItem> Agents
- bool IsLoading
- DateTime LastUpdated
- bool IsMonitorConnected
```

**Nuovi metodi:**
```csharp
- RefreshClusterAsync()  // Chiama Monitor e popola Agents
- RefreshCommand         // Comando per refresh manuale
```

**Funzionamento:**
1. Timer automatico ogni 5 secondi chiama `RefreshClusterAsync()`
2. `RefreshClusterAsync()` chiama `MonitorService.GetClusterStatusAsync()`
3. I dati JSON dal Monitor vengono deserializzati in `ClusterStatusResponse`
4. Per ogni agente nel JSON, viene creato un `AgentStatusItem` e aggiunto a `Agents`
5. `LastUpdated` viene aggiornato con l'ora corrente

#### **Views/DashboardPage.xaml**

**Nuova sezione "Stato Cluster in Tempo Reale":**

1. **Header con Refresh:**
   - Titolo: "Stato Cluster in Tempo Reale"
   - Last Updated: mostra timestamp ultimo aggiornamento
   - Pulsante "⟳ Refresh": refresh manuale

2. **Indicatore di caricamento:**
   - ProgressBar indeterminato quando `IsLoading = true`

3. **Lista Agenti:**
   - ItemsControl con DataTemplate personalizzato
   - Per ogni agente mostra:
     - **Indicatore Health**: pallino verde (online) o rosso (offline)
     - **Nome + URL**: nome agente e URL completo
     - **Status**: nome identificativo agente
     - **Uptime**: tempo di attività (formato hh:mm:ss)
     - **Last Task**: ultimo task/comando eseguito

4. **Messaggio quando nessun agente:**
   - Visibile solo quando `Agents.Count == 0`
   - Indica all'utente di verificare che Monitor sia avviato

#### **App.xaml**
Registrato `ZeroToVisibilityConverter` nelle risorse globali.

---

## Flusso di Dati

```
Timer (5s) → DashboardViewModel.RefreshClusterAsync()
    ↓
MonitorService.GetClusterStatusAsync()
    ↓
HTTP GET http://localhost:5004/cluster/status
    ↓
JSON Response con lista agenti
    ↓
Deserializzazione in ClusterStatusResponse
    ↓
Mapping a ObservableCollection<AgentStatusItem>
    ↓
UI aggiornata automaticamente (binding WPF)
```

---

## Esempio JSON Visualizzato

### Request
```
GET http://localhost:5004/cluster/status
```

### Response
```json
{
  "Success": true,
  "Timestamp": "2026-01-01T11:06:15.1572088Z",
  "Agents": [
    {
      "Name": "orchestrator",
      "Url": "http://localhost:5001",
      "Status": {
        "Agent": "orchestrator",
        "Uptime": "00:17:35",
        "Version": "1.0.0",
        "LastCommand": "test-loadbalancing-3"
      }
    },
    {
      "Name": "worker01",
      "Url": "http://localhost:5002",
      "Status": {
        "Agent": "worker01",
        "Uptime": "00:35:00",
        "Version": "1.0.0",
        "LastTask": "test-loadbalancing-3"
      }
    },
    {
      "Name": "worker02",
      "Url": "http://localhost:5003",
      "Status": {
        "Agent": "worker02",
        "Uptime": "00:22:29",
        "Version": "1.0.0",
        "LastTask": "test-loadbalancing-2"
      }
    }
  ]
}
```

### Visualizzazione in UI

```
┌─────────────────────────────────────────────────────────────────────────┐
│ Stato Cluster in Tempo Reale              Aggiornato: 11:06:15  [⟳ Refresh] │
├─────────────────────────────────────────────────────────────────────────┤
│ ● orchestrator                     orchestrator    00:17:35    test-lo... │
│   http://localhost:5001                                                 │
├─────────────────────────────────────────────────────────────────────────┤
│ ● worker01                         worker01        00:35:00    test-lo... │
│   http://localhost:5002                                                 │
├─────────────────────────────────────────────────────────────────────────┤
│ ● worker02                         worker02        00:22:29    test-lo... │
│   http://localhost:5003                                                 │
└─────────────────────────────────────────────────────────────────────────┘
```

**Legenda:**
- ● Verde = Agente online e healthy
- ● Rosso = Agente offline o non raggiungibile

---

## Configurazione

### Prerequisiti
- **Agent.Monitor** deve essere in esecuzione su porta 5004
- **Agent.Orchestrator** su porta 5001
- **Agent.Worker01** su porta 5002
- **Agent.Worker02** su porta 5003

### Avvio Cluster Completo
```bash
# 1. Avvia Monitor
cd Agent.Monitor
dotnet run

# 2. Avvia Orchestrator
cd Agent.Orchestrator
dotnet run

# 3. Avvia Worker01
cd Agent.Worker01
dotnet run

# 4. Avvia Worker02
cd Agent.Worker02
dotnet run

# 5. Avvia Control Center UI
cd ControlCenter.UI
dotnet run
```

### Verifica Manuale
```bash
# Test endpoint Monitor
curl http://localhost:5004/cluster/status

# Test endpoint Monitor (health)
curl http://localhost:5004/cluster/health
```

---

## Funzionalità

### ✅ Implementate
- Visualizzazione stato cluster in tempo reale
- Refresh automatico ogni 5 secondi
- Refresh manuale con pulsante
- Indicatore Last Updated
- Indicatore di caricamento
- Lista agenti con health status
- Gestione errori (Monitor offline)

### 🔄 Future Enhancement
- Click su agente per aprire dettagli
- Filtraggio agenti per tipo
- Storico uptime/downtime
- Alert quando agente va offline
- Export dati in CSV/JSON
- Grafici di utilizzo

---

## Note Tecniche

1. **Threading WPF**: 
   - `Dispatcher.InvokeAsync` usato per aggiornare `ObservableCollection` da thread background
   
2. **JSON Serialization**:
   - `PropertyNameCaseInsensitive = true` per gestire PascalCase dal Monitor
   
3. **Polling**:
   - Timer System.Threading.Timer con intervallo 5s
   - Timer viene disposto correttamente in `DashboardViewModel.Dispose()`

4. **Gestione Errori**:
   - Se Monitor non è raggiungibile, viene mostrato messaggio
   - La UI non si blocca se un agente è offline

---

## Testing

### Test Scenario 1: Tutti gli agenti online
1. Avvia tutti gli agenti
2. Apri Control Center UI → Dashboard
3. Verifica che tutti gli agenti siano mostrati con pallino verde
4. Verifica uptime crescente

### Test Scenario 2: Un agente offline
1. Avvia solo Orchestrator e Worker01
2. Worker02 rimane spento
3. Verifica che Worker02 non appaia nella lista (o appaia come offline)

### Test Scenario 3: Monitor offline
1. Spegni Agent.Monitor
2. Verifica messaggio "Monitor non raggiungibile"
3. Riavvia Monitor
4. Verifica che dopo 5s (o refresh manuale) gli agenti riappaiano

---

## Troubleshooting

**Problema: Nessun agente viene visualizzato**
- Verifica che Agent.Monitor sia in esecuzione su porta 5004
- Verifica con `curl http://localhost:5004/cluster/status`
- Controlla log Debug Output in Visual Studio

**Problema: Uptime non si aggiorna**
- Verifica che il timer sia attivo (5s refresh)
- Clicca manualmente su "Refresh"
- Verifica `LastUpdated` timestamp

**Problema: JSON deserializzazione fallisce**
- Verifica che Monitor restituisca PascalCase
- Verifica `PropertyNameCaseInsensitive = true` in MonitorService
- Controlla log eccezioni

---

## File Summary

| File | Tipo | Descrizione |
|------|------|-------------|
| `Models/AgentStatusItem.cs` | Nuovo | Modello agente per UI |
| `Services/MonitorService.cs` | Nuovo | Client HTTP per Monitor |
| `Converters/ZeroToVisibilityConverter.cs` | Nuovo | Converter WPF |
| `ViewModels/DashboardViewModel.cs` | Modificato | Logica Dashboard + Monitor |
| `Views/DashboardPage.xaml` | Modificato | UI Dashboard con lista agenti |
| `App.xaml` | Modificato | Registrazione converter |

---

## Conclusione

La Dashboard ora mostra lo stato reale del cluster IndigoLab in tempo reale, collegandosi direttamente ad Agent.Monitor. Gli agenti vengono visualizzati con health status, uptime e ultimo task eseguito, con refresh automatico ogni 5 secondi.
