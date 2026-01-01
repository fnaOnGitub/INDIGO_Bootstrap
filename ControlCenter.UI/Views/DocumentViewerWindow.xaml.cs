using System.IO;
using System.Windows;

namespace ControlCenter.UI.Views;

/// <summary>
/// Interaction logic for DocumentViewerWindow.xaml
/// </summary>
public partial class DocumentViewerWindow : Window
{
    public string DocumentTitle { get; set; }

    public DocumentViewerWindow(string title, string filePath)
    {
        InitializeComponent();
        
        DocumentTitle = title;
        Title = title;
        
        LoadDocument(filePath);
    }

    private async void LoadDocument(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                var content = await File.ReadAllTextAsync(filePath);
                ContentTextBlock.Text = content;
            }
            else
            {
                ContentTextBlock.Text = $"Documento non trovato: {filePath}";
            }
        }
        catch (Exception ex)
        {
            ContentTextBlock.Text = $"Errore durante il caricamento del documento:\n{ex.Message}";
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
