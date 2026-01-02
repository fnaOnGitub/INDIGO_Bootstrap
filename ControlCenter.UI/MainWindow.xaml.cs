using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ControlCenter.UI.Views;

namespace ControlCenter.UI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        // Carica la pagina Dashboard all'avvio
        NavigateTo(new DashboardPage());
    }

    private void BtnDashboard_Click(object sender, RoutedEventArgs e)
    {
        NavigateTo(new DashboardPage());
        UpdateActiveButton(BtnDashboard);
    }

    private void BtnAgents_Click(object sender, RoutedEventArgs e)
    {
        NavigateTo(new AgentsPage());
        UpdateActiveButton(BtnAgents);
    }

    private void BtnClusterLogs_Click(object sender, RoutedEventArgs e)
    {
        NavigateTo(new ClusterLogsView());
        UpdateActiveButton(BtnClusterLogs);
    }

    private void BtnDocumentation_Click(object sender, RoutedEventArgs e)
    {
        NavigateTo(new DocumentationPage());
        UpdateActiveButton(BtnDocumentation);
    }

    private void BtnAbout_Click(object sender, RoutedEventArgs e)
    {
        NavigateTo(new AboutPage());
        UpdateActiveButton(BtnAbout);
    }

    private void NavigateTo(UserControl page)
    {
        ContentArea.Content = page;
    }

    private void UpdateActiveButton(Button activeButton)
    {
        // Reset tutti i pulsanti
        var buttons = new[] { BtnDashboard, BtnAgents, BtnClusterLogs, BtnDocumentation, BtnAbout };
        foreach (var btn in buttons)
        {
            btn.Background = Brushes.Transparent;
        }
        
        // Evidenzia il pulsante attivo
        activeButton.Background = (SolidColorBrush)FindResource("IndigoPrimaryBrush");
    }
}
