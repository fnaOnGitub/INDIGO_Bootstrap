using System.Windows;

namespace ControlCenter.UI.Views;

/// <summary>
/// Interaction logic for ExplainDialog.xaml
/// </summary>
public partial class ExplainDialog : Window
{
    public ExplainDialog(ExplanationData explanationData)
    {
        InitializeComponent();
        
        // Popola i controlli
        NarrativeTextBlock.Text = explanationData.NarrativeExplanation;
        TechnicalReasonTextBlock.Text = explanationData.TechnicalReason;
        DependenciesTextBlock.Text = explanationData.Dependencies;
        ImpactConfirmTextBlock.Text = explanationData.ImpactIfConfirm;
        ImpactCancelTextBlock.Text = explanationData.ImpactIfCancel;
        AlternativesTextBlock.Text = explanationData.Alternatives;
        FullTechnicalDetailsTextBlock.Text = explanationData.FullTechnicalDetails;
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}

/// <summary>
/// Dati per la spiegazione di uno step
/// </summary>
public class ExplanationData
{
    public string NarrativeExplanation { get; set; } = "";
    public string TechnicalReason { get; set; } = "";
    public string Dependencies { get; set; } = "";
    public string ImpactIfConfirm { get; set; } = "";
    public string ImpactIfCancel { get; set; } = "";
    public string Alternatives { get; set; } = "";
    public string FullTechnicalDetails { get; set; } = "";
    public string StepId { get; set; } = "";
    public string StepType { get; set; } = "";
}
