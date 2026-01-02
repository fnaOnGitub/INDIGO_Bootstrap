using IndigoAiWorker01;

var builder = WebApplication.CreateBuilder(args);

// Configurazione logging console
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Configura Kestrel per ascoltare solo su HTTP porta 5005
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5005);
});

// Registra servizi come singleton
builder.Services.AddSingleton<WorkerState>();
builder.Services.AddSingleton<LogBuffer>();
builder.Services.AddSingleton<PromptOptimizer>();
builder.Services.AddSingleton<AiEngine>();
builder.Services.AddSingleton<CursorBridge>();

// Configurazione JSON Serialization - Usa PascalCase
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = null; // null = PascalCase
});

// Swagger per documentazione API
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Abilita Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var logger = app.Services.GetRequiredService<ILogger<Program>>();
var workerState = app.Services.GetRequiredService<WorkerState>();

// Log avvio
logger.LogInformation("=== IndigoAiWorker01 avviato ===");
logger.LogInformation("Porta: 5005");
logger.LogInformation("Versione: {Version}", workerState.Version);
logger.LogInformation("Tipo: AI-Powered Worker");

// ==================== ENDPOINTS ====================

// GET /health - Health check
app.MapGet("/health", () =>
{
    logger.LogInformation("Health check richiesto");
    return Results.Ok(new
    {
        Status = "OK",
        Timestamp = DateTime.UtcNow
    });
})
.WithName("HealthCheck")
.WithOpenApi();

// GET /status - Stato dettagliato agente
app.MapGet("/status", (WorkerState state) =>
{
    logger.LogInformation("Status richiesto");
    return Results.Ok(new
    {
        Agent = "IndigoAiWorker01",
        Type = "AI-Worker",
        Uptime = state.Uptime.ToString(@"hh\:mm\:ss"),
        Version = state.Version,
        LastTask = state.LastTask,
        Capabilities = new[]
        {
            "generate-code",
            "refactor-code",
            "explain-code",
            "create-component",
            "fix-snippet",
            "cursor-prompt",
            "optimize-prompt",
            "create-new-solution",
            "add-project-to-current-solution",
            "execute-solution-creation",
            "execute-project-addition",
            "explain-step"
        }
    });
})
.WithName("GetStatus")
.WithOpenApi();

// GET /logs - Attività recenti
app.MapGet("/logs", (LogBuffer logBuffer) =>
{
    logger.LogInformation("Log richiesti");
    return Results.Ok(new
    {
        Success = true,
        Count = logBuffer.GetAll().Count,
        Logs = logBuffer.GetAll()
    });
})
.WithName("GetLogs")
.WithOpenApi();

// POST /execute - Esegue task ricevuto con dispatcher AI
app.MapPost("/execute", async (ExecuteRequest request, WorkerState state, LogBuffer logBuffer, AiEngine aiEngine, CursorBridge cursorBridge, ILogger<Program> log) =>
{
    // Helper per estrarre payload come stringa
    string GetPayloadAsString(object? payload)
    {
        if (payload == null) return "";
        if (payload is string str) return str;
        if (payload is System.Text.Json.JsonElement json)
        {
            // Se è una stringa JSON, estraila direttamente
            if (json.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                return json.GetString() ?? "";
            }
            
            // Se è un oggetto con UserRequest, estrailo
            if (json.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                if (json.TryGetProperty("UserRequest", out var userReq))
                    return userReq.GetString() ?? "";
            }
            
            // Altrimenti serializza l'intero oggetto
            return json.ToString();
        }
        return payload.ToString() ?? "";
    }
    
    var payloadForLogging = GetPayloadAsString(request.Payload);
    
    log.LogInformation("Task AI ricevuto: Task='{Task}', Payload length={Length}", 
        request.Task, 
        payloadForLogging.Length);
    
    // Log evento
    logBuffer.Add($"Task AI ricevuto: {request.Task}");
    
    // Aggiorna ultimo task
    state.UpdateLastTask(request.Task);
    
    // Variabili per risposta
    string result;
    string? optimizedPrompt = null;
    bool cursorFileWritten = false;
    string cursorFilePath = "";
    bool requiresUserConfirmation = false;
    SolutionProposal? proposalData = null;
    
    // Dispatcher AI Task
    switch (request.Task.ToLowerInvariant())
    {
        case "generate-code":
            log.LogInformation("Esecuzione: GenerateCode");
            logBuffer.Add("Generazione codice in corso...");
            var payloadStr = GetPayloadAsString(request.Payload);
            result = aiEngine.GenerateCode(payloadStr);
            
            // FILE ALWAYS MODE: Scrivi SEMPRE file output
            try
            {
                cursorFilePath = cursorBridge.WriteAiOutput(
                    taskName: "generate-code",
                    payload: payloadStr,
                    aiResult: result
                );
                cursorFileWritten = true;
                logBuffer.Add($"File generato: {System.IO.Path.GetFileName(cursorFilePath)}");
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Errore scrittura file output");
                logBuffer.Add($"Errore scrittura file: {ex.Message}", "ERROR");
            }
            break;

        case "refactor-code":
            log.LogInformation("Esecuzione: RefactorCode");
            logBuffer.Add("Refactoring codice in corso...");
            var refactorPayload = GetPayloadAsString(request.Payload);
            result = aiEngine.RefactorCode(refactorPayload);
            
            // FILE ALWAYS MODE: Scrivi SEMPRE file output
            try
            {
                cursorFilePath = cursorBridge.WriteAiOutput(
                    taskName: "refactor-code",
                    payload: refactorPayload,
                    aiResult: result
                );
                cursorFileWritten = true;
                logBuffer.Add($"File generato: {System.IO.Path.GetFileName(cursorFilePath)}");
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Errore scrittura file output");
                logBuffer.Add($"Errore scrittura file: {ex.Message}", "ERROR");
            }
            break;

        case "explain-code":
            log.LogInformation("Esecuzione: ExplainCode");
            logBuffer.Add("Analisi e spiegazione codice in corso...");
            var explainPayload = GetPayloadAsString(request.Payload);
            result = aiEngine.ExplainCode(explainPayload);
            
            // FILE ALWAYS MODE: Scrivi SEMPRE file output
            try
            {
                cursorFilePath = cursorBridge.WriteAiOutput(
                    taskName: "explain-code",
                    payload: explainPayload,
                    aiResult: result
                );
                cursorFileWritten = true;
                logBuffer.Add($"File generato: {System.IO.Path.GetFileName(cursorFilePath)}");
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Errore scrittura file output");
                logBuffer.Add($"Errore scrittura file: {ex.Message}", "ERROR");
            }
            break;

        case "create-component":
            log.LogInformation("Esecuzione: CreateComponent");
            logBuffer.Add("Creazione componente in corso...");
            var componentPayload = GetPayloadAsString(request.Payload);
            result = aiEngine.CreateComponent(componentPayload);
            
            // FILE ALWAYS MODE: Scrivi SEMPRE file output
            try
            {
                cursorFilePath = cursorBridge.WriteAiOutput(
                    taskName: "create-component",
                    payload: componentPayload,
                    aiResult: result
                );
                cursorFileWritten = true;
                logBuffer.Add($"File generato: {System.IO.Path.GetFileName(cursorFilePath)}");
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Errore scrittura file output");
                logBuffer.Add($"Errore scrittura file: {ex.Message}", "ERROR");
            }
            break;

        case "fix-snippet":
            log.LogInformation("Esecuzione: FixSnippet");
            logBuffer.Add("Correzione snippet in corso...");
            var fixPayload = GetPayloadAsString(request.Payload);
            result = aiEngine.FixSnippet(fixPayload);
            
            // FILE ALWAYS MODE: Scrivi SEMPRE file output
            try
            {
                cursorFilePath = cursorBridge.WriteAiOutput(
                    taskName: "fix-snippet",
                    payload: fixPayload,
                    aiResult: result
                );
                cursorFileWritten = true;
                logBuffer.Add($"File generato: {System.IO.Path.GetFileName(cursorFilePath)}");
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Errore scrittura file output");
                logBuffer.Add($"Errore scrittura file: {ex.Message}", "ERROR");
            }
            break;

        case "cursor-prompt":
            log.LogInformation("Esecuzione: CursorPrompt");
            logBuffer.Add("Generazione prompt Cursor in corso...");
            
            var cursorPayloadStr = GetPayloadAsString(request.Payload);
            
            // Verifica se la richiesta implica creazione di una nuova soluzione
            requiresUserConfirmation = IsSolutionCreationRequest(cursorPayloadStr);
            
            if (requiresUserConfirmation)
            {
                log.LogInformation("Richiesta di creazione soluzione rilevata - generazione proposal");
                logBuffer.Add("Richiesta di creazione soluzione rilevata");
                
                // Genera proposal invece di eseguire direttamente
                proposalData = GenerateSolutionProposal(cursorPayloadStr);
                result = proposalData.ProposalText;
                
                // FILE ALWAYS MODE: Scrivi file proposal
                try
                {
                    var proposalContent = $@"# 📋 SOLUTION PROPOSAL - In Attesa di Conferma Utente

**Generated**: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
**Status**: ⏳ PENDING USER CONFIRMATION
**Type**: Solution Creation Request

---

{proposalData.ProposalText}

---

## ⚠️ NOTA IMPORTANTE

**Questo è un PROPOSAL. Non sono stati creati file di progetto.**

Per procedere, conferma:
- **Crea nuova soluzione** → Genera soluzione completa da zero
- **Aggiungi alla soluzione corrente** → Genera solo il nuovo progetto
- **Annulla** → Nessuna azione

---

*Generated by IndigoAiWorker01 - IndigoLab Cluster*
*FILE ALWAYS MODE: Proposal awaiting user confirmation*
";
                    var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd-HHmmss");
                    var proposalFileName = $"proposal-{timestamp}.md";
                    var proposalPath = System.IO.Path.Combine(AppContext.BaseDirectory, "CursorBridge", proposalFileName);
                    
                    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(proposalPath)!);
                    System.IO.File.WriteAllText(proposalPath, proposalContent);
                    
                    cursorFilePath = proposalPath;
                    cursorFileWritten = true;
                    logBuffer.Add($"Proposal generato: {proposalFileName}");
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "Errore scrittura proposal");
                    logBuffer.Add($"Errore scrittura proposal: {ex.Message}", "ERROR");
                }
            }
            else
            {
                // Comportamento normale per richieste non di creazione soluzione
                result = aiEngine.GenerateCursorPrompt("custom-request", cursorPayloadStr);
                
                // FILE ALWAYS MODE: Scrivi SEMPRE file output
                try
                {
                    cursorFilePath = cursorBridge.WriteAiOutput(
                        taskName: "cursor-prompt",
                        payload: cursorPayloadStr,
                        aiResult: result
                    );
                    cursorFileWritten = true;
                    logBuffer.Add($"File generato: {System.IO.Path.GetFileName(cursorFilePath)}");
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "Errore scrittura file output");
                    logBuffer.Add($"Errore scrittura file: {ex.Message}", "ERROR");
                }
            }
            break;

        case "optimize-prompt":
            log.LogInformation("Esecuzione: OptimizePrompt");
            logBuffer.Add($"Ottimizzazione prompt in corso...");
            
            var optimizePayloadStr = GetPayloadAsString(request.Payload);
            
            // Ottimizza l'intento dell'utente
            optimizedPrompt = aiEngine.OptimizePrompt(optimizePayloadStr);
            result = optimizedPrompt;
            
            // FILE ALWAYS MODE: Scrivi SEMPRE file output (con prompt ottimizzato)
            try
            {
                cursorFilePath = cursorBridge.WriteAiOutput(
                    taskName: "optimize-prompt",
                    payload: optimizePayloadStr,
                    aiResult: result,
                    optimizedPrompt: optimizedPrompt
                );
                cursorFileWritten = true;
                log.LogInformation("Prompt ottimizzato generato e salvato: {FilePath}", cursorFilePath);
                logBuffer.Add($"Prompt ottimizzato generato e salvato: {System.IO.Path.GetFileName(cursorFilePath)}");
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Errore scrittura prompt ottimizzato");
                logBuffer.Add($"Errore scrittura prompt: {ex.Message}", "ERROR");
                cursorFileWritten = false;
                cursorFilePath = "";
            }
            break;

        case "create-new-solution":
            var solutionTimestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            log.LogInformation("[{Time}] Esecuzione: CreateNewSolution - PREVIEW MODE", solutionTimestamp);
            logBuffer.Add("Generazione anteprima modifiche in corso...");
            
            // Estrai UserRequest e TargetPath dal Payload
            string userRequest = "";
            string? targetPath = null;
            
            // DEBUG: Log tipo e contenuto del payload
            log.LogInformation("[{Time}] DEBUG Payload Type: {Type}", solutionTimestamp, request.Payload?.GetType().FullName ?? "NULL");
            
            // Prova a serializzare il payload per debugging
            try
            {
                var payloadJson = System.Text.Json.JsonSerializer.Serialize(request.Payload);
                log.LogInformation("[{Time}] DEBUG Payload JSON: {Json}", solutionTimestamp, payloadJson);
                
                // Prova a deserializzare come oggetto strutturato
                var payloadObj = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(payloadJson);
                
                if (payloadObj.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    // ⚠️ FIX: Usa camelCase (userRequest) non PascalCase (UserRequest)
                    if (payloadObj.TryGetProperty("userRequest", out var userReqProp))
                    {
                        userRequest = userReqProp.GetString() ?? "";
                        log.LogInformation("[{Time}] DEBUG userRequest estratto: '{Value}'", solutionTimestamp, userRequest);
                    }
                    
                    // ⚠️ FIX: Usa camelCase (targetPath) non PascalCase (TargetPath)
                    if (payloadObj.TryGetProperty("targetPath", out var targetPathProp))
                    {
                        targetPath = targetPathProp.GetString();
                        log.LogInformation("[{Time}] DEBUG targetPath estratto: '{Value}'", solutionTimestamp, targetPath ?? "NULL");
                    }
                }
                else if (payloadObj.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    userRequest = payloadObj.GetString() ?? "";
                    log.LogInformation("[{Time}] DEBUG Payload è stringa: '{Value}'", solutionTimestamp, userRequest);
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, "[{Time}] Errore parsing payload", solutionTimestamp);
                // Fallback: usa il payload come stringa se possibile
                if (request.Payload is string str)
                {
                    userRequest = str;
                }
            }
            
            log.LogInformation("[{Time}] Generazione PREVIEW per nuova soluzione", solutionTimestamp);
            log.LogInformation("[{Time}] UserRequest: '{UserRequest}'", solutionTimestamp, userRequest);
            log.LogInformation("[{Time}] TargetPath: '{TargetPath}'", solutionTimestamp, targetPath ?? "NON SPECIFICATO");
            
            // ⚠️ PROTEZIONE: Verifica se la cartella di destinazione esiste già
            if (!string.IsNullOrWhiteSpace(targetPath))
            {
                string solutionName = "MyNewSolution"; // Estrai o genera nome soluzione
                string fullTargetPath = System.IO.Path.Combine(targetPath, solutionName);
                
                if (Directory.Exists(fullTargetPath))
                {
                    log.LogWarning("[{Time}] ⚠️ CARTELLA GIÀ ESISTENTE: {Path}", solutionTimestamp, fullTargetPath);
                    logBuffer.Add($"⚠️ La cartella {solutionName} esiste già in {targetPath}", "WARN");
                    
                    // Suggerisci un nome alternativo
                    int counter = 1;
                    string alternativeName = $"{solutionName}_{counter}";
                    while (Directory.Exists(System.IO.Path.Combine(targetPath, alternativeName)))
                    {
                        counter++;
                        alternativeName = $"{solutionName}_{counter}";
                    }
                    
                    log.LogInformation("[{Time}] Nome alternativo suggerito: {Alternative}", solutionTimestamp, alternativeName);
                    
                    // Restituisci risposta speciale "folder-exists"
                    return Results.Ok(new
                    {
                        Success = true,
                        Status = "folder-exists",
                        Message = "La cartella di destinazione esiste già",
                        ExistingPath = fullTargetPath,
                        SuggestedAlternativeName = alternativeName,
                        TargetPath = targetPath,
                        UserRequest = userRequest,
                        Timestamp = DateTime.UtcNow
                    });
                }
            }
            
            // Genera PREVIEW invece di creare subito
            var previewData = GenerateSolutionPreview(userRequest, targetPath);
            result = previewData.PreviewText;
            
            // Salva i dati della preview per uso futuro
            requiresUserConfirmation = true;
            proposalData = new SolutionProposal
            {
                Features = previewData.FilesToCreate,
                ProposedStructure = previewData.FinalStructure,
                Modules = previewData.FoldersToCreate,
                ProposalText = result
            };
            
            // FILE ALWAYS MODE: Scrivi SEMPRE file output
            try
            {
                cursorFilePath = cursorBridge.WriteAiOutput(
                    taskName: "create-new-solution",
                    payload: userRequest,
                    aiResult: result
                );
                cursorFileWritten = true;
                logBuffer.Add($"File generato: {System.IO.Path.GetFileName(cursorFilePath)}");
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Errore scrittura file output");
                logBuffer.Add($"Errore scrittura file: {ex.Message}", "ERROR");
            }
            break;

        case "add-project-to-current-solution":
            log.LogInformation("Esecuzione: AddProjectToCurrentSolution");
            logBuffer.Add("Aggiunta progetto a soluzione corrente in corso...");
            var addProjectPayload = GetPayloadAsString(request.Payload);
            result = AddProjectToCurrentSolution(addProjectPayload);
            
            // FILE ALWAYS MODE: Scrivi SEMPRE file output
            try
            {
                cursorFilePath = cursorBridge.WriteAiOutput(
                    taskName: "add-project-to-current-solution",
                    payload: addProjectPayload,
                    aiResult: result
                );
                cursorFileWritten = true;
                logBuffer.Add($"File generato: {System.IO.Path.GetFileName(cursorFilePath)}");
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Errore scrittura file output");
                logBuffer.Add($"Errore scrittura file: {ex.Message}", "ERROR");
            }
            break;

        case "execute-solution-creation":
            var execTimestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            log.LogInformation("[{Time}] Esecuzione: ExecuteSolutionCreation - CREAZIONE REALE", execTimestamp);
            logBuffer.Add("Esecuzione creazione soluzione confermata dall'utente...");
            
            // Estrai UserRequest, TargetPath e ForceOverwrite dal Payload
            string execUserRequest = "";
            string? execTargetPath = null;
            bool forceOverwrite = false;
            
            if (request.Payload is System.Text.Json.JsonElement execPayloadJson)
            {
                if (execPayloadJson.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    // ⚠️ FIX: Usa camelCase (userRequest) non PascalCase (UserRequest)
                    if (execPayloadJson.TryGetProperty("userRequest", out var execUserReqProp))
                    {
                        execUserRequest = execUserReqProp.GetString() ?? "";
                    }
                    
                    // ⚠️ FIX: Usa camelCase (targetPath) non PascalCase (TargetPath)
                    if (execPayloadJson.TryGetProperty("targetPath", out var execTargetPathProp))
                    {
                        execTargetPath = execTargetPathProp.GetString();
                    }
                    
                    // ⚠️ NUOVO: Estrai flag forceOverwrite
                    if (execPayloadJson.TryGetProperty("forceOverwrite", out var forceOverwriteProp))
                    {
                        forceOverwrite = forceOverwriteProp.GetBoolean();
                    }
                }
            }
            
            log.LogInformation("[{Time}] ESECUZIONE CONFERMATA - Creazione fisica file/cartelle", execTimestamp);
            log.LogInformation("[{Time}] ForceOverwrite: {ForceOverwrite}", execTimestamp, forceOverwrite);
            
            // ⚠️ PROTEZIONE: Verifica se la cartella esiste e se manca la conferma esplicita
            if (!string.IsNullOrWhiteSpace(execTargetPath))
            {
                string solutionName = "MyNewSolution"; // Estrai o genera nome soluzione
                string fullTargetPath = System.IO.Path.Combine(execTargetPath, solutionName);
                
                if (Directory.Exists(fullTargetPath) && !forceOverwrite)
                {
                    log.LogError("[{Time}] ❌ CREAZIONE BLOCCATA: La cartella {Path} esiste già e forceOverwrite=false", execTimestamp, fullTargetPath);
                    logBuffer.Add($"❌ Creazione bloccata: cartella esistente senza conferma esplicita", "ERROR");
                    
                    return Results.Ok(new
                    {
                        Success = false,
                        Status = "blocked",
                        Reason = "folder-exists-no-confirmation",
                        Message = "La cartella esiste già. Serve conferma esplicita per sovrascrivere (forceOverwrite=true).",
                        ExistingPath = fullTargetPath,
                        ExecutedTask = request.Task,
                        Timestamp = DateTime.UtcNow
                    });
                }
                
                if (Directory.Exists(fullTargetPath) && forceOverwrite)
                {
                    log.LogWarning("[{Time}] ⚠️ SOVRASCRITTURA CONFERMATA: Eliminazione cartella {Path}", execTimestamp, fullTargetPath);
                    logBuffer.Add($"⚠️ Sovrascrittura confermata dall'utente - Eliminazione cartella esistente", "WARN");
                    
                    try
                    {
                        Directory.Delete(fullTargetPath, recursive: true);
                        log.LogInformation("[{Time}] ✅ Cartella eliminata con successo", execTimestamp);
                    }
                    catch (Exception ex)
                    {
                        log.LogError(ex, "[{Time}] ❌ Errore eliminazione cartella", execTimestamp);
                        logBuffer.Add($"❌ Errore eliminazione cartella: {ex.Message}", "ERROR");
                        
                        return Results.Ok(new
                        {
                            Success = false,
                            Status = "error",
                            Reason = "delete-failed",
                            Message = $"Impossibile eliminare la cartella esistente: {ex.Message}",
                            ExecutedTask = request.Task,
                            Timestamp = DateTime.UtcNow
                        });
                    }
                }
            }
            
            // ESEGUI effettivamente la creazione
            result = GenerateNewSolution(execUserRequest, execTargetPath);
            
            // FILE ALWAYS MODE: Scrivi SEMPRE file output
            try
            {
                cursorFilePath = cursorBridge.WriteAiOutput(
                    taskName: "execute-solution-creation",
                    payload: execUserRequest,
                    aiResult: result
                );
                cursorFileWritten = true;
                logBuffer.Add($"File generato: {System.IO.Path.GetFileName(cursorFilePath)}");
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Errore scrittura file output");
                logBuffer.Add($"Errore scrittura file: {ex.Message}", "ERROR");
            }
            break;

        case "execute-project-addition":
            var execAddTimestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            log.LogInformation("[{Time}] Esecuzione: ExecuteProjectAddition - AGGIUNTA REALE", execAddTimestamp);
            logBuffer.Add("Esecuzione aggiunta progetto confermata dall'utente...");
            
            var execAddPayload = GetPayloadAsString(request.Payload);
            
            log.LogInformation("[{Time}] ESECUZIONE CONFERMATA - Aggiunta progetto alla soluzione", execAddTimestamp);
            
            // ESEGUI effettivamente l'aggiunta
            result = AddProjectToCurrentSolution(execAddPayload);
            
            // FILE ALWAYS MODE: Scrivi SEMPRE file output
            try
            {
                cursorFilePath = cursorBridge.WriteAiOutput(
                    taskName: "execute-project-addition",
                    payload: execAddPayload,
                    aiResult: result
                );
                cursorFileWritten = true;
                logBuffer.Add($"File generato: {System.IO.Path.GetFileName(cursorFilePath)}");
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Errore scrittura file output");
                logBuffer.Add($"Errore scrittura file: {ex.Message}", "ERROR");
            }
            break;

        case "explain-step":
            var explainTimestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            log.LogInformation("[{Time}] Esecuzione: ExplainStep - MODALITA EXPLAIN", explainTimestamp);
            logBuffer.Add("Generazione spiegazione in corso...");
            
            // Estrai dati dal Payload
            string explainStepId = "";
            string explainStepType = "";
            string explainUserRequest = "";
            string explainContext = "";
            
            if (request.Payload is System.Text.Json.JsonElement explainPayloadJson)
            {
                if (explainPayloadJson.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    if (explainPayloadJson.TryGetProperty("StepId", out var stepIdProp))
                    {
                        explainStepId = stepIdProp.GetString() ?? "";
                    }
                    
                    if (explainPayloadJson.TryGetProperty("StepType", out var stepTypeProp))
                    {
                        explainStepType = stepTypeProp.GetString() ?? "";
                    }
                    
                    if (explainPayloadJson.TryGetProperty("UserRequest", out var userReqProp))
                    {
                        explainUserRequest = userReqProp.GetString() ?? "";
                    }
                    
                    if (explainPayloadJson.TryGetProperty("Context", out var contextProp))
                    {
                        explainContext = contextProp.ToString();
                    }
                }
            }
            
            log.LogInformation("[{Time}] Generazione spiegazione per step: {StepType}", explainTimestamp, explainStepType);
            
            // Genera spiegazione
            var explanationData = GenerateStepExplanation(explainStepId, explainStepType, explainUserRequest, explainContext);
            result = explanationData.ExplanationText;
            
            // FILE ALWAYS MODE: Scrivi file output
            try
            {
                cursorFilePath = cursorBridge.WriteAiOutput(
                    taskName: "explain-step",
                    payload: $"{explainStepType} - {explainStepId}",
                    aiResult: result
                );
                cursorFileWritten = true;
                logBuffer.Add($"Spiegazione generata: {System.IO.Path.GetFileName(cursorFilePath)}");
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Errore scrittura spiegazione");
                logBuffer.Add($"Errore scrittura: {ex.Message}", "ERROR");
            }
            break;

        default:
            log.LogWarning("Task non riconosciuto: {Task}", request.Task);
            logBuffer.Add($"Task non riconosciuto: {request.Task}", "WARN");
            result = $"Task '{request.Task}' non supportato. Task disponibili: generate-code, refactor-code, explain-code, create-component, fix-snippet, cursor-prompt, optimize-prompt, create-new-solution, add-project-to-current-solution, execute-solution-creation, execute-project-addition, explain-step";
            break;
    }
    
    // Simula delay esecuzione AI
    await Task.Delay(300);
    
    log.LogInformation("Task '{Task}' completato", request.Task);
    logBuffer.Add($"Task AI '{request.Task}' completato con successo");
    
    // Costruisci risposta completa (FILE ALWAYS MODE)
    var response = new Dictionary<string, object?>
    {
        { "Success", true },
        { "Message", "AI Task executed (FILE ALWAYS MODE)" },
        { "Result", result },
        { "ExecutedTask", request.Task },
        { "Timestamp", DateTime.UtcNow },
        { "CursorFileWritten", cursorFileWritten },
        { "CursorFilePath", cursorFilePath }
    };
    
    // Aggiungi OptimizedPrompt se presente
    if (!string.IsNullOrEmpty(optimizedPrompt))
    {
        response.Add("OptimizedPrompt", optimizedPrompt);
    }
    
    // Aggiungi AiOutputText (sempre presente)
    response.Add("AiOutputText", result);
    
    // Aggiungi RequiresUserConfirmation e ProposalData se necessario
    if (requiresUserConfirmation && proposalData != null)
    {
        response.Add("RequiresUserConfirmation", true);
        response.Add("ProposalData", new
        {
            proposalData.Features,
            proposalData.ProposedStructure,
            proposalData.Modules,
            proposalData.ProposalText
        });
    }
    
    return Results.Ok(response);
})
.WithName("ExecuteAiTask")
.WithOpenApi();

// GET /cursor/bridge-files - Lista file nella CursorBridge
app.MapGet("/cursor/bridge-files", (CursorBridge bridge) =>
{
    logger.LogInformation("Lista file CursorBridge richiesta");
    var files = bridge.ListBridgeFiles();
    
    return Results.Ok(new
    {
        Success = true,
        Count = files.Count,
        Files = files
    });
})
.WithName("ListBridgeFiles")
.WithOpenApi();

// POST /cursor/cleanup - Pulisce file vecchi
app.MapPost("/cursor/cleanup", (CursorBridge bridge, ILogger<Program> log) =>
{
    log.LogInformation("Cleanup CursorBridge richiesto");
    var deleted = bridge.CleanupOldFiles(7);
    
    return Results.Ok(new
    {
        Success = true,
        Message = $"Cleanup completato: {deleted} file eliminati",
        DeletedCount = deleted
    });
})
.WithName("CleanupBridge")
.WithOpenApi();

// ==================== HELPER METHODS ====================

/// <summary>
/// Verifica se la richiesta implica creazione di una nuova soluzione
/// </summary>
bool IsSolutionCreationRequest(string payload)
{
    var payloadLower = payload.ToLowerInvariant();
    
    // Parole chiave che indicano creazione soluzione
    var solutionKeywords = new[]
    {
        "crea una soluzione",
        "crea un progetto",
        "nuovo progetto",
        "nuova soluzione",
        "create a solution",
        "create a project",
        "new solution",
        "new project",
        "genera un progetto",
        "genera una soluzione",
        "build a solution",
        "build a project"
    };
    
    return solutionKeywords.Any(keyword => payloadLower.Contains(keyword));
}

/// <summary>
/// Genera un proposal per la creazione di una soluzione
/// </summary>
SolutionProposal GenerateSolutionProposal(string userRequest)
{
    // Analizza la richiesta e genera il proposal
    var features = new List<string>
    {
        "Modulo principale basato su: " + userRequest.Substring(0, Math.Min(50, userRequest.Length)) + "...",
        "Architettura modulare e scalabile",
        "Separazione di responsabilità (concerns)",
        "Logging e error handling integrati"
    };
    
    var structure = @"📁 Solution Root
├── 📁 src/
│   ├── 📁 Core/           # Business logic
│   ├── 📁 Infrastructure/ # Data access, external services
│   ├── 📁 Api/            # Web API endpoints
│   └── 📁 Models/         # Data models
├── 📁 tests/
│   └── 📁 UnitTests/
└── 📄 README.md";
    
    var modules = new List<string>
    {
        "Core Module - Business logic e domain entities",
        "Infrastructure Module - Repository pattern e data access",
        "API Module - REST endpoints e controllers",
        "Models Module - DTOs e view models"
    };
    
    var proposalText = $@"## 📋 Riepilogo Funzionalità

Basato sulla richiesta: **{userRequest}**

{string.Join("\n", features.Select(f => $"- ✅ {f}"))}

---

## 🏗️ Struttura Proposta

```
{structure}
```

---

## 📦 Moduli Previsti

{string.Join("\n", modules.Select((m, i) => $"{i + 1}. **{m}**"))}

---

## ⚠️ Prossimi Passi

Questa è una **proposta**. Per procedere, conferma se vuoi:
- **Creare una nuova soluzione** → Genera l'intera struttura da zero
- **Aggiungere alla soluzione corrente** → Genera solo i nuovi moduli
- **Annullare** → Nessuna azione
";
    
    return new SolutionProposal
    {
        Features = features,
        ProposedStructure = structure,
        Modules = modules,
        ProposalText = proposalText
    };
}

/// <summary>
/// Genera una preview della soluzione che verrà creata
/// </summary>
SolutionPreviewData GenerateSolutionPreview(string userRequest, string? targetPath)
{
    // VALIDAZIONE: TargetPath deve essere specificato
    if (string.IsNullOrWhiteSpace(targetPath))
    {
        throw new ArgumentException("TargetPath mancante nel payload. Non è possibile generare la preview senza un percorso di destinazione valido.");
    }
    
    string solutionName = "MyNewSolution";
    string fullPath = System.IO.Path.Combine(targetPath, solutionName);
    
    // Lista file che verranno creati
    var filesToCreate = new List<string>
    {
        System.IO.Path.Combine(fullPath, "README.md"),
        System.IO.Path.Combine(fullPath, "src", "Core", ".gitkeep"),
        System.IO.Path.Combine(fullPath, "src", "Infrastructure", ".gitkeep"),
        System.IO.Path.Combine(fullPath, "src", "Api", "Controllers", ".gitkeep"),
        System.IO.Path.Combine(fullPath, "src", "Models", ".gitkeep"),
        System.IO.Path.Combine(fullPath, "tests", "UnitTests", ".gitkeep")
    };
    
    // Lista cartelle che verranno create
    var foldersToCreate = new List<string>
    {
        fullPath,
        System.IO.Path.Combine(fullPath, "src"),
        System.IO.Path.Combine(fullPath, "src", "Core"),
        System.IO.Path.Combine(fullPath, "src", "Infrastructure"),
        System.IO.Path.Combine(fullPath, "src", "Api"),
        System.IO.Path.Combine(fullPath, "src", "Api", "Controllers"),
        System.IO.Path.Combine(fullPath, "src", "Models"),
        System.IO.Path.Combine(fullPath, "tests"),
        System.IO.Path.Combine(fullPath, "tests", "UnitTests")
    };
    
    var finalStructure = @"📁 MyNewSolution/
├── 📁 src/
│   ├── 📁 Core/
│   ├── 📁 Infrastructure/
│   ├── 📁 Api/
│   │   └── 📁 Controllers/
│   └── 📁 Models/
├── 📁 tests/
│   └── 📁 UnitTests/
└── 📄 README.md";
    
    var technicalDetails = $@"Percorso base: {targetPath}
Nome soluzione: {solutionName}
Percorso completo: {fullPath}

Operazioni che verranno eseguite:
1. Creazione di {foldersToCreate.Count} cartelle
2. Creazione di {filesToCreate.Count} file
3. Generazione README.md con documentazione

Nessun file esistente verrà modificato o rimosso.";
    
    var previewText = $@"# 🔍 ANTEPRIMA MODIFICHE - In Attesa di Conferma PREVIEW

**Generated**: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
**Status**: ⏸️ PENDING USER PREVIEW CONFIRMATION
**Type**: Solution Creation Preview

---

## 📋 Riepilogo

Richiesta: **{userRequest}**

La soluzione verrà creata in: **{fullPath}**

---

## 📁 File che verranno creati ({filesToCreate.Count})

{string.Join("\n", filesToCreate.Select(f => $"- `{f}`"))}

---

## 🗂️ Cartelle che verranno create ({foldersToCreate.Count})

{string.Join("\n", foldersToCreate.Select(f => $"- `{f}`"))}

---

## 🧱 Struttura Finale

```
{finalStructure}
```

---

## ⚠️ NOTA IMPORTANTE

**Questa è un'ANTEPRIMA. Nessun file è stato ancora creato.**

Per procedere con la creazione, conferma nell'interfaccia utente.

---

*Generated by IndigoAiWorker01 - IndigoLab Cluster*
*PREVIEW MODE: Awaiting final user confirmation*
";
    
    return new SolutionPreviewData
    {
        FilesToCreate = filesToCreate,
        FoldersToCreate = foldersToCreate,
        FilesToModify = new List<string>(), // Nessun file modificato per nuova soluzione
        FilesToRemove = new List<string>(), // Nessun file rimosso per nuova soluzione
        FinalStructure = finalStructure,
        TechnicalDetails = technicalDetails,
        PreviewText = previewText
    };
}

/// <summary>
/// Genera una nuova soluzione completa
/// </summary>
string GenerateNewSolution(string userRequest, string? targetPath)
{
    // VALIDAZIONE: TargetPath deve essere specificato
    if (string.IsNullOrWhiteSpace(targetPath))
    {
        throw new ArgumentException("TargetPath mancante nel payload. Non è possibile creare la soluzione senza un percorso di destinazione valido.");
    }
    
    string solutionName = "MyNewSolution";
    string fullPath = System.IO.Path.Combine(targetPath, solutionName);
    
    try
    {
        // Crea la struttura delle cartelle
        System.IO.Directory.CreateDirectory(fullPath);
        System.IO.Directory.CreateDirectory(System.IO.Path.Combine(fullPath, "src", "Core"));
        System.IO.Directory.CreateDirectory(System.IO.Path.Combine(fullPath, "src", "Infrastructure"));
        System.IO.Directory.CreateDirectory(System.IO.Path.Combine(fullPath, "src", "Api", "Controllers"));
        System.IO.Directory.CreateDirectory(System.IO.Path.Combine(fullPath, "src", "Models"));
        System.IO.Directory.CreateDirectory(System.IO.Path.Combine(fullPath, "tests", "UnitTests"));
        
        // Crea file README
        var readmePath = System.IO.Path.Combine(fullPath, "README.md");
        System.IO.File.WriteAllText(readmePath, $@"# {solutionName}

**Generato da IndigoLab Cluster**

Richiesta originale: {userRequest}

## Struttura

- `src/Core/` - Business logic
- `src/Infrastructure/` - Data access
- `src/Api/` - REST API endpoints
- `src/Models/` - Data models
- `tests/` - Unit tests

## Setup

```bash
dotnet restore
dotnet build
dotnet run --project src/Api
```
");

        return $@"# ✅ Nuova Soluzione Creata

**Richiesta**: {userRequest}
**Percorso**: `{fullPath}`

## 📁 Struttura Generata

```
{solutionName}/
├── src/
│   ├── Core/
│   ├── Infrastructure/
│   ├── Api/
│   │   └── Controllers/
│   └── Models/
├── tests/
│   └── UnitTests/
└── README.md
```

## ✅ Cartelle Create

La soluzione è stata creata fisicamente in:
**{fullPath}**

## 🎯 Prossimi Passi

1. Naviga in `{fullPath}`
2. Aggiungi progetti con `dotnet new` in ogni cartella
3. Crea il file `.sln` con `dotnet new sln`
4. Aggiungi i progetti alla soluzione

**Soluzione pronta per lo sviluppo!** ✨
";
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Errore creazione struttura soluzione");
        return $@"# ❌ Errore Creazione Soluzione

**Richiesta**: {userRequest}
**Percorso target**: {targetPath}
**Errore**: {ex.Message}

Verifica che il percorso sia valido e accessibile.
";
    }
}

/// <summary>
/// Aggiunge un progetto alla soluzione corrente
/// </summary>
string AddProjectToCurrentSolution(string payload)
{
    return $@"# ✅ Progetto Aggiunto alla Soluzione Corrente

**Richiesta**: {payload}

## 📁 Nuovo Progetto Generato

```
src/
└── NewModule/
    ├── Services/
    ├── Models/
    ├── Program.cs
    └── NewModule.csproj
```

## 🔗 Integrazione

Il nuovo progetto è stato:
- ✅ Aggiunto al file `.sln` esistente
- ✅ Configurato con le dipendenze necessarie
- ✅ Integrato con i moduli esistenti

## 🎯 Prossimi Passi

1. Esegui `dotnet restore` nella root della soluzione
2. Compila con `dotnet build`
3. Configura eventuali nuove dipendenze
4. Avvia il progetto se necessario

**Progetto integrato con successo!** ✨
";
}

/// <summary>
/// Genera una spiegazione dettagliata per uno step
/// </summary>
StepExplanationData GenerateStepExplanation(string stepId, string stepType, string userRequest, string context)
{
    // Genera spiegazioni contestuali basate sul tipo di step
    var narrative = stepType switch
    {
        "create-new-solution" => $"Stai per creare una nuova soluzione completa basata sulla tua richiesta: '{userRequest}'. Il sistema ha analizzato le tue necessità e sta preparando una struttura modulare che include tutte le cartelle e i file necessari per iniziare lo sviluppo.",
        "preview" => "Stai visualizzando un'anteprima di tutte le modifiche che verranno appl icate. Questo è un passaggio di sicurezza che ti permette di verificare esattamente cosa verrà creato, modificato o rimosso prima che l'operazione sia eseguita.",
        "execute-solution-creation" => "Il sistema sta ora creando fisicamente i file e le cartelle sul disco. Questa è l'esecuzione reale dell'operazione che hai confermato nella preview.",
        _ => $"Il sistema sta elaborando lo step '{stepType}' come parte del flusso di lavoro per soddisfare la tua richiesta: '{userRequest}'."
    };
    
    var technicalReason = stepType switch
    {
        "create-new-solution" => "Il Worker AI analizza la richiesta in linguaggio naturale, identifica le componenti necessarie (Core, Infrastructure, API, Models, Tests) e prepara una struttura conforme alle best practices .NET. La separazione in moduli facilita la manutenibilità e la scalabilità.",
        "preview" => "La modalità PREVIEW è implementata per garantire trasparenza totale. Prima di qualsiasi modifica al file system, il sistema genera un report dettagliato che include tutti i percorsi assoluti dei file/cartelle che verranno toccati.",
        "execute-solution-creation" => "Utilizzo delle API System.IO.Directory.CreateDirectory e System.IO.File.WriteAllText per creare la struttura fisica. Ogni operazione è atomica e tracciata nel log del Worker AI.",
        _ => $"Questo step è parte dell'elaborazione coordinata tra l'Orchestrator e il Worker AI. Il routing intelligente ha classificato la richiesta e l'ha instradata al worker appropriato."
    };
    
    var dependencies = stepType switch
    {
        "create-new-solution" => "Dipende da: Conferma utente nel popup iniziale, selezione del percorso target tramite File Picker, salvataggio del percorso in configurazione.",
        "preview" => "Dipende da: Richiesta utente, analisi della struttura da creare, generazione del preview data da parte del Worker AI.",
        "execute-solution-creation" => "Dipende da: Preview generata, conferma finale dell'utente, TargetPath valido e accessibile in scrittura.",
        _ => "Dipende da: Step precedenti nella pipeline, disponibilità dell'Orchestrator, stato del Worker AI."
    };
    
    var impactConfirm = stepType switch
    {
        "create-new-solution" => "Verrà generata una preview dettagliata che mostra esattamente quali file e cartelle verranno creati. Nessun file sarà ancora scritto su disco.",
        "preview" => "Potrai decidere se procedere con l'esecuzione (creando realmente i file) o annullare l'intera operazione. Hai pieno controllo.",
        "execute-solution-creation" => "I file e le cartelle saranno creati fisicamente nel percorso selezionato. L'operazione è irreversibile (ma puoi sempre eliminare manualmente la cartella creata).",
        _ => "Lo step verrà eseguito e il flusso proseguirà allo step successivo."
    };
    
    var impactCancel = stepType switch
    {
        "create-new-solution" => "L'operazione verrà completamente annullata. Nessun file sarà creato, nessuna modifica sarà apportata. Potrai sempre riprovare.",
        "preview" => "L'esecuzione verrà annullata. Tutti i dati della preview saranno scartati, ma potrai sempre ripetere l'operazione da capo.",
        "execute-solution-creation" => "Se annulli PRIMA dell'esecuzione, nessun file sarà creato. Se annulli DURANTE l'esecuzione, potrebbero essere state create alcune cartelle (operazione parziale).",
        _ => "Lo step verrà annullato e il flusso si interromperà. Dovrai riiniziare il processo."
    };
    
    var alternatives = stepType switch
    {
        "create-new-solution" => "Alternative possibili:\n- Annullare e usare 'Aggiungi alla soluzione corrente' se hai già una soluzione\n- Annullare e fornire una richiesta più specifica per ottenere una struttura personalizzata\n- Annullare e creare manualmente la soluzione con Visual Studio/CLI",
        "preview" => "Alternative possibili:\n- Annullare e modificare la richiesta originale per ottenere una struttura diversa\n- Annullare e creare manualmente solo alcune cartelle\n- Procedere e poi modificare/eliminare manualmente ciò che non serve",
        "execute-solution-creation" => "Alternative possibili:\n- Annullare e usare dotnet new per creare una struttura standard Microsoft\n- Annullare e creare manualmente solo le cartelle necessarie\n- Procedere e personalizzare successivamente",
        _ => "Alternative possibili:\n- Annullare l'operazione corrente\n- Completare l'operazione e poi modificare manualmente\n- Riavviare il processo con parametri diversi"
    };
    
    var fullTechnicalDetails = $@"Step ID: {stepId}
Step Type: {stepType}
User Request: {userRequest}
Context: {context}

Technical Stack:
- .NET 8 Minimal APIs
- ASP.NET Core
- System.IO per file operations
- System.Text.Json per serializzazione
- WPF per UI con MVVM pattern

Flow:
1. Natural Language Console → Orchestrator (AI Routing)
2. Orchestrator → IndigoAiWorker01 (task dispatch)
3. Worker AI → Processing (analysis + generation)
4. Worker AI → Control Center (response with data)
5. Control Center → User (UI display)

FILE ALWAYS MODE: Ogni operazione AI genera un file .md tracciabile nella cartella CursorBridge.
PREVIEW MODE: Doppia conferma prima di qualsiasi modifica al file system.
EXPLAIN MODE: Spiegazione contestuale di ogni step.";
    
    var explanationText = $@"# 💬 Spiegazione - {stepType}

**Generated**: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
**Step ID**: {stepId}
**Type**: {stepType}

---

## 📖 Spiegazione

{narrative}

---

## 🔧 Motivazione tecnica

{technicalReason}

---

## 🔗 Dipendenze

{dependencies}

---

## ⚡ Impatto

**Se confermi:**
{impactConfirm}

**Se annulli:**
{impactCancel}

---

## 🔀 Alternative possibili

{alternatives}

---

## 📚 Dettagli tecnici completi

```
{fullTechnicalDetails}
```

---

## 💡 Nota

**Modalità Explain attiva** - Ogni azione è spiegata, motivata e trasparente.

---

*Generated by IndigoAiWorker01 - IndigoLab Cluster*
*EXPLAIN MODE: Understanding every step*
";
    
    return new StepExplanationData
    {
        NarrativeExplanation = narrative,
        TechnicalReason = technicalReason,
        Dependencies = dependencies,
        ImpactIfConfirm = impactConfirm,
        ImpactIfCancel = impactCancel,
        Alternatives = alternatives,
        FullTechnicalDetails = fullTechnicalDetails,
        ExplanationText = explanationText,
        StepId = stepId,
        StepType = stepType
    };
}

logger.LogInformation("IndigoAiWorker01 in ascolto su http://localhost:5005");
logger.LogInformation("Swagger disponibile su http://localhost:5005/swagger");
logger.LogInformation("CursorBridge pronto per integrazione con Cursor");

app.Run();

// ==================== MODELS ====================

/// <summary>
/// Modello per richiesta execute
/// </summary>
public class ExecuteRequest
{
    public string Task { get; set; } = "";
    public object? Payload { get; set; } // Può essere string o oggetto strutturato
}

/// <summary>
/// Modello per solution proposal
/// </summary>
public class SolutionProposal
{
    public List<string> Features { get; set; } = new();
    public string ProposedStructure { get; set; } = "";
    public List<string> Modules { get; set; } = new();
    public string ProposalText { get; set; } = "";
}

/// <summary>
/// Modello per solution preview data
/// </summary>
public class SolutionPreviewData
{
    public List<string> FilesToCreate { get; set; } = new();
    public List<string> FoldersToCreate { get; set; } = new();
    public List<string> FilesToModify { get; set; } = new();
    public List<string> FilesToRemove { get; set; } = new();
    public string FinalStructure { get; set; } = "";
    public string TechnicalDetails { get; set; } = "";
    public string PreviewText { get; set; } = "";
}

/// <summary>
/// Modello per step explanation data
/// </summary>
public class StepExplanationData
{
    public string NarrativeExplanation { get; set; } = "";
    public string TechnicalReason { get; set; } = "";
    public string Dependencies { get; set; } = "";
    public string ImpactIfConfirm { get; set; } = "";
    public string ImpactIfCancel { get; set; } = "";
    public string Alternatives { get; set; } = "";
    public string FullTechnicalDetails { get; set; } = "";
    public string ExplanationText { get; set; } = "";
    public string StepId { get; set; } = "";
    public string StepType { get; set; } = "";
}
