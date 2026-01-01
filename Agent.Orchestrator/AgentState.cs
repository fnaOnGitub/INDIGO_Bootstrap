namespace Agent.Orchestrator;

/// <summary>
/// Gestisce lo stato dell'agente Orchestrator
/// </summary>
public class AgentState
{
    private readonly DateTime _startTime;
    
    public AgentState()
    {
        _startTime = DateTime.UtcNow;
        LastCommand = "none";
        Version = "1.0.0";
    }
    
    public string LastCommand { get; set; }
    public string Version { get; }
    
    public TimeSpan Uptime => DateTime.UtcNow - _startTime;
    
    public void UpdateLastCommand(string command)
    {
        LastCommand = command;
    }
}
