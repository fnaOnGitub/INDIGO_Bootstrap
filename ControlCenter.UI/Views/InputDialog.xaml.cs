using System.Windows;
using System.Windows.Input;

namespace ControlCenter.UI.Views;

/// <summary>
/// Dialog semplice per input testo
/// </summary>
public partial class InputDialog : Window
{
    public string UserInput { get; private set; } = "";

    public InputDialog(string title, string label, string defaultValue = "")
    {
        InitializeComponent();
        TitleText.Text = title;
        LabelText.Text = label;
        InputTextBox.Text = defaultValue;
        InputTextBox.SelectAll();
        InputTextBox.Focus();
    }

    private void BtnOK_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(InputTextBox.Text))
        {
            UserInput = InputTextBox.Text.Trim();
            DialogResult = true;
            Close();
        }
        else
        {
            MessageBox.Show("Il nome non può essere vuoto.", "Errore", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void InputTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            BtnOK_Click(sender, e);
        }
        else if (e.Key == Key.Escape)
        {
            BtnCancel_Click(sender, e);
        }
    }
}
