# Indigo Control Center (WPF)

## Descrizione
Control Center è l'interfaccia grafica WPF per la gestione del cluster distribuito IndigoLab. Permette di orchestrare il provisioning dei server, la generazione degli agenti, la configurazione della comunicazione e il deployment del cluster completo.

## Tecnologie
- **.NET 8** - Framework principale
- **WPF** - Framework UI (nessun WinUI 3, nessun WindowsAppSDK)
- **MVVM Toolkit** - CommunityToolkit.Mvvm per pattern MVVM
- **HttpClient** - Comunicazione con Bootstrapper LocalApiServer

## Architettura
Il Control Center comunica **solo** tramite HTTP con il **Bootstrapper LocalApiServer** (`http://localhost:5000`).

### Struttura Progetto
```
ControlCenter.UI/
├── Models/              # Data models
├── ViewModels/          # MVVM ViewModels
├── Views/               # XAML Views e Windows
├── Services/            # HTTP client e servizi
├── Converters/          # Value converters WPF
└── Assets/              # Risorse (docs, immagini)
```

### Pagine Disponibili
1. **DashboardPage** - Stato cluster, azioni rapide, log viewer
2. **AgentsPage** - Lista agenti con dettagli
3. **DocumentationPage** - Visualizzazione documenti markdown
4. **AboutPage** - Informazioni sull'applicazione

### Finestre Modali
- **AgentDetailWindow** - Dettagli di un agente specifico
- **DocumentViewerWindow** - Visualizzazione file markdown

## Compilazione
```bash
dotnet build ControlCenter.UI.csproj
```

## Esecuzione
```bash
dotnet run --project ControlCenter.UI.csproj
```

## Requisiti
- .NET 8 SDK
- Windows 10/11 con desktop environment

## API Endpoints (Bootstrapper)
Il Control Center comunica con questi endpoint del Bootstrapper:

- `GET /api/status` - Stato del cluster
- `GET /api/logs` - Log sistema
- `POST /api/command/provision` - Provisioning server
- `POST /api/command/build-cluster` - Generazione docker-compose
- `POST /api/command/generate-agents` - Generazione agenti
- `POST /api/command/validate` - Validazione cluster
- `POST /api/command/deploy` - Deployment completo

## Note
Questo progetto **NON include**:
- WindowsAppSDK
- WinUI 3
- XAML Compiler esterno (solo WPF standard)
- Logica di provisioning/build (delegata al Bootstrapper)

La UI è stata migrata da WinUI 3 a WPF .NET 8 per eliminare complessità e problemi di compilazione.
