using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using ControlCenter.UI.Models;
using ControlCenter.UI.ViewModels;

namespace ControlCenter.UI.Views;

/// <summary>
/// Interaction logic for AgentDetailWindow.xaml
/// </summary>
public partial class AgentDetailWindow : Window
{
    private readonly AgentDetailViewModel _viewModel;
    private readonly DispatcherTimer _logTimer;
    private bool _isLogsVisible = false;

    public AgentDetailWindow(AgentInfoViewModel agent)
    {
        InitializeComponent();
        
        _viewModel = new AgentDetailViewModel(agent);
        DataContext = _viewModel;

        // Setup auto-refresh timer
        _logTimer = new DispatcherTimer();
        _logTimer.Interval = TimeSpan.FromSeconds(5);
        _logTimer.Tick += async (s, e) => await LoadLogsAsync();

        // Subscribe to auto-refresh changes
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(_viewModel.IsAutoRefreshEnabled))
            {
                if (_viewModel.IsAutoRefreshEnabled && _isLogsVisible)
                {
                    _logTimer.Start();
                }
                else
                {
                    _logTimer.Stop();
                }
            }
        };
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        _logTimer.Stop();
        Close();
    }

    private async void ToggleLogs_Click(object sender, RoutedEventArgs e)
    {
        _isLogsVisible = !_isLogsVisible;

        if (_isLogsVisible)
        {
            // Mostra la sezione log
            LogsSection.Visibility = Visibility.Visible;
            ToggleLogsButton.Content = "📋 Nascondi Log";

            // Carica i log
            await LoadLogsAsync();

            // Avvia auto-refresh se abilitato
            if (_viewModel.IsAutoRefreshEnabled)
            {
                _logTimer.Start();
            }
        }
        else
        {
            // Nascondi la sezione log
            LogsSection.Visibility = Visibility.Collapsed;
            ToggleLogsButton.Content = "📋 Mostra Log";

            // Ferma auto-refresh
            _logTimer.Stop();
        }
    }

    private async void RefreshLogs_Click(object sender, RoutedEventArgs e)
    {
        await LoadLogsAsync();
    }

    private async Task LoadLogsAsync()
    {
        await _viewModel.LoadLogsAsync();
    }

    protected override void OnClosed(EventArgs e)
    {
        _logTimer.Stop();
        base.OnClosed(e);
    }

    private void OpenCursorFolder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!string.IsNullOrEmpty(_viewModel.AiTaskResult?.CursorFilePath))
            {
                var folderPath = Path.GetDirectoryName(_viewModel.AiTaskResult.CursorFilePath);
                if (!string.IsNullOrEmpty(folderPath) && Directory.Exists(folderPath))
                {
                    Process.Start("explorer.exe", folderPath);
                }
                else
                {
                    MessageBox.Show("Cartella non trovata.", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Errore nell'apertura della cartella:\n{ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void ReloadFilePreview_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.LoadFilePreviewAsync();
    }
}
