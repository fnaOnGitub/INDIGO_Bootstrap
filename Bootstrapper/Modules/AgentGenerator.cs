using Serilog;

namespace Bootstrapper.Modules;

/// <summary>
/// Genera progetti agenti .NET 8 Minimal API da template
/// </summary>
public class AgentGenerator
{
    private const string TemplateDirectory = "Templates/agent-template";
    private const string OutputDirectory = "output";

    /// <summary>
    /// Genera tutti gli agenti definiti nella configurazione cluster
    /// </summary>
    public async Task GenerateAgents(ClusterConfig config)
    {
        try
        {
            Log.Information("=== GENERAZIONE AGENTI ===");
            Log.Information($"Numero agenti da generare: {config.Agents.Count}");

            // Verifica template
            if (!Directory.Exists(TemplateDirectory))
            {
                throw new DirectoryNotFoundException($"Directory template non trovata: {TemplateDirectory}");
            }

            // Crea directory output
            Directory.CreateDirectory(OutputDirectory);

            // Genera ogni agente
            foreach (var agent in config.Agents)
            {
                Log.Information($"Generazione agente: {agent.Name}");
                await GenerateSingleAgent(agent, config);
            }

            Log.Information("Generazione agenti completata con successo");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Errore durante generazione agenti");
            throw;
        }
    }

    /// <summary>
    /// Genera un singolo agente da template
    /// </summary>
    private async Task GenerateSingleAgent(AgentInfo agent, ClusterConfig config)
    {
        var agentPath = Path.Combine(OutputDirectory, agent.Name);

        // Crea directory agente
        Directory.CreateDirectory(agentPath);

        // Copia e personalizza files da template
        await CopyAndCustomizeTemplate(agent, agentPath);

        // Genera .csproj
        await GenerateProjectFile(agent, agentPath);

        // Genera Dockerfile personalizzato
        await GenerateDockerfile(agent, agentPath);

        // Genera appsettings.json personalizzato
        await GenerateAppSettings(agent, agentPath);

        Log.Information($"Agente {agent.Name} generato in: {agentPath}");
    }

    /// <summary>
    /// Copia e personalizza i file template
    /// </summary>
    private async Task CopyAndCustomizeTemplate(AgentInfo agent, string targetPath)
    {
        // Copia Program.cs
        var programTemplate = Path.Combine(TemplateDirectory, "Program.cs");
        var programTarget = Path.Combine(targetPath, "Program.cs");

        if (File.Exists(programTemplate))
        {
            var content = await File.ReadAllTextAsync(programTemplate);
            
            // Personalizza il contenuto (se necessario)
            content = content.Replace("{{AGENT_NAME}}", agent.Name);
            content = content.Replace("{{AGENT_TYPE}}", agent.Type);

            await File.WriteAllTextAsync(programTarget, content);
            Log.Debug($"Program.cs copiato per {agent.Name}");
        }
    }

    /// <summary>
    /// Genera file .csproj per l'agente
    /// </summary>
    private async Task GenerateProjectFile(AgentInfo agent, string targetPath)
    {
        var csprojContent = $@"<Project Sdk=""Microsoft.NET.Sdk.Web"">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <AssemblyName>{agent.Name}</AssemblyName>
    <RootNamespace>{agent.Name.Replace("-", "_")}</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""Serilog"" Version=""4.1.0"" />
    <PackageReference Include=""Serilog.AspNetCore"" Version=""8.0.3"" />
    <PackageReference Include=""Serilog.Sinks.Console"" Version=""6.0.0"" />
    <PackageReference Include=""Serilog.Sinks.File"" Version=""6.0.0"" />
    <PackageReference Include=""Swashbuckle.AspNetCore"" Version=""7.2.0"" />
  </ItemGroup>

</Project>
";

        var csprojPath = Path.Combine(targetPath, $"{agent.Name}.csproj");
        await File.WriteAllTextAsync(csprojPath, csprojContent);

        Log.Debug($".csproj generato per {agent.Name}");
    }

    /// <summary>
    /// Genera Dockerfile personalizzato per l'agente
    /// </summary>
    private async Task GenerateDockerfile(AgentInfo agent, string targetPath)
    {
        var dockerfileContent = $@"# Dockerfile per agente {agent.Name}
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY [""{agent.Name}.csproj"", ""./""]
RUN dotnet restore ""{agent.Name}.csproj""
COPY . .
RUN dotnet build ""{agent.Name}.csproj"" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish ""{agent.Name}.csproj"" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Crea directory logs
RUN mkdir -p /app/logs

# Variabili ambiente
ENV AGENT_NAME={agent.Name}
ENV AGENT_TYPE={agent.Type}

ENTRYPOINT [""dotnet"", ""{agent.Name}.dll""]
";

        var dockerfilePath = Path.Combine(targetPath, "Dockerfile");
        await File.WriteAllTextAsync(dockerfilePath, dockerfileContent);

        Log.Debug($"Dockerfile generato per {agent.Name}");
    }

    /// <summary>
    /// Genera appsettings.json personalizzato
    /// </summary>
    private async Task GenerateAppSettings(AgentInfo agent, string targetPath)
    {
        var logLevel = agent.Environment.ContainsKey("LOG_LEVEL") 
            ? agent.Environment["LOG_LEVEL"] 
            : "Information";

        var appSettingsContent = $@"{{
  ""Logging"": {{
    ""LogLevel"": {{
      ""Default"": ""{logLevel}"",
      ""Microsoft.AspNetCore"": ""Warning""
    }}
  }},
  ""AllowedHosts"": ""*"",
  ""Serilog"": {{
    ""MinimumLevel"": {{
      ""Default"": ""{logLevel}"",
      ""Override"": {{
        ""Microsoft"": ""Warning"",
        ""System"": ""Warning""
      }}
    }},
    ""WriteTo"": [
      {{
        ""Name"": ""Console""
      }},
      {{
        ""Name"": ""File"",
        ""Args"": {{
          ""path"": ""logs/{agent.Name}-.log"",
          ""rollingInterval"": ""Day"",
          ""outputTemplate"": ""{{Timestamp:yyyy-MM-dd HH:mm:ss.fff}} [{{Level}}] {{Message}}{{NewLine}}{{Exception}}""
        }}
      }}
    ]
  }},
  ""Agent"": {{
    ""Name"": ""{agent.Name}"",
    ""Type"": ""{agent.Type}"",
    ""Port"": {agent.Port}
  }}
}}
";

        var appSettingsPath = Path.Combine(targetPath, "appsettings.json");
        await File.WriteAllTextAsync(appSettingsPath, appSettingsContent);

        Log.Debug($"appsettings.json generato per {agent.Name}");
    }

    /// <summary>
    /// Genera README per l'agente
    /// </summary>
    public async Task GenerateAgentReadme(AgentInfo agent, string targetPath)
    {
        var readmeContent = $@"# {agent.Name}

## Informazioni Agente

- **Nome**: {agent.Name}
- **Tipo**: {agent.Type}
- **Porta**: {agent.Port}

## Esecuzione Locale

```bash
dotnet run
```

## Build Docker

```bash
docker build -t {agent.Name}:latest .
```

## Esecuzione Docker

```bash
docker run -p {agent.Port}:80 {agent.Name}:latest
```

## Endpoints

- Health: `GET /health`
- Metadata: `GET /metadata`
- Echo: `POST /echo`
- Log: `POST /log`

## Variabili Ambiente

{string.Join("\n", agent.Environment.Select(kv => $"- `{kv.Key}`: {kv.Value}"))}
";

        var readmePath = Path.Combine(targetPath, "README.md");
        await File.WriteAllTextAsync(readmePath, readmeContent);

        Log.Debug($"README.md generato per {agent.Name}");
    }
}
