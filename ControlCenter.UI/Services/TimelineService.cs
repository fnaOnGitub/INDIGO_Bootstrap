using System.Collections.ObjectModel;
using ControlCenter.UI.Models;

namespace ControlCenter.UI.Services;

/// <summary>
/// Servizio per gestire la timeline operativa del cluster
/// </summary>
public class TimelineService
{
    private readonly ObservableCollection<TimelineStep> _steps = new();
    private TimelineStep? _currentStep;

    public ObservableCollection<TimelineStep> Steps => _steps;
    public TimelineStep? CurrentStep => _currentStep;

    /// <summary>
    /// Aggiunge un nuovo step alla timeline
    /// </summary>
    public void AddStep(string title, string description, TimelineStepType type = TimelineStepType.Info)
    {
        // Completa lo step precedente
        if (_currentStep != null)
        {
            _currentStep.IsActive = false;
            _currentStep.IsCompleted = true;
        }

        // Crea nuovo step
        var icon = GetIconForType(type);
        var step = new TimelineStep
        {
            Title = title,
            Description = description,
            Type = type,
            Icon = icon,
            Timestamp = DateTime.Now,
            IsActive = true,
            IsCompleted = false
        };

        _steps.Add(step);
        _currentStep = step;
    }

    /// <summary>
    /// Aggiorna lo step corrente
    /// </summary>
    public void UpdateCurrentStep(string description)
    {
        if (_currentStep != null)
        {
            _currentStep.Description = description;
        }
    }

    /// <summary>
    /// Completa lo step corrente
    /// </summary>
    public void CompleteCurrentStep()
    {
        if (_currentStep != null)
        {
            _currentStep.IsActive = false;
            _currentStep.IsCompleted = true;
            _currentStep = null;
        }
    }

    /// <summary>
    /// Pulisce la timeline
    /// </summary>
    public void Clear()
    {
        _steps.Clear();
        _currentStep = null;
    }

    /// <summary>
    /// Ottiene l'icona appropriata per il tipo di step
    /// </summary>
    private string GetIconForType(TimelineStepType type)
    {
        return type switch
        {
            TimelineStepType.Input => "Edit",
            TimelineStepType.Routing => "DoubleChevronRight",
            TimelineStepType.Processing => "Globe",
            TimelineStepType.Output => "Document",
            TimelineStepType.Autonomous => "Sync",
            TimelineStepType.Dialog => "CommentSolid",
            TimelineStepType.Success => "CheckMark",
            TimelineStepType.Error => "Error",
            _ => "Info"
        };
    }
}
