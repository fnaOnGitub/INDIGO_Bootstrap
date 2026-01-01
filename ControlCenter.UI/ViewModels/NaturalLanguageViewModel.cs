using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControlCenter.UI.Models;
using ControlCenter.UI.Services;

namespace ControlCenter.UI.ViewModels;

/// <summary>
/// ViewModel per la Natural Language Console
/// </summary>
public partial class NaturalLanguageViewModel : ObservableObject
{
    private readonly BootstrapperClient _client;
    private readonly TimelineService _timelineService;
    private readonly HealthCheckService _healthCheck;
    private readonly AutoRecoveryService _autoRecovery;

    [ObservableProperty]
    private string _userInput = "";

    [ObservableProperty]
    private bool _isExecuting = false;

    [ObservableProperty]
    private string _currentStatus = "Verifica Orchestrator in corso...";

    [ObservableProperty]
    private string _currentStepTitle = "";

    [ObservableProperty]
    private string _currentStepDescription = "";

    [ObservableProperty]
    private bool _hasCurrentStep = false;

    [ObservableProperty]
    private bool _isOrchestratorOnline = false;

    [ObservableProperty]
    private string _orchestratorStatus = "Verifica in corso...";

    [ObservableProperty]
    private int _orchestratorPort = 0;

    [ObservableProperty]
    private string _orchestratorResponseTime = "---";

    public ObservableCollection<TimelineStep> TimelineSteps => _timelineService.Steps;

    public NaturalLanguageViewModel()
    {
        _client = new BootstrapperClient();
        _timelineService = new TimelineService();
        _healthCheck = new HealthCheckService();
        _autoRecovery = new AutoRecoveryService(_healthCheck);

        // Verifica Orchestrator all'avvio
        _ = InitializeOrchestratorAsync();
    }

    /// <summary>
    /// Inizializza e verifica l'Orchestrator all'avvio
    /// </summary>
    private async Task InitializeOrchestratorAsync()
    {
        CurrentStatus = "Verifica Orchestrator in corso...";
        OrchestratorStatus = "🔍 Ricerca in corso...";

        // Verifica se è già attivo
        var checkResult = await _healthCheck.CheckOrchestratorAsync();

        if (checkResult.IsOnline && checkResult.Port.HasValue)
        {
            // Orchestrator trovato!
            IsOrchestratorOnline = true;
            OrchestratorPort = checkResult.Port.Value;
            OrchestratorStatus = $"✅ Online su porta {OrchestratorPort}";
            OrchestratorResponseTime = $"{_healthCheck.LastResponseTime.TotalMilliseconds:F0}ms";
            CurrentStatus = "✅ Pronto ad eseguire il tuo comando";

            // Aggiorna client
            _client.UpdateOrchestratorPort(checkResult.Port.Value);

            return;
        }

        // Orchestrator non trovato, tenta avvio automatico
        OrchestratorStatus = "⏳ Avvio automatico in corso...";
        CurrentStatus = "⏳ Avvio Orchestrator...";

        var startResult = await _autoRecovery.StartOrchestratorAsync();

        if (startResult.Success)
        {
            // Orchestrator avviato con successo
            var recheckResult = await _healthCheck.CheckOrchestratorAsync();
            
            if (recheckResult.IsOnline && recheckResult.Port.HasValue)
            {
                IsOrchestratorOnline = true;
                OrchestratorPort = recheckResult.Port.Value;
                OrchestratorStatus = $"✅ Avviato su porta {OrchestratorPort}";
                OrchestratorResponseTime = $"{_healthCheck.LastResponseTime.TotalMilliseconds:F0}ms";
                CurrentStatus = "✅ Pronto ad eseguire il tuo comando";

                _client.UpdateOrchestratorPort(recheckResult.Port.Value);

                MessageBox.Show(
                    $"Orchestrator avviato automaticamente su porta {OrchestratorPort}",
                    "Orchestrator Avviato",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
        }
        else
        {
            // Avvio fallito
            IsOrchestratorOnline = false;
            OrchestratorStatus = "❌ Offline";
            CurrentStatus = "❌ Orchestrator non disponibile";

            var result = MessageBox.Show(
                $"Impossibile avviare l'Orchestrator:\n{startResult.Message}\n\nVuoi aprire la cartella dell'agente?",
                "Errore Orchestrator",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );

            if (result == MessageBoxResult.Yes)
            {
                _autoRecovery.OpenOrchestratorFolder();
            }
        }
    }

    /// <summary>
    /// Esegue il comando in linguaggio naturale
    /// </summary>
    [RelayCommand]
    private async Task ExecuteAsync()
    {
        if (string.IsNullOrWhiteSpace(UserInput))
        {
            MessageBox.Show("Scrivi cosa vuoi che il cluster faccia!", "Input Richiesto", 
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // Verifica che l'Orchestrator sia online
        if (!IsOrchestratorOnline)
        {
            var result = MessageBox.Show(
                "L'Orchestrator non è attivo.\nVuoi tentare di avviarlo automaticamente?",
                "Orchestrator Offline",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );

            if (result == MessageBoxResult.Yes)
            {
                await InitializeOrchestratorAsync();
                
                if (!IsOrchestratorOnline)
                {
                    MessageBox.Show(
                        "Impossibile avviare l'Orchestrator.\nAvvialo manualmente e riprova.",
                        "Errore",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                    return;
                }
            }
            else
            {
                return;
            }
        }

        IsExecuting = true;
        CurrentStatus = "Elaborazione in corso...";

        try
        {
            // Step 1: Input ricevuto
            _timelineService.AddStep(
                "Input ricevuto",
                $"Comando: {TruncateText(UserInput, 60)}",
                TimelineStepType.Input
            );
            UpdateCurrentStepDisplay();
            await Task.Delay(500); // Simula elaborazione

            // Step 2: Analisi linguaggio naturale
            _timelineService.AddStep(
                "Analisi linguaggio naturale",
                "Classificazione automatica come AI Task",
                TimelineStepType.Routing
            );
            UpdateCurrentStepDisplay();
            await Task.Delay(500);

            // Step 3: Routing verso Orchestrator
            _timelineService.AddStep(
                "Invio a Orchestrator",
                "Instradamento verso cluster IndigoLab",
                TimelineStepType.Routing
            );
            UpdateCurrentStepDisplay();
            await Task.Delay(300);

            // Step 4: Dispatch reale al cluster
            var response = await _client.DispatchTaskAsync("", "cursor-prompt", UserInput);

            if (response?.Success == true)
            {
                // Step 5: Classificazione
                var workerType = response.WorkerType ?? "Unknown";
                var isAiTask = response.IsAiTask;

                _timelineService.AddStep(
                    isAiTask ? "Classificato come AI Task" : "Classificato come Task Standard",
                    $"Instradato a: {workerType}",
                    TimelineStepType.Routing
                );
                UpdateCurrentStepDisplay();
                await Task.Delay(500);

                // Step 6: Elaborazione
                _timelineService.AddStep(
                    "Elaborazione in corso",
                    $"Worker: {response.Worker}",
                    TimelineStepType.Processing
                );
                UpdateCurrentStepDisplay();
                await Task.Delay(800);

                // Step 7: Risultato (parse WorkerResult se presente)
                if (response.WorkerResult != null)
                {
                    var resultMsg = ParseWorkerResult(response.WorkerResult);

                    _timelineService.AddStep(
                        "Task completato",
                        resultMsg,
                        TimelineStepType.Success
                    );
                    UpdateCurrentStepDisplay();

                    // Step 8: File generati (se AI task)
                    if (isAiTask)
                    {
                        await Task.Delay(500);
                        _timelineService.AddStep(
                            "File generato in CursorBridge",
                            "FILE ALWAYS MODE attivo - output tracciabile",
                            TimelineStepType.Output
                        );
                        UpdateCurrentStepDisplay();

                        await Task.Delay(500);
                        _timelineService.AddStep(
                            "CursorMonitorAgent attivo",
                            "Monitoraggio autonomo in corso",
                            TimelineStepType.Autonomous
                        );
                        UpdateCurrentStepDisplay();
                    }
                }

                // Step finale
                await Task.Delay(500);
                _timelineService.AddStep(
                    "✓ Operazione completata",
                    "Il cluster ha elaborato la tua richiesta",
                    TimelineStepType.Success
                );
                UpdateCurrentStepDisplay();

                CurrentStatus = "✓ Completato con successo";
            }
            else
            {
                _timelineService.AddStep(
                    "Errore durante l'elaborazione",
                    response?.Message ?? "Errore sconosciuto",
                    TimelineStepType.Error
                );
                UpdateCurrentStepDisplay();
                CurrentStatus = "✗ Errore nell'elaborazione";
            }

            // Pulisci input dopo completamento
            await Task.Delay(1000);
            UserInput = "";
        }
        catch (Exception ex)
        {
            _timelineService.AddStep(
                "Errore di comunicazione",
                $"Impossibile contattare il cluster: {ex.Message}",
                TimelineStepType.Error
            );
            UpdateCurrentStepDisplay();
            CurrentStatus = "✗ Errore di comunicazione";

            MessageBox.Show($"Errore: {ex.Message}", "Errore", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsExecuting = false;
            _timelineService.CompleteCurrentStep();
            HasCurrentStep = false;
        }
    }

    /// <summary>
    /// Pulisce la timeline
    /// </summary>
    [RelayCommand]
    private void ClearTimeline()
    {
        _timelineService.Clear();
        CurrentStatus = IsOrchestratorOnline ? "Pronto ad eseguire il tuo comando" : "Orchestrator offline";
        CurrentStepTitle = "";
        CurrentStepDescription = "";
        HasCurrentStep = false;
    }

    /// <summary>
    /// Riavvia l'Orchestrator manualmente
    /// </summary>
    [RelayCommand]
    private async Task RestartOrchestratorAsync()
    {
        var result = MessageBox.Show(
            "Vuoi riavviare l'Orchestrator?",
            "Riavvio Orchestrator",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question
        );

        if (result == MessageBoxResult.Yes)
        {
            CurrentStatus = "⏳ Riavvio Orchestrator...";
            OrchestratorStatus = "⏳ Riavvio in corso...";

            await InitializeOrchestratorAsync();
        }
    }

    /// <summary>
    /// Apre la cartella dell'Orchestrator
    /// </summary>
    [RelayCommand]
    private void OpenOrchestratorFolder()
    {
        var success = _autoRecovery.OpenOrchestratorFolder();
        
        if (!success)
        {
            MessageBox.Show(
                "Impossibile trovare la cartella Agent.Orchestrator",
                "Errore",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
    }

    /// <summary>
    /// Aggiorna il display dello step corrente
    /// </summary>
    private void UpdateCurrentStepDisplay()
    {
        var current = _timelineService.CurrentStep;
        if (current != null)
        {
            CurrentStepTitle = current.Title;
            CurrentStepDescription = current.Description;
            HasCurrentStep = true;
            CurrentStatus = current.Title;
        }
        else
        {
            HasCurrentStep = false;
        }
    }

    /// <summary>
    /// Estrae informazioni dal WorkerResult
    /// </summary>
    private string ParseWorkerResult(object workerResult)
    {
        try
        {
            // Tentativo di parsing come JsonElement
            if (workerResult is System.Text.Json.JsonElement json)
            {
                if (json.TryGetProperty("Message", out var msgProp))
                {
                    return msgProp.GetString() ?? "Task eseguito";
                }
                if (json.TryGetProperty("Result", out var resProp))
                {
                    return resProp.GetString() ?? "Task eseguito";
                }
            }

            return "Task eseguito con successo";
        }
        catch
        {
            return "Task eseguito";
        }
    }

    /// <summary>
    /// Tronca il testo per la visualizzazione
    /// </summary>
    private string TruncateText(string text, int maxLength)
    {
        if (text.Length <= maxLength)
            return text;

        return text.Substring(0, maxLength) + "...";
    }
}
