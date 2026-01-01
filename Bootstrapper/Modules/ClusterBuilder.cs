using Scriban;
using Serilog;

namespace Bootstrapper.Modules;

/// <summary>
/// Gestisce la generazione di docker-compose.yaml, reti, volumi, Traefik
/// </summary>
public class ClusterBuilder
{
    private const string OutputDirectory = "output";
    private const string DockerComposeTemplate = "Templates/docker-compose.yaml.scriban";

    /// <summary>
    /// Costruisce il cluster: genera docker-compose.yaml e file di configurazione
    /// </summary>
    public async Task BuildCluster(ClusterConfig config)
    {
        try
        {
            Log.Information("=== BUILD CLUSTER ===");
            Log.Information($"Cluster: {config.ClusterName}");

            // Crea directory output
            Directory.CreateDirectory(OutputDirectory);

            // Genera docker-compose.yaml da template Scriban
            await GenerateDockerCompose(config);

            // Genera file di configurazione per Traefik (se abilitato)
            if (config.Network.UseTraefik)
            {
                await GenerateTraefikConfig(config);
            }

            // Genera script di avvio cluster
            await GenerateStartupScript(config);

            Log.Information("Build cluster completato con successo");
            Log.Information($"Output generato in: {Path.GetFullPath(OutputDirectory)}");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Errore durante build cluster");
            throw;
        }
    }

    /// <summary>
    /// Genera docker-compose.yaml usando template Scriban
    /// </summary>
    private async Task GenerateDockerCompose(ClusterConfig config)
    {
        Log.Information("Generazione docker-compose.yaml...");

        // Carica template Scriban
        if (!File.Exists(DockerComposeTemplate))
        {
            throw new FileNotFoundException($"Template non trovato: {DockerComposeTemplate}");
        }

        var templateContent = await File.ReadAllTextAsync(DockerComposeTemplate);
        var template = Template.Parse(templateContent);

        // Prepara dati per il template
        var templateData = new
        {
            cluster_name = config.ClusterName,
            network_subnet = config.Network.SubnetCidr,
            use_traefik = config.Network.UseTraefik,
            communication_broker = config.Communication.MessageBroker,
            cache_provider = config.Communication.CacheProvider,
            agents = config.Agents.Select(a => new
            {
                name = a.Name,
                role = a.Type,
                port = a.Port,
                log_level = a.Environment.ContainsKey("LOG_LEVEL") ? a.Environment["LOG_LEVEL"] : "Information",
                environment = a.Environment.Select(kv => new { name = kv.Key, value = kv.Value }).ToList()
            }).ToList()
        };

        // Renderizza template
        var dockerComposeContent = template.Render(templateData);

        // Salva file
        var outputPath = Path.Combine(OutputDirectory, "docker-compose.yaml");
        await File.WriteAllTextAsync(outputPath, dockerComposeContent);

        Log.Information($"docker-compose.yaml generato: {outputPath}");
    }

    /// <summary>
    /// Genera configurazione Traefik
    /// </summary>
    private async Task GenerateTraefikConfig(ClusterConfig config)
    {
        Log.Information("Generazione configurazione Traefik...");

        var traefikConfig = @"# Traefik Static Configuration
api:
  dashboard: true
  insecure: true

entryPoints:
  web:
    address: ':80'
  
providers:
  docker:
    exposedByDefault: false
    network: " + config.ClusterName + @"_network

log:
  level: INFO
";

        var outputPath = Path.Combine(OutputDirectory, "traefik.yaml");
        await File.WriteAllTextAsync(outputPath, traefikConfig);

        Log.Information($"Traefik config generato: {outputPath}");
    }

    /// <summary>
    /// Genera script di avvio per il cluster
    /// </summary>
    private async Task GenerateStartupScript(ClusterConfig config)
    {
        Log.Information("Generazione script di avvio...");

        // Script PowerShell per Windows
        var psScript = $@"# Script di avvio cluster {config.ClusterName}
Write-Host ""=== AVVIO CLUSTER {config.ClusterName} ==="" -ForegroundColor Green

# Verifica Docker
if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {{
    Write-Host ""ERRORE: Docker non trovato. Installare Docker Desktop."" -ForegroundColor Red
    exit 1
}}

Write-Host ""Docker trovato: $(docker --version)"" -ForegroundColor Cyan

# Avvia cluster con docker-compose
Write-Host ""Avvio servizi con docker-compose..."" -ForegroundColor Yellow
docker-compose -f docker-compose.yaml up -d

# Attendi che i servizi siano pronti
Write-Host ""Attendo avvio servizi..."" -ForegroundColor Yellow
Start-Sleep -Seconds 10

# Verifica stato servizi
Write-Host ""Stato servizi:"" -ForegroundColor Cyan
docker-compose -f docker-compose.yaml ps

Write-Host """" 
Write-Host ""=== CLUSTER AVVIATO ==="" -ForegroundColor Green
Write-Host ""Traefik Dashboard: http://localhost:8080"" -ForegroundColor Cyan
{string.Join("\n", config.Agents.Select(a => $"Write-Host \"{a.Name}: http://localhost:{a.Port}\" -ForegroundColor Cyan"))}
Write-Host """"
Write-Host ""Per fermare il cluster: docker-compose -f docker-compose.yaml down"" -ForegroundColor Yellow
";

        var psPath = Path.Combine(OutputDirectory, "start-cluster.ps1");
        await File.WriteAllTextAsync(psPath, psScript);

        // Script bash per Linux/WSL
        var bashScript = $@"#!/bin/bash
# Script di avvio cluster {config.ClusterName}

echo ""=== AVVIO CLUSTER {config.ClusterName} ===""

# Verifica Docker
if ! command -v docker &> /dev/null; then
    echo ""ERRORE: Docker non trovato""
    exit 1
fi

echo ""Docker trovato: $(docker --version)""

# Avvia cluster
echo ""Avvio servizi con docker-compose...""
docker-compose -f docker-compose.yaml up -d

# Attendi avvio
echo ""Attendo avvio servizi...""
sleep 10

# Stato servizi
echo ""Stato servizi:""
docker-compose -f docker-compose.yaml ps

echo """"
echo ""=== CLUSTER AVVIATO ===""
echo ""Traefik Dashboard: http://localhost:8080""
{string.Join("\n", config.Agents.Select(a => $"echo \"{a.Name}: http://localhost:{a.Port}\""))}
echo """"
echo ""Per fermare: docker-compose -f docker-compose.yaml down""
";

        var bashPath = Path.Combine(OutputDirectory, "start-cluster.sh");
        await File.WriteAllTextAsync(bashPath, bashScript);

        Log.Information($"Script PowerShell generato: {psPath}");
        Log.Information($"Script Bash generato: {bashPath}");
    }

    /// <summary>
    /// Genera file .env per docker-compose
    /// </summary>
    public async Task GenerateEnvironmentFile(ClusterConfig config)
    {
        Log.Information("Generazione file .env...");

        var envContent = $@"# Environment variables per docker-compose
CLUSTER_NAME={config.ClusterName}
NETWORK_SUBNET={config.Network.SubnetCidr}
MESSAGE_BROKER={config.Communication.MessageBroker}
CACHE_PROVIDER={config.Communication.CacheProvider}
USE_JWT={config.Communication.UseJwt.ToString().ToLower()}
";

        var envPath = Path.Combine(OutputDirectory, ".env");
        await File.WriteAllTextAsync(envPath, envContent);

        Log.Information($".env generato: {envPath}");
    }
}
