# Agent.Orchestrator

Micro-servizio .NET 8 che funge da agente orchestratore per il cluster IndigoLab.

## Descrizione

L'Agent.Orchestrator è un servizio HTTP minimale che espone endpoint per:
- Health check
- Monitoraggio stato
- Ricezione e dispatch di task

## Porta

L'agente ascolta su: **http://localhost:5001**

## Endpoints

### GET /health
Health check dell'agente.

**Risposta:**
```json
{
  "status": "OK",
  "timestamp": "2026-01-01T10:15:00.000Z"
}
```

### GET /status
Informazioni dettagliate sullo stato dell'agente.

**Risposta:**
```json
{
  "agent": "orchestrator",
  "uptime": "01:23:45",
  "version": "1.0.0",
  "lastCommand": "deploy-workflow"
}
```

### POST /dispatch
Riceve un task da eseguire.

**Richiesta:**
```json
{
  "task": "deploy-workflow",
  "payload": "{ \"step\": 1 }"
}
```

**Risposta:**
```json
{
  "success": true,
  "message": "Task dispatched",
  "receivedTask": "deploy-workflow",
  "timestamp": "2026-01-01T10:15:00.000Z"
}
```

## Avvio

### Modalità Development

```bash
cd Agent.Orchestrator
dotnet run
```

L'agente si avvierà su **http://localhost:5001**

### Swagger UI

Documentazione interattiva disponibile su:
```
http://localhost:5001/swagger
```

## Test rapido

```bash
# Health check
curl http://localhost:5001/health

# Status
curl http://localhost:5001/status

# Dispatch task
curl -X POST http://localhost:5001/dispatch \
  -H "Content-Type: application/json" \
  -d '{"task":"test-task","payload":"test-payload"}'
```

## Struttura Progetto

```
Agent.Orchestrator/
├── Program.cs              # Entry point con configurazione endpoint
├── AgentState.cs          # Gestione stato agente (uptime, last command)
├── appsettings.json       # Configurazione
└── README.md              # Questo file
```

## Log

L'agente utilizza logging console con output dettagliato:
- Avvio agente
- Health check richiesti
- Status richiesti
- Task ricevuti con payload

## Tecnologie

- .NET 8
- ASP.NET Core Minimal API
- Swagger/OpenAPI
