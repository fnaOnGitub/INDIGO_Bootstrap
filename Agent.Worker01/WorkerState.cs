namespace Agent.Worker01;

/// <summary>
/// Gestisce lo stato dell'agente Worker01
/// </summary>
public class WorkerState
{
    private readonly DateTime _startTime;
    
    public WorkerState()
    {
        _startTime = DateTime.UtcNow;
        LastTask = "none";
        Version = "1.0.0";
    }
    
    public string LastTask { get; set; }
    public string Version { get; }
    
    public TimeSpan Uptime => DateTime.UtcNow - _startTime;
    
    public void UpdateLastTask(string task)
    {
        LastTask = task;
    }
}
