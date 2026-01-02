using System.Windows;
using ControlCenter.UI.Services;
using Microsoft.Win32;

namespace ControlCenter.UI.Views;

/// <summary>
/// Interaction logic for SolutionConfirmationDialog.xaml
/// </summary>
public partial class SolutionConfirmationDialog : Window
{
    private readonly ConfigService _configService;

    public UserChoice UserChoice { get; private set; } = UserChoice.Cancel;
    public string OriginalPayload { get; private set; } = "";
    public string? SelectedTargetPath { get; private set; }
    public event EventHandler? ExplainRequested;

    public SolutionConfirmationDialog(ProposalData proposalData, string originalPayload, ConfigService configService)
    {
        InitializeComponent();
        
        _configService = configService;
        OriginalPayload = originalPayload;
        
        // Popola i controlli
        FeaturesListBox.ItemsSource = proposalData.Features;
        StructureTextBlock.Text = proposalData.ProposedStructure;
        ModulesListBox.ItemsSource = proposalData.Modules;
    }

    private void Explain_Click(object sender, RoutedEventArgs e)
    {
        // Solleva l'evento per richiedere spiegazione
        ExplainRequested?.Invoke(this, EventArgs.Empty);
    }

    private void CreateNew_Click(object sender, RoutedEventArgs e)
    {
        // Chiedi il percorso prima di confermare
        var targetPath = SelectTargetPath();
        
        if (string.IsNullOrEmpty(targetPath))
        {
            // Utente ha annullato la selezione del percorso
            MessageBox.Show(
                "Devi selezionare un percorso per creare la soluzione.",
                "Percorso Richiesto",
                MessageBoxButton.OK,
                MessageBoxImage.Warning
            );
            return;
        }

        SelectedTargetPath = targetPath;
        UserChoice = UserChoice.CreateNewSolution;
        DialogResult = true;
        Close();
    }

    private void AddToCurrent_Click(object sender, RoutedEventArgs e)
    {
        // Per "Aggiungi alla soluzione corrente" non serve il percorso
        UserChoice = UserChoice.AddToCurrentSolution;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        UserChoice = UserChoice.Cancel;
        DialogResult = false;
        Close();
    }

    /// <summary>
    /// Mostra un dialog per selezionare il percorso di creazione
    /// </summary>
    private string? SelectTargetPath()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Seleziona percorso per la nuova soluzione",
            Multiselect = false
        };

        // Preseleziona il percorso salvato se esiste
        if (!string.IsNullOrEmpty(_configService.DefaultSolutionPath) &&
            System.IO.Directory.Exists(_configService.DefaultSolutionPath))
        {
            dialog.InitialDirectory = _configService.DefaultSolutionPath;
        }

        if (dialog.ShowDialog() == true)
        {
            var selectedPath = dialog.FolderName;
            
            // Salva il percorso in configurazione
            _configService.DefaultSolutionPath = selectedPath;
            
            return selectedPath;
        }

        return null;
    }
}

/// <summary>
/// Enum per la scelta dell'utente
/// </summary>
public enum UserChoice
{
    Cancel,
    CreateNewSolution,
    AddToCurrentSolution
}
