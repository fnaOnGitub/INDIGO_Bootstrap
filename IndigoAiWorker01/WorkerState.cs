namespace IndigoAiWorker01;

/// <summary>
/// Stato interno di IndigoAiWorker01
/// </summary>
public class WorkerState
{
    private readonly DateTime _startTime;
    private string _lastTask = "N/A";

    public WorkerState()
    {
        _startTime = DateTime.UtcNow;
    }

    public DateTime StartTime => _startTime;
    
    public string Version => "1.0.0-AI";
    
    public TimeSpan Uptime => DateTime.UtcNow - _startTime;
    
    public string LastTask => _lastTask;

    public void UpdateLastTask(string task)
    {
        _lastTask = task;
    }
}
