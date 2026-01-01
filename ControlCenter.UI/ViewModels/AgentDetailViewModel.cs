using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControlCenter.UI.Models;
using ControlCenter.UI.Services;

namespace ControlCenter.UI.ViewModels;

/// <summary>
/// ViewModel per AgentDetailModal
/// </summary>
public partial class AgentDetailViewModel : ObservableObject
{
    private readonly AgentService _agentService;

    public AgentDetailViewModel(AgentInfoViewModel agent)
    {
        Agent = agent;
        _agentService = new AgentService();
    }

    [ObservableProperty]
    private AgentInfoViewModel _agent = new();

    // Test Agent Properties
    [ObservableProperty]
    private bool _isTesting;

    [ObservableProperty]
    private bool _testCompleted;

    [ObservableProperty]
    private bool _testSuccess;

    [ObservableProperty]
    private string _testMessage = "";

    // Dispatch Task Properties
    [ObservableProperty]
    private string _taskName = "";

    [ObservableProperty]
    private string _taskPayload = "";

    [ObservableProperty]
    private bool _isDispatching;

    [ObservableProperty]
    private bool _dispatchCompleted;

    [ObservableProperty]
    private bool _dispatchSuccess;

    [ObservableProperty]
    private string _dispatchMessage = "";

    // Log Properties
    [ObservableProperty]
    private bool _isLoadingLogs;

    [ObservableProperty]
    private string _logsText = "Nessun log disponibile. Clicca 'Aggiorna Log' per caricare.";

    [ObservableProperty]
    private int _logCount;

    [ObservableProperty]
    private bool _isAutoRefreshEnabled;

    // AI Task Result Properties
    [ObservableProperty]
    private bool _isAiTaskResult;

    [ObservableProperty]
    private AiTaskResultModel? _aiTaskResult;

    [RelayCommand]
    private async Task TestAgentAsync()
    {
        // Log quando il comando parte
        Debug.WriteLine($"[AgentDetailViewModel] Comando TestAgent avviato per agente: {Agent.Name}");
        
        // Imposta stato iniziale
        IsTesting = true;
        TestCompleted = false;
        TestSuccess = false;
        TestMessage = "Testing in corso...";
        
        try
        {
            // Chiama il Bootstrapper per testare l'agente
            var result = await _agentService.TestAgentAsync(Agent.Name);
            
            // Log quando arriva la risposta
            Debug.WriteLine($"[AgentDetailViewModel] Risposta ricevuta dal service");
            
            // Imposta il risultato
            TestSuccess = result.Success;
            
            // Log del valore di TestSuccess
            Debug.WriteLine($"[AgentDetailViewModel] TestSuccess = {TestSuccess}");
            
            // Costruisce il messaggio
            if (result.Success)
            {
                TestMessage = $"✅ Test riuscito!\n\n{result.Message}";
            }
            else
            {
                TestMessage = $"❌ Test fallito\n\n{result.Message}";
            }
            
            // Aggiunge dettagli se presenti
            if (!string.IsNullOrEmpty(result.Details))
            {
                TestMessage += $"\n\nDettagli:\n{result.Details}";
            }
            
            // Log del valore di TestMessage
            Debug.WriteLine($"[AgentDetailViewModel] TestMessage = {TestMessage}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AgentDetailViewModel] Eccezione durante test: {ex.Message}");
            TestSuccess = false;
            TestMessage = $"❌ Errore durante il test\n\n{ex.Message}";
            
            // Log dei valori finali in caso di errore
            Debug.WriteLine($"[AgentDetailViewModel] TestSuccess = {TestSuccess}");
            Debug.WriteLine($"[AgentDetailViewModel] TestMessage = {TestMessage}");
        }
        finally
        {
            TestCompleted = true;
            IsTesting = false;
            Debug.WriteLine($"[AgentDetailViewModel] Test completato. IsTesting = {IsTesting}, TestCompleted = {TestCompleted}");
        }
    }

    [RelayCommand]
    private async Task DispatchTaskAsync()
    {
        // Log quando il comando parte
        Debug.WriteLine($"[AgentDetailViewModel] Comando DispatchTask avviato per agente: {Agent.Name}");
        Debug.WriteLine($"[AgentDetailViewModel] TaskName: {TaskName}, TaskPayload: {TaskPayload}");
        
        // Validazione input
        if (string.IsNullOrWhiteSpace(TaskName))
        {
            DispatchSuccess = false;
            DispatchMessage = "❌ Errore: Il nome del task è obbligatorio";
            DispatchCompleted = true;
            return;
        }
        
        // Imposta stato iniziale
        IsDispatching = true;
        DispatchCompleted = false;
        DispatchSuccess = false;
        DispatchMessage = "Dispatching task in corso...";
        
        try
        {
            // Chiama l'Orchestrator tramite il servizio
            var result = await _agentService.DispatchTaskAsync(Agent.Name, TaskName, TaskPayload ?? "");
            
            // Log quando arriva la risposta
            Debug.WriteLine($"[AgentDetailViewModel] Risposta dispatch ricevuta dal service");
            
            // Imposta il risultato
            DispatchSuccess = result.Success;
            
            // Log del valore di DispatchSuccess
            Debug.WriteLine($"[AgentDetailViewModel] DispatchSuccess = {DispatchSuccess}");
            
            // Costruisce il messaggio
            if (result.Success)
            {
                DispatchMessage = $"✅ Task dispatched con successo!\n\n{result.Message}";
                
                // Processa risultato AI Task se disponibile
                if (result.IsAiTask && result.WorkerResult != null)
                {
                    ProcessAiTaskResult(result.WorkerResult.Value);
                }
                else if (result.WorkerResult != null)
                {
                    DispatchMessage += $"\n\nRisultato dal worker:\n{result.WorkerResult}";
                }
            }
            else
            {
                DispatchMessage = $"❌ Dispatch fallito\n\n{result.Message}";
                IsAiTaskResult = false;
            }
            
            // Log del valore di DispatchMessage
            Debug.WriteLine($"[AgentDetailViewModel] DispatchMessage = {DispatchMessage}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AgentDetailViewModel] Eccezione durante dispatch: {ex.Message}");
            DispatchSuccess = false;
            DispatchMessage = $"❌ Errore durante il dispatch\n\n{ex.Message}";
            
            // Log dei valori finali in caso di errore
            Debug.WriteLine($"[AgentDetailViewModel] DispatchSuccess = {DispatchSuccess}");
            Debug.WriteLine($"[AgentDetailViewModel] DispatchMessage = {DispatchMessage}");
        }
        finally
        {
            DispatchCompleted = true;
            IsDispatching = false;
            Debug.WriteLine($"[AgentDetailViewModel] Dispatch completato. IsDispatching = {IsDispatching}, DispatchCompleted = {DispatchCompleted}");
        }
    }

    /// <summary>
    /// Carica i log dall'agente
    /// </summary>
    public async Task LoadLogsAsync()
    {
        Debug.WriteLine($"[AgentDetailViewModel] Caricamento log per agente: {Agent.Name}");

        IsLoadingLogs = true;

        try
        {
            // Costruisce l'URL dell'agente
            var agentUrl = $"http://localhost:{Agent.Port}";

            // Chiama l'endpoint /logs
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            var response = await httpClient.GetAsync($"{agentUrl}/logs");

            if (response.IsSuccessStatusCode)
            {
                var logsResponse = await response.Content.ReadFromJsonAsync<LogsResponse>();

                if (logsResponse != null && logsResponse.Success && logsResponse.Logs != null && logsResponse.Logs.Count > 0)
                {
                    LogCount = logsResponse.Count;

                    // Formatta i log in una stringa multilinea
                    var logsLines = logsResponse.Logs.Select(log =>
                    {
                        var timestamp = log.Timestamp.ToLocalTime().ToString("HH:mm:ss");
                        var level = log.Level.PadRight(5);
                        return $"[{timestamp}] [{level}] {log.Message}";
                    });

                    LogsText = string.Join("\n", logsLines);

                    Debug.WriteLine($"[AgentDetailViewModel] {LogCount} log caricati con successo");
                }
                else
                {
                    LogCount = 0;
                    LogsText = "Nessun log disponibile per questo agente.";
                    Debug.WriteLine($"[AgentDetailViewModel] Nessun log disponibile");
                }
            }
            else
            {
                LogCount = 0;
                LogsText = $"Errore nel caricamento dei log: HTTP {response.StatusCode}";
                Debug.WriteLine($"[AgentDetailViewModel] Errore HTTP: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            LogCount = 0;
            LogsText = $"Errore nel caricamento dei log:\n{ex.Message}";
            Debug.WriteLine($"[AgentDetailViewModel] Eccezione durante caricamento log: {ex.Message}");
        }
        finally
        {
            IsLoadingLogs = false;
        }
    }

    /// <summary>
    /// Processa il risultato di un AI Task
    /// </summary>
    private void ProcessAiTaskResult(System.Text.Json.JsonElement workerResult)
    {
        try
        {
            Debug.WriteLine($"[AgentDetailViewModel] Processamento risultato AI Task");

            // Estrai i campi dal JSON
            var optimizedPrompt = workerResult.TryGetProperty("OptimizedPrompt", out var promptProp) 
                ? promptProp.GetString() 
                : workerResult.TryGetProperty("Result", out var resultProp) 
                    ? resultProp.GetString() 
                    : "Prompt non disponibile";

            var cursorFilePath = workerResult.TryGetProperty("CursorFilePath", out var fileProp) 
                ? fileProp.GetString() 
                : "File non generato";

            var cursorFileWritten = workerResult.TryGetProperty("CursorFileWritten", out var writtenProp) 
                ? writtenProp.GetBoolean() 
                : false;

            // Crea il modello del risultato
            AiTaskResult = new AiTaskResultModel
            {
                OptimizedPrompt = optimizedPrompt ?? "Prompt non disponibile",
                CursorFilePath = cursorFilePath ?? "File non generato",
                CursorFileWritten = cursorFileWritten
            };

            // Carica l'anteprima del file se esiste
            if (cursorFileWritten && !string.IsNullOrEmpty(cursorFilePath))
            {
                LoadFilePreviewAsync().ConfigureAwait(false);
            }
            else
            {
                AiTaskResult.FilePreview = "File non disponibile per l'anteprima.";
            }

            IsAiTaskResult = true;

            Debug.WriteLine($"[AgentDetailViewModel] AI Task Result processato con successo");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AgentDetailViewModel] Errore durante processamento AI Task Result: {ex.Message}");
            IsAiTaskResult = false;
        }
    }

    /// <summary>
    /// Carica l'anteprima del file generato
    /// </summary>
    public async Task LoadFilePreviewAsync()
    {
        if (AiTaskResult == null || string.IsNullOrEmpty(AiTaskResult.CursorFilePath))
        {
            return;
        }

        try
        {
            Debug.WriteLine($"[AgentDetailViewModel] Caricamento anteprima file: {AiTaskResult.CursorFilePath}");

            if (File.Exists(AiTaskResult.CursorFilePath))
            {
                var content = await File.ReadAllTextAsync(AiTaskResult.CursorFilePath);
                AiTaskResult.FilePreview = content;

                Debug.WriteLine($"[AgentDetailViewModel] Anteprima file caricata ({content.Length} caratteri)");
            }
            else
            {
                AiTaskResult.FilePreview = $"File non trovato: {AiTaskResult.CursorFilePath}";
                Debug.WriteLine($"[AgentDetailViewModel] File non trovato");
            }
        }
        catch (Exception ex)
        {
            AiTaskResult.FilePreview = $"Errore nel caricamento dell'anteprima:\n{ex.Message}";
            Debug.WriteLine($"[AgentDetailViewModel] Errore caricamento anteprima: {ex.Message}");
        }
    }
}

/// <summary>
/// Modello per la risposta dell'endpoint /logs
/// </summary>
public class LogsResponse
{
    public bool Success { get; set; }
    public int Count { get; set; }
    public List<LogEntry> Logs { get; set; } = new();
}

/// <summary>
/// Singolo evento di log
/// </summary>
public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = "INFO";
    public string Message { get; set; } = "";
}

/// <summary>
/// Modello per il risultato di un AI Task
/// </summary>
public partial class AiTaskResultModel : ObservableObject
{
    [ObservableProperty]
    private string _optimizedPrompt = "";

    [ObservableProperty]
    private string _cursorFilePath = "";

    [ObservableProperty]
    private bool _cursorFileWritten;

    [ObservableProperty]
    private string _filePreview = "Caricamento anteprima in corso...";
}
