namespace IndigoAiWorker01;

/// <summary>
/// Bridge per comunicare con Cursor attraverso file system
/// </summary>
public class CursorBridge
{
    private readonly ILogger<CursorBridge> _logger;
    private readonly string _cursorBridgePath;
    private readonly string _cursorDirectPath;

    public CursorBridge(ILogger<CursorBridge> logger)
    {
        _logger = logger;
        
        // Cartella indiretta (nella workspace del worker)
        _cursorBridgePath = Path.Combine(AppContext.BaseDirectory, "CursorBridge");
        
        // Cartella diretta (nella .cursor del progetto)
        var workspaceRoot = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..");
        _cursorDirectPath = Path.Combine(workspaceRoot, ".cursor", "ai-requests");

        // Crea cartelle se non esistono
        Directory.CreateDirectory(_cursorBridgePath);
        
        try
        {
            Directory.CreateDirectory(_cursorDirectPath);
            _logger.LogInformation("Cartella .cursor/ai-requests creata/verificata");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Impossibile creare .cursor/ai-requests. Usando solo CursorBridge locale.");
        }
    }

    /// <summary>
    /// Scrive un file nella cartella CursorBridge (integrazione indiretta)
    /// </summary>
    public string WriteToCursorBridge(string task, string content)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd-HHmmss");
        var filename = $"request-{task}-{timestamp}.md";
        var filepath = Path.Combine(_cursorBridgePath, filename);

        try
        {
            File.WriteAllText(filepath, content);
            _logger.LogInformation("File scritto in CursorBridge: {Filepath}", filepath);
            return filepath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore scrittura file CursorBridge");
            throw;
        }
    }

    /// <summary>
    /// Scrive un file nella cartella .cursor/ai-requests (integrazione diretta)
    /// </summary>
    public string WriteToCursorDirect(string task, string content)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd-HHmmss");
        var filename = $"{task}-{timestamp}.md";
        var filepath = Path.Combine(_cursorDirectPath, filename);

        try
        {
            File.WriteAllText(filepath, content);
            _logger.LogInformation("File scritto in .cursor/ai-requests: {Filepath}", filepath);
            return filepath;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Impossibile scrivere in .cursor/ai-requests. Usando CursorBridge.");
            return WriteToCursorBridge(task, content);
        }
    }

    /// <summary>
    /// Scrive un prompt strutturato per Cursor
    /// </summary>
    public (bool Success, string FilePath) WriteCursorPrompt(string task, string content, bool useDirect = true)
    {
        try
        {
            string filepath = useDirect 
                ? WriteToCursorDirect(task, content)
                : WriteToCursorBridge(task, content);

            return (true, filepath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore scrittura prompt Cursor");
            return (false, "");
        }
    }

    /// <summary>
    /// Scrive un prompt ottimizzato nella CursorBridge
    /// </summary>
    public string WriteOptimizedPrompt(string optimizedPrompt)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd-HHmmss");
        var filename = $"optimized-{timestamp}.md";
        var filepath = Path.Combine(_cursorBridgePath, filename);

        try
        {
            File.WriteAllText(filepath, optimizedPrompt);
            _logger.LogInformation("Prompt ottimizzato scritto in CursorBridge: {Filepath}", filepath);
            return filepath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore scrittura prompt ottimizzato");
            throw;
        }
    }

    /// <summary>
    /// FILE ALWAYS MODE: Scrive SEMPRE un file .md per ogni task AI
    /// </summary>
    public string WriteAiOutput(string taskName, string payload, string aiResult, string? optimizedPrompt = null)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd-HHmmss");
        var filename = $"ai-output-{taskName}-{timestamp}.md";
        var filepath = Path.Combine(_cursorBridgePath, filename);

        try
        {
            // Costruisce contenuto standardizzato
            var content = $@"# 🤖 AI Task Output - IndigoAiWorker01

**Generated**: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
**Task**: `{taskName}`
**Worker**: IndigoAiWorker01 (AI-Powered)
**Status**: ✅ Completed

---

## 📥 Input (Payload)

```
{payload}
```

---

## 🧠 AI Output

{aiResult}

---

";

            // Aggiungi sezione Prompt Ottimizzato se presente
            if (!string.IsNullOrEmpty(optimizedPrompt))
            {
                content += $@"## 📝 Optimized Prompt (Cursor-Ready)

{optimizedPrompt}

---

";
            }

            // Footer
            content += $@"## ℹ️ Metadata

- **File**: `{filename}`
- **Path**: `{filepath}`
- **Timestamp**: {DateTime.UtcNow:o}
- **Worker Type**: AI-Powered
- **Task Type**: {taskName}

---

*Generated by IndigoAiWorker01 - IndigoLab Cluster*
*FILE ALWAYS MODE: Every AI task produces a traceable output file*
";

            File.WriteAllText(filepath, content);
            _logger.LogInformation("AI Output scritto in CursorBridge (FILE ALWAYS MODE): {Filepath}", filepath);
            return filepath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore scrittura AI Output");
            throw;
        }
    }

    /// <summary>
    /// Lista tutti i file nella CursorBridge
    /// </summary>
    public List<string> ListBridgeFiles()
    {
        try
        {
            return Directory.GetFiles(_cursorBridgePath, "*.md")
                .Select(Path.GetFileName)
                .ToList()!;
        }
        catch
        {
            return new List<string>();
        }
    }

    /// <summary>
    /// Pulisce file vecchi nella CursorBridge (più vecchi di N giorni)
    /// </summary>
    public int CleanupOldFiles(int daysOld = 7)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-daysOld);
            var files = Directory.GetFiles(_cursorBridgePath, "*.md");
            var deleted = 0;

            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                if (fileInfo.LastWriteTimeUtc < cutoffDate)
                {
                    File.Delete(file);
                    deleted++;
                }
            }

            _logger.LogInformation("Pulizia CursorBridge: {Deleted} file eliminati", deleted);
            return deleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante cleanup CursorBridge");
            return 0;
        }
    }
}
