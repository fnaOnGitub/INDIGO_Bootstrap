namespace CursorMonitorAgent;

/// <summary>
/// Monitora file system per rilevare eventi Cursor
/// </summary>
public class CursorFileMonitor : IDisposable
{
    private readonly ILogger<CursorFileMonitor> _logger;
    private readonly LogBuffer _logBuffer;
    private readonly AgentState _agentState;
    private readonly List<FileSystemWatcher> _watchers = new();
    private readonly List<CursorInstance> _cursorInstances;
    private readonly TaskGenerator _taskGenerator;

    public CursorFileMonitor(
        ILogger<CursorFileMonitor> logger,
        LogBuffer logBuffer,
        AgentState agentState,
        TaskGenerator taskGenerator)
    {
        _logger = logger;
        _logBuffer = logBuffer;
        _agentState = agentState;
        _taskGenerator = taskGenerator;
        
        // Configurazione istanze Cursor monitorate
        _cursorInstances = new List<CursorInstance>
        {
            new CursorInstance
            {
                Name = "IndigoAiWorker01-CursorBridge",
                Path = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "IndigoAiWorker01", "bin", "Debug", "net8.0", "CursorBridge"),
                IsActive = true
            }
        };
    }

    /// <summary>
    /// Avvia il monitoraggio di tutte le istanze Cursor
    /// </summary>
    public void Start()
    {
        _logger.LogInformation("=== Avvio CursorFileMonitor ===");
        _logBuffer.Add("Avvio monitoraggio file system");

        foreach (var instance in _cursorInstances.Where(i => i.IsActive))
        {
            try
            {
                // Normalizza e verifica il path
                var fullPath = Path.GetFullPath(instance.Path);
                
                if (!Directory.Exists(fullPath))
                {
                    _logger.LogWarning("Cartella non trovata per {Name}: {Path}", instance.Name, fullPath);
                    _logger.LogWarning("Tentativo di creazione cartella...");
                    Directory.CreateDirectory(fullPath);
                    _logger.LogInformation("Cartella creata: {Path}", fullPath);
                }

                var watcher = new FileSystemWatcher(fullPath)
                {
                    Filter = "*.md",
                    NotifyFilter = NotifyFilters.FileName
                                 | NotifyFilters.LastWrite
                                 | NotifyFilters.CreationTime,
                    IncludeSubdirectories = false,
                    EnableRaisingEvents = true
                };

                // Evento: nuovo file creato
                watcher.Created += (sender, e) => OnFileCreated(e, instance);
                
                // Evento: file modificato
                watcher.Changed += (sender, e) => OnFileChanged(e, instance);
                
                // Evento: file eliminato
                watcher.Deleted += (sender, e) => OnFileDeleted(e, instance);

                _watchers.Add(watcher);
                
                _logger.LogInformation("Monitoraggio attivo per {Name}: {Path}", instance.Name, fullPath);
                _logBuffer.Add($"Monitoraggio attivo: {instance.Name} ({fullPath})");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore avvio monitoraggio per {Name}", instance.Name);
                _logBuffer.Add($"Errore monitoraggio {instance.Name}: {ex.Message}", "ERROR");
            }
        }

        _logger.LogInformation("CursorFileMonitor avviato con successo ({Count} istanze)", _watchers.Count);
    }

    /// <summary>
    /// Evento: nuovo file creato
    /// </summary>
    private void OnFileCreated(FileSystemEventArgs e, CursorInstance instance)
    {
        try
        {
            _logger.LogInformation("[{Instance}] Nuovo file creato: {FileName}", instance.Name, e.Name);
            _logBuffer.Add($"[{instance.Name}] File creato: {e.Name}");
            _agentState.UpdateLastEvent($"File creato: {e.Name}");

            // Analizza il file e genera task se necessario
            Task.Run(() => AnalyzeFileAndGenerateTask(e.FullPath, instance, "created"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore elaborazione file creato: {File}", e.FullPath);
        }
    }

    /// <summary>
    /// Evento: file modificato
    /// </summary>
    private void OnFileChanged(FileSystemEventArgs e, CursorInstance instance)
    {
        try
        {
            _logger.LogInformation("[{Instance}] File modificato: {FileName}", instance.Name, e.Name);
            _logBuffer.Add($"[{instance.Name}] File modificato: {e.Name}");
            _agentState.UpdateLastEvent($"File modificato: {e.Name}");

            // Analizza il file e genera task se necessario
            Task.Run(() => AnalyzeFileAndGenerateTask(e.FullPath, instance, "modified"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore elaborazione file modificato: {File}", e.FullPath);
        }
    }

    /// <summary>
    /// Evento: file eliminato
    /// </summary>
    private void OnFileDeleted(FileSystemEventArgs e, CursorInstance instance)
    {
        try
        {
            _logger.LogInformation("[{Instance}] File eliminato: {FileName}", instance.Name, e.Name);
            _logBuffer.Add($"[{instance.Name}] File eliminato: {e.Name}");
            _agentState.UpdateLastEvent($"File eliminato: {e.Name}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore elaborazione file eliminato: {File}", e.FullPath);
        }
    }

    /// <summary>
    /// Analizza il contenuto di un file e genera task automatici se necessario
    /// </summary>
    private async Task AnalyzeFileAndGenerateTask(string filePath, CursorInstance instance, string eventType)
    {
        try
        {
            // Attendi un attimo per essere sicuri che il file sia completamente scritto
            await Task.Delay(500);

            if (!File.Exists(filePath))
            {
                _logger.LogWarning("File non più esistente: {Path}", filePath);
                return;
            }

            var fileName = Path.GetFileName(filePath);
            var content = await File.ReadAllTextAsync(filePath);

            _logger.LogInformation("Analisi file: {FileName} ({Length} caratteri)", fileName, content.Length);

            // Determina se serve generare un task automatico
            var taskSuggestion = _taskGenerator.AnalyzeContent(content, fileName, eventType);

            if (taskSuggestion != null)
            {
                _logger.LogInformation("Task suggerito: {Task}", taskSuggestion.TaskName);
                _logBuffer.Add($"Task suggerito: {taskSuggestion.TaskName} (da {fileName})");
                
                // Il task verrà inviato all'Orchestrator tramite endpoint dedicato
                // Per ora lo registriamo solo
            }
            else
            {
                _logger.LogInformation("Nessun task suggerito per: {FileName}", fileName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore analisi file: {Path}", filePath);
            _logBuffer.Add($"Errore analisi file: {ex.Message}", "ERROR");
        }
    }

    /// <summary>
    /// Ferma il monitoraggio
    /// </summary>
    public void Stop()
    {
        _logger.LogInformation("Arresto CursorFileMonitor...");
        _logBuffer.Add("Arresto monitoraggio file system");

        foreach (var watcher in _watchers)
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
        }

        _watchers.Clear();
        _logger.LogInformation("CursorFileMonitor arrestato");
    }

    public void Dispose()
    {
        Stop();
    }
}

/// <summary>
/// Rappresenta un'istanza Cursor monitorata
/// </summary>
public class CursorInstance
{
    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
    public bool IsActive { get; set; }
}
