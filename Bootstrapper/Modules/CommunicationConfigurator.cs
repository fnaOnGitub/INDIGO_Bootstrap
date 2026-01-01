using Serilog;

namespace Bootstrapper.Modules;

/// <summary>
/// Configura Redis/RabbitMQ, code, topic, API interne, JWT
/// </summary>
public class CommunicationConfigurator
{
    private const string OutputDirectory = "output";

    /// <summary>
    /// Configura il sistema di comunicazione del cluster
    /// </summary>
    public async Task ConfigureCommunication(ClusterConfig config)
    {
        try
        {
            Log.Information("=== CONFIGURAZIONE COMUNICAZIONE ===");

            // Crea directory output
            Directory.CreateDirectory(OutputDirectory);

            // Configura Message Broker (RabbitMQ/Kafka)
            if (config.Communication.MessageBroker == "RabbitMQ")
            {
                await ConfigureRabbitMQ(config);
            }
            else if (config.Communication.MessageBroker == "Kafka")
            {
                await ConfigureKafka(config);
            }

            // Configura Cache Provider (Redis)
            if (config.Communication.CacheProvider == "Redis")
            {
                await ConfigureRedis(config);
            }

            // Configura JWT (se abilitato)
            if (config.Communication.UseJwt)
            {
                await ConfigureJWT(config);
            }

            // Genera configurazione API interne
            await GenerateInternalApiConfig(config);

            // Genera script di test comunicazione
            await GenerateCommunicationTestScript(config);

            Log.Information("Configurazione comunicazione completata con successo");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Errore durante configurazione comunicazione");
            throw;
        }
    }

    /// <summary>
    /// Configura RabbitMQ: code, exchange, binding
    /// </summary>
    private async Task ConfigureRabbitMQ(ClusterConfig config)
    {
        Log.Information("Configurazione RabbitMQ...");

        var rabbitMQConfig = $@"# RabbitMQ Configuration per {config.ClusterName}

## Connection Settings
Host: rabbitmq
Port: 5672
Management UI: http://localhost:15672
Username: admin
Password: admin123

## Exchanges
- indigo.events (topic)
- indigo.commands (direct)
- indigo.logs (fanout)

## Queues per agente:
{string.Join("\n", config.Agents.Select(a => $"- {a.Name}.queue"))}

## Bindings
{string.Join("\n", config.Agents.Select(a => $"- {a.Name}.queue -> indigo.events (routing key: {a.Type}.*)"))}

## Code Examples

### Publish Message (C#)
```csharp
var factory = new ConnectionFactory() {{ HostName = ""rabbitmq"" }};
using var connection = factory.CreateConnection();
using var channel = connection.CreateModel();

channel.ExchangeDeclare(""indigo.events"", ExchangeType.Topic);
var body = Encoding.UTF8.GetBytes(""Hello from {config.ClusterName}"");
channel.BasicPublish(""indigo.events"", ""{config.Agents[0].Type}.event"", null, body);
```

### Consume Messages (C#)
```csharp
var factory = new ConnectionFactory() {{ HostName = ""rabbitmq"" }};
using var connection = factory.CreateConnection();
using var channel = connection.CreateModel();

var consumer = new EventingBasicConsumer(channel);
consumer.Received += (model, ea) => {{
    var body = ea.Body.ToArray();
    var message = Encoding.UTF8.GetString(body);
    Console.WriteLine($""Received: {{message}}"");
}};

channel.BasicConsume(""{config.Agents[0].Name}.queue"", true, consumer);
```
";

        var configPath = Path.Combine(OutputDirectory, "rabbitmq-config.md");
        await File.WriteAllTextAsync(configPath, rabbitMQConfig);

        // Genera script di setup RabbitMQ
        var setupScript = $@"#!/bin/bash
# Script di setup RabbitMQ per {config.ClusterName}

echo ""Configurazione RabbitMQ per {config.ClusterName}...""

# Attendi che RabbitMQ sia pronto
sleep 5

# Crea exchanges
docker exec {config.ClusterName}_rabbitmq rabbitmqadmin declare exchange name=indigo.events type=topic
docker exec {config.ClusterName}_rabbitmq rabbitmqadmin declare exchange name=indigo.commands type=direct
docker exec {config.ClusterName}_rabbitmq rabbitmqadmin declare exchange name=indigo.logs type=fanout

# Crea code per ogni agente
{string.Join("\n", config.Agents.Select(a => $"docker exec {config.ClusterName}_rabbitmq rabbitmqadmin declare queue name={a.Name}.queue durable=true"))}

# Crea bindings
{string.Join("\n", config.Agents.Select(a => $"docker exec {config.ClusterName}_rabbitmq rabbitmqadmin declare binding source=indigo.events destination={a.Name}.queue routing_key={a.Type}.*"))}

echo ""RabbitMQ configurato con successo""
";

        var scriptPath = Path.Combine(OutputDirectory, "setup-rabbitmq.sh");
        await File.WriteAllTextAsync(scriptPath, setupScript);

        Log.Information($"RabbitMQ config generato: {configPath}");
        Log.Information($"RabbitMQ setup script: {scriptPath}");
    }

    /// <summary>
    /// Configura Kafka (placeholder)
    /// </summary>
    private async Task ConfigureKafka(ClusterConfig config)
    {
        Log.Information("Configurazione Kafka...");

        var kafkaConfig = $@"# Kafka Configuration per {config.ClusterName}

## Connection Settings
Bootstrap Servers: localhost:9092

## Topics
{string.Join("\n", config.Agents.Select(a => $"- {a.Name}.events"))}
- indigo.global.events
- indigo.logs

## Consumer Groups
{string.Join("\n", config.Agents.Select(a => $"- {a.Name}.group"))}
";

        var configPath = Path.Combine(OutputDirectory, "kafka-config.md");
        await File.WriteAllTextAsync(configPath, kafkaConfig);

        Log.Information($"Kafka config generato: {configPath}");
    }

    /// <summary>
    /// Configura Redis per caching
    /// </summary>
    private async Task ConfigureRedis(ClusterConfig config)
    {
        Log.Information("Configurazione Redis...");

        var redisConfig = $@"# Redis Configuration per {config.ClusterName}

## Connection Settings
Host: redis
Port: 6379
Database: 0

## Key Patterns
- agent:{{agent_name}}:status
- agent:{{agent_name}}:config
- cluster:{config.ClusterName}:state
- cache:*

## Usage Examples (C#)

### Connection
```csharp
var redis = ConnectionMultiplexer.Connect(""redis:6379"");
var db = redis.GetDatabase();
```

### Set/Get
```csharp
db.StringSet(""agent:{config.Agents[0].Name}:status"", ""active"");
var status = db.StringGet(""agent:{config.Agents[0].Name}:status"");
```

### Pub/Sub
```csharp
var sub = redis.GetSubscriber();
sub.Subscribe(""agent.events"", (channel, message) => {{
    Console.WriteLine($""Received: {{message}}"");
}});

sub.Publish(""agent.events"", ""Hello from {config.ClusterName}"");
```
";

        var configPath = Path.Combine(OutputDirectory, "redis-config.md");
        await File.WriteAllTextAsync(configPath, redisConfig);

        Log.Information($"Redis config generato: {configPath}");
    }

    /// <summary>
    /// Configura JWT per autenticazione
    /// </summary>
    private async Task ConfigureJWT(ClusterConfig config)
    {
        Log.Information("Configurazione JWT...");

        // Genera secret key
        var secretKey = GenerateSecretKey();

        var jwtConfig = $@"# JWT Configuration per {config.ClusterName}

## Settings
Issuer: {config.ClusterName}
Audience: indigo-agents
SecretKey: {secretKey}
TokenLifetime: 60 minutes

## Usage Example (C#)

### Token Generation
```csharp
var tokenHandler = new JwtSecurityTokenHandler();
var key = Encoding.ASCII.GetBytes(""{secretKey}"");

var tokenDescriptor = new SecurityTokenDescriptor
{{
    Subject = new ClaimsIdentity(new[] {{ new Claim(""agent"", ""{config.Agents[0].Name}"") }}),
    Expires = DateTime.UtcNow.AddMinutes(60),
    Issuer = ""{config.ClusterName}"",
    Audience = ""indigo-agents"",
    SigningCredentials = new SigningCredentials(
        new SymmetricSecurityKey(key), 
        SecurityAlgorithms.HmacSha256Signature)
}};

var token = tokenHandler.CreateToken(tokenDescriptor);
var tokenString = tokenHandler.WriteToken(token);
```

### Token Validation
```csharp
var tokenHandler = new JwtSecurityTokenHandler();
var key = Encoding.ASCII.GetBytes(""{secretKey}"");

tokenHandler.ValidateToken(tokenString, new TokenValidationParameters
{{
    ValidateIssuerSigningKey = true,
    IssuerSigningKey = new SymmetricSecurityKey(key),
    ValidateIssuer = true,
    ValidIssuer = ""{config.ClusterName}"",
    ValidateAudience = true,
    ValidAudience = ""indigo-agents"",
    ValidateLifetime = true
}}, out SecurityToken validatedToken);
```
";

        var configPath = Path.Combine(OutputDirectory, "jwt-config.md");
        await File.WriteAllTextAsync(configPath, jwtConfig);

        // Salva secret key in file separato
        var secretPath = Path.Combine(OutputDirectory, ".jwt-secret");
        await File.WriteAllTextAsync(secretPath, secretKey);

        Log.Information($"JWT config generato: {configPath}");
        Log.Information($"JWT secret salvato: {secretPath}");
    }

    /// <summary>
    /// Genera configurazione API interne tra agenti
    /// </summary>
    private async Task GenerateInternalApiConfig(ClusterConfig config)
    {
        Log.Information("Generazione configurazione API interne...");

        var apiConfig = $@"# Internal API Configuration per {config.ClusterName}

## Agent Endpoints

{string.Join("\n\n", config.Agents.Select(a => $@"### {a.Name}
- Base URL: http://{a.Name}
- Health: GET http://{a.Name}/health
- Metadata: GET http://{a.Name}/metadata
- Echo: POST http://{a.Name}/echo
- Log: POST http://{a.Name}/log
- Port (host): {a.Port}"))}

## Service Discovery

Gli agenti possono comunicare tra loro usando i nomi dei container come hostname.
Docker Compose configura automaticamente il DNS interno.

## Example: Agent-to-Agent Communication

```csharp
var httpClient = new HttpClient();
var response = await httpClient.GetAsync(""http://{config.Agents[0].Name}/health"");
var content = await response.Content.ReadAsStringAsync();
```
";

        var configPath = Path.Combine(OutputDirectory, "internal-api-config.md");
        await File.WriteAllTextAsync(configPath, apiConfig);

        Log.Information($"Internal API config generato: {configPath}");
    }

    /// <summary>
    /// Genera script di test per verificare la comunicazione
    /// </summary>
    private async Task GenerateCommunicationTestScript(ClusterConfig config)
    {
        Log.Information("Generazione script di test comunicazione...");

        var testScript = $@"# Test Communication Script per {config.ClusterName}

## Test Health Endpoints

{string.Join("\n", config.Agents.Select(a => $"curl http://localhost:{a.Port}/health"))}

## Test Redis Connection

```bash
docker exec {config.ClusterName}_redis redis-cli ping
docker exec {config.ClusterName}_redis redis-cli set test:key ""hello""
docker exec {config.ClusterName}_redis redis-cli get test:key
```

## Test RabbitMQ

```bash
# Management UI
open http://localhost:15672

# Test publish
docker exec {config.ClusterName}_rabbitmq rabbitmqadmin publish exchange=indigo.events routing_key=test.event payload=""test message""
```

## Test Agent Communication

```bash
# Echo test
curl -X POST http://localhost:{config.Agents[0].Port}/echo -H ""Content-Type: text/plain"" -d ""Hello from test""
```
";

        var testPath = Path.Combine(OutputDirectory, "test-communication.md");
        await File.WriteAllTextAsync(testPath, testScript);

        Log.Information($"Test script generato: {testPath}");
    }

    /// <summary>
    /// Genera una secret key per JWT
    /// </summary>
    private string GenerateSecretKey()
    {
        var randomBytes = new byte[32];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}
