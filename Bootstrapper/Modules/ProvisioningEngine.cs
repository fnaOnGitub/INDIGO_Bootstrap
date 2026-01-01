using Serilog;

namespace Bootstrapper.Modules;

/// <summary>
/// Gestisce il provisioning dei server: installazione Docker, WSL2, configurazione firewall, utenti
/// </summary>
public class ProvisioningEngine
{
    private readonly RemoteExecutor _remoteExecutor;

    public ProvisioningEngine(RemoteExecutor remoteExecutor)
    {
        _remoteExecutor = remoteExecutor;
    }

    /// <summary>
    /// Esegue il provisioning completo di tutti i server configurati
    /// </summary>
    public async Task ProvisionServers(ServersConfig config)
    {
        Log.Information("=== INIZIO PROVISIONING SERVER ===");

        foreach (var server in config.Servers)
        {
            Log.Information($"Provisioning server: {server.Name} ({server.Hostname})");
            await ProvisionSingleServer(server);
        }

        Log.Information("=== PROVISIONING COMPLETATO ===");
    }

    /// <summary>
    /// Esegue il provisioning di un singolo server
    /// </summary>
    private async Task ProvisionSingleServer(ServerInfo server)
    {
        try
        {
            // 1. Test connettività WinRM
            Log.Information($"[{server.Name}] Test connettività WinRM...");
            var isConnected = await _remoteExecutor.TestWinRMConnection(
                server.Hostname, 
                server.Username, 
                server.Password
            );

            if (!isConnected)
            {
                throw new Exception($"Impossibile connettersi a {server.Hostname} via WinRM");
            }

            // 2. Crea sessione persistente
            var runspace = _remoteExecutor.CreateRemoteSession(
                server.Hostname, 
                server.Username, 
                server.Password
            );

            // 3. Verifica sistema operativo
            Log.Information($"[{server.Name}] Verifica sistema operativo...");
            await VerifyOperatingSystem(server.Hostname);

            // 4. Configurazione firewall
            Log.Information($"[{server.Name}] Configurazione firewall...");
            await ConfigureFirewall(server.Hostname);

            // 5. Installazione WSL2 (se richiesto)
            if (server.InstallWsl2)
            {
                Log.Information($"[{server.Name}] Installazione WSL2...");
                await InstallWSL2(server.Hostname);
            }

            // 6. Installazione Docker Desktop (se richiesto)
            if (server.InstallDocker)
            {
                Log.Information($"[{server.Name}] Installazione Docker Desktop...");
                await InstallDockerDesktop(server.Hostname);
            }

            // 7. Configurazione utenti e permessi
            Log.Information($"[{server.Name}] Configurazione utenti e permessi...");
            await ConfigureUsers(server.Hostname, server.Username);

            // 8. Installazione PowerShell 7 (se necessario)
            Log.Information($"[{server.Name}] Verifica PowerShell 7...");
            await EnsurePowerShell7(server.Hostname);

            Log.Information($"[{server.Name}] Provisioning completato con successo");
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"[{server.Name}] Errore durante provisioning");
            throw;
        }
    }

    /// <summary>
    /// Verifica che il sistema operativo sia Windows 10/11 o Windows Server
    /// </summary>
    private async Task VerifyOperatingSystem(string hostname)
    {
        var script = @"
            $os = Get-CimInstance Win32_OperatingSystem
            Write-Output ""OS: $($os.Caption)""
            Write-Output ""Version: $($os.Version)""
            Write-Output ""Architecture: $($os.OSArchitecture)""
        ";

        var result = await _remoteExecutor.ExecuteRemoteCommand(hostname, script);

        if (!result.Success)
        {
            throw new Exception($"Impossibile verificare sistema operativo: {result.Errors}");
        }

        Log.Information($"[{hostname}] {result.Output}");

        if (!result.Output.Contains("Windows"))
        {
            throw new Exception("Sistema operativo non supportato. Richiesto Windows 10/11 o Windows Server");
        }
    }

    /// <summary>
    /// Configura regole firewall per Docker, WinRM, e porte agenti
    /// </summary>
    private async Task ConfigureFirewall(string hostname)
    {
        var script = @"
            # Abilita WinRM (se non già abilitato)
            Enable-PSRemoting -Force -SkipNetworkProfileCheck

            # Regole firewall per Docker
            New-NetFirewallRule -DisplayName ""Docker API"" -Direction Inbound -LocalPort 2375 -Protocol TCP -Action Allow -ErrorAction SilentlyContinue
            New-NetFirewallRule -DisplayName ""Docker Swarm"" -Direction Inbound -LocalPort 2377 -Protocol TCP -Action Allow -ErrorAction SilentlyContinue
            New-NetFirewallRule -DisplayName ""Docker Overlay"" -Direction Inbound -LocalPort 4789 -Protocol UDP -Action Allow -ErrorAction SilentlyContinue
            New-NetFirewallRule -DisplayName ""Docker Network"" -Direction Inbound -LocalPort 7946 -Protocol TCP -Action Allow -ErrorAction SilentlyContinue
            New-NetFirewallRule -DisplayName ""Docker Network UDP"" -Direction Inbound -LocalPort 7946 -Protocol UDP -Action Allow -ErrorAction SilentlyContinue

            # Regole per porte agenti (range 5000-6000)
            New-NetFirewallRule -DisplayName ""Indigo Agents"" -Direction Inbound -LocalPort 5000-6000 -Protocol TCP -Action Allow -ErrorAction SilentlyContinue

            Write-Output ""Firewall configurato con successo""
        ";

        var result = await _remoteExecutor.ExecuteRemoteCommand(hostname, script);

        if (!result.Success)
        {
            throw new Exception($"Errore durante configurazione firewall: {result.Errors}");
        }

        Log.Information($"[{hostname}] Firewall configurato");
    }

    /// <summary>
    /// Installa WSL2 sul server Windows
    /// </summary>
    private async Task InstallWSL2(string hostname)
    {
        var script = @"
            # Controlla se WSL è già installato
            $wslInstalled = Get-Command wsl -ErrorAction SilentlyContinue

            if ($wslInstalled) {
                Write-Output ""WSL già installato""
                wsl --status
            } else {
                Write-Output ""Installazione WSL2...""
                
                # Abilita funzionalità WSL
                dism.exe /online /enable-feature /featurename:Microsoft-Windows-Subsystem-Linux /all /norestart
                dism.exe /online /enable-feature /featurename:VirtualMachinePlatform /all /norestart

                # Scarica e installa kernel update per WSL2
                $kernelUrl = ""https://wslstorestorage.blob.core.windows.net/wslblob/wsl_update_x64.msi""
                $kernelPath = ""$env:TEMP\wsl_update_x64.msi""
                Invoke-WebRequest -Uri $kernelUrl -OutFile $kernelPath
                Start-Process msiexec.exe -ArgumentList ""/i $kernelPath /quiet"" -Wait

                # Imposta WSL2 come versione default
                wsl --set-default-version 2

                Write-Output ""WSL2 installato. RIAVVIO RICHIESTO per completare installazione""
                Write-Output ""REBOOT_REQUIRED""
            }
        ";

        var result = await _remoteExecutor.ExecuteRemoteCommand(hostname, script);

        if (!result.Success)
        {
            throw new Exception($"Errore durante installazione WSL2: {result.Errors}");
        }

        Log.Information($"[{hostname}] WSL2: {result.Output}");
    }

    /// <summary>
    /// Installa Docker Desktop su Windows
    /// </summary>
    private async Task InstallDockerDesktop(string hostname)
    {
        var script = @"
            # Verifica se è Windows Server (Docker Desktop non supportato)
            $os = Get-CimInstance Win32_OperatingSystem
            if ($os.Caption -like '*Server*') {
                Write-Output ""Docker Desktop non supportato su Windows Server""
                exit 1
            }

            # Controlla se Docker è già installato
            $dockerInstalled = Get-Command docker -ErrorAction SilentlyContinue

            if ($dockerInstalled) {
                Write-Output ""Docker già installato""
                docker --version
            } else {
                Write-Output ""Installazione Docker Desktop...""
                
                # Scarica Docker Desktop installer
                $dockerUrl = ""https://desktop.docker.com/win/main/amd64/Docker%20Desktop%20Installer.exe""
                $dockerPath = ""$env:TEMP\DockerDesktopInstaller.exe""
                
                Invoke-WebRequest -Uri $dockerUrl -OutFile $dockerPath
                
                # Installa Docker Desktop
                Start-Process $dockerPath -ArgumentList ""install --quiet"" -Wait
                
                Write-Output ""Docker Desktop installato. Riavvio del servizio...""
                
                # Avvia servizio Docker
                Start-Service docker -ErrorAction SilentlyContinue
                
                Write-Output ""Docker installato con successo""
            }
        ";

        var result = await _remoteExecutor.ExecuteRemoteCommand(hostname, script);

        if (!result.Success)
        {
            throw new Exception($"Errore durante installazione Docker: {result.Errors}");
        }

        Log.Information($"[{hostname}] Docker: {result.Output}");
    }

    /// <summary>
    /// Configura utenti e gruppi per Docker
    /// </summary>
    private async Task ConfigureUsers(string hostname, string username)
    {
        var script = $@"
            # Aggiungi utente al gruppo docker-users
            Add-LocalGroupMember -Group 'docker-users' -Member '{username}' -ErrorAction SilentlyContinue

            Write-Output ""Utente {username} aggiunto a docker-users""
        ";

        var result = await _remoteExecutor.ExecuteRemoteCommand(hostname, script);

        if (!result.Success)
        {
            Log.Warning($"[{hostname}] Attenzione durante configurazione utenti: {result.Errors}");
        }
        else
        {
            Log.Information($"[{hostname}] Utenti configurati");
        }
    }

    /// <summary>
    /// Verifica/installa PowerShell 7
    /// </summary>
    private async Task EnsurePowerShell7(string hostname)
    {
        var script = @"
            # Controlla versione PowerShell
            $psVersion = $PSVersionTable.PSVersion.Major

            if ($psVersion -ge 7) {
                Write-Output ""PowerShell $psVersion già installato""
            } else {
                Write-Output ""Installazione PowerShell 7...""
                
                # Installa tramite MSI
                $psUrl = ""https://github.com/PowerShell/PowerShell/releases/latest/download/PowerShell-7.4.1-win-x64.msi""
                $psPath = ""$env:TEMP\pwsh.msi""
                Invoke-WebRequest -Uri $psUrl -OutFile $psPath
                Start-Process msiexec.exe -ArgumentList ""/i $psPath /quiet"" -Wait
                
                Write-Output ""PowerShell 7 installato""
            }
        ";

        var result = await _remoteExecutor.ExecuteRemoteCommand(hostname, script);

        if (!result.Success)
        {
            Log.Warning($"[{hostname}] Attenzione durante verifica PowerShell 7: {result.Errors}");
        }
        else
        {
            Log.Information($"[{hostname}] {result.Output}");
        }
    }
}
