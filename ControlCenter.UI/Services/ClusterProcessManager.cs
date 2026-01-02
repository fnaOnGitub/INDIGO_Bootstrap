using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ControlCenter.UI.Services;

/// <summary>
/// Stato di un agente
/// </summary>
public enum AgentStatus
{
    NotStarted,
    Starting,
    Running,
    Crashed
}

/// <summary>
/// Informazioni diagnostiche su un agente
/// </summary>
public class AgentDiagnostics
{
    public AgentStatus Status { get; set; } = AgentStatus.NotStarted;
    public DateTime? LastOutputTime { get; set; }
    public DateTime? StartTime { get; set; }
    public int OutputLinesReceived { get; set; }
    public int ErrorLinesReceived { get; set; }
    public string? LastError { get; set; }
    public int? ExitCode { get; set; }
    public bool ReceivedOutputAfterStart { get; set; }
}

/// <summary>
/// Gestisce l'avvio e l'arresto dei processi del cluster in background
/// </summary>
public class ClusterProcessManager
{
    private readonly LogService _logService;
    private readonly Dictionary<string, Process> _processes = new();
    private readonly Dictionary<string, AgentDiagnostics> _diagnostics = new();
    private readonly Dictionary<string, CancellationTokenSource> _watchdogTokens = new();
    private readonly string _basePath;

    public ClusterProcessManager(LogService logService)
    {
        _logService = logService;
        _basePath = @"c:\Users\filip\OneDrive\Documents\02_AREAS\FNA_Coding\VISUAL_STUDIO\INDIGOLAB\INDIGO_BOOTHSTRAPPER";
    }

    /// <summary>
    /// Avvia un singolo agente in background
    /// </summary>
    public bool StartAgent(string agentName, string projectSubPath)
    {
        try
        {
            // Se l'agente è già in esecuzione, fermalo prima
            if (_processes.ContainsKey(agentName))
            {
                StopAgent(agentName);
            }

            // Inizializza diagnostica
            _diagnostics[agentName] = new AgentDiagnostics
            {
                Status = AgentStatus.Starting,
                StartTime = DateTime.Now,
                OutputLinesReceived = 0,
                ErrorLinesReceived = 0,
                ReceivedOutputAfterStart = false
            };

            var projectPath = Path.Combine(_basePath, projectSubPath);

            _logService.AppendLog(agentName, $"=== Avvio {agentName} ===");
            _logService.AppendLog(agentName, $"[DIAG] Percorso: {projectPath}");
            _logService.AppendLog(agentName, $"[DIAG] Stato: STARTING");

            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --project \"{projectPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = projectPath
            };

            var process = new Process
            {
                StartInfo = psi,
                EnableRaisingEvents = true
            };

            // Gestione output
            process.OutputDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    if (_diagnostics.TryGetValue(agentName, out var diag))
                    {
                        diag.OutputLinesReceived++;
                        diag.LastOutputTime = DateTime.Now;
                        
                        if (!diag.ReceivedOutputAfterStart)
                        {
                            diag.ReceivedOutputAfterStart = true;
                            diag.Status = AgentStatus.Running;
                            _logService.AppendLog(agentName, "[DIAG] Primo output ricevuto - Stato: RUNNING", LogLevel.Info);
                        }
                    }
                    
                    _logService.AppendLog(agentName, e.Data, LogLevel.Info);
                }
            };

            // Gestione errori
            process.ErrorDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    if (_diagnostics.TryGetValue(agentName, out var diag))
                    {
                        diag.ErrorLinesReceived++;
                        diag.LastError = e.Data;
                        diag.LastOutputTime = DateTime.Now;
                    }
                    
                    _logService.AppendLog(agentName, $"[ERR] {e.Data}", LogLevel.Error);
                }
            };

            // Gestione exit
            process.Exited += (s, e) =>
            {
                var exitCode = process.ExitCode;
                
                if (_diagnostics.TryGetValue(agentName, out var diag))
                {
                    diag.Status = AgentStatus.Crashed;
                    diag.ExitCode = exitCode;
                    
                    var uptime = DateTime.Now - (diag.StartTime ?? DateTime.Now);
                    
                    if (uptime.TotalSeconds < 5)
                    {
                        _logService.AppendLog(agentName, 
                            $"[FATAL] Processo crashato IMMEDIATAMENTE dopo l'avvio (Uptime: {uptime.TotalSeconds:F1}s, ExitCode: {exitCode})",
                            LogLevel.Error);
                    }
                    else
                    {
                        _logService.AppendLog(agentName, 
                            $"[FATAL] Processo terminato inaspettatamente (Uptime: {uptime.TotalSeconds:F1}s, ExitCode: {exitCode})",
                            LogLevel.Error);
                    }
                    
                    _logService.AppendLog(agentName, $"[DIAG] Stato: CRASHED");
                }
                else
                {
                    _logService.AppendLog(agentName, $"=== Processo terminato (ExitCode: {exitCode}) ===",
                        exitCode == 0 ? LogLevel.Info : LogLevel.Error);
                }
                
                // Cancella watchdog
                if (_watchdogTokens.TryGetValue(agentName, out var cts))
                {
                    cts.Cancel();
                    _watchdogTokens.Remove(agentName);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            _processes[agentName] = process;

            _logService.AppendLog(agentName, $"[DIAG] Processo avviato (PID: {process.Id})");
            
            // Avvia watchdog per verificare se l'output arriva
            StartOutputWatchdog(agentName);

            return true;
        }
        catch (Exception ex)
        {
            _logService.AppendLog(agentName, $"[FATAL] ERRORE avvio: {ex.Message}", LogLevel.Error);
            _logService.AppendLog(agentName, $"[DIAG] Exception: {ex.GetType().Name}");
            
            if (_diagnostics.TryGetValue(agentName, out var diag))
            {
                diag.Status = AgentStatus.Crashed;
                diag.LastError = ex.Message;
            }
            
            return false;
        }
    }

    /// <summary>
    /// Avvia un watchdog che verifica se l'agente produce output
    /// </summary>
    private void StartOutputWatchdog(string agentName)
    {
        var cts = new CancellationTokenSource();
        _watchdogTokens[agentName] = cts;
        
        Task.Run(async () =>
        {
            try
            {
                // Attendi 5 secondi
                await Task.Delay(5000, cts.Token);
                
                // Verifica se è arrivato output
                if (_diagnostics.TryGetValue(agentName, out var diag))
                {
                    if (!diag.ReceivedOutputAfterStart)
                    {
                        _logService.AppendLog(agentName, 
                            "[WARN] Nessun output ricevuto dall'agente dopo 5 secondi dall'avvio", 
                            LogLevel.Warning);
                        _logService.AppendLog(agentName, 
                            "[DIAG] L'agente potrebbe essere bloccato o non funzionare correttamente");
                    }
                }
            }
            catch (TaskCanceledException)
            {
                // Watchdog cancellato (normale se il processo termina)
            }
        }, cts.Token);
    }

    /// <summary>
    /// Ferma un singolo agente
    /// </summary>
    public void StopAgent(string agentName)
    {
        // Cancella watchdog
        if (_watchdogTokens.TryGetValue(agentName, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
            _watchdogTokens.Remove(agentName);
        }
        
        if (_processes.TryGetValue(agentName, out var process))
        {
            try
            {
                if (!process.HasExited)
                {
                    _logService.AppendLog(agentName, "[DIAG] Arresto processo...");
                    process.Kill(true); // Kill anche i processi figli
                    process.WaitForExit(5000);
                }

                process.Dispose();
                _processes.Remove(agentName);

                if (_diagnostics.TryGetValue(agentName, out var diag))
                {
                    diag.Status = AgentStatus.NotStarted;
                }

                _logService.AppendLog(agentName, "[DIAG] Processo arrestato");
            }
            catch (Exception ex)
            {
                _logService.AppendLog(agentName, $"[ERR] Errore arresto: {ex.Message}", LogLevel.Error);
            }
        }
    }

    /// <summary>
    /// Avvia tutti gli agenti del cluster
    /// </summary>
    public void StartAllAgents()
    {
        _logService.AppendLog("System", "=== AVVIO CLUSTER COMPLETO ===");

        // Avvia Orchestrator
        StartAgent("Orchestrator", "Agent.Orchestrator");
        System.Threading.Thread.Sleep(3000);

        // Avvia AI Worker
        StartAgent("IndigoAiWorker01", "IndigoAiWorker01");
        System.Threading.Thread.Sleep(2000);

        _logService.AppendLog("System", "=== CLUSTER AVVIATO ===");
    }

    /// <summary>
    /// Ferma tutti gli agenti
    /// </summary>
    public void StopAllAgents()
    {
        _logService.AppendLog("System", "=== ARRESTO CLUSTER ===");

        var agentNames = _processes.Keys.ToList();
        foreach (var agentName in agentNames)
        {
            StopAgent(agentName);
        }

        _logService.AppendLog("System", "=== CLUSTER ARRESTATO ===");
    }

    /// <summary>
    /// Verifica se un agente è in esecuzione
    /// </summary>
    public bool IsAgentRunning(string agentName)
    {
        return _processes.TryGetValue(agentName, out var process) && !process.HasExited;
    }

    /// <summary>
    /// Ottiene lo stato di tutti gli agenti
    /// </summary>
    public Dictionary<string, bool> GetAgentStatuses()
    {
        return _processes.ToDictionary(
            kvp => kvp.Key,
            kvp => !kvp.Value.HasExited
        );
    }

    /// <summary>
    /// Ottiene lo stato dettagliato di un agente
    /// </summary>
    public AgentStatus GetAgentStatus(string agentName)
    {
        if (_diagnostics.TryGetValue(agentName, out var diag))
        {
            return diag.Status;
        }
        return AgentStatus.NotStarted;
    }

    /// <summary>
    /// Ottiene le diagnostiche complete di un agente
    /// </summary>
    public AgentDiagnostics? GetAgentDiagnostics(string agentName)
    {
        return _diagnostics.TryGetValue(agentName, out var diag) ? diag : null;
    }

    /// <summary>
    /// Ottiene tutte le diagnostiche
    /// </summary>
    public Dictionary<string, AgentDiagnostics> GetAllDiagnostics()
    {
        return new Dictionary<string, AgentDiagnostics>(_diagnostics);
    }
}
