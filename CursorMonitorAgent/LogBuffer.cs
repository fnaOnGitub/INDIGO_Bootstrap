namespace CursorMonitorAgent;

/// <summary>
/// Buffer thread-safe per log eventi
/// </summary>
public class LogBuffer
{
    private readonly List<LogEntry> _logs = new();
    private readonly object _lock = new();
    private readonly int _maxEntries;

    public LogBuffer(int maxEntries = 100)
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
            var entry = new LogEntry
            {
                Timestamp = DateTime.UtcNow,
                Level = level,
                Message = message
            };

            _logs.Add(entry);

            // Mantieni solo gli ultimi N eventi
            if (_logs.Count > _maxEntries)
            {
                _logs.RemoveAt(0);
            }
        }
    }

    /// <summary>
    /// Recupera tutti i log
    /// </summary>
    public List<LogEntry> GetAll()
    {
        lock (_lock)
        {
            return _logs.ToList();
        }
    }

    /// <summary>
    /// Pulisce tutti i log
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
