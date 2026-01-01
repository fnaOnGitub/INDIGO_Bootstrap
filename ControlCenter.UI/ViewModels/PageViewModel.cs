using CommunityToolkit.Mvvm.ComponentModel;

namespace ControlCenter.UI.ViewModels;

/// <summary>
/// ViewModel per PageView (visualizza contenuti markdown/documentazione)
/// </summary>
public partial class PageViewModel : ObservableObject
{
    public PageViewModel(string title, string content)
    {
        Title = title;
        Content = content;
    }

    [ObservableProperty]
    private string _title = "";

    [ObservableProperty]
    private string _content = "";
}
