using System;
using System.Configuration;
using System.Data;
using System.Windows;
using ControlCenter.UI.Services;

namespace ControlCenter.UI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public LogService LogService { get; private set; } = null!;
    public ClusterProcessManager ClusterProcessManager { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Inizializza servizi
        LogService = new LogService();
        ClusterProcessManager = new ClusterProcessManager(LogService);

        // Log di avvio
        LogService.AppendLog("System", "=== CONTROL CENTER AVVIATO ===");
        LogService.AppendLog("System", $"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

        // ⭐ AVVIO AUTOMATICO DEL CLUSTER
        LogService.AppendLog("System", "🚀 Avvio automatico del cluster...");
        
        // Avvia in un Task separato per non bloccare la UI
        System.Threading.Tasks.Task.Run(async () =>
        {
            // Piccolo delay per permettere alla UI di caricarsi
            await System.Threading.Tasks.Task.Delay(1000);
            
            try
            {
                ClusterProcessManager.StartAllAgents();
                LogService.AppendLog("System", "✅ Cluster avviato automaticamente!");
            }
            catch (Exception ex)
            {
                LogService.AppendLog("System", $"❌ Errore avvio automatico: {ex.Message}", Services.LogLevel.Error);
            }
        });
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Ferma tutti gli agenti prima di uscire
        LogService.AppendLog("System", "=== ARRESTO CONTROL CENTER ===");
        ClusterProcessManager?.StopAllAgents();

        base.OnExit(e);
    }
}

