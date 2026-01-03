using System.Windows;

namespace ControlCenter.UI.Views;

/// <summary>
/// Dialog per gestire conflitto cartella esistente
/// </summary>
public partial class FolderExistsDialog : Window
{
    public FolderExistsAction UserAction { get; private set; } = FolderExistsAction.Cancel;
    public string? NewSolutionName { get; private set; }

    public FolderExistsDialog(string existingPath, string suggestedAlternativeName)
    {
        InitializeComponent();
        ExistingPathText.Text = existingPath;
        SuggestedNameText.Text = suggestedAlternativeName;
        NewSolutionName = suggestedAlternativeName;
    }

    private void BtnOverwrite_Click(object sender, RoutedEventArgs e)
    {
        // Conferma ulteriore per evitare cancellazioni accidentali
        var result = MessageBox.Show(
            "Sei sicuro di voler sovrascrivere la cartella esistente?\n\nQuesta operazione eliminerà tutti i file presenti nella cartella.",
            "⚠️ Conferma sovrascrittura",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No
        );

        if (result == MessageBoxResult.Yes)
        {
            UserAction = FolderExistsAction.Overwrite;
            DialogResult = true;
            Close();
        }
    }

    private void BtnUseSuggested_Click(object sender, RoutedEventArgs e)
    {
        // Usa direttamente il nome suggerito senza richiedere input
        UserAction = FolderExistsAction.UseSuggestedName;
        DialogResult = true;
        Close();
    }

    private void BtnDifferentName_Click(object sender, RoutedEventArgs e)
    {
        // Mostra dialog per inserire nuovo nome personalizzato
        var inputDialog = new InputDialog("Nuovo nome soluzione", "Inserisci il nome per la nuova soluzione:", NewSolutionName ?? "MyNewSolution_1");
        
        if (inputDialog.ShowDialog() == true)
        {
            NewSolutionName = inputDialog.UserInput;
            UserAction = FolderExistsAction.UseCustomName;
            DialogResult = true;
            Close();
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        UserAction = FolderExistsAction.Cancel;
        DialogResult = false;
        Close();
    }
}

/// <summary>
/// Azione scelta dall'utente
/// </summary>
public enum FolderExistsAction
{
    Cancel,
    Overwrite,
    UseSuggestedName,
    UseCustomName
}
