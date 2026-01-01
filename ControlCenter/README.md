# ControlCenter.Core

## Descrizione
Libreria di classi .NET 8 che contiene la logica di business per il Control Center di IndigoLab.

Questo progetto **NON contiene interfacce grafiche** (nessun XAML), solo:
- Models
- ViewModels (MVVM)
- Services (HTTP client, business logic)

## Tecnologie
- **.NET 8** - Class Library
- **CommunityToolkit.Mvvm** - MVVM toolkit
- **System.Net.Http.Json** - HTTP client con supporto JSON

## Struttura
```
ControlCenter.Core/
├── Models/              # Data models e DTOs
├── ViewModels/          # MVVM ViewModels con ObservableObject
├── Services/            # HTTP client, AgentService, DocumentService
└── Assets/              # Risorse statiche (docs)
```

## Utilizzo
Questo progetto è referenziato da `ControlCenter.UI` (WPF) per fornire la logica di business senza dipendenze UI.

## Compilazione
```bash
dotnet build ControlCenter.csproj
```

## Note
- **Nessun riferimento a WinUI 3 o WindowsAppSDK**
- **Nessun file XAML**
- **Cross-platform** (.NET 8 standard, non Windows-specific)
- Può essere referenziato da qualsiasi UI (WPF, MAUI, Blazor, Console)
