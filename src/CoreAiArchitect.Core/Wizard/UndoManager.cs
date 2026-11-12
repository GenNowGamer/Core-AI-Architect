using System.Text.Json;
using CoreAiArchitect.Core.Json;
using CoreAiArchitect.Core.Models.Ssot;

namespace CoreAiArchitect.Core.Wizard;

public sealed class UndoState
{
    public string Description { get; init; } = string.Empty;
    public WizardStep Step { get; init; }
    public string AnswersJson { get; init; } = string.Empty;
    public string DocumentJson { get; init; } = string.Empty;
}

public sealed class UndoManager
{
    private readonly Stack<UndoState> _undoStack = new();
    private readonly Stack<UndoState> _redoStack = new();
    private readonly JsonSerializerOptions _options = SsotJsonOptions.Create();
    private bool _isPerformingUndoRedo;

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;
    public int UndoCount => _undoStack.Count;
    public int RedoCount => _redoStack.Count;

    public event EventHandler? StateChanged;

    public void RecordSnapshot(string description, WizardStep step, WizardAnswers answers, SsotDocument? document)
    {
        if (_isPerformingUndoRedo) return;

        var snapshot = new UndoState
        {
            Description = description,
            Step = step,
            AnswersJson = JsonSerializer.Serialize(answers, _options),
            DocumentJson = document != null ? JsonSerializer.Serialize(document, _options) : string.Empty
        };

        _undoStack.Push(snapshot);
        _redoStack.Clear();

        // Limit stack depth to 50
        if (_undoStack.Count > 50)
        {
            var items = _undoStack.ToArray();
            _undoStack.Clear();
            for (var i = items.Length - 2; i >= 0; i--)
            {
                _undoStack.Push(items[i]);
            }
        }

        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public UndoState? Undo(WizardStep currentStep, WizardAnswers currentAnswers, SsotDocument? currentDoc)
    {
        if (!CanUndo) return null;

        _isPerformingUndoRedo = true;
        try
        {
            var currentState = new UndoState
            {
                Description = "Current",
                Step = currentStep,
                AnswersJson = JsonSerializer.Serialize(currentAnswers, _options),
                DocumentJson = currentDoc != null ? JsonSerializer.Serialize(currentDoc, _options) : string.Empty
            };
            _redoStack.Push(currentState);

            var previousState = _undoStack.Pop();
            StateChanged?.Invoke(this, EventArgs.Empty);
            return previousState;
        }
        finally
        {
            _isPerformingUndoRedo = false;
        }
    }

    public UndoState? Redo(WizardStep currentStep, WizardAnswers currentAnswers, SsotDocument? currentDoc)
    {
        if (!CanRedo) return null;

        _isPerformingUndoRedo = true;
        try
        {
            var currentState = new UndoState
            {
                Description = "Current",
                Step = currentStep,
                AnswersJson = JsonSerializer.Serialize(currentAnswers, _options),
                DocumentJson = currentDoc != null ? JsonSerializer.Serialize(currentDoc, _options) : string.Empty
            };
            _undoStack.Push(currentState);

            var nextState = _redoStack.Pop();
            StateChanged?.Invoke(this, EventArgs.Empty);
            return nextState;
        }
        finally
        {
            _isPerformingUndoRedo = false;
        }
    }

    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}

