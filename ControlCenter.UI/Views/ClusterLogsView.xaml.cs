using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ControlCenter.UI.Services;

namespace ControlCenter.UI.Views;

public partial class ClusterLogsView : UserControl
{
    private readonly LogService _logService;
    private string _selectedAgent = "System";

    public ClusterLogsView()
    {
        InitializeComponent();
        
        // Ottieni LogService dall'app
        _logService = ((App)Application.Current).LogService;
        
        // Sottoscrivi agli aggiornamenti
        _logService.LogUpdated += OnLogUpdated;
        
        // Carica log iniziali
        RefreshLogs();
    }

    private void OnLogUpdated(object? sender, LogUpdatedEventArgs e)
    {
        // Aggiorna solo se è l'agente selezionato
        if (e.AgentName == _selectedAgent)
        {
            Dispatcher.Invoke(() =>
            {
                // Aggiungi il nuovo log alla fine
                LogTextBox.AppendText(e.Entry.FormattedMessage + Environment.NewLine);
                
                // Auto-scroll
                LogTextBox.ScrollToEnd();
            });
        }
    }

    private void SelectAgent_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string agentName)
        {
            _selectedAgent = agentName;
            RefreshLogs();
        }
    }

    private void RefreshLogs()
    {
        var logs = _logService.GetLogs(_selectedAgent);
        
        // Costruisci il testo completo dei log
        var logText = string.Join(Environment.NewLine, logs.Select(log => log.FormattedMessage));
        
        LogTextBox.Text = logText;
        
        // Auto-scroll alla fine
        LogTextBox.ScrollToEnd();
    }

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        RefreshLogs();
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        _logService.ClearLogs(_selectedAgent);
        LogTextBox.Clear();
    }
}
