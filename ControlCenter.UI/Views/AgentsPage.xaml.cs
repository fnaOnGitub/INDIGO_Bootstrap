using System.Windows;
using System.Windows.Controls;
using ControlCenter.UI.Models;
using ControlCenter.UI.ViewModels;

namespace ControlCenter.UI.Views;

/// <summary>
/// Interaction logic for AgentsPage.xaml
/// </summary>
public partial class AgentsPage : UserControl
{
    public AgentsPage()
    {
        InitializeComponent();
        DataContext = new AgentsViewModel();
    }

    private void DetailsButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is AgentInfoViewModel agent)
        {
            var detailWindow = new AgentDetailWindow(agent);
            detailWindow.Owner = Window.GetWindow(this);
            detailWindow.ShowDialog();
        }
    }
}
