namespace ControlCenter.UI.Models;

/// <summary>
/// Stato corrente del cluster (identico al server LocalApiServer)
/// </summary>
public class ClusterStatus
{
    public bool IsProvisioning { get; set; }
    public bool IsBuildingCluster { get; set; }
    public bool IsGeneratingAgents { get; set; }
    public bool IsConfiguringCommunication { get; set; }
    public bool IsValidating { get; set; }

    public bool ProvisioningCompleted { get; set; }
    public bool ClusterBuilt { get; set; }
    public bool AgentsGenerated { get; set; }
    public bool CommunicationConfigured { get; set; }
    public bool ValidationPassed { get; set; }

    public string LastCommand { get; set; } = "";
    public DateTime? LastCommandTime { get; set; }

    /// <summary>
    /// Verifica se è in corso un'operazione
    /// </summary>
    public bool IsBusy => IsProvisioning || IsBuildingCluster || IsGeneratingAgents || 
                          IsConfiguringCommunication || IsValidating;
}
