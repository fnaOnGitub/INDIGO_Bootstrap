using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControlCenter.UI.Models;
using ControlCenter.UI.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace ControlCenter.UI.ViewModels;

/// <summary>
/// ViewModel per DocumentationPage
/// </summary>
public partial class DocumentationViewModel : ObservableObject
{
    private readonly DocumentService _documentService;

    public DocumentationViewModel()
    {
        _documentService = new DocumentService();
        Documents = new ObservableCollection<DocumentInfo>();
        _ = LoadDocumentsAsync();
    }

    [ObservableProperty]
    private ObservableCollection<DocumentInfo> _documents;

    [ObservableProperty]
    private bool _isDocsAvailable;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [RelayCommand]
    private async Task LoadDocumentsAsync()
    {
        IsLoading = true;
        Documents.Clear();

        try
        {
            var availableDocs = await _documentService.GetDocumentsAsync();

            if (availableDocs.Count == 0)
            {
                IsDocsAvailable = false;
                StatusMessage = "📁 Nessun documento disponibile nella cartella Assets/Docs";
                return;
            }

            foreach (var doc in availableDocs)
            {
                Documents.Add(doc);
            }

            IsDocsAvailable = true;
            StatusMessage = $"✅ {Documents.Count} documento/i caricato/i";
        }
        catch
        {
            IsDocsAvailable = false;
            StatusMessage = "❌ Errore durante il caricamento dei documenti";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public DocumentService GetDocumentService() => _documentService;
}
