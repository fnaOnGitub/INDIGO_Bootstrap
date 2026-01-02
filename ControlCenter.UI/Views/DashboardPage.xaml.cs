using System;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using ControlCenter.UI.ViewModels;

namespace ControlCenter.UI.Views;

/// <summary>
/// Interaction logic for DashboardPage.xaml
/// </summary>
public partial class DashboardPage : UserControl
{
    private DispatcherTimer _refreshTimer;
    private ObservableCollection<WorkerStatusViewModel> _workers = new();

    public DashboardPage()
    {
        InitializeComponent();
        DataContext = new DashboardViewModel();

        // Inizializza lista workers
        InitializeWorkers();
        WorkersItemsControl.ItemsSource = _workers;

        // Timer per aggiornamento automatico ogni 2 secondi
        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _refreshTimer.Tick += (s, e) => UpdateWorkersStatus();
        _refreshTimer.Start();

        // Primo aggiornamento
        UpdateWorkersStatus();
    }

    private void InitializeWorkers()
    {
        _workers.Clear();
        _workers.Add(new WorkerStatusViewModel
        {
            Name = "Orchestrator",
            Description = "Coordinatore centrale - Routing intelligente",
            Port = 5001
        });
        _workers.Add(new WorkerStatusViewModel
        {
            Name = "IndigoAiWorker01",
            Description = "AI Worker - Generazione codice e soluzioni",
            Port = 5005
        });
    }

    private void UpdateWorkersStatus()
    {
        try
        {
            var app = (App)Application.Current;
            var diagnostics = app.ClusterProcessManager.GetAllDiagnostics();

            // Aggiorna Orchestrator
            var orchestrator = _workers[0];
            if (diagnostics.TryGetValue("Orchestrator", out var orchDiag))
            {
                orchestrator.Status = orchDiag.Status;
                orchestrator.Diagnostics = orchDiag;
            }
            else
            {
                orchestrator.Status = Services.AgentStatus.NotStarted;
            }

            // Aggiorna AI Worker
            var aiWorker = _workers[1];
            if (diagnostics.TryGetValue("IndigoAiWorker01", out var aiDiag))
            {
                aiWorker.Status = aiDiag.Status;
                aiWorker.Diagnostics = aiDiag;
            }
            else
            {
                aiWorker.Status = Services.AgentStatus.NotStarted;
            }

            // Aggiorna status generale
            var allRunning = orchestrator.Status == Services.AgentStatus.Running && 
                            aiWorker.Status == Services.AgentStatus.Running;
            var anyCrashed = orchestrator.Status == Services.AgentStatus.Crashed || 
                            aiWorker.Status == Services.AgentStatus.Crashed;
            var anyStarting = orchestrator.Status == Services.AgentStatus.Starting || 
                             aiWorker.Status == Services.AgentStatus.Starting;

            if (anyCrashed)
            {
                ClusterStatusText.Text = "❌ Cluster in errore (agente crashato)";
                ClusterStatusText.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
            }
            else if (allRunning)
            {
                ClusterStatusText.Text = "✅ Cluster operativo";
                ClusterStatusText.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129));
            }
            else if (anyStarting)
            {
                ClusterStatusText.Text = "⏳ Cluster in avvio...";
                ClusterStatusText.Foreground = new SolidColorBrush(Color.FromRgb(245, 158, 11));
            }
            else
            {
                ClusterStatusText.Text = "⏹️ Cluster fermo";
                ClusterStatusText.Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Errore aggiornamento status: {ex.Message}");
        }
    }

    private void RefreshWorkers_Click(object sender, RoutedEventArgs e)
    {
        UpdateWorkersStatus();
    }

    private void StartCluster_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ClusterStatusText.Text = "⏳ Avvio cluster in corso...";
            ClusterStatusText.Foreground = System.Windows.Media.Brushes.Orange;

            var app = (App)Application.Current;
            app.ClusterProcessManager.StartAllAgents();

            ClusterStatusText.Text = "✅ Cluster avviato!";
            ClusterStatusText.Foreground = System.Windows.Media.Brushes.Green;

            MessageBox.Show(
                "Cluster avviato con successo!\n\nVai su 'Cluster Logs' per vedere i log in tempo reale.",
                "Cluster Avviato",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }
        catch (System.Exception ex)
        {
            ClusterStatusText.Text = "❌ Errore durante l'avvio";
            ClusterStatusText.Foreground = System.Windows.Media.Brushes.Red;

            MessageBox.Show(
                $"Errore durante l'avvio del cluster:\n\n{ex.Message}",
                "Errore",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
    }

    private void StopCluster_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ClusterStatusText.Text = "⏳ Arresto cluster in corso...";
            ClusterStatusText.Foreground = System.Windows.Media.Brushes.Orange;

            var app = (App)Application.Current;
            app.ClusterProcessManager.StopAllAgents();

            ClusterStatusText.Text = "⏹️ Cluster fermato";
            ClusterStatusText.Foreground = System.Windows.Media.Brushes.Gray;

            MessageBox.Show(
                "Cluster fermato con successo!",
                "Cluster Fermato",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }
        catch (System.Exception ex)
        {
            ClusterStatusText.Text = "❌ Errore durante l'arresto";
            ClusterStatusText.Foreground = System.Windows.Media.Brushes.Red;

            MessageBox.Show(
                $"Errore durante l'arresto del cluster:\n\n{ex.Message}",
                "Errore",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
    }
}
