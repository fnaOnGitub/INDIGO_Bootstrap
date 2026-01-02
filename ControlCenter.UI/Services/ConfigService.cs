using System.IO;
using System.Text.Json;

namespace ControlCenter.UI.Services;

/// <summary>
/// Servizio per gestire la configurazione del Control Center
/// </summary>
public class ConfigService
{
    private readonly string _configFilePath;
    private ControlCenterConfig _config;

    public ConfigService()
    {
        // Salva il file di configurazione nella cartella utente
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var configDir = Path.Combine(appDataPath, "IndigoLab", "ControlCenter");
        Directory.CreateDirectory(configDir);
        
        _configFilePath = Path.Combine(configDir, "ControlCenterConfig.json");
        _config = LoadConfig();
    }

    /// <summary>
    /// Percorso predefinito per la creazione di soluzioni
    /// </summary>
    public string? DefaultSolutionPath
    {
        get => _config.DefaultSolutionPath;
        set
        {
            _config.DefaultSolutionPath = value;
            SaveConfig();
        }
    }

    /// <summary>
    /// Carica la configurazione dal file
    /// </summary>
    private ControlCenterConfig LoadConfig()
    {
        try
        {
            if (File.Exists(_configFilePath))
            {
                var json = File.ReadAllText(_configFilePath);
                var config = JsonSerializer.Deserialize<ControlCenterConfig>(json);
                return config ?? new ControlCenterConfig();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Errore caricamento config: {ex.Message}");
        }

        return new ControlCenterConfig();
    }

    /// <summary>
    /// Salva la configurazione su file
    /// </summary>
    private void SaveConfig()
    {
        try
        {
            var json = JsonSerializer.Serialize(_config, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_configFilePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Errore salvataggio config: {ex.Message}");
        }
    }

    /// <summary>
    /// Ottiene il percorso del file di configurazione
    /// </summary>
    public string GetConfigFilePath() => _configFilePath;
}

/// <summary>
/// Modello per la configurazione del Control Center
/// </summary>
public class ControlCenterConfig
{
    public string? DefaultSolutionPath { get; set; }
}
