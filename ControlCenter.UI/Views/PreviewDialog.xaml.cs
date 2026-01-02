using System.Collections.Generic;
using System.Windows;

namespace ControlCenter.UI.Views;

/// <summary>
/// Interaction logic for PreviewDialog.xaml
/// </summary>
public partial class PreviewDialog : Window
{
    public UserPreviewChoice UserChoice { get; private set; } = UserPreviewChoice.Cancel;
    public PreviewData PreviewData { get; private set; }
    public event EventHandler? ExplainRequested;

    public PreviewDialog(PreviewData previewData)
    {
        InitializeComponent();
        
        PreviewData = previewData;
        
        // Popola i controlli
        FilesToCreateList.ItemsSource = previewData.FilesToCreate;
        FoldersToCreateList.ItemsSource = previewData.FoldersToCreate;
        FinalStructureTextBlock.Text = previewData.FinalStructure;
        TechnicalDetailsTextBlock.Text = previewData.TechnicalDetails;
        
        // Mostra/nascondi sezioni opzionali
        if (previewData.FilesToModify != null && previewData.FilesToModify.Count > 0)
        {
            FilesToModifyTitle.Visibility = Visibility.Visible;
            FilesToModifyList.Visibility = Visibility.Visible;
            FilesToModifyList.ItemsSource = previewData.FilesToModify;
        }
        
        if (previewData.FilesToRemove != null && previewData.FilesToRemove.Count > 0)
        {
            FilesToRemoveTitle.Visibility = Visibility.Visible;
            FilesToRemoveList.Visibility = Visibility.Visible;
            FilesToRemoveList.ItemsSource = previewData.FilesToRemove;
        }
    }

    private void Explain_Click(object sender, RoutedEventArgs e)
    {
        // Solleva l'evento per richiedere spiegazione
        ExplainRequested?.Invoke(this, EventArgs.Empty);
    }

    private void Proceed_Click(object sender, RoutedEventArgs e)
    {
        UserChoice = UserPreviewChoice.Proceed;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        UserChoice = UserPreviewChoice.Cancel;
        DialogResult = false;
        Close();
    }
}

/// <summary>
/// Scelta dell'utente nel preview dialog
/// </summary>
public enum UserPreviewChoice
{
    Cancel,
    Proceed
}

/// <summary>
/// Dati per l'anteprima delle modifiche
/// </summary>
public class PreviewData
{
    public List<string> FilesToCreate { get; set; } = new();
    public List<string> FoldersToCreate { get; set; } = new();
    public List<string>? FilesToModify { get; set; }
    public List<string>? FilesToRemove { get; set; }
    public string FinalStructure { get; set; } = "";
    public string TechnicalDetails { get; set; } = "";
    public string OperationType { get; set; } = ""; // "create-new-solution" o "add-project-to-current-solution"
}
