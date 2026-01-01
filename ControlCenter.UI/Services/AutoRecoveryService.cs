using System.Diagnostics;
using System.IO;

namespace ControlCenter.UI.Services;

/// <summary>
/// Servizio per il ripristino automatico dell'Orchestrator
/// </summary>
public class AutoRecoveryService
{
    private readonly HealthCheckService _healthCheck;
    private Process? _orchestratorProcess;

    public bool IsStarting { get; private set; }
    public string LastError { get; private set; } = "";

    public AutoRecoveryService(HealthCheckService healthCheck)
    {
        _healthCheck = healthCheck;
    }

    /// <summary>
    /// Tenta di avviare automaticamente l'Orchestrator
    /// </summary>
    public async Task<(bool Success, string Message)> StartOrchestratorAsync()
    {
        if (IsStarting)
        {
            return (false, "Avvio già in corso");
        }

        IsStarting = true;
        LastError = "";

        try
        {
            // Trova la cartella del progetto Orchestrator
            var orchestratorPath = FindOrchestratorPath();
            
            if (string.IsNullOrEmpty(orchestratorPath))
            {
                LastError = "Cartella Agent.Orchestrator non trovata";
                return (false, LastError);
            }

            // Avvia il processo dotnet run
            var processInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "run",
                WorkingDirectory = orchestratorPath,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            _orchestratorProcess = Process.Start(processInfo);

            if (_orchestratorProcess == null)
            {
                LastError = "Impossibile avviare il processo";
                return (false, LastError);
            }

            // Attendi avvio (max 15 secondi)
            for (int i = 0; i < 15; i++)
            {
                await Task.Delay(1000);
                
                var checkResult = await _healthCheck.CheckOrchestratorAsync();
                if (checkResult.IsOnline)
                {
                    return (true, $"Orchestrator avviato con successo su porta {checkResult.Port}");
                }
            }

            LastError = "Timeout: Orchestrator non risponde dopo 15 secondi";
            return (false, LastError);
        }
        catch (Exception ex)
        {
            LastError = $"Errore durante l'avvio: {ex.Message}";
            return (false, LastError);
        }
        finally
        {
            IsStarting = false;
        }
    }

    /// <summary>
    /// Trova il percorso della cartella Agent.Orchestrator
    /// </summary>
    private string? FindOrchestratorPath()
    {
        try
        {
            // Prova percorsi relativi dalla cartella corrente
            var currentDir = Directory.GetCurrentDirectory();
            
            // Percorso 1: ../Agent.Orchestrator
            var path1 = Path.Combine(currentDir, "..", "Agent.Orchestrator");
            if (Directory.Exists(path1))
                return Path.GetFullPath(path1);

            // Percorso 2: ../../Agent.Orchestrator
            var path2 = Path.Combine(currentDir, "..", "..", "Agent.Orchestrator");
            if (Directory.Exists(path2))
                return Path.GetFullPath(path2);

            // Percorso 3: cerca nella struttura del progetto
            var projectRoot = FindProjectRoot(currentDir);
            if (projectRoot != null)
            {
                var path3 = Path.Combine(projectRoot, "Agent.Orchestrator");
                if (Directory.Exists(path3))
                    return path3;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Trova la root del progetto INDIGO_BOOTHSTRAPPER
    /// </summary>
    private string? FindProjectRoot(string startPath)
    {
        var current = new DirectoryInfo(startPath);
        
        while (current != null)
        {
            // Cerca la cartella che contiene Agent.Orchestrator
            var orchestratorDir = Path.Combine(current.FullName, "Agent.Orchestrator");
            if (Directory.Exists(orchestratorDir))
            {
                return current.FullName;
            }

            // Cerca file .sln
            if (current.GetFiles("*.sln").Any())
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return null;
    }

    /// <summary>
    /// Ferma il processo Orchestrator se è stato avviato da questo servizio
    /// </summary>
    public void StopOrchestrator()
    {
        try
        {
            if (_orchestratorProcess != null && !_orchestratorProcess.HasExited)
            {
                _orchestratorProcess.Kill(true);
                _orchestratorProcess.Dispose();
                _orchestratorProcess = null;
            }
        }
        catch
        {
            // Ignora errori di terminazione
        }
    }

    /// <summary>
    /// Apre la cartella dell'Orchestrator in Esplora File
    /// </summary>
    public bool OpenOrchestratorFolder()
    {
        try
        {
            var path = FindOrchestratorPath();
            if (string.IsNullOrEmpty(path))
                return false;

            Process.Start("explorer.exe", path);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
