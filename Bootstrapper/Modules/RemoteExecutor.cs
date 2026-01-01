using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Security;
using Serilog;

namespace Bootstrapper.Modules;

/// <summary>
/// Gestisce sessioni PowerShell Remoting (WinRM) e l'esecuzione di comandi remoti
/// </summary>
public class RemoteExecutor
{
    private readonly Dictionary<string, Runspace> _activeRunspaces = new();

    /// <summary>
    /// Crea una sessione PowerShell remota su un server target
    /// </summary>
    public Runspace CreateRemoteSession(string hostname, string username, string password)
    {
        try
        {
            Log.Information($"Creazione sessione remota su {hostname} con utente {username}");

            // Converti password in SecureString
            var securePassword = ConvertToSecureString(password);
            var credential = new PSCredential(username, securePassword);

            // Configura connessione remota
            var connectionInfo = new WSManConnectionInfo(
                new Uri($"http://{hostname}:5985/wsman"),
                "http://schemas.microsoft.com/powershell/Microsoft.PowerShell",
                credential
            );

            connectionInfo.AuthenticationMechanism = AuthenticationMechanism.Default;
            connectionInfo.SkipCACheck = true;
            connectionInfo.SkipCNCheck = true;
            connectionInfo.SkipRevocationCheck = true;

            // Crea e apri runspace
            var runspace = RunspaceFactory.CreateRunspace(connectionInfo);
            runspace.Open();

            // Salva per riutilizzo
            _activeRunspaces[hostname] = runspace;

            Log.Information($"Sessione remota su {hostname} creata con successo");
            return runspace;
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Errore durante creazione sessione remota su {hostname}");
            throw;
        }
    }

    /// <summary>
    /// Esegue un comando PowerShell su una sessione remota esistente
    /// </summary>
    public async Task<PSResult> ExecuteRemoteCommand(string hostname, string command)
    {
        try
        {
            Log.Debug($"Esecuzione comando su {hostname}: {command}");

            if (!_activeRunspaces.ContainsKey(hostname))
            {
                throw new InvalidOperationException($"Nessuna sessione attiva per {hostname}");
            }

            var runspace = _activeRunspaces[hostname];

            using var ps = PowerShell.Create();
            ps.Runspace = runspace;
            ps.AddScript(command);

            // Esegui in modo asincrono
            var results = await Task.Run(() => ps.Invoke());

            // Raccogli output e errori
            var output = string.Join(Environment.NewLine, results.Select(r => r.ToString()));
            var errors = string.Join(Environment.NewLine, ps.Streams.Error.Select(e => e.ToString()));
            var hasErrors = ps.HadErrors;

            Log.Debug($"Comando su {hostname} completato. Errori: {hasErrors}");

            return new PSResult
            {
                Success = !hasErrors,
                Output = output,
                Errors = errors
            };
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Errore durante esecuzione comando su {hostname}");
            return new PSResult
            {
                Success = false,
                Output = "",
                Errors = ex.Message
            };
        }
    }

    /// <summary>
    /// Esegue un comando PowerShell creando una nuova sessione one-shot
    /// </summary>
    public async Task<PSResult> ExecuteRemoteCommandOneShot(
        string hostname, 
        string username, 
        string password, 
        string command)
    {
        Runspace? runspace = null;
        try
        {
            runspace = CreateRemoteSession(hostname, username, password);
            
            using var ps = PowerShell.Create();
            ps.Runspace = runspace;
            ps.AddScript(command);

            var results = await Task.Run(() => ps.Invoke());

            var output = string.Join(Environment.NewLine, results.Select(r => r.ToString()));
            var errors = string.Join(Environment.NewLine, ps.Streams.Error.Select(e => e.ToString()));
            var hasErrors = ps.HadErrors;

            return new PSResult
            {
                Success = !hasErrors,
                Output = output,
                Errors = errors
            };
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Errore durante esecuzione one-shot su {hostname}");
            return new PSResult
            {
                Success = false,
                Output = "",
                Errors = ex.Message
            };
        }
        finally
        {
            runspace?.Close();
            runspace?.Dispose();
        }
    }

    /// <summary>
    /// Testa la connettività WinRM verso un server
    /// </summary>
    public async Task<bool> TestWinRMConnection(string hostname, string username, string password)
    {
        try
        {
            Log.Information($"Test connettività WinRM su {hostname}");

            var result = await ExecuteRemoteCommandOneShot(
                hostname,
                username,
                password,
                "Write-Output 'WinRM OK'"
            );

            if (result.Success && result.Output.Contains("WinRM OK"))
            {
                Log.Information($"Test WinRM su {hostname} riuscito");
                return true;
            }

            Log.Warning($"Test WinRM su {hostname} fallito: {result.Errors}");
            return false;
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Errore durante test WinRM su {hostname}");
            return false;
        }
    }

    /// <summary>
    /// Esegue uno script PowerShell da file
    /// </summary>
    public async Task<PSResult> ExecuteRemoteScript(string hostname, string scriptPath)
    {
        try
        {
            if (!File.Exists(scriptPath))
            {
                throw new FileNotFoundException($"Script non trovato: {scriptPath}");
            }

            var scriptContent = await File.ReadAllTextAsync(scriptPath);
            return await ExecuteRemoteCommand(hostname, scriptContent);
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Errore durante esecuzione script {scriptPath} su {hostname}");
            return new PSResult
            {
                Success = false,
                Output = "",
                Errors = ex.Message
            };
        }
    }

    /// <summary>
    /// Chiude una sessione remota
    /// </summary>
    public void CloseSession(string hostname)
    {
        if (_activeRunspaces.ContainsKey(hostname))
        {
            Log.Information($"Chiusura sessione remota su {hostname}");
            _activeRunspaces[hostname].Close();
            _activeRunspaces[hostname].Dispose();
            _activeRunspaces.Remove(hostname);
        }
    }

    /// <summary>
    /// Chiude tutte le sessioni attive
    /// </summary>
    public void CloseAllSessions()
    {
        Log.Information("Chiusura di tutte le sessioni remote");
        foreach (var hostname in _activeRunspaces.Keys.ToList())
        {
            CloseSession(hostname);
        }
    }

    /// <summary>
    /// Copia un file su un server remoto
    /// </summary>
    public async Task<bool> CopyFileToRemote(
        string hostname, 
        string localPath, 
        string remotePath)
    {
        try
        {
            Log.Information($"Copia file {localPath} su {hostname}:{remotePath}");

            var fileContent = await File.ReadAllBytesAsync(localPath);
            var base64Content = Convert.ToBase64String(fileContent);

            var script = $@"
                $bytes = [System.Convert]::FromBase64String('{base64Content}')
                [System.IO.File]::WriteAllBytes('{remotePath}', $bytes)
                Write-Output 'File copiato con successo'
            ";

            var result = await ExecuteRemoteCommand(hostname, script);
            return result.Success;
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Errore durante copia file su {hostname}");
            return false;
        }
    }

    private SecureString ConvertToSecureString(string password)
    {
        var securePassword = new SecureString();
        foreach (char c in password)
        {
            securePassword.AppendChar(c);
        }
        securePassword.MakeReadOnly();
        return securePassword;
    }
}

/// <summary>
/// Risultato di un comando PowerShell remoto
/// </summary>
public class PSResult
{
    public bool Success { get; set; }
    public string Output { get; set; } = "";
    public string Errors { get; set; } = "";
}
