namespace CursorMonitorAgent;

/// <summary>
/// Analizza contenuto file e genera task automatici
/// </summary>
public class TaskGenerator
{
    private readonly ILogger<TaskGenerator> _logger;

    public TaskGenerator(ILogger<TaskGenerator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Analizza il contenuto di un file e suggerisce task automatici
    /// </summary>
    public TaskSuggestion? AnalyzeContent(string content, string fileName, string eventType)
    {
        _logger.LogInformation("Analisi contenuto: {FileName} (evento: {EventType})", fileName, eventType);

        // Regole di analisi per diversi scenari

        // 1. Rileva errori di compilazione
        if (content.Contains("error CS", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("compilation failed", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("build failed", StringComparison.OrdinalIgnoreCase))
        {
            return new TaskSuggestion
            {
                TaskName = "fix-compilation-errors",
                Payload = ExtractErrorMessages(content),
                Priority = TaskPriority.High,
                Reason = "Rilevati errori di compilazione nel file",
                SourceFile = fileName
            };
        }

        // 2. Rileva richieste di UI
        if (content.Contains("create ui", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("wpf", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("dashboard", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("interface", StringComparison.OrdinalIgnoreCase))
        {
            return new TaskSuggestion
            {
                TaskName = "generate-ui",
                Payload = ExtractUiRequirements(content),
                Priority = TaskPriority.Medium,
                Reason = "Rilevata richiesta di generazione UI",
                SourceFile = fileName
            };
        }

        // 3. Rileva richieste di test
        if (content.Contains("add test", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("unit test", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("testing", StringComparison.OrdinalIgnoreCase))
        {
            return new TaskSuggestion
            {
                TaskName = "add-tests",
                Payload = ExtractTestRequirements(content),
                Priority = TaskPriority.Low,
                Reason = "Rilevata richiesta di test",
                SourceFile = fileName
            };
        }

        // 4. Rileva problemi di struttura
        if (content.Contains("refactor", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("restructure", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("organize", StringComparison.OrdinalIgnoreCase))
        {
            return new TaskSuggestion
            {
                TaskName = "improve-structure",
                Payload = ExtractStructureIssues(content),
                Priority = TaskPriority.Medium,
                Reason = "Rilevata richiesta di miglioramento struttura",
                SourceFile = fileName
            };
        }

        // 5. Rileva mancanza di documentazione
        if (content.Contains("document", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("readme", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("guide", StringComparison.OrdinalIgnoreCase))
        {
            return new TaskSuggestion
            {
                TaskName = "add-documentation",
                Payload = ExtractDocumentationNeeds(content),
                Priority = TaskPriority.Low,
                Reason = "Rilevata richiesta di documentazione",
                SourceFile = fileName
            };
        }

        // 6. Rileva file "ai-output" completati (FILE ALWAYS MODE)
        if (fileName.StartsWith("ai-output-", StringComparison.OrdinalIgnoreCase))
        {
            if (content.Contains("✅ Completed", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("File AI completato: {FileName}", fileName);
                // Questo file è un output AI già completato, nessun task suggerito
                return null;
            }
        }

        _logger.LogInformation("Nessun task suggerito per: {FileName}", fileName);
        return null;
    }

    private string ExtractErrorMessages(string content)
    {
        // Estrai i messaggi di errore (stub)
        var lines = content.Split('\n');
        var errors = lines.Where(l => 
            l.Contains("error", StringComparison.OrdinalIgnoreCase) ||
            l.Contains("failed", StringComparison.OrdinalIgnoreCase))
            .Take(10);
        
        return string.Join("\n", errors);
    }

    private string ExtractUiRequirements(string content)
    {
        // Estrai requisiti UI (stub)
        return content.Length > 500 ? content.Substring(0, 500) : content;
    }

    private string ExtractTestRequirements(string content)
    {
        // Estrai requisiti test (stub)
        return content.Length > 500 ? content.Substring(0, 500) : content;
    }

    private string ExtractStructureIssues(string content)
    {
        // Estrai problemi struttura (stub)
        return content.Length > 500 ? content.Substring(0, 500) : content;
    }

    private string ExtractDocumentationNeeds(string content)
    {
        // Estrai necessità documentazione (stub)
        return content.Length > 500 ? content.Substring(0, 500) : content;
    }
}

/// <summary>
/// Suggerimento di task automatico
/// </summary>
public class TaskSuggestion
{
    public string TaskName { get; set; } = "";
    public string Payload { get; set; } = "";
    public TaskPriority Priority { get; set; }
    public string Reason { get; set; } = "";
    public string SourceFile { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Priorità task
/// </summary>
public enum TaskPriority
{
    Low,
    Medium,
    High,
    Critical
}
