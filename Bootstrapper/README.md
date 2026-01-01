# INDIGO BOOTSTRAPPER

Sistema di provisioning e orchestrazione per cluster di agenti .NET 8 distribuiti.

## Descrizione

**INDIGO BOOTSTRAPPER** è un motore CLI .NET 8 che automatizza:
- Provisioning di server Windows remoti (Docker, WSL2, firewall, utenti)
- Generazione di cluster containerizzati con docker-compose
- Creazione di agenti .NET 8 Minimal API da template
- Configurazione di sistemi di comunicazione (RabbitMQ/Kafka, Redis)
- Validazione completa dell'infrastruttura

## Stack Tecnologico

- **.NET 8 Console Application**
- **PowerShell 7** + **WinRM/PowerShell Remoting**
- **Docker Desktop** + **WSL2**
- **Scriban** (template engine)
- **YamlDotNet** (configurazione)
- **Serilog** (logging)
- **Docker.DotNet** (gestione container)

## Struttura Progetto

```
Bootstrapper/
├── Program.cs                      # Entry point applicazione
├── Bootstrapper.csproj             # Progetto .NET 8
├── Modules/                        # Moduli funzionali
│   ├── RemoteExecutor.cs          # PowerShell Remoting
│   ├── ProvisioningEngine.cs      # Provisioning server
│   ├── ClusterBuilder.cs          # Generazione docker-compose
│   ├── AgentGenerator.cs          # Generazione agenti
│   ├── CommunicationConfigurator.cs # Setup RabbitMQ/Redis/JWT
│   ├── ValidationSuite.cs         # Test e validazione
│   └── LocalApiServer.cs          # API HTTP per Control Center
├── Templates/                      # Template per generazione
│   ├── agent-template/            # Template agente base
│   │   ├── Program.cs
│   │   ├── Dockerfile
│   │   └── appsettings.json
│   └── docker-compose.yaml.scriban
├── Config/                         # Configurazioni YAML
│   ├── servers.yaml               # Configurazione server
│   └── cluster.yaml               # Configurazione cluster
└── Logs/                          # Log Serilog
```

## Installazione

### Prerequisiti

- Windows 10/11 o Windows Server
- .NET 8 SDK
- PowerShell 7
- Docker Desktop (opzionale per sviluppo locale)

### Setup

1. **Clone o scarica il progetto**

2. **Ripristina dipendenze NuGet**
   ```powershell
   dotnet restore
   ```

3. **Build del progetto**
   ```powershell
   dotnet build
   ```

4. **Configura server e cluster**
   
   Modifica i file in `Config/`:
   - `servers.yaml`: server remoti da provisionare
   - `cluster.yaml`: definizione agenti e rete

## Utilizzo

### Comandi CLI

```powershell
# Provisioning server remoti
Bootstrapper.exe provision

# Genera docker-compose e configurazioni
Bootstrapper.exe build-cluster

# Genera progetti agenti da template
Bootstrapper.exe generate-agents

# Configura comunicazione (RabbitMQ, Redis, JWT)
Bootstrapper.exe configure-communication

# Valida cluster completo
Bootstrapper.exe validate

# Workflow completo (tutti i comandi in sequenza)
Bootstrapper.exe deploy

# Modalità server (API HTTP su localhost:5000)
Bootstrapper.exe serve
```

### Workflow Tipico

1. **Configura** `Config/servers.yaml` e `Config/cluster.yaml`

2. **Esegui deploy completo**:
   ```powershell
   Bootstrapper.exe deploy
   ```

3. **Output generato** in `output/`:
   - `docker-compose.yaml`
   - Progetti agenti generati
   - Script di avvio
   - Configurazioni RabbitMQ/Redis/JWT

4. **Avvia cluster**:
   ```powershell
   cd output
   .\start-cluster.ps1
   ```

5. **Valida deployment**:
   ```powershell
   Bootstrapper.exe validate
   ```

## Configurazione

### servers.yaml

Definisce i server remoti da provisionare:

```yaml
servers:
  - name: server-01
    hostname: 192.168.1.100
    username: Administrator
    password: P@ssw0rd123
    installDocker: true
    installWsl2: true
```

### cluster.yaml

Definisce cluster, agenti e comunicazione:

```yaml
clusterName: indigo-cluster

agents:
  - name: agent-orchestrator
    type: orchestrator
    port: 5001
    environment:
      LOG_LEVEL: Information

  - name: agent-worker-01
    type: worker
    port: 5002
    environment:
      LOG_LEVEL: Information

network:
  subnetCidr: 172.20.0.0/16
  useTraefik: true

communication:
  messageBroker: RabbitMQ
  cacheProvider: Redis
  useJwt: true
```

## Moduli

### RemoteExecutor
Gestisce sessioni PowerShell Remoting (WinRM) per eseguire comandi su server remoti.

**Funzionalità:**
- Creazione sessioni remote persistenti
- Esecuzione comandi PowerShell
- Test connettività WinRM
- Copia file su server remoti

### ProvisioningEngine
Provisioning automatizzato di server Windows.

**Funzionalità:**
- Installazione Docker Desktop
- Installazione WSL2
- Configurazione firewall
- Setup utenti e permessi
- Installazione PowerShell 7

### ClusterBuilder
Generazione configurazioni Docker Compose.

**Funzionalità:**
- Rendering template Scriban
- Generazione docker-compose.yaml
- Configurazione reti e volumi
- Setup Traefik reverse proxy
- Script di avvio cluster

### AgentGenerator
Generazione progetti agenti .NET 8 da template.

**Funzionalità:**
- Clonazione da template base
- Generazione .csproj personalizzati
- Dockerfile per ogni agente
- Configurazione Serilog
- Health check endpoints

### CommunicationConfigurator
Setup infrastruttura di comunicazione.

**Funzionalità:**
- Configurazione RabbitMQ (code, exchange, binding)
- Setup Redis (caching, pub/sub)
- Generazione JWT secret
- Documentazione API interne

### ValidationSuite
Suite completa di test e validazione.

**Funzionalità:**
- Test connettività WinRM
- Verifica Docker e container
- Test API agenti (health, metadata)
- Validazione RabbitMQ e Redis
- Test connettività tra agenti
- Heartbeat monitoring

### LocalApiServer
API HTTP locale per integrazione con WinUI 3 Control Center.

**Endpoints:**
- `GET /api/status` - Stato cluster
- `GET /api/logs` - Log recenti
- `POST /api/command/provision` - Esegui provisioning
- `POST /api/command/build-cluster` - Build cluster
- `POST /api/command/generate-agents` - Genera agenti
- `POST /api/command/configure-communication` - Configura comunicazione
- `POST /api/command/validate` - Valida cluster
- `POST /api/command/deploy` - Deploy completo
- `GET /api/health` - Health check

## Logging

I log vengono scritti in `Logs/` usando Serilog:
- Console output per monitoraggio real-time
- File rolling giornalieri: `Logs/bootstrapper-YYYYMMDD.log`

## Integrazione Control Center

Il Bootstrapper espone API HTTP su `http://localhost:5000` per integrazione con **INDIGO CONTROL CENTER** (WinUI 3).

La Control Center UI può:
- Visualizzare stato cluster in tempo reale
- Eseguire comandi via API
- Monitorare log
- Gestire agenti

## Output Generato

Dopo l'esecuzione, la cartella `output/` contiene:

```
output/
├── docker-compose.yaml           # Configurazione cluster
├── .env                          # Variabili ambiente
├── traefik.yaml                  # Config Traefik
├── start-cluster.ps1             # Script avvio Windows
├── start-cluster.sh              # Script avvio Linux
├── rabbitmq-config.md            # Documentazione RabbitMQ
├── redis-config.md               # Documentazione Redis
├── jwt-config.md                 # Configurazione JWT
├── .jwt-secret                   # JWT secret key
├── internal-api-config.md        # API interne
├── test-communication.md         # Script di test
├── agent-orchestrator/           # Progetto agente
│   ├── Program.cs
│   ├── agent-orchestrator.csproj
│   ├── Dockerfile
│   ├── appsettings.json
│   └── README.md
├── agent-worker-01/
└── ...
```

## Troubleshooting

### Errore WinRM Connection
- Verifica che WinRM sia abilitato sul server target:
  ```powershell
  Enable-PSRemoting -Force
  ```
- Controlla firewall (porta 5985)

### Docker non trovato
- Installa Docker Desktop
- Riavvia sistema dopo installazione WSL2

### Container non si avviano
- Verifica port binding (nessun conflitto porte)
- Controlla log Docker: `docker-compose logs`

### Validazione fallita
- Esegui `Bootstrapper.exe validate` per dettagli
- Controlla log in `Logs/`

## Requisiti Server Remoti

Per il provisioning remoto:
- Windows 10/11 o Windows Server 2019+
- WinRM abilitato
- Account amministratore
- Connessione di rete al server

## Licenza

Progetto IndigoLab - Internal Use

## Contatti

Per supporto tecnico, consultare la documentazione IndigoLab interna.

---

**Versione**: 1.0.0  
**Ultima modifica**: 2025-12-31
