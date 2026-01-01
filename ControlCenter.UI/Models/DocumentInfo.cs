namespace ControlCenter.UI.Models;

/// <summary>
/// Rappresenta un documento di configurazione/documentazione
/// </summary>
public class DocumentInfo
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string Icon { get; set; } = "\uE8A5"; // Document icon
}
