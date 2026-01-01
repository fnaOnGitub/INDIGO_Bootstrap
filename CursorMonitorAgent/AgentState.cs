namespace CursorMonitorAgent;

/// <summary>
/// Stato dell'agente CursorMonitorAgent
/// </summary>
public class AgentState
{
    private readonly DateTime _startTime;
    private string _lastEvent;

    public AgentState()
    {
        _startTime = DateTime.UtcNow;
        _lastEvent = "Nessun evento";
    }

    public string Version => "1.0.0";
    
    public TimeSpan Uptime => DateTime.UtcNow - _startTime;
    
    public string LastEvent => _lastEvent;

    public void UpdateLastEvent(string eventDescription)
    {
        _lastEvent = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} - {eventDescription}";
    }
}
