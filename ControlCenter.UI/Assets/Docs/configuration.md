# Configurazione

Il Bootstrapper utilizza file YAML per configurare server e cluster.

## servers.yaml

Definisce i server remoti da configurare:

```yaml
servers:
  - name: "Server01"
    hostname: "192.168.1.100"
    username: "admin"
    password: "SecurePassword123!"
    install_docker: true
    install_wsl2: true

  - name: "Server02"
    hostname: "192.168.1.101"
    username: "admin"
    password: "SecurePassword123!"
    install_docker: true
    install_wsl2: false
```

### Parametri

- `name`: Nome identificativo del server
- `hostname`: IP o hostname del server
- `username`: Username per WinRM
- `password`: Password per WinRM
- `install_docker`: Installa Docker Desktop (Windows 10/11 Pro)
- `install_wsl2`: Installa WSL2 (richiede reboot)

## cluster.yaml

Definisce la configurazione del cluster:

```yaml
cluster_name: "indigo-cluster"

agents:
  - name: "agent-orchestrator"
    type: "orchestrator"
    port: 5001
    environment:
      LOG_LEVEL: "Information"

  - name: "agent-worker-01"
    type: "worker"
    port: 5002
    environment:
      LOG_LEVEL: "Debug"

network:
  subnet: "172.20.0.0/16"
  traefik:
    enabled: true
    dashboard_port: 8080

communication:
  message_broker: "RabbitMQ"
  cache_provider: "Redis"
  jwt:
    issuer: "IndigoLab"
    audience: "IndigoAgents"
    expiry_minutes: 60
```

### Sezioni

- **agents**: Lista degli agenti da generare
- **network**: Configurazione rete Docker
- **communication**: Message broker, cache, JWT

## Best Practices

1. **Non committare credenziali**: Aggiungi `servers.yaml` al `.gitignore`
2. **Usa password complesse**: Minimo 12 caratteri con maiuscole, minuscole, numeri
3. **Testa connettività**: Usa `Test-WSMan` prima di eseguire provisioning
4. **Backup configurazioni**: Mantieni copie dei file YAML
