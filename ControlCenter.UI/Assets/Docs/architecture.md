# Architettura del Sistema

Il progetto **IndigoLab** è composto da due componenti principali che lavorano insieme per orchestrare un cluster distribuito di agenti .NET.

## Componenti

### 1. Bootstrapper Engine (Backend)

Console Application .NET 8 che gestisce:

- **Provisioning**: Configurazione server remoti via PowerShell Remoting
- **Build**: Generazione docker-compose.yaml con Scriban
- **Generation**: Creazione template agenti .NET 8 Minimal API
- **Configuration**: Setup RabbitMQ, Redis, JWT
- **Validation**: Test di connettività e health check
- **API Server**: Espone endpoint HTTP locali per il Control Center

Tecnologie:
- PowerShell 7 + WinRM
- Docker.DotNet SDK
- Scriban Templates
- YamlDotNet
- Serilog

### 2. Control Center (Frontend)

Desktop Application WinUI 3 che fornisce:

- Dashboard tempo reale del cluster
- Esecuzione comandi remoti
- Visualizzazione log e stati
- Dettagli agenti
- Accesso alla documentazione

Tecnologie:
- WinUI 3
- MVVM (CommunityToolkit.Mvvm)
- HttpClient (comunicazione con Bootstrapper)

## Flusso di Lavoro

```
Control Center (UI)
    ↓ HTTP
LocalApiServer (Bootstrapper)
    ↓
Modules (ProvisioningEngine, ClusterBuilder, etc.)
    ↓ PowerShell Remoting
Remote Servers
    ↓ Docker
Agent Containers (.NET 8)
```

## Comunicazione

Il Control Center **non** accede direttamente a file o server. Tutte le operazioni passano attraverso il `LocalApiServer` del Bootstrapper (porta 5000).

## Sicurezza

- JWT per autenticazione agenti
- WinRM con credenziali configurate
- Docker network isolation
- Firewall rules configurate automaticamente
