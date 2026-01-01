using System.Windows.Controls;
using ControlCenter.UI.ViewModels;

namespace ControlCenter.UI.Views;

/// <summary>
/// Interaction logic for DashboardPage.xaml
/// </summary>
public partial class DashboardPage : UserControl
{
    public DashboardPage()
    {
        InitializeComponent();
        DataContext = new DashboardViewModel();
    }
}
