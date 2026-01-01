# Agent.Worker01

Micro-servizio .NET 8 che esegue task inviati dall'Orchestrator nel cluster IndigoLab.

## Descrizione

L'Agent.Worker01 è un worker service HTTP minimale che espone endpoint per:
- Health check
- Monitoraggio stato
- Esecuzione di task ricevuti

## Porta

L'agente ascolta su: **http://localhost:5002**

## Endpoints

### GET /health
Health check dell'agente.

**Risposta:**
```json
{
  "status": "OK",
  "timestamp": "2026-01-01T10:20:00.000Z"
}
```

### GET /status
Informazioni dettagliate sullo stato dell'agente worker.

**Risposta:**
```json
{
  "agent": "worker01",
  "uptime": "01:23:45",
  "version": "1.0.0",
  "lastTask": "process-data"
}
```

### POST /execute
Esegue un task ricevuto dall'orchestrator.

**Richiesta:**
```json
{
  "task": "process-data",
  "payload": "{ \"data\": [1, 2, 3] }"
}
```

**Risposta:**
```json
{
  "success": true,
  "message": "Task executed",
  "result": "Task 'process-data' completato con successo. Payload processato: 23 caratteri.",
  "executedTask": "process-data",
  "timestamp": "2026-01-01T10:20:00.000Z"
}
```

## Comportamento

- Il worker simula l'esecuzione di un task con un delay di **500ms**
- Ogni task ricevuto aggiorna il campo `lastTask` nello stato
- Tutti i task e i risultati sono loggati in console

## Avvio

### Modalità Development

```bash
cd Agent.Worker01
dotnet run
```

L'agente si avvierà su **http://localhost:5002**

### Swagger UI

Documentazione interattiva disponibile su:
```
http://localhost:5002/swagger
```

## Test rapido

```bash
# Health check
curl http://localhost:5002/health

# Status
curl http://localhost:5002/status

# Execute task
curl -X POST http://localhost:5002/execute \
  -H "Content-Type: application/json" \
  -d '{"task":"compute","payload":"test-data"}'
```

## Struttura Progetto

```
Agent.Worker01/
├── Program.cs              # Entry point con configurazione endpoint
├── WorkerState.cs         # Gestione stato worker (uptime, last task)
├── appsettings.json       # Configurazione
└── README.md              # Questo file
```

## Log

L'agente utilizza logging console con output dettagliato:
- Avvio worker
- Health check richiesti
- Status richiesti
- Task ricevuti e completati con dettagli payload

## Integrazione con Orchestrator

Questo worker è progettato per ricevere task dall'Agent.Orchestrator (porta 5001) e eseguirli in modo indipendente.

## Tecnologie

- .NET 8
- ASP.NET Core Minimal API
- Swagger/OpenAPI
- Task async/await per simulazione esecuzione
