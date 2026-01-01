namespace Agent.Orchestrator;

/// <summary>
/// Buffer thread-safe per mantenere gli ultimi eventi/log
/// </summary>
public class LogBuffer
{
    private readonly List<LogEntry> _logs = new();
    private readonly object _lock = new();
    private readonly int _maxEntries;

    public LogBuffer(int maxEntries = 50)
    {
        _maxEntries = maxEntries;
    }

    /// <summary>
    /// Aggiunge un evento al buffer
    /// </summary>
    public void Add(string message, string level = "INFO")
    {
        lock (_lock)
        {
            _logs.Add(new LogEntry
            {
                Timestamp = DateTime.UtcNow,
                Level = level,
                Message = message
            });

            // Mantieni solo gli ultimi N elementi
            if (_logs.Count > _maxEntries)
            {
                _logs.RemoveAt(0);
            }
        }
    }

    /// <summary>
    /// Ottiene tutti gli eventi nel buffer
    /// </summary>
    public List<LogEntry> GetAll()
    {
        lock (_lock)
        {
            return _logs.ToList();
        }
    }

    /// <summary>
    /// Svuota il buffer
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _logs.Clear();
        }
    }
}

/// <summary>
/// Singolo evento di log
/// </summary>
public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = "INFO";
    public string Message { get; set; } = "";
}
