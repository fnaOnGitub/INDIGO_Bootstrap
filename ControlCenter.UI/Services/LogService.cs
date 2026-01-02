using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace ControlCenter.UI.Services;

/// <summary>
/// Servizio per la gestione centralizzata dei log degli agenti
/// </summary>
public class LogService
{
    private readonly ConcurrentDictionary<string, List<LogEntry>> _logs = new();
    private readonly object _lock = new();

    public event EventHandler<LogUpdatedEventArgs>? LogUpdated;

    /// <summary>
    /// Aggiunge una riga di log per un agente specifico
    /// </summary>
    public void AppendLog(string agentName, string message, LogLevel level = LogLevel.Info)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        var entry = new LogEntry
        {
            Timestamp = DateTime.Now,
            AgentName = agentName,
            Message = message.Trim(),
            Level = level
        };

        lock (_lock)
        {
            if (!_logs.ContainsKey(agentName))
            {
                _logs[agentName] = new List<LogEntry>();
            }

            _logs[agentName].Add(entry);

            // Mantieni solo gli ultimi 1000 log per agente per evitare memory leak
            if (_logs[agentName].Count > 1000)
            {
                _logs[agentName].RemoveAt(0);
            }
        }

        // Notifica gli observer
        LogUpdated?.Invoke(this, new LogUpdatedEventArgs
        {
            AgentName = agentName,
            Entry = entry
        });
    }

    /// <summary>
    /// Ottiene tutti i log di un agente specifico
    /// </summary>
    public List<LogEntry> GetLogs(string agentName)
    {
        lock (_lock)
        {
            return _logs.TryGetValue(agentName, out var logs)
                ? new List<LogEntry>(logs)
                : new List<LogEntry>();
        }
    }

    /// <summary>
    /// Ottiene tutti i log di tutti gli agenti
    /// </summary>
    public Dictionary<string, List<LogEntry>> GetAllLogs()
    {
        lock (_lock)
        {
            return _logs.ToDictionary(
                kvp => kvp.Key,
                kvp => new List<LogEntry>(kvp.Value)
            );
        }
    }

    /// <summary>
    /// Pulisce i log di un agente specifico
    /// </summary>
    public void ClearLogs(string agentName)
    {
        lock (_lock)
        {
            if (_logs.ContainsKey(agentName))
            {
                _logs[agentName].Clear();
            }
        }
    }

    /// <summary>
    /// Pulisce tutti i log
    /// </summary>
    public void ClearAllLogs()
    {
        lock (_lock)
        {
            _logs.Clear();
        }
    }

    /// <summary>
    /// Ottiene la lista degli agenti che hanno log
    /// </summary>
    public List<string> GetAgentNames()
    {
        lock (_lock)
        {
            return _logs.Keys.ToList();
        }
    }
}

/// <summary>
/// Rappresenta una singola entry di log
/// </summary>
public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public string AgentName { get; set; } = "";
    public string Message { get; set; } = "";
    public LogLevel Level { get; set; }

    public string FormattedTimestamp => Timestamp.ToString("HH:mm:ss.fff");

    public string FormattedMessage => $"[{FormattedTimestamp}] {Message}";
}

/// <summary>
/// Livello di log
/// </summary>
public enum LogLevel
{
    Info,
    Warning,
    Error
}

/// <summary>
/// Event args per notifiche di log
/// </summary>
public class LogUpdatedEventArgs : EventArgs
{
    public string AgentName { get; set; } = "";
    public LogEntry Entry { get; set; } = new();
}
