namespace CursorMonitorAgent;

/// <summary>
/// Analizza contenuto file e genera task automatici con classificazione AI intelligente
/// </summary>
public class TaskGenerator
{
    private readonly ILogger<TaskGenerator> _logger;

    public TaskGenerator(ILogger<TaskGenerator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Verifica se il task name contiene "ai" (case-insensitive)
    /// </summary>
    private bool TaskNameContainsAi(string taskName)
    {
        return taskName.Contains("ai", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifica se il payload contiene verbi creativi
    /// </summary>
    private bool IsCreativePayload(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
            return false;
        
        var creativeVerbs = new[]
        {
            "crea", "genera", "sviluppa", "costruisci", 
            "implementa", "progetta", "ottimizza", "analizza",
            "create", "generate", "develop", "build",
            "implement", "design", "optimize", "analyze"
        };
        
        var payloadLower = payload.ToLowerInvariant();
        return creativeVerbs.Any(verb => payloadLower.Contains(verb));
    }

    /// <summary>
    /// Verifica se il payload è in linguaggio naturale (non strutturato)
    /// </summary>
    private bool IsNaturalLanguage(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
            return false;
        
        var trimmed = payload.Trim();
        
        // Non è linguaggio naturale se inizia con caratteri di struttura dati
        if (trimmed.StartsWith("{") || trimmed.StartsWith("[") || 
            trimmed.StartsWith("<") || trimmed.StartsWith("---"))
        {
            return false;
        }
        
        // È linguaggio naturale se contiene spazi e parole comuni
        var hasSpaces = trimmed.Contains(' ');
        var hasCommonWords = trimmed.Split(' ').Length > 3;
        
        return hasSpaces && hasCommonWords;
    }

    /// <summary>
    /// Determina se un task dovrebbe essere classificato come AI task
    /// </summary>
    private bool ShouldBeAiTask(string taskName, string payload)
    {
        // Criteri intelligent routing
        return TaskNameContainsAi(taskName) || 
               IsCreativePayload(payload) || 
               IsNaturalLanguage(payload);
    }

    /// <summary>
    /// Analizza il contenuto di un file e suggerisce task automatici con classificazione AI intelligente
    /// </summary>
    public TaskSuggestion? AnalyzeContent(string content, string fileName, string eventType)
    {
        _logger.LogInformation("Analisi contenuto: {FileName} (evento: {EventType})", fileName, eventType);

        TaskSuggestion? suggestion = null;

        // Regole di analisi per diversi scenari

        // 1. Rileva errori di compilazione
        if (content.Contains("error CS", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("compilation failed", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("build failed", StringComparison.OrdinalIgnoreCase))
        {
            var payload = ExtractErrorMessages(content);
            suggestion = new TaskSuggestion
            {
                TaskName = "fix-ai-compilation-errors", // Nome con "ai" per trigger AI routing
                Payload = payload,
                Priority = TaskPriority.High,
                Reason = "Rilevati errori di compilazione nel file",
                SourceFile = fileName,
                IsAiTask = true // Classificato come AI task
            };
        }

        // 2. Rileva richieste di UI
        else if (content.Contains("create ui", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("wpf", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("dashboard", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("interface", StringComparison.OrdinalIgnoreCase))
        {
            var payload = ExtractUiRequirements(content);
            suggestion = new TaskSuggestion
            {
                TaskName = "generate-ui", // Verbo creativo "generate"
                Payload = payload,
                Priority = TaskPriority.Medium,
                Reason = "Rilevata richiesta di generazione UI",
                SourceFile = fileName,
                IsAiTask = IsCreativePayload(payload) || IsNaturalLanguage(payload)
            };
        }

        // 3. Rileva richieste di test
        else if (content.Contains("add test", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("unit test", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("testing", StringComparison.OrdinalIgnoreCase))
        {
            var payload = ExtractTestRequirements(content);
            suggestion = new TaskSuggestion
            {
                TaskName = "generate-ai-tests", // Nome con "ai" + verbo creativo
                Payload = payload,
                Priority = TaskPriority.Low,
                Reason = "Rilevata richiesta di test",
                SourceFile = fileName,
                IsAiTask = true
            };
        }

        // 4. Rileva problemi di struttura
        else if (content.Contains("refactor", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("restructure", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("organize", StringComparison.OrdinalIgnoreCase))
        {
            var payload = ExtractStructureIssues(content);
            suggestion = new TaskSuggestion
            {
                TaskName = "optimize-structure", // Verbo creativo "optimize"
                Payload = payload,
                Priority = TaskPriority.Medium,
                Reason = "Rilevata richiesta di miglioramento struttura",
                SourceFile = fileName,
                IsAiTask = true
            };
        }

        // 5. Rileva mancanza di documentazione
        else if (content.Contains("document", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("readme", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("guide", StringComparison.OrdinalIgnoreCase))
        {
            var payload = ExtractDocumentationNeeds(content);
            suggestion = new TaskSuggestion
            {
                TaskName = "generate-ai-documentation", // Nome con "ai" + verbo creativo
                Payload = payload,
                Priority = TaskPriority.Low,
                Reason = "Rilevata richiesta di documentazione",
                SourceFile = fileName,
                IsAiTask = true
            };
        }

        // 6. Rileva file "ai-output" completati (FILE ALWAYS MODE)
        else if (fileName.StartsWith("ai-output-", StringComparison.OrdinalIgnoreCase))
        {
            if (content.Contains("✅ Completed", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("File AI completato: {FileName}", fileName);
                // Questo file è un output AI già completato, nessun task suggerito
                return null;
            }
        }

        // Verifica finale classificazione AI
        if (suggestion != null)
        {
            // Applica intelligent routing
            suggestion.IsAiTask = ShouldBeAiTask(suggestion.TaskName, suggestion.Payload);
            
            _logger.LogInformation("Task suggerito: {TaskName} (AI: {IsAi})", 
                suggestion.TaskName, suggestion.IsAiTask);
            
            return suggestion;
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
/// Suggerimento di task automatico con classificazione AI intelligente
/// </summary>
public class TaskSuggestion
{
    public string TaskName { get; set; } = "";
    public string Payload { get; set; } = "";
    public TaskPriority Priority { get; set; }
    public string Reason { get; set; } = "";
    public string SourceFile { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool IsAiTask { get; set; } = false; // ⭐ Nuovo: classificazione AI
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
