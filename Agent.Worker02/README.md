# Agent.Worker02

Micro-servizio .NET 8 che esegue task inviati dall'Orchestrator nel cluster IndigoLab.

## Descrizione

L'Agent.Worker02 è un worker service HTTP minimale che espone endpoint per:
- Health check
- Monitoraggio stato
- Esecuzione di task ricevuti

## Porta

L'agente ascolta su: **http://localhost:5003**

## Endpoints

### GET /health
Health check dell'agente.

**Risposta:**
```json
{
  "Status": "OK",
  "Timestamp": "2026-01-01T10:30:00.000Z"
}
```

### GET /status
Informazioni dettagliate sullo stato dell'agente worker.

**Risposta:**
```json
{
  "Agent": "worker02",
  "Uptime": "01:23:45",
  "Version": "1.0.0",
  "LastTask": "process-data"
}
```

### POST /execute
Esegue un task ricevuto dall'orchestrator.

**Richiesta:**
```json
{
  "Task": "process-data",
  "Payload": "{ \"data\": [1, 2, 3] }"
}
```

**Risposta:**
```json
{
  "Success": true,
  "Message": "Task executed",
  "Result": "Task 'process-data' completato con successo. Payload processato: 23 caratteri.",
  "ExecutedTask": "process-data",
  "Timestamp": "2026-01-01T10:30:00.000Z"
}
```

## Comportamento

- Il worker simula l'esecuzione di un task con un delay di **500ms**
- Ogni task ricevuto aggiorna il campo `LastTask` nello stato
- Tutti i task e i risultati sono loggati in console
- JSON restituito in **PascalCase**

## Avvio

### Modalità Development

```bash
cd Agent.Worker02
dotnet run
```

L'agente si avvierà su **http://localhost:5003**

### Swagger UI

Documentazione interattiva disponibile su:
```
http://localhost:5003/swagger
```

## Test rapido

```bash
# Health check
curl http://localhost:5003/health

# Status
curl http://localhost:5003/status

# Execute task
curl -X POST http://localhost:5003/execute \
  -H "Content-Type: application/json" \
  -d '{"task":"compute","payload":"test-data"}'
```

## Struttura Progetto

```
Agent.Worker02/
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
- JSON Serialization PascalCase
