namespace ControlCenter.UI.Models;

/// <summary>
/// Entry di log dal Bootstrapper
/// </summary>
public class LogEntry
{
    public string Timestamp { get; set; } = "";
    public string Level { get; set; } = "";
    public string Message { get; set; } = "";
}
