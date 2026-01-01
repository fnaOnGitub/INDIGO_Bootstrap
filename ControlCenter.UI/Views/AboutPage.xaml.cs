using System.Windows.Controls;
using ControlCenter.UI.ViewModels;

namespace ControlCenter.UI.Views;

/// <summary>
/// Interaction logic for AboutPage.xaml
/// </summary>
public partial class AboutPage : UserControl
{
    public AboutPage()
    {
        InitializeComponent();
        DataContext = new AboutViewModel();
    }
}
