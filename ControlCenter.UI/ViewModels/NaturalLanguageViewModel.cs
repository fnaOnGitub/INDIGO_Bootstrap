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
    private readonly ConfigService _configService;

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
        _configService = new ConfigService();

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
                // Estrai RequiresUserConfirmation e ProposalData da WorkerResult
                bool requiresConfirmation = false;
                ProposalData? proposalData = null;

                if (response.WorkerResult != null)
                {
                    var workerResultJson = (System.Text.Json.JsonElement)response.WorkerResult;
                    
                    // Verifica se richiede conferma utente
                    if (workerResultJson.TryGetProperty("RequiresUserConfirmation", out var confirmProp))
                    {
                        requiresConfirmation = confirmProp.GetBoolean();
                    }

                    // Estrai ProposalData se presente
                    if (requiresConfirmation && workerResultJson.TryGetProperty("ProposalData", out var proposalProp))
                    {
                        proposalData = new ProposalData
                        {
                            Features = new List<string>(),
                            ProposedStructure = "",
                            Modules = new List<string>(),
                            ProposalText = ""
                        };

                        if (proposalProp.TryGetProperty("Features", out var featuresProp))
                        {
                            foreach (var feature in featuresProp.EnumerateArray())
                            {
                                proposalData.Features.Add(feature.GetString() ?? "");
                            }
                        }

                        if (proposalProp.TryGetProperty("ProposedStructure", out var structProp))
                        {
                            proposalData.ProposedStructure = structProp.GetString() ?? "";
                        }

                        if (proposalProp.TryGetProperty("Modules", out var modulesProp))
                        {
                            foreach (var module in modulesProp.EnumerateArray())
                            {
                                proposalData.Modules.Add(module.GetString() ?? "");
                            }
                        }

                        if (proposalProp.TryGetProperty("ProposalText", out var textProp))
                        {
                            proposalData.ProposalText = textProp.GetString() ?? "";
                        }
                    }
                }

                // Verifica se richiede conferma utente
                if (requiresConfirmation && proposalData != null)
                {
                    _timelineService.AddStep(
                        "⏸️ Conferma richiesta",
                        "Il sistema richiede la tua conferma per procedere",
                        TimelineStepType.Dialog
                    );
                    UpdateCurrentStepDisplay();
                    CurrentStatus = "⏸️ In attesa di conferma utente...";
                    IsExecuting = false;

                    // Mostra dialog di conferma
                    await HandleUserConfirmationAsync(proposalData, UserInput);
                    return;
                }

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

    /// <summary>
    /// Gestisce la conferma utente per creazione soluzione
    /// </summary>
    private async Task HandleUserConfirmationAsync(ProposalData proposalData, string originalPayload)
    {
        // STEP 1: Mostra dialog di conferma iniziale
        var dialog = new Views.SolutionConfirmationDialog(proposalData, originalPayload, _configService);
        
        // Sottoscrivi all'evento ExplainRequested
        dialog.ExplainRequested += async (sender, e) =>
        {
            await HandleExplainRequestAsync("initial-confirmation", "create-new-solution", originalPayload);
        };
        
        var dialogResult = dialog.ShowDialog();

        if (dialogResult == true)
        {
            var userChoice = dialog.UserChoice;
            string taskName = "";
            string taskDescription = "";
            string? targetPath = null;

            switch (userChoice)
            {
                case Views.UserChoice.CreateNewSolution:
                    taskName = "create-new-solution";
                    taskDescription = "Generazione anteprima modifiche";
                    targetPath = dialog.SelectedTargetPath;
                    break;

                case Views.UserChoice.AddToCurrentSolution:
                    taskName = "add-project-to-current-solution";
                    taskDescription = "Generazione anteprima modifiche";
                    break;

                case Views.UserChoice.Cancel:
                    _timelineService.AddStep(
                        "❌ Operazione annullata",
                        "L'utente ha annullato la creazione della soluzione",
                        TimelineStepType.Info
                    );
                    UpdateCurrentStepDisplay();
                    CurrentStatus = "✅ Pronto ad eseguire il tuo comando";
                    return;
            }

            // L'utente ha confermato, genera PREVIEW
            IsExecuting = true;
            CurrentStatus = $"🔍 {taskDescription}";

            // STEP 2: Genera anteprima modifiche
            _timelineService.AddStep(
                "🔍 Generazione anteprima",
                "Preparazione preview delle modifiche da applicare",
                TimelineStepType.Processing
            );
            UpdateCurrentStepDisplay();
            await Task.Delay(300);

            // Se c'è un targetPath, aggiorna la timeline
            if (!string.IsNullOrEmpty(targetPath))
            {
                _timelineService.AddStep(
                    "📁 Percorso selezionato",
                    $"Percorso: {targetPath}",
                    TimelineStepType.Info
                );
                UpdateCurrentStepDisplay();
                await Task.Delay(300);

                _timelineService.AddStep(
                    "💾 Percorso salvato",
                    "Il percorso è stato salvato in configurazione",
                    TimelineStepType.Success
                );
                UpdateCurrentStepDisplay();
                await Task.Delay(300);
            }

            // Invia task per generare preview
            var previewResponse = await _client.DispatchTaskAsync("", taskName, originalPayload, targetPath);

            // ⚠️ NUOVO: Gestisci risposta "folder-exists"
            if (previewResponse?.Status == "folder-exists")
            {
                _timelineService.AddStep(
                    "⚠️ Cartella esistente rilevata",
                    "La cartella di destinazione contiene già una soluzione",
                    TimelineStepType.Info
                );
                UpdateCurrentStepDisplay();
                await Task.Delay(300);

                // Mostra dialog per gestire conflitto
                await HandleFolderExistsConflictAsync(previewResponse, taskName, originalPayload, targetPath);
                IsExecuting = false;
                return;
            }

            if (previewResponse?.Success == true)
            {
                _timelineService.AddStep(
                    "✅ Anteprima generata",
                    "Preview delle modifiche pronta",
                    TimelineStepType.Success
                );
                UpdateCurrentStepDisplay();
                await Task.Delay(300);

                // STEP 3: Mostra preview dialog con conferma finale
                await HandlePreviewConfirmationAsync(taskName, originalPayload, targetPath, taskDescription);
            }
            else
            {
                _timelineService.AddStep(
                    "❌ Errore",
                    previewResponse?.Message ?? "Errore durante generazione preview",
                    TimelineStepType.Error
                );
                UpdateCurrentStepDisplay();
                CurrentStatus = "❌ Errore durante generazione preview";
                IsExecuting = false;
            }
        }
        else
        {
            // L'utente ha annullato o chiuso il dialog
            _timelineService.AddStep(
                "❌ Operazione annullata",
                "L'utente ha annullato la creazione della soluzione",
                TimelineStepType.Info
            );
            UpdateCurrentStepDisplay();
            CurrentStatus = "✅ Pronto ad eseguire il tuo comando";
        }
    }

    /// <summary>
    /// Gestisce la conferma finale dopo la preview
    /// </summary>
    private async Task HandlePreviewConfirmationAsync(string operationType, string originalPayload, string? targetPath, string operationDescription, bool forceOverwrite = false)
    {
        // Crea preview data (in futuro sarà estratto dalla risposta del Worker)
        var previewData = new Views.PreviewData
        {
            FilesToCreate = new List<string>
            {
                "README.md",
                "src/Core/.gitkeep",
                "src/Infrastructure/.gitkeep",
                "src/Api/Controllers/.gitkeep",
                "src/Models/.gitkeep",
                "tests/UnitTests/.gitkeep"
            },
            FoldersToCreate = new List<string>
            {
                targetPath ?? "MyNewSolution",
                "src",
                "src/Core",
                "src/Infrastructure",
                "src/Api",
                "src/Api/Controllers",
                "src/Models",
                "tests",
                "tests/UnitTests"
            },
            FinalStructure = @"📁 MyNewSolution/
├── 📁 src/
│   ├── 📁 Core/
│   ├── 📁 Infrastructure/
│   ├── 📁 Api/
│   │   └── 📁 Controllers/
│   └── 📁 Models/
├── 📁 tests/
│   └── 📁 UnitTests/
└── 📄 README.md",
            TechnicalDetails = $"Operazione: {operationType}\nPercorso: {targetPath}\nDescrizione: {operationDescription}",
            OperationType = operationType
        };

        _timelineService.AddStep(
            "⏸️ Conferma PREVIEW richiesta",
            "Verifica le modifiche prima di procedere",
            TimelineStepType.Dialog
        );
        UpdateCurrentStepDisplay();
        CurrentStatus = "⏸️ In attesa conferma PREVIEW...";
        IsExecuting = false;

        // Mostra preview dialog
        var previewDialog = new Views.PreviewDialog(previewData);
        
        // Sottoscrivi all'evento ExplainRequested
        previewDialog.ExplainRequested += async (sender, e) =>
        {
            await HandleExplainRequestAsync("preview", operationType, originalPayload);
        };
        
        var previewResult = previewDialog.ShowDialog();

        if (previewResult == true && previewDialog.UserChoice == Views.UserPreviewChoice.Proceed)
        {
            // Utente ha confermato, ESEGUI l'operazione
            IsExecuting = true;
            
            _timelineService.AddStep(
                "▶️ Esecuzione confermata",
                "Inizio creazione fisica dei file",
                TimelineStepType.Info
            );
            UpdateCurrentStepDisplay();
            CurrentStatus = $"▶️ Esecuzione: {operationDescription}";
            await Task.Delay(500);

            // Determina il task di esecuzione basato sull'operazione
            string executeTaskName = operationType == "create-new-solution" 
                ? "execute-solution-creation" 
                : "execute-project-addition";

            _timelineService.AddStep(
                "🔨 Creazione in corso",
                "Scrittura file e cartelle sul disco",
                TimelineStepType.Processing
            );
            UpdateCurrentStepDisplay();
            await Task.Delay(300);

            // ESEGUI l'operazione reale
            var executeResponse = await _client.DispatchTaskAsync("", executeTaskName, originalPayload, targetPath, forceOverwrite);

            if (executeResponse?.Success == true)
            {
                _timelineService.AddStep(
                    "✅ Operazione completata",
                    $"Soluzione creata con successo in {targetPath}",
                    TimelineStepType.Success
                );
                UpdateCurrentStepDisplay();
                CurrentStatus = "✅ Operazione completata con successo!";
            }
            else
            {
                _timelineService.AddStep(
                    "❌ Errore",
                    executeResponse?.Message ?? "Errore durante esecuzione",
                    TimelineStepType.Error
                );
                UpdateCurrentStepDisplay();
                CurrentStatus = "❌ Errore durante esecuzione";
            }

            IsExecuting = false;
        }
        else
        {
            // Utente ha annullato la preview
            _timelineService.AddStep(
                "❌ Operazione annullata",
                "L'utente ha annullato dopo aver visto la preview",
                TimelineStepType.Info
            );
            UpdateCurrentStepDisplay();
            CurrentStatus = "✅ Pronto ad eseguire il tuo comando";
        }
    }

    /// <summary>
    /// Gestisce la richiesta di spiegazione per uno step
    /// </summary>
    private async Task HandleExplainRequestAsync(string stepId, string stepType, string userRequest)
    {
        _timelineService.AddStep(
            "💬 Spiegazione richiesta",
            $"Generazione spiegazione per '{stepType}'",
            TimelineStepType.Info
        );
        UpdateCurrentStepDisplay();
        
        // Prepara il payload per explain-step
        var explainPayload = new
        {
            StepId = stepId,
            StepType = stepType,
            UserRequest = userRequest,
            Context = new
            {
                Timestamp = DateTime.Now,
                Source = "Control Center UI"
            }
        };

        // Invia richiesta di spiegazione all'Orchestrator
        var explainResponse = await _client.DispatchTaskAsync("", "explain-step", 
            System.Text.Json.JsonSerializer.Serialize(explainPayload));

        if (explainResponse?.Success == true && explainResponse.WorkerResult != null)
        {
            _timelineService.AddStep(
                "📘 Spiegazione ricevuta",
                "Spiegazione dettagliata disponibile",
                TimelineStepType.Success
            );
            UpdateCurrentStepDisplay();

            // Estrai i dati della spiegazione dal WorkerResult
            var explanationData = ExtractExplanationData(explainResponse.WorkerResult, stepId, stepType);

            // Mostra dialog di spiegazione
            var explainDialog = new Views.ExplainDialog(explanationData);
            explainDialog.ShowDialog();
        }
        else
        {
            _timelineService.AddStep(
                "❌ Errore spiegazione",
                "Impossibile generare la spiegazione",
                TimelineStepType.Error
            );
            UpdateCurrentStepDisplay();

            MessageBox.Show(
                "Impossibile generare la spiegazione. Riprova più tardi.",
                "Errore",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
    }

    /// <summary>
    /// Estrae i dati della spiegazione dal WorkerResult
    /// </summary>
    private Views.ExplanationData ExtractExplanationData(object workerResult, string stepId, string stepType)
    {
        try
        {
            if (workerResult is System.Text.Json.JsonElement json)
            {
                // Estrai il risultato testuale completo
                string explanationText = "";
                if (json.TryGetProperty("Result", out var resultProp))
                {
                    explanationText = resultProp.GetString() ?? "";
                }

                // Parsing semplificato: usa il testo markdown generato
                // In una versione più avanzata, potresti parsare il markdown per estrarre le sezioni
                return new Views.ExplanationData
                {
                    NarrativeExplanation = ExtractSection(explanationText, "## 📖 Spiegazione", "## 🔧"),
                    TechnicalReason = ExtractSection(explanationText, "## 🔧 Motivazione tecnica", "## 🔗"),
                    Dependencies = ExtractSection(explanationText, "## 🔗 Dipendenze", "## ⚡"),
                    ImpactIfConfirm = ExtractSection(explanationText, "**Se confermi:**", "**Se annulli:**"),
                    ImpactIfCancel = ExtractSection(explanationText, "**Se annulli:**", "## 🔀"),
                    Alternatives = ExtractSection(explanationText, "## 🔀 Alternative possibili", "## 📚"),
                    FullTechnicalDetails = ExtractSection(explanationText, "## 📚 Dettagli tecnici completi", "## 💡"),
                    StepId = stepId,
                    StepType = stepType
                };
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Errore estrazione explanation data: {ex.Message}");
        }

        // Fallback
        return new Views.ExplanationData
        {
            NarrativeExplanation = "Spiegazione non disponibile",
            TechnicalReason = "Dettagli tecnici non disponibili",
            Dependencies = "Dipendenze non disponibili",
            ImpactIfConfirm = "Impatto non disponibile",
            ImpactIfCancel = "Impatto non disponibile",
            Alternatives = "Alternative non disponibili",
            FullTechnicalDetails = "Dettagli completi non disponibili",
            StepId = stepId,
            StepType = stepType
        };
    }

    /// <summary>
    /// Estrae una sezione da un testo markdown
    /// </summary>
    private string ExtractSection(string text, string startMarker, string endMarker)
    {
        try
        {
            var startIndex = text.IndexOf(startMarker);
            if (startIndex == -1) return "";

            startIndex += startMarker.Length;
            var endIndex = text.IndexOf(endMarker, startIndex);
            if (endIndex == -1) endIndex = text.Length;

            var section = text.Substring(startIndex, endIndex - startIndex).Trim();
            
            // Rimuovi eventuali separatori markdown
            section = section.Replace("---", "").Trim();
            
            return section;
        }
        catch
        {
            return "";
        }
    }

    /// <summary>
    /// Re-invia il task con un nuovo targetPath
    /// </summary>
    private async Task ExecuteTaskWithNewPathAsync(string taskName, string originalPayload, string newTargetPath)
    {
        try
        {
            _timelineService.AddStep(
                "🔨 Generazione anteprima",
                "Preparazione preview con nuovo percorso",
                TimelineStepType.Processing
            );
            UpdateCurrentStepDisplay();

            // Invia task per generare preview con il nuovo percorso
            var previewResponse = await _client.DispatchTaskAsync("", taskName, originalPayload, newTargetPath);

            // Verifica se c'è ancora un conflitto (caso improbabile ma possibile)
            if (previewResponse?.Status == "folder-exists")
            {
                _timelineService.AddStep(
                    "⚠️ Cartella ancora esistente",
                    "Il nuovo percorso risulta ancora occupato",
                    TimelineStepType.Info
                );
                UpdateCurrentStepDisplay();
                await Task.Delay(300);

                // Re-chiama ricorsivamente il conflitto handler
                await HandleFolderExistsConflictAsync(previewResponse, taskName, originalPayload, newTargetPath);
                return;
            }

            if (previewResponse?.Success == true)
            {
                _timelineService.AddStep(
                    "🔍 Anteprima generata",
                    "Preview delle modifiche pronta",
                    TimelineStepType.Output
                );
                UpdateCurrentStepDisplay();
                await Task.Delay(300);

                // Continua con il normale flusso di conferma preview
                var taskDescription = $"Creazione soluzione in: {newTargetPath}";
                await HandlePreviewConfirmationAsync(taskName, originalPayload, newTargetPath, taskDescription, forceOverwrite: false);
            }
            else
            {
                _timelineService.AddStep(
                    "❌ Errore generazione preview",
                    previewResponse?.Message ?? "Errore sconosciuto",
                    TimelineStepType.Error
                );
                UpdateCurrentStepDisplay();
                CurrentStatus = "❌ Errore generazione preview";
            }
        }
        catch (Exception ex)
        {
            _timelineService.AddStep(
                "❌ Errore",
                $"Errore durante re-invio task: {ex.Message}",
                TimelineStepType.Error
            );
            UpdateCurrentStepDisplay();
            CurrentStatus = "❌ Errore durante re-invio task";
            System.Diagnostics.Debug.WriteLine($"Errore ExecuteTaskWithNewPathAsync: {ex}");
        }
    }

    /// <summary>
    /// Gestisce il conflitto quando la cartella di destinazione esiste già
    /// </summary>
    private async Task HandleFolderExistsConflictAsync(dynamic folderExistsResponse, string taskName, string originalPayload, string? targetPath)
    {
        try
        {
            string existingPath = folderExistsResponse.ExistingPath ?? "Percorso sconosciuto";
            string suggestedAlternativeName = folderExistsResponse.SuggestedAlternativeName ?? "MyNewSolution_1";

            // Mostra dialog per gestire conflitto
            var dialog = new Views.FolderExistsDialog(existingPath, suggestedAlternativeName);
            var result = dialog.ShowDialog();

            if (result == true)
            {
                switch (dialog.UserAction)
                {
                    case Views.FolderExistsAction.Overwrite:
                        // L'utente ha confermato la sovrascrittura
                        _timelineService.AddStep(
                            "🔥 Sovrascrittura confermata",
                            "L'utente ha confermato la sovrascrittura della cartella esistente",
                            TimelineStepType.Info
                        );
                        UpdateCurrentStepDisplay();
                        await Task.Delay(300);

                        // Vai direttamente alla preview (il controllo folder-exists è già stato fatto)
                        var taskDescription = "Creazione soluzione con sovrascrittura";
                        await HandlePreviewConfirmationAsync(taskName, originalPayload, targetPath, taskDescription, forceOverwrite: true);
                        break;

                    case Views.FolderExistsAction.UseSuggestedName:
                        // L'utente ha accettato il nome suggerito
                        _timelineService.AddStep(
                            "✅ Nome suggerito accettato",
                            $"Nuovo nome: {suggestedAlternativeName}",
                            TimelineStepType.Info
                        );
                        UpdateCurrentStepDisplay();
                        await Task.Delay(300);

                        // Aggiorna il targetPath con il nome suggerito
                        var parentFolder = System.IO.Path.GetDirectoryName(existingPath) ?? targetPath ?? "";
                        var newTargetPath = System.IO.Path.Combine(parentFolder, suggestedAlternativeName);
                        
                        _timelineService.AddStep(
                            "📁 Nuovo percorso selezionato",
                            $"Percorso: {newTargetPath}",
                            TimelineStepType.Info
                        );
                        UpdateCurrentStepDisplay();
                        await Task.Delay(300);

                        // Re-invia il task con il nuovo targetPath
                        await ExecuteTaskWithNewPathAsync(taskName, originalPayload, newTargetPath);
                        break;

                    case Views.FolderExistsAction.UseCustomName:
                        // L'utente ha scelto un nome personalizzato
                        _timelineService.AddStep(
                            "✏️ Nome personalizzato selezionato",
                            $"Nuovo nome: {dialog.NewSolutionName}",
                            TimelineStepType.Info
                        );
                        UpdateCurrentStepDisplay();
                        await Task.Delay(300);

                        // Aggiorna il targetPath con il nome personalizzato
                        var parentFolderCustom = System.IO.Path.GetDirectoryName(existingPath) ?? targetPath ?? "";
                        var customTargetPath = System.IO.Path.Combine(parentFolderCustom, dialog.NewSolutionName ?? "MyNewSolution");
                        
                        _timelineService.AddStep(
                            "📁 Nuovo percorso selezionato",
                            $"Percorso: {customTargetPath}",
                            TimelineStepType.Info
                        );
                        UpdateCurrentStepDisplay();
                        await Task.Delay(300);

                        // Re-invia il task con il nuovo targetPath
                        await ExecuteTaskWithNewPathAsync(taskName, originalPayload, customTargetPath);
                        break;

                    case Views.FolderExistsAction.Cancel:
                        _timelineService.AddStep(
                            "❌ Operazione annullata",
                            "L'utente ha annullato la creazione per evitare sovrascrittura",
                            TimelineStepType.Info
                        );
                        UpdateCurrentStepDisplay();
                        CurrentStatus = "✅ Pronto ad eseguire il tuo comando";
                        break;
                }
            }
            else
            {
                // Dialog chiuso senza scelta
                _timelineService.AddStep(
                    "❌ Operazione annullata",
                    "L'utente ha chiuso il dialog senza scegliere",
                    TimelineStepType.Info
                );
                UpdateCurrentStepDisplay();
                CurrentStatus = "✅ Pronto ad eseguire il tuo comando";
            }
        }
        catch (Exception ex)
        {
            _timelineService.AddStep(
                "❌ Errore",
                $"Errore gestione conflitto: {ex.Message}",
                TimelineStepType.Error
            );
            UpdateCurrentStepDisplay();
            CurrentStatus = "❌ Errore gestione conflitto cartella";
            System.Diagnostics.Debug.WriteLine($"Errore HandleFolderExistsConflictAsync: {ex}");
        }
    }
}
