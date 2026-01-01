namespace Agent.Monitor;

/// <summary>
/// Stato interno dell'Agent Monitor
/// </summary>
public class MonitorState
{
    private readonly DateTime _startTime;

    public MonitorState()
    {
        _startTime = DateTime.UtcNow;
    }

    public DateTime StartTime => _startTime;
    
    public string Version => "1.0.0";
    
    public TimeSpan Uptime => DateTime.UtcNow - _startTime;
}
