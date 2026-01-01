# Agent.Monitor

Micro-servizio .NET 8 per monitorare lo stato del cluster IndigoLab.

## Descrizione

Agent.Monitor è un agente di monitoraggio che raccoglie e aggrega informazioni sullo stato di tutti gli agenti nel cluster (Orchestrator, Worker01, Worker02).

## Endpoint

### GET /health
Controlla lo stato di salute del Monitor stesso.

**Risposta:**
```json
{
  "Status": "OK",
  "Timestamp": "2026-01-01T10:00:00.000Z"
}
```

### GET /status
Restituisce informazioni dettagliate sul Monitor (uptime, versione).

**Risposta:**
```json
{
  "Agent": "monitor",
  "Uptime": "01:23:45",
  "Version": "1.0.0"
}
```

### GET /cluster/health
Aggrega lo stato di salute di tutti gli agenti nel cluster.

**Risposta:**
```json
{
  "Success": true,
  "Timestamp": "2026-01-01T10:00:00.000Z",
  "Agents": [
    {
      "Name": "orchestrator",
      "Url": "http://localhost:5001",
      "Health": {
        "Status": "OK",
        "Timestamp": "2026-01-01T10:00:00.000Z"
      }
    },
    {
      "Name": "worker01",
      "Url": "http://localhost:5002",
      "Health": {
        "Status": "OK",
        "Timestamp": "2026-01-01T10:00:00.000Z"
      }
    },
    {
      "Name": "worker02",
      "Url": "http://localhost:5003",
      "Health": {
        "Status": "OK",
        "Timestamp": "2026-01-01T10:00:00.000Z"
      }
    }
  ]
}
```

### GET /cluster/status
Aggrega lo status dettagliato di tutti gli agenti nel cluster.

**Risposta:**
```json
{
  "Success": true,
  "Timestamp": "2026-01-01T10:00:00.000Z",
  "Agents": [
    {
      "Name": "orchestrator",
      "Url": "http://localhost:5001",
      "Status": {
        "Agent": "orchestrator",
        "Uptime": "02:15:30",
        "Version": "1.0.0",
        "LastCommand": "test-task"
      }
    },
    {
      "Name": "worker01",
      "Url": "http://localhost:5002",
      "Status": {
        "Agent": "worker01",
        "Uptime": "02:10:20",
        "Version": "1.0.0",
        "LastTask": "process-data"
      }
    },
    {
      "Name": "worker02",
      "Url": "http://localhost:5003",
      "Status": {
        "Agent": "worker02",
        "Uptime": "01:45:10",
        "Version": "1.0.0",
        "LastTask": "analyze-logs"
      }
    }
  ]
}
```

## Agenti Monitorati

- **Orchestrator** (porta 5001): Coordina i task e distribuisce il carico
- **Worker01** (porta 5002): Esegue task assegnati
- **Worker02** (porta 5003): Esegue task assegnati

## Utilizzo

### Avvio agente
```bash
cd Agent.Monitor
dotnet run
```

L'agente sarà disponibile su `http://localhost:5004`

### Test rapido
```bash
# Health check del Monitor
curl http://localhost:5004/health

# Status del Monitor
curl http://localhost:5004/status

# Health check del cluster
curl http://localhost:5004/cluster/health

# Status del cluster
curl http://localhost:5004/cluster/status
```

## Configurazione

- **Porta**: 5004
- **Versione**: 1.0.0
- **Timeout HttpClient**: 5 secondi
- **Target Framework**: .NET 8.0

## Gestione Errori

Se un agente non è raggiungibile, il Monitor restituisce comunque una risposta aggregata indicando lo stato OFFLINE per quell'agente specifico, senza bloccare l'intera richiesta.

Esempio di agente offline:
```json
{
  "Name": "worker01",
  "Url": "http://localhost:5002",
  "Health": {
    "Status": "OFFLINE",
    "Message": "Connection refused"
  }
}
```
