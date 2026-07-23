using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CoreAiArchitect.App.ViewModels.Fields;
using CoreAiArchitect.Core.Models.FormSchema;
using CoreAiArchitect.Core.Models.Ssot;
using CoreAiArchitect.Core.SchemaEngine;

namespace CoreAiArchitect.App.ViewModels;

public partial class SectionViewModel : ObservableObject
{
    private readonly SchemaInterpreter _interpreter = new();

    public SectionViewModel(
        SectionDefinition definition,
        SsotDocument document,
        Action onChanged,
        ICommand? finalizeSsotCommand = null)
    {
        Definition = definition;
        Document = document;
        OnChanged = onChanged;
        Id = definition.Id;
        Label = definition.Label;
        Editor = definition.Editor;
        FinalizeSsotCommand = finalizeSsotCommand;
        RefreshFields();
    }

    public SectionDefinition Definition { get; }
    public SsotDocument Document { get; }
    private Action OnChanged { get; }
    public string Id { get; }
    public string Label { get; }
    public string Editor { get; }
    public string HelpKey => $"section.{Id}";
    public ICommand? FinalizeSsotCommand { get; }
    public bool ShowFinalizeButton => FinalizeSsotCommand is not null;

    public ObservableCollection<FieldViewModelBase> Fields { get; } = new();

    public void RefreshFields()
    {
        // Builder sections are edited by dedicated VMs that write live to the document.
        // Keeping field VMs here would stale-overwrite those edits on Commit.
        if (IsBuilderEditor(Editor))
        {
            Fields.Clear();
            return;
        }

        Commit();
        Fields.Clear();
        foreach (var field in _interpreter.GetVisibleFields(Definition, Document))
        {
            var vm = FieldViewModelFactory.Create(field, Document);
            vm.PropertyChanged += (_, _) =>
            {
                vm.Commit();
                OnChanged();
            };
            Fields.Add(vm);
        }
    }

    public void Commit()
    {
        if (IsBuilderEditor(Editor)) return;
        foreach (var field in Fields)
            field.Commit();
    }

    private static bool IsBuilderEditor(string editor) =>
        editor.Equals("workflowbuilder", StringComparison.OrdinalIgnoreCase)
        || editor.Equals("uibuilder", StringComparison.OrdinalIgnoreCase);
}
