namespace CursorMonitorAgent;

/// <summary>
/// Gestisce il dialogo con l'utente tramite Control Center
/// </summary>
public class UserDialogService
{
    private readonly ILogger<UserDialogService> _logger;
    private readonly LogBuffer _logBuffer;
    private readonly List<UserQuestion> _pendingQuestions = new();
    private readonly object _lock = new();

    public UserDialogService(ILogger<UserDialogService> logger, LogBuffer logBuffer)
    {
        _logger = logger;
        _logBuffer = logBuffer;
    }

    /// <summary>
    /// Crea una nuova domanda per l'utente
    /// </summary>
    public UserQuestion AskUser(string question, string context, List<string>? options = null)
    {
        lock (_lock)
        {
            var userQuestion = new UserQuestion
            {
                Id = Guid.NewGuid().ToString(),
                Question = question,
                Context = context,
                Options = options ?? new List<string>(),
                Status = QuestionStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _pendingQuestions.Add(userQuestion);
            
            _logger.LogInformation("Nuova domanda per utente: {Question} (ID: {Id})", question, userQuestion.Id);
            _logBuffer.Add($"Domanda creata: {question}");

            return userQuestion;
        }
    }

    /// <summary>
    /// Recupera tutte le domande pendenti
    /// </summary>
    public List<UserQuestion> GetPendingQuestions()
    {
        lock (_lock)
        {
            return _pendingQuestions
                .Where(q => q.Status == QuestionStatus.Pending)
                .ToList();
        }
    }

    /// <summary>
    /// Recupera una domanda specifica
    /// </summary>
    public UserQuestion? GetQuestion(string id)
    {
        lock (_lock)
        {
            return _pendingQuestions.FirstOrDefault(q => q.Id == id);
        }
    }

    /// <summary>
    /// Risponde a una domanda
    /// </summary>
    public bool AnswerQuestion(string id, string answer)
    {
        lock (_lock)
        {
            var question = _pendingQuestions.FirstOrDefault(q => q.Id == id);
            
            if (question == null)
            {
                _logger.LogWarning("Domanda non trovata: {Id}", id);
                return false;
            }

            question.Answer = answer;
            question.Status = QuestionStatus.Answered;
            question.AnsweredAt = DateTime.UtcNow;

            _logger.LogInformation("Domanda risposta: {Question} -> {Answer}", question.Question, answer);
            _logBuffer.Add($"Risposta ricevuta per: {question.Question}");

            return true;
        }
    }

    /// <summary>
    /// Cancella una domanda
    /// </summary>
    public bool CancelQuestion(string id)
    {
        lock (_lock)
        {
            var question = _pendingQuestions.FirstOrDefault(q => q.Id == id);
            
            if (question == null)
            {
                return false;
            }

            question.Status = QuestionStatus.Cancelled;
            _logger.LogInformation("Domanda cancellata: {Id}", id);
            _logBuffer.Add($"Domanda cancellata: {question.Question}");

            return true;
        }
    }

    /// <summary>
    /// Rimuove domande vecchie (più di 1 ora)
    /// </summary>
    public int CleanupOldQuestions()
    {
        lock (_lock)
        {
            var cutoffTime = DateTime.UtcNow.AddHours(-1);
            var oldQuestions = _pendingQuestions
                .Where(q => q.CreatedAt < cutoffTime && q.Status != QuestionStatus.Pending)
                .ToList();

            foreach (var q in oldQuestions)
            {
                _pendingQuestions.Remove(q);
            }

            if (oldQuestions.Count > 0)
            {
                _logger.LogInformation("Rimosse {Count} domande vecchie", oldQuestions.Count);
            }

            return oldQuestions.Count;
        }
    }
}

/// <summary>
/// Rappresenta una domanda per l'utente
/// </summary>
public class UserQuestion
{
    public string Id { get; set; } = "";
    public string Question { get; set; } = "";
    public string Context { get; set; } = "";
    public List<string> Options { get; set; } = new();
    public string Answer { get; set; } = "";
    public QuestionStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? AnsweredAt { get; set; }
}

/// <summary>
/// Stato di una domanda
/// </summary>
public enum QuestionStatus
{
    Pending,
    Answered,
    Cancelled,
    Expired
}
