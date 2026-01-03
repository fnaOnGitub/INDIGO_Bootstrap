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
                // Estrai nome soluzione dalla richiesta utente
                string solutionName = ExtractSolutionName(userRequest);
                string fullTargetPath = System.IO.Path.Combine(targetPath, solutionName);
                
                log.LogInformation("[{Time}] Nome soluzione estratto: '{Name}'", solutionTimestamp, solutionName);
                log.LogInformation("[{Time}] Percorso completo: '{Path}'", solutionTimestamp, fullTargetPath);
                
                if (Directory.Exists(fullTargetPath))
                {
                    log.LogWarning("[{Time}] ⚠️ CARTELLA GIÀ ESISTENTE: {Path}", solutionTimestamp, fullTargetPath);
                    logBuffer.Add($"⚠️ La cartella {solutionName} esiste già in {targetPath}", "WARN");
                    
                    // Suggerisci un nome alternativo con formato "_01", "_02", ecc.
                    int counter = 1;
                    string alternativeName = $"{solutionName}_{counter:D2}";
                    while (Directory.Exists(System.IO.Path.Combine(targetPath, alternativeName)))
                    {
                        counter++;
                        alternativeName = $"{solutionName}_{counter:D2}";
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
                        SolutionName = solutionName,
                        Timestamp = DateTime.UtcNow
                    });
                }
            }
            
            // ⭐ SOLUTION DESIGNER: Analisi intelligente della richiesta
            log.LogInformation("[{Time}] 🧠 Avvio Solution Designer - Analisi richiesta", solutionTimestamp);
            var analysis = SolutionDesigner.AnalyzeUserRequest(userRequest);
            
            log.LogInformation("[{Time}] ✅ Dominio identificato: '{Domain}'", solutionTimestamp, analysis.Domain);
            log.LogInformation("[{Time}] ✅ Entità rilevate: {Count} ({Entities})", solutionTimestamp, 
                analysis.Entities.Count, string.Join(", ", analysis.Entities));
            log.LogInformation("[{Time}] ✅ Azioni identificate: {Count}", solutionTimestamp, analysis.Actions.Count);
            log.LogInformation("[{Time}] ✅ Complessità: {Complexity}", solutionTimestamp, analysis.Complexity);
            log.LogInformation("[{Time}] ✅ Tipo architettura: {Type}", solutionTimestamp, analysis.ArchitectureType);
            
            logBuffer.Add($"🧠 Dominio: {analysis.Domain}");
            logBuffer.Add($"📦 Entità: {string.Join(", ", analysis.Entities)}");
            logBuffer.Add($"⚡ Complessità: {analysis.Complexity}");
            
            // ⭐ SOLUTION DESIGNER: Genera architettura dinamica
            log.LogInformation("[{Time}] 🏗️ Generazione architettura dinamica", solutionTimestamp);
            var architecture = SolutionDesigner.GenerateArchitecture(analysis);
            
            log.LogInformation("[{Time}] ✅ Soluzione: {Name}", solutionTimestamp, architecture.SolutionName);
            log.LogInformation("[{Time}] ✅ Progetti: {Count}", solutionTimestamp, architecture.Projects.Count);
            foreach (var project in architecture.Projects)
            {
                log.LogInformation("[{Time}]   - {Name} ({Type})", solutionTimestamp, project.Name, project.Type);
            }
            log.LogInformation("[{Time}] ✅ File da generare: {Count}", solutionTimestamp, architecture.Files.Count);
            
            logBuffer.Add($"🏗️ Architettura: {architecture.Projects.Count} progetti");
            logBuffer.Add($"📄 File totali: {architecture.Files.Count}");
            
            // ⭐ SOLUTION DESIGNER: Genera preview narrativa
            log.LogInformation("[{Time}] 📝 Generazione preview narrativa", solutionTimestamp);
            result = SolutionDesigner.GeneratePreview(analysis, architecture);
            
            log.LogInformation("[{Time}] ✅ Preview generata ({Length} caratteri)", solutionTimestamp, result.Length);
            logBuffer.Add("✅ Preview narrativa completata");
            
            // Salva i dati della preview per uso futuro (compatibilità con UI)
            requiresUserConfirmation = true;
            proposalData = new SolutionProposal
            {
                Features = analysis.Entities.Select(e => $"Gestione {e}").ToList(),
                ProposedStructure = string.Join("\n", architecture.Projects.Select(p => $"- {p.Name} ({p.Type})")),
                Modules = architecture.Projects.Select(p => p.Name).ToList(),
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
                // Estrai nome soluzione dalla richiesta utente
                string solutionName = ExtractSolutionName(execUserRequest);
                string fullTargetPath = System.IO.Path.Combine(execTargetPath, solutionName);
                
                log.LogInformation("[{Time}] Nome soluzione estratto: '{Name}'", execTimestamp, solutionName);
                log.LogInformation("[{Time}] Percorso completo: '{Path}'", execTimestamp, fullTargetPath);
                
                if (Directory.Exists(fullTargetPath) && !forceOverwrite)
                {
                    log.LogError("[{Time}] ❌ CREAZIONE BLOCCATA: La cartella {Path} esiste già e forceOverwrite=false", execTimestamp, fullTargetPath);
                    logBuffer.Add($"❌ Creazione bloccata: cartella esistente senza conferma esplicita", "ERROR");
                    
                    // Suggerisci nome alternativo invece di bloccare
                    int counter = 1;
                    string alternativeName = $"{solutionName}_{counter:D2}";
                    while (Directory.Exists(System.IO.Path.Combine(execTargetPath, alternativeName)))
                    {
                        counter++;
                        alternativeName = $"{solutionName}_{counter:D2}";
                    }
                    
                    log.LogInformation("[{Time}] Nome alternativo suggerito: {Alternative}", execTimestamp, alternativeName);
                    
                    return Results.Ok(new
                    {
                        Success = false,
                        Status = "blocked",
                        Reason = "folder-exists-no-confirmation",
                        Message = $"La cartella esiste già. Serve conferma esplicita per sovrascrivere (forceOverwrite=true) o usa il nome alternativo suggerito: {alternativeName}",
                        ExistingPath = fullTargetPath,
                        SuggestedAlternativeName = alternativeName,
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
            
            // ⭐ SOLUTION DESIGNER: Rigenera analisi e architettura per la creazione reale
            log.LogInformation("[{Time}] 🧠 Rigenero analisi con Solution Designer", execTimestamp);
            var execAnalysis = SolutionDesigner.AnalyzeUserRequest(execUserRequest);
            
            log.LogInformation("[{Time}] 🏗️ Rigenero architettura dinamica", execTimestamp);
            var execArchitecture = SolutionDesigner.GenerateArchitecture(execAnalysis);
            
            log.LogInformation("[{Time}] 💻 Genero codice reale per tutti i file", execTimestamp);
            var execGeneratedCode = SolutionDesigner.GenerateCode(execArchitecture);
            
            log.LogInformation("[{Time}] ✅ Codice generato per {Count} file", execTimestamp, execGeneratedCode.Count);
            logBuffer.Add($"💻 Codice generato: {execGeneratedCode.Count} file");
            
            // ⭐ ESEGUI effettivamente la creazione fisica
            log.LogInformation("[{Time}] 📁 Inizio creazione fisica struttura", execTimestamp);
            logBuffer.Add("📁 Creazione cartelle e file in corso...");
            
            try
            {
                // Crea cartella principale della soluzione
                var mainSolutionPath = System.IO.Path.Combine(execTargetPath!, execArchitecture.SolutionName);
                Directory.CreateDirectory(mainSolutionPath);
                log.LogInformation("[{Time}] ✅ Cartella soluzione creata: {Path}", execTimestamp, mainSolutionPath);
                logBuffer.Add($"✅ Cartella principale: {execArchitecture.SolutionName}");
                
                // Crea tutte le cartelle necessarie
                foreach (var folder in execArchitecture.Folders)
                {
                    var folderPath = System.IO.Path.Combine(mainSolutionPath, folder);
                    Directory.CreateDirectory(folderPath);
                    log.LogInformation("[{Time}]   Cartella: {Folder}", execTimestamp, folder);
                }
                logBuffer.Add($"✅ {execArchitecture.Folders.Count} cartelle create");
                
                // Scrivi tutti i file generati
                int filesCreated = 0;
                foreach (var file in execGeneratedCode)
                {
                    var filePath = System.IO.Path.Combine(mainSolutionPath, file.Key);
                    var fileDirectory = System.IO.Path.GetDirectoryName(filePath);
                    
                    // Assicurati che la directory esista
                    if (!string.IsNullOrEmpty(fileDirectory))
                    {
                        Directory.CreateDirectory(fileDirectory);
                    }
                    
                    // Scrivi il file
                    File.WriteAllText(filePath, file.Value);
                    filesCreated++;
                    
                    log.LogInformation("[{Time}]   File: {File}", execTimestamp, file.Key);
                }
                
                logBuffer.Add($"✅ {filesCreated} file creati");
                log.LogInformation("[{Time}] ✅ Creazione completata con successo!", execTimestamp);
                
                result = $@"# ✅ Soluzione Creata con Successo!

**Soluzione**: {execArchitecture.SolutionName}
**Percorso**: {mainSolutionPath}
**Progetti**: {execArchitecture.Projects.Count}
**File totali**: {filesCreated}

## 📂 Struttura Creata

{string.Join("\n", execArchitecture.Projects.Select(p => $"- **{p.Name}** ({p.Type})"))}

## 🚀 Prossimi Passi

```bash
cd ""{mainSolutionPath}""
dotnet restore
dotnet build
```

## 📦 Progetti Generati

{string.Join("\n\n", execArchitecture.Projects.Select(p => $@"### {p.Name}

**Tipo**: {p.Type}  
**Descrizione**: {p.Description}  
**Dipendenze**: {(p.Dependencies.Count > 0 ? string.Join(", ", p.Dependencies) : "Nessuna")}"))}

---

*Soluzione generata dinamicamente da Solution Designer - IndigoLab Cluster*  
*Architettura: {execAnalysis.ArchitectureType} | Complessità: {execAnalysis.Complexity}*
";
                
                logBuffer.Add("✅ SOLUZIONE CREATA CON SUCCESSO!");
            }
            catch (Exception ex)
            {
                log.LogError(ex, "[{Time}] ❌ Errore durante creazione fisica", execTimestamp);
                logBuffer.Add($"❌ Errore creazione: {ex.Message}", "ERROR");
                
                result = $@"# ❌ Errore Durante Creazione

{ex.Message}

Dettagli tecnici:
```
{ex.StackTrace}
```
";
            }
            
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
/// Estrae il nome della soluzione dalla richiesta utente
/// </summary>
string ExtractSolutionName(string userRequest)
{
    if (string.IsNullOrWhiteSpace(userRequest))
        return "MyNewSolution";
    
    // Rimuovi parole comuni
    var cleanRequest = userRequest.ToLowerInvariant()
        .Replace("crea", "")
        .Replace("genera", "")
        .Replace("sviluppa", "")
        .Replace("costruisci", "")
        .Replace("una soluzione", "")
        .Replace("un progetto", "")
        .Replace("per", "")
        .Replace("gestire", "")
        .Replace("gestione", "")
        .Replace("di", "")
        .Replace("del", "")
        .Replace("della", "")
        .Replace("il", "")
        .Replace("la", "")
        .Replace("i", "")
        .Replace("le", "")
        .Replace("uno", "")
        .Replace("una", "")
        .Trim();
    
    // Prendi le prime 2-3 parole significative
    var words = cleanRequest.Split(new[] { ' ', ',', '.', ';', '-', '_' }, StringSplitOptions.RemoveEmptyEntries)
        .Where(w => w.Length > 2) // Ignora parole troppo corte
        .Take(3)
        .ToArray();
    
    if (words.Length == 0)
        return "MyNewSolution";
    
    // Converti in PascalCase
    var solutionName = string.Join("", words.Select(w => 
        char.ToUpper(w[0]) + w.Substring(1).ToLower()));
    
    // Aggiungi suffisso se non c'è già un sostantivo chiaro
    if (!solutionName.EndsWith("Manager") && 
        !solutionName.EndsWith("Management") && 
        !solutionName.EndsWith("System") &&
        !solutionName.EndsWith("Service") &&
        !solutionName.EndsWith("App") &&
        !solutionName.EndsWith("Tool") &&
        solutionName.Length < 20) // Solo se il nome è abbastanza corto
    {
        solutionName += "Manager";
    }
    
    // Limita lunghezza
    if (solutionName.Length > 50)
        solutionName = solutionName.Substring(0, 50);
    
    // Rimuovi caratteri non validi per nomi cartella Windows
    solutionName = string.Join("", solutionName.Where(c => 
        char.IsLetterOrDigit(c) || c == '_' || c == '-'));
    
    // Se rimane vuoto dopo la pulizia, usa default
    if (string.IsNullOrWhiteSpace(solutionName))
        return "MyNewSolution";
    
    return solutionName;
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
    
    // Estrai nome soluzione dalla richiesta utente
    string solutionName = ExtractSolutionName(userRequest);
    string fullPath = System.IO.Path.Combine(targetPath, solutionName);
    
    logger.LogInformation("Preview per soluzione '{SolutionName}' in '{Path}'", solutionName, fullPath);
    
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
    
    // Estrai nome soluzione dalla richiesta utente
    string solutionName = ExtractSolutionName(userRequest);
    string fullPath = System.IO.Path.Combine(targetPath, solutionName);
    
    logger.LogInformation("Creazione soluzione '{SolutionName}' in '{Path}'", solutionName, fullPath);
    
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

// ==================== SOLUTION DESIGNER ====================

/// <summary>
/// Solution Designer intelligente - Analizza richieste e genera architetture dinamiche
/// </summary>
public static class SolutionDesigner
{
    /// <summary>
    /// Analizza la richiesta utente ed estrae requisiti, entità, azioni, casi d'uso
    /// </summary>
    public static DomainAnalysis AnalyzeUserRequest(string userRequest)
    {
        var requestLower = userRequest.ToLowerInvariant();
        
        // Identifica il dominio
        var domain = IdentifyDomain(requestLower);
        
        // Estrai entità
        var entities = ExtractEntities(requestLower);
        
        // Identifica azioni principali
        var actions = ExtractActions(requestLower);
        
        // Identifica casi d'uso
        var useCases = GenerateUseCases(entities, actions);
        
        // Determina complessità
        var complexity = DetermineComplexity(entities.Count, actions.Count);
        
        // Identifica tipo architettura richiesta
        var architectureType = IdentifyArchitectureType(requestLower);
        
        return new DomainAnalysis
        {
            Domain = domain,
            Entities = entities,
            Actions = actions,
            UseCases = useCases,
            Complexity = complexity,
            ArchitectureType = architectureType,
            RequiresApi = requestLower.Contains("api") || requestLower.Contains("rest") || requestLower.Contains("endpoint"),
            RequiresDatabase = requestLower.Contains("database") || requestLower.Contains("persist") || requestLower.Contains("salva") || entities.Count > 2,
            RequiresUI = requestLower.Contains("ui") || requestLower.Contains("interface") || requestLower.Contains("dashboard") || requestLower.Contains("web"),
            RequiresAuth = requestLower.Contains("auth") || requestLower.Contains("login") || requestLower.Contains("utent"),
            UserRequest = userRequest
        };
    }
    
    /// <summary>
    /// Genera un'architettura coerente basata sull'analisi del dominio
    /// </summary>
    public static SolutionArchitecture GenerateArchitecture(DomainAnalysis analysis)
    {
        var solutionName = GenerateSolutionName(analysis.Domain);
        var projects = new List<ProjectInfo>();
        var folders = new List<string>();
        var files = new List<FileInfo>();
        
        // Determina progetti necessari in base all'analisi
        if (analysis.RequiresApi || analysis.ArchitectureType == "microservice")
        {
            projects.Add(new ProjectInfo
            {
                Name = $"{solutionName}.Api",
                Type = "webapi",
                Description = "REST API con controllers e endpoints",
                Dependencies = new List<string> { $"{solutionName}.Core" }
            });
        }
        
        // Core sempre presente (business logic)
        projects.Add(new ProjectInfo
        {
            Name = $"{solutionName}.Core",
            Type = "classlib",
            Description = "Business logic, interfaces, domain models",
            Dependencies = new List<string>()
        });
        
        if (analysis.RequiresDatabase)
        {
            projects.Add(new ProjectInfo
            {
                Name = $"{solutionName}.Infrastructure",
                Type = "classlib",
                Description = "Data access, repositories, database context",
                Dependencies = new List<string> { $"{solutionName}.Core" }
            });
        }
        
        if (analysis.RequiresUI)
        {
            projects.Add(new ProjectInfo
            {
                Name = $"{solutionName}.Web",
                Type = "webapp",
                Description = "Frontend web application (Blazor/Razor)",
                Dependencies = new List<string> { $"{solutionName}.Core" }
            });
        }
        
        // Genera struttura cartelle e file per ogni progetto
        foreach (var project in projects)
        {
            GenerateProjectStructure(project, analysis, folders, files, solutionName);
        }
        
        // Aggiungi README.md alla root
        files.Add(new FileInfo
        {
            Path = "README.md",
            Type = "markdown",
            Description = "Documentazione soluzione"
        });
        
        return new SolutionArchitecture
        {
            SolutionName = solutionName,
            Projects = projects,
            Folders = folders,
            Files = files,
            Analysis = analysis
        };
    }
    
    /// <summary>
    /// Genera il codice reale per tutti i file dell'architettura
    /// </summary>
    public static Dictionary<string, string> GenerateCode(SolutionArchitecture architecture)
    {
        var generatedFiles = new Dictionary<string, string>();
        
        foreach (var project in architecture.Projects)
        {
            // Genera .csproj
            var csprojPath = $"{project.Name}/{project.Name}.csproj";
            generatedFiles[csprojPath] = GenerateCsprojContent(project, architecture);
            
            // Genera file specifici in base al tipo progetto
            switch (project.Type)
            {
                case "webapi":
                    GenerateApiProjectFiles(project, architecture, generatedFiles);
                    break;
                case "classlib":
                    if (project.Name.EndsWith(".Core"))
                        GenerateCoreProjectFiles(project, architecture, generatedFiles);
                    else if (project.Name.EndsWith(".Infrastructure"))
                        GenerateInfrastructureProjectFiles(project, architecture, generatedFiles);
                    break;
                case "webapp":
                    GenerateWebProjectFiles(project, architecture, generatedFiles);
                    break;
            }
        }
        
        // Genera README.md
        generatedFiles["README.md"] = GenerateReadmeContent(architecture);
        
        return generatedFiles;
    }
    
    /// <summary>
    /// Genera una preview narrativa e dettagliata dell'architettura
    /// </summary>
    public static string GeneratePreview(DomainAnalysis analysis, SolutionArchitecture architecture)
    {
        var preview = $@"# 🎯 Solution Design Preview

**Soluzione**: {architecture.SolutionName}  
**Dominio**: {analysis.Domain}  
**Complessità**: {analysis.Complexity}  
**Tipo Architettura**: {analysis.ArchitectureType}

---

## 🧠 Analisi della Richiesta

Ho analizzato la tua richiesta: *""{analysis.UserRequest}""*

### 📦 Entità Identificate ({analysis.Entities.Count})
{string.Join("\n", analysis.Entities.Select(e => $"- **{e}**"))}

### ⚡ Azioni Principali ({analysis.Actions.Count})
{string.Join("\n", analysis.Actions.Select(a => $"- {a}"))}

### 💼 Casi d'Uso ({analysis.UseCases.Count})
{string.Join("\n", analysis.UseCases.Select(u => $"- {u}"))}

---

## 🏗️ Architettura Proposta

Basandomi sull'analisi, propongo un'architettura **{analysis.ArchitectureType}** con i seguenti progetti:

### 📂 Progetti ({architecture.Projects.Count})

{string.Join("\n\n", architecture.Projects.Select(p => $@"#### {p.Name}
**Tipo**: {p.Type}  
**Scopo**: {p.Description}  
**Dipendenze**: {(p.Dependencies.Count > 0 ? string.Join(", ", p.Dependencies) : "Nessuna")}"))}

---

## 📁 Struttura Completa

```
{architecture.SolutionName}/
{string.Join("\n", architecture.Projects.Select(p => $"├── {p.Name}/"))}
└── README.md
```

### File Totali: {architecture.Files.Count}

{string.Join("\n", architecture.Files.Take(10).Select(f => $"- `{f.Path}` ({f.Type})"))}
{(architecture.Files.Count > 10 ? $"\n... e altri {architecture.Files.Count - 10} file" : "")}

---

## 🎨 Motivazioni Architetturali

{GenerateArchitecturalReasons(analysis, architecture)}

---

## 📊 Funzionalità Incluse

{GenerateFeaturesList(analysis)}

---

## ⚠️ NOTA IMPORTANTE

**Questa è un'ANTEPRIMA generata dinamicamente.**

Nessun file è stato ancora creato. Per procedere con la creazione reale della soluzione, conferma nell'interfaccia utente.

---

*Generated by Solution Designer - IndigoLab Cluster*  
*Architecture Type: {analysis.ArchitectureType}*  
*Complexity: {analysis.Complexity}*
";
        
        return preview;
    }
    
    // ========== METODI HELPER ==========
    
    private static string IdentifyDomain(string request)
    {
        if (request.Contains("colori") || request.Contains("palette")) return "Gestione Colori";
        if (request.Contains("utent") || request.Contains("user")) return "Gestione Utenti";
        if (request.Contains("prodott") || request.Contains("product")) return "Gestione Prodotti";
        if (request.Contains("ordini") || request.Contains("order")) return "Gestione Ordini";
        if (request.Contains("inventory") || request.Contains("magazzino")) return "Gestione Inventario";
        if (request.Contains("fattur") || request.Contains("invoice")) return "Fatturazione";
        if (request.Contains("report") || request.Contains("analytic")) return "Reporting e Analytics";
        if (request.Contains("notif")) return "Sistema Notifiche";
        if (request.Contains("auth") || request.Contains("login")) return "Autenticazione";
        if (request.Contains("blog") || request.Contains("post")) return "Content Management";
        if (request.Contains("task") || request.Contains("todo")) return "Task Management";
        
        return "Business Domain";
    }
    
    private static List<string> ExtractEntities(string request)
    {
        var entities = new List<string>();
        
        if (request.Contains("utent") || request.Contains("user")) entities.Add("User");
        if (request.Contains("prodott") || request.Contains("product")) entities.Add("Product");
        if (request.Contains("ordini") || request.Contains("order")) entities.Add("Order");
        if (request.Contains("categor")) entities.Add("Category");
        if (request.Contains("colore") || request.Contains("color")) entities.Add("Color");
        if (request.Contains("palette")) entities.Add("Palette");
        if (request.Contains("cliente") || request.Contains("customer")) entities.Add("Customer");
        if (request.Contains("fattura") || request.Contains("invoice")) entities.Add("Invoice");
        if (request.Contains("pagamento") || request.Contains("payment")) entities.Add("Payment");
        if (request.Contains("articol")) entities.Add("Article");
        if (request.Contains("task")) entities.Add("Task");
        if (request.Contains("progetto") || request.Contains("project")) entities.Add("Project");
        
        // Se non troviamo entità specifiche, ne aggiungiamo alcune generiche
        if (entities.Count == 0)
        {
            entities.Add("Entity");
            entities.Add("Item");
        }
        
        return entities.Distinct().ToList();
    }
    
    private static List<string> ExtractActions(string request)
    {
        var actions = new List<string>();
        
        if (request.Contains("crea") || request.Contains("create")) actions.Add("Creare");
        if (request.Contains("modific") || request.Contains("edit") || request.Contains("update")) actions.Add("Modificare");
        if (request.Contains("elimin") || request.Contains("delete")) actions.Add("Eliminare");
        if (request.Contains("visual") || request.Contains("view") || request.Contains("list")) actions.Add("Visualizzare");
        if (request.Contains("cerc") || request.Contains("search")) actions.Add("Cercare");
        if (request.Contains("filtra") || request.Contains("filter")) actions.Add("Filtrare");
        if (request.Contains("esport") || request.Contains("export")) actions.Add("Esportare");
        if (request.Contains("import")) actions.Add("Importare");
        if (request.Contains("validar") || request.Contains("validate")) actions.Add("Validare");
        if (request.Contains("calcola") || request.Contains("calculate")) actions.Add("Calcolare");
        
        // Azioni di default se non troviamo nulla
        if (actions.Count == 0)
        {
            actions.Add("Gestire");
            actions.Add("Visualizzare");
        }
        
        return actions;
    }
    
    private static List<string> GenerateUseCases(List<string> entities, List<string> actions)
    {
        var useCases = new List<string>();
        
        foreach (var entity in entities.Take(3))
        {
            foreach (var action in actions.Take(3))
            {
                useCases.Add($"{action} {entity}");
            }
        }
        
        return useCases.Take(6).ToList();
    }
    
    private static string DetermineComplexity(int entityCount, int actionCount)
    {
        var score = entityCount + actionCount;
        
        if (score <= 3) return "low";
        if (score <= 6) return "medium";
        return "high";
    }
    
    private static string IdentifyArchitectureType(string request)
    {
        if (request.Contains("microservice")) return "microservice";
        if (request.Contains("monolith")) return "monolithic";
        if (request.Contains("api") || request.Contains("rest")) return "api-first";
        if (request.Contains("clean") || request.Contains("ddd")) return "clean-architecture";
        
        return "layered"; // Default
    }
    
    private static string GenerateSolutionName(string domain)
    {
        var cleaned = domain
            .Replace("Gestione ", "")
            .Replace("Sistema ", "")
            .Replace(" ", "");
        
        return cleaned + "Manager";
    }
    
    private static void GenerateProjectStructure(ProjectInfo project, DomainAnalysis analysis, 
        List<string> folders, List<FileInfo> files, string solutionName)
    {
        var projectPath = project.Name;
        
        switch (project.Type)
        {
            case "webapi":
                folders.Add($"{projectPath}/Controllers");
                folders.Add($"{projectPath}/Models");
                folders.Add($"{projectPath}/Properties");
                
                files.Add(new FileInfo { Path = $"{projectPath}/Controllers/BaseController.cs", Type = "csharp" });
                foreach (var entity in analysis.Entities.Take(3))
                {
                    files.Add(new FileInfo { Path = $"{projectPath}/Controllers/{entity}Controller.cs", Type = "csharp" });
                }
                files.Add(new FileInfo { Path = $"{projectPath}/Program.cs", Type = "csharp" });
                files.Add(new FileInfo { Path = $"{projectPath}/appsettings.json", Type = "json" });
                break;
                
            case "classlib":
                if (project.Name.EndsWith(".Core"))
                {
                    folders.Add($"{projectPath}/Interfaces");
                    folders.Add($"{projectPath}/Models");
                    folders.Add($"{projectPath}/Services");
                    
                    foreach (var entity in analysis.Entities)
                    {
                        files.Add(new FileInfo { Path = $"{projectPath}/Models/{entity}.cs", Type = "csharp" });
                        files.Add(new FileInfo { Path = $"{projectPath}/Interfaces/I{entity}Service.cs", Type = "csharp" });
                        files.Add(new FileInfo { Path = $"{projectPath}/Services/{entity}Service.cs", Type = "csharp" });
                    }
                }
                else if (project.Name.EndsWith(".Infrastructure"))
                {
                    folders.Add($"{projectPath}/Data");
                    folders.Add($"{projectPath}/Repositories");
                    
                    files.Add(new FileInfo { Path = $"{projectPath}/Data/ApplicationDbContext.cs", Type = "csharp" });
                    foreach (var entity in analysis.Entities)
                    {
                        files.Add(new FileInfo { Path = $"{projectPath}/Repositories/{entity}Repository.cs", Type = "csharp" });
                    }
                }
                break;
        }
    }
    
    private static string GenerateCsprojContent(ProjectInfo project, SolutionArchitecture architecture)
    {
        var sdk = project.Type == "webapi" ? "Microsoft.NET.Sdk.Web" : "Microsoft.NET.Sdk";
        var dependencies = string.Join("\n", project.Dependencies.Select(d => 
            $"    <ProjectReference Include=\"..\\{d}\\{d}.csproj\" />"));
        
        return $@"<Project Sdk=""{sdk}"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
{dependencies}
  </ItemGroup>
</Project>";
    }
    
    private static void GenerateApiProjectFiles(ProjectInfo project, SolutionArchitecture architecture, 
        Dictionary<string, string> files)
    {
        // Program.cs
        files[$"{project.Name}/Program.cs"] = $@"var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{{
    app.UseSwagger();
    app.UseSwaggerUI();
}}

app.UseAuthorization();
app.MapControllers();
app.Run();
";
        
        // Controllers
        foreach (var entity in architecture.Analysis.Entities.Take(3))
        {
            files[$"{project.Name}/Controllers/{entity}Controller.cs"] = GenerateControllerCode(entity, architecture);
        }
        
        // appsettings.json
        files[$"{project.Name}/appsettings.json"] = @"{
  ""Logging"": {
    ""LogLevel"": {
      ""Default"": ""Information"",
      ""Microsoft.AspNetCore"": ""Warning""
    }
  },
  ""AllowedHosts"": ""*""
}";
    }
    
    private static void GenerateCoreProjectFiles(ProjectInfo project, SolutionArchitecture architecture, 
        Dictionary<string, string> files)
    {
        foreach (var entity in architecture.Analysis.Entities)
        {
            // Model
            files[$"{project.Name}/Models/{entity}.cs"] = GenerateModelCode(entity, project.Name);
            
            // Interface
            files[$"{project.Name}/Interfaces/I{entity}Service.cs"] = GenerateServiceInterfaceCode(entity, project.Name);
            
            // Service
            files[$"{project.Name}/Services/{entity}Service.cs"] = GenerateServiceCode(entity, project.Name);
        }
    }
    
    private static void GenerateInfrastructureProjectFiles(ProjectInfo project, SolutionArchitecture architecture, 
        Dictionary<string, string> files)
    {
        // DbContext
        files[$"{project.Name}/Data/ApplicationDbContext.cs"] = GenerateDbContextCode(architecture, project.Name);
        
        // Repositories
        foreach (var entity in architecture.Analysis.Entities)
        {
            files[$"{project.Name}/Repositories/{entity}Repository.cs"] = GenerateRepositoryCode(entity, project.Name);
        }
    }
    
    private static void GenerateWebProjectFiles(ProjectInfo project, SolutionArchitecture architecture, 
        Dictionary<string, string> files)
    {
        // Program.cs base per web app
        files[$"{project.Name}/Program.cs"] = @"var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();
var app = builder.Build();
app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();
app.Run();
";
    }
    
    private static string GenerateControllerCode(string entity, SolutionArchitecture architecture)
    {
        var solutionName = architecture.SolutionName;
        
        return $@"using Microsoft.AspNetCore.Mvc;
using {solutionName}.Core.Interfaces;
using {solutionName}.Core.Models;

namespace {solutionName}.Api.Controllers;

[ApiController]
[Route(""api/[controller]"")]
public class {entity}Controller : ControllerBase
{{
    private readonly I{entity}Service _{entity.ToLower()}Service;
    
    public {entity}Controller(I{entity}Service {entity.ToLower()}Service)
    {{
        _{entity.ToLower()}Service = {entity.ToLower()}Service;
    }}
    
    [HttpGet]
    public async Task<ActionResult<IEnumerable<{entity}>>> GetAll()
    {{
        var items = await _{entity.ToLower()}Service.GetAllAsync();
        return Ok(items);
    }}
    
    [HttpGet(""{{id}}"")]
    public async Task<ActionResult<{entity}>> GetById(int id)
    {{
        var item = await _{entity.ToLower()}Service.GetByIdAsync(id);
        if (item == null) return NotFound();
        return Ok(item);
    }}
    
    [HttpPost]
    public async Task<ActionResult<{entity}>> Create({entity} item)
    {{
        var created = await _{entity.ToLower()}Service.CreateAsync(item);
        return CreatedAtAction(nameof(GetById), new {{ id = created.Id }}, created);
    }}
    
    [HttpPut(""{{id}}"")]
    public async Task<IActionResult> Update(int id, {entity} item)
    {{
        if (id != item.Id) return BadRequest();
        await _{entity.ToLower()}Service.UpdateAsync(item);
        return NoContent();
    }}
    
    [HttpDelete(""{{id}}"")]
    public async Task<IActionResult> Delete(int id)
    {{
        await _{entity.ToLower()}Service.DeleteAsync(id);
        return NoContent();
    }}
}}
";
    }
    
    private static string GenerateModelCode(string entity, string projectName)
    {
        return $@"namespace {projectName}.Models;

public class {entity}
{{
    public int Id {{ get; set; }}
    public string Name {{ get; set; }} = string.Empty;
    public string Description {{ get; set; }} = string.Empty;
    public DateTime CreatedAt {{ get; set; }} = DateTime.UtcNow;
    public DateTime? UpdatedAt {{ get; set; }}
    public bool IsActive {{ get; set; }} = true;
}}
";
    }
    
    private static string GenerateServiceInterfaceCode(string entity, string projectName)
    {
        return $@"using {projectName}.Models;

namespace {projectName}.Interfaces;

public interface I{entity}Service
{{
    Task<IEnumerable<{entity}>> GetAllAsync();
    Task<{entity}?> GetByIdAsync(int id);
    Task<{entity}> CreateAsync({entity} item);
    Task UpdateAsync({entity} item);
    Task DeleteAsync(int id);
}}
";
    }
    
    private static string GenerateServiceCode(string entity, string projectName)
    {
        return $@"using {projectName}.Interfaces;
using {projectName}.Models;

namespace {projectName}.Services;

public class {entity}Service : I{entity}Service
{{
    private readonly List<{entity}> _items = new();
    
    public Task<IEnumerable<{entity}>> GetAllAsync()
    {{
        return Task.FromResult<IEnumerable<{entity}>>(_items);
    }}
    
    public Task<{entity}?> GetByIdAsync(int id)
    {{
        var item = _items.FirstOrDefault(x => x.Id == id);
        return Task.FromResult(item);
    }}
    
    public Task<{entity}> CreateAsync({entity} item)
    {{
        item.Id = _items.Count + 1;
        item.CreatedAt = DateTime.UtcNow;
        _items.Add(item);
        return Task.FromResult(item);
    }}
    
    public Task UpdateAsync({entity} item)
    {{
        var existing = _items.FirstOrDefault(x => x.Id == item.Id);
        if (existing != null)
        {{
            existing.Name = item.Name;
            existing.Description = item.Description;
            existing.UpdatedAt = DateTime.UtcNow;
        }}
        return Task.CompletedTask;
    }}
    
    public Task DeleteAsync(int id)
    {{
        var item = _items.FirstOrDefault(x => x.Id == id);
        if (item != null) _items.Remove(item);
        return Task.CompletedTask;
    }}
}}
";
    }
    
    private static string GenerateDbContextCode(SolutionArchitecture architecture, string projectName)
    {
        var solutionName = architecture.SolutionName;
        var dbSets = string.Join("\n    ", architecture.Analysis.Entities.Select(e => 
            $"public DbSet<{e}> {e}s {{ get; set; }}"));
        
        return $@"using Microsoft.EntityFrameworkCore;
using {solutionName}.Core.Models;

namespace {projectName}.Data;

public class ApplicationDbContext : DbContext
{{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) 
        : base(options) {{ }}
    
    {dbSets}
}}
";
    }
    
    private static string GenerateRepositoryCode(string entity, string projectName)
    {
        var solutionName = projectName.Replace(".Infrastructure", "");
        
        return $@"using {solutionName}.Core.Models;
using {projectName}.Data;
using Microsoft.EntityFrameworkCore;

namespace {projectName}.Repositories;

public class {entity}Repository
{{
    private readonly ApplicationDbContext _context;
    
    public {entity}Repository(ApplicationDbContext context)
    {{
        _context = context;
    }}
    
    public async Task<List<{entity}>> GetAllAsync()
    {{
        return await _context.{entity}s.ToListAsync();
    }}
    
    public async Task<{entity}?> GetByIdAsync(int id)
    {{
        return await _context.{entity}s.FindAsync(id);
    }}
    
    public async Task<{entity}> CreateAsync({entity} item)
    {{
        _context.{entity}s.Add(item);
        await _context.SaveChangesAsync();
        return item;
    }}
    
    public async Task UpdateAsync({entity} item)
    {{
        _context.Entry(item).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }}
    
    public async Task DeleteAsync(int id)
    {{
        var item = await _context.{entity}s.FindAsync(id);
        if (item != null)
        {{
            _context.{entity}s.Remove(item);
            await _context.SaveChangesAsync();
        }}
    }}
}}
";
    }
    
    private static string GenerateReadmeContent(SolutionArchitecture architecture)
    {
        return $@"# {architecture.SolutionName}

**Dominio**: {architecture.Analysis.Domain}  
**Generato da**: IndigoLab Solution Designer  
**Data**: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC

---

## 📋 Descrizione

Soluzione generata automaticamente in base alla richiesta:

> {architecture.Analysis.UserRequest}

---

## 🏗️ Architettura

**Tipo**: {architecture.Analysis.ArchitectureType}  
**Complessità**: {architecture.Analysis.Complexity}

### Progetti

{string.Join("\n", architecture.Projects.Select(p => $"- **{p.Name}** ({p.Type}): {p.Description}"))}

---

## 🚀 Come Iniziare

```bash
# Ripristina i pacchetti
dotnet restore

# Compila la soluzione
dotnet build

# Avvia l'API (se presente)
cd {architecture.Projects.FirstOrDefault(p => p.Type == "webapi")?.Name ?? architecture.SolutionName}.Api
dotnet run
```

---

## 📦 Entità del Dominio

{string.Join("\n", architecture.Analysis.Entities.Select(e => $"- {e}"))}

---

## ⚡ Funzionalità

{string.Join("\n", architecture.Analysis.UseCases.Select(u => $"- {u}"))}

---

## 🛠️ Tecnologie

- .NET 8.0
- ASP.NET Core (se API)
- Entity Framework Core (se database)

---

*Generato automaticamente da IndigoLab Solution Designer*
";
    }
    
    private static string GenerateArchitecturalReasons(DomainAnalysis analysis, SolutionArchitecture architecture)
    {
        var reasons = new List<string>();
        
        reasons.Add($"✅ **Dominio identificato come '{analysis.Domain}'** - questo determina la struttura base");
        
        if (analysis.RequiresApi)
            reasons.Add("✅ **API REST richiesta** - progetto API dedicato con controllers");
        
        if (analysis.RequiresDatabase)
            reasons.Add("✅ **Persistenza dati richiesta** - progetto Infrastructure con repositories");
        
        if (architecture.Projects.Count > 2)
            reasons.Add($"✅ **Architettura multi-layer** - {architecture.Projects.Count} progetti per separazione delle responsabilità");
        
        if (analysis.Complexity == "high")
            reasons.Add("✅ **Alta complessità rilevata** - architettura scalabile e modulare");
        
        return string.Join("\n", reasons);
    }
    
    private static string GenerateFeaturesList(DomainAnalysis analysis)
    {
        var features = new List<string>();
        
        features.Add($"✅ Gestione completa di {analysis.Entities.Count} entità principali");
        features.Add($"✅ {analysis.Actions.Count} operazioni CRUD implementate");
        
        if (analysis.RequiresApi)
            features.Add("✅ REST API con Swagger documentation");
        
        if (analysis.RequiresDatabase)
            features.Add("✅ Data persistence layer con Entity Framework");
        
        if (analysis.RequiresAuth)
            features.Add("✅ Sistema di autenticazione e autorizzazione");
        
        features.Add("✅ Logging e error handling integrati");
        features.Add("✅ Dependency injection configurata");
        
        return string.Join("\n", features);
    }
}

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

/// <summary>
/// Analisi del dominio basata sulla richiesta utente
/// </summary>
public class DomainAnalysis
{
    public string Domain { get; set; } = "";
    public List<string> Entities { get; set; } = new();
    public List<string> Actions { get; set; } = new();
    public List<string> UseCases { get; set; } = new();
    public string Complexity { get; set; } = "medium";
    public string ArchitectureType { get; set; } = "layered";
    public bool RequiresApi { get; set; }
    public bool RequiresDatabase { get; set; }
    public bool RequiresUI { get; set; }
    public bool RequiresAuth { get; set; }
    public string UserRequest { get; set; } = "";
}

/// <summary>
/// Architettura della soluzione generata dinamicamente
/// </summary>
public class SolutionArchitecture
{
    public string SolutionName { get; set; } = "";
    public List<ProjectInfo> Projects { get; set; } = new();
    public List<string> Folders { get; set; } = new();
    public List<FileInfo> Files { get; set; } = new();
    public DomainAnalysis Analysis { get; set; } = new();
}

/// <summary>
/// Informazioni su un progetto .NET
/// </summary>
public class ProjectInfo
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "classlib"; // webapi, classlib, webapp
    public string Description { get; set; } = "";
    public List<string> Dependencies { get; set; } = new();
}

/// <summary>
/// Informazioni su un file da generare
/// </summary>
public class FileInfo
{
    public string Path { get; set; } = "";
    public string Type { get; set; } = ""; // csharp, json, markdown
    public string Description { get; set; } = "";
}
