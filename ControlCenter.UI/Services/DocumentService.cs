using ControlCenter.UI.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ControlCenter.UI.Services;

/// <summary>
/// Servizio per gestire il caricamento della documentazione dalla cartella Assets/Docs
/// </summary>
public class DocumentService
{
    private readonly string _docsPath;

    public DocumentService()
    {
        // Path alla cartella Assets/Docs del progetto
        var appPath = AppContext.BaseDirectory;
        _docsPath = Path.Combine(appPath, "Assets", "Docs");
    }

    /// <summary>
    /// Carica tutti i documenti markdown dalla cartella Assets/Docs
    /// </summary>
    public async Task<List<DocumentInfo>> GetDocumentsAsync()
    {
        var documents = new List<DocumentInfo>();

        try
        {
            // Verifica che la cartella esista
            if (!Directory.Exists(_docsPath))
            {
                return documents;
            }

            // Cerca tutti i file .md nella cartella
            var markdownFiles = Directory.GetFiles(_docsPath, "*.md", SearchOption.TopDirectoryOnly);

            foreach (var filePath in markdownFiles)
            {
                try
                {
                    // Leggi il contenuto del file
                    var content = await File.ReadAllTextAsync(filePath);
                    
                    // Estrai il titolo dalla prima riga (se inizia con #)
                    var title = ExtractTitle(content, Path.GetFileNameWithoutExtension(filePath));
                    
                    // Estrai la descrizione (primi 100 caratteri dopo il titolo)
                    var description = ExtractDescription(content);

                    documents.Add(new DocumentInfo
                    {
                        Title = title,
                        Description = description,
                        FilePath = filePath,
                        Icon = "\uE8A5" // Document icon
                    });
                }
                catch
                {
                    // Ignora file che non possono essere letti
                    continue;
                }
            }

            return documents.OrderBy(d => d.Title).ToList();
        }
        catch
        {
            return documents;
        }
    }

    /// <summary>
    /// Carica il contenuto di un documento specifico
    /// </summary>
    public async Task<string> LoadDocumentContentAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return $"⚠️ File non trovato: {filePath}";
            }

            return await File.ReadAllTextAsync(filePath);
        }
        catch (Exception ex)
        {
            return $"❌ Errore durante il caricamento del documento:\n\n{ex.Message}";
        }
    }

    /// <summary>
    /// Estrae il titolo dal contenuto markdown (prima riga con #)
    /// </summary>
    private string ExtractTitle(string content, string fallbackTitle)
    {
        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("# "))
            {
                return trimmed.Substring(2).Trim();
            }
        }

        return fallbackTitle;
    }

    /// <summary>
    /// Estrae la descrizione dal contenuto markdown (primi 100 caratteri di testo)
    /// </summary>
    private string ExtractDescription(string content)
    {
        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            
            // Salta titoli e righe vuote
            if (trimmed.StartsWith("#") || string.IsNullOrWhiteSpace(trimmed))
            {
                continue;
            }

            // Prendi i primi 100 caratteri
            if (trimmed.Length > 100)
            {
                return trimmed.Substring(0, 97) + "...";
            }

            return trimmed;
        }

        return "Nessuna descrizione disponibile";
    }
}
