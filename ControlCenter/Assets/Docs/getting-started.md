# Getting Started

Benvenuto in **Indigo Control Center**, l'interfaccia grafica per il Bootstrapper Engine.

## Prerequisiti

- Windows 10/11
- .NET 8 Runtime
- Bootstrapper Engine installato e configurato

## Primo avvio

1. Avvia il **Bootstrapper Engine** con il comando `serve`
2. Apri **Indigo Control Center**
3. Verifica la connessione dalla Dashboard

## Comandi disponibili

- **Provision**: Prepara i server remoti (Docker, WSL2, firewall)
- **Build Cluster**: Genera il docker-compose.yaml
- **Generate Agents**: Crea i progetti .NET 8 degli agenti
- **Configure Communication**: Configura RabbitMQ, Redis, JWT
- **Validate**: Testa connettività e health degli agenti
- **Deploy**: Esegue l'intero workflow

## Risoluzione problemi

Se il Control Center non si connette al Bootstrapper, verifica che:
- Il Bootstrapper sia in esecuzione con il comando `serve`
- La porta 5000 sia disponibile
- Il firewall non blocchi la connessione locale
