using System.Windows;
using System.Windows.Controls;
using ControlCenter.UI.Models;
using ControlCenter.UI.ViewModels;

namespace ControlCenter.UI.Views;

/// <summary>
/// Interaction logic for DocumentationPage.xaml
/// </summary>
public partial class DocumentationPage : UserControl
{
    public DocumentationPage()
    {
        InitializeComponent();
        DataContext = new DocumentationViewModel();
    }

    private void OpenButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is DocumentInfo doc)
        {
            var docWindow = new DocumentViewerWindow(doc.Title, doc.FilePath);
            docWindow.Owner = Window.GetWindow(this);
            docWindow.ShowDialog();
        }
    }
}
