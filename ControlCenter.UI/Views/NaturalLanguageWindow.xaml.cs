using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ControlCenter.UI.Services;

namespace ControlCenter.UI.Views;

/// <summary>
/// Natural Language Console - Interfaccia principale centrata sul linguaggio naturale
/// </summary>
public partial class NaturalLanguageWindow : Window
{
    private readonly LogService _logService;
    private string _selectedLogAgent = "System";

    public NaturalLanguageWindow()
    {
        InitializeComponent();

        // Ottieni LogService dall'app
        _logService = ((App)Application.Current).LogService;

        // Sottoscrivi agli aggiornamenti log
        _logService.LogUpdated += OnLogUpdated;

        // Carica log iniziali
        RefreshLogs();
    }

    private void OnLogUpdated(object? sender, LogUpdatedEventArgs e)
    {
        // Aggiorna solo se è l'agente selezionato
        if (e.AgentName == _selectedLogAgent)
        {
            Dispatcher.Invoke(() =>
            {
                // Aggiungi il nuovo log alla fine
                LogTextBox.AppendText(e.Entry.FormattedMessage + Environment.NewLine);

                // Mantieni solo gli ultimi 100 log per performance
                var lines = LogTextBox.Text.Split(Environment.NewLine);
                if (lines.Length > 100)
                {
                    var recentLines = lines.Skip(lines.Length - 100);
                    LogTextBox.Text = string.Join(Environment.NewLine, recentLines);
                }

                // Auto-scroll
                LogTextBox.ScrollToEnd();
            });
        }
    }

    private void SelectLogAgent_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string agentName)
        {
            _selectedLogAgent = agentName;

            // Reset stili pulsanti
            BtnLogSystem.Background = new SolidColorBrush(Color.FromRgb(49, 50, 68));
            BtnLogOrchestrator.Background = new SolidColorBrush(Color.FromRgb(49, 50, 68));
            BtnLogAiWorker.Background = new SolidColorBrush(Color.FromRgb(49, 50, 68));

            // Evidenzia pulsante selezionato
            button.Background = new SolidColorBrush(Color.FromRgb(99, 102, 241));

            // Ricarica log
            RefreshLogs();
        }
    }

    private void RefreshLogs()
    {
        var logs = _logService.GetLogs(_selectedLogAgent);

        // Prendi solo gli ultimi 100 log
        var recentLogs = logs.TakeLast(100);

        // Costruisci il testo completo dei log
        var logText = string.Join(Environment.NewLine, recentLogs.Select(log => log.FormattedMessage));

        LogTextBox.Text = logText;

        // Auto-scroll alla fine
        LogTextBox.ScrollToEnd();
    }

    protected override void OnClosed(EventArgs e)
    {
        // Unsub da eventi
        _logService.LogUpdated -= OnLogUpdated;

        base.OnClosed(e);
    }
}
