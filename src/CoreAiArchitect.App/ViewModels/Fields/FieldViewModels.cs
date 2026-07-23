using System.Collections;
using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreAiArchitect.App.Help;
using CoreAiArchitect.Core.Models.FormSchema;
using CoreAiArchitect.Core.Models.Ssot;
using CoreAiArchitect.Core.SchemaEngine;

namespace CoreAiArchitect.App.ViewModels.Fields;

public abstract partial class FieldViewModelBase : ObservableObject
{
    protected FieldViewModelBase(FieldDefinition definition, object target, string? helpPathPrefix = null)
    {
        Definition = definition;
        Target = target;
        Id = definition.Id;
        Label = definition.Label + (definition.Required ? " *" : string.Empty);
        Path = definition.Path;
        Type = definition.Type;
        HelpKey = string.IsNullOrWhiteSpace(helpPathPrefix)
            ? definition.Path
            : $"{helpPathPrefix.Trim().TrimEnd('.')}.{definition.Path}";
        HelpSummary = HelpCatalog.GetSummary(HelpKey);
    }

    public FieldDefinition Definition { get; }
    protected object Target { get; }
    public string Id { get; }
    public string Label { get; }
    public string Path { get; }
    public string Type { get; }
    public string HelpKey { get; }
    public string HelpSummary { get; }

    public abstract void Commit();

    [RelayCommand]
    private void ShowHelp() => HelpBroker.Show(HelpKey);
}

public partial class StringFieldViewModel : FieldViewModelBase
{
    [ObservableProperty] private string _value = string.Empty;

    public StringFieldViewModel(FieldDefinition definition, object target, string? helpPathPrefix = null)
        : base(definition, target, helpPathPrefix)
    {
        Value = Convert.ToString(PathBinder.GetValue(target, definition.Path), CultureInfo.InvariantCulture) ?? string.Empty;
    }

    public override void Commit() => PathBinder.SetValue(Target, Path, Value);
}

public partial class TextFieldViewModel : StringFieldViewModel
{
    public TextFieldViewModel(FieldDefinition definition, object target, string? helpPathPrefix = null)
        : base(definition, target, helpPathPrefix) { }
}

public partial class EnumFieldViewModel : FieldViewModelBase
{
    [ObservableProperty] private string? _selectedValue;

    public ObservableCollection<string> Values { get; }

    public EnumFieldViewModel(FieldDefinition definition, object target, string? helpPathPrefix = null)
        : base(definition, target, helpPathPrefix)
    {
        Values = new ObservableCollection<string>(definition.Values ?? new List<string>());
        var existing = Convert.ToString(PathBinder.GetValue(target, definition.Path), CultureInfo.InvariantCulture);
        SelectedValue = string.IsNullOrWhiteSpace(existing) ? null : existing;
    }

    public override void Commit() => PathBinder.SetValue(Target, Path, SelectedValue ?? string.Empty);
}

public partial class BooleanFieldViewModel : FieldViewModelBase
{
    [ObservableProperty] private bool _value;

    public BooleanFieldViewModel(FieldDefinition definition, object target, string? helpPathPrefix = null)
        : base(definition, target, helpPathPrefix)
    {
        var raw = PathBinder.GetValue(target, definition.Path);
        Value = raw is bool b && b;
    }

    public override void Commit() => PathBinder.SetValue(Target, Path, Value);
}

public partial class NumberFieldViewModel : FieldViewModelBase
{
    [ObservableProperty] private string _valueText = "0";

    public NumberFieldViewModel(FieldDefinition definition, object target, string? helpPathPrefix = null)
        : base(definition, target, helpPathPrefix)
    {
        var raw = PathBinder.GetValue(target, definition.Path);
        ValueText = Convert.ToString(raw ?? 0, CultureInfo.InvariantCulture) ?? "0";
    }

    public override void Commit()
    {
        if (!double.TryParse(ValueText, NumberStyles.Any, CultureInfo.InvariantCulture, out var number))
            number = 0;
        PathBinder.SetValue(Target, Path, number);
    }
}

public partial class StringListFieldViewModel : FieldViewModelBase
{
    [ObservableProperty] private string _draftItem = string.Empty;

    public ObservableCollection<string> Items { get; } = new();

    public StringListFieldViewModel(FieldDefinition definition, object target, string? helpPathPrefix = null)
        : base(definition, target, helpPathPrefix)
    {
        if (PathBinder.GetValue(target, definition.Path) is IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                var text = Convert.ToString(item, CultureInfo.InvariantCulture);
                if (!string.IsNullOrWhiteSpace(text))
                    Items.Add(text);
            }
        }
    }

    [RelayCommand]
    private void AddItem()
    {
        if (string.IsNullOrWhiteSpace(DraftItem)) return;
        Items.Add(DraftItem.Trim());
        DraftItem = string.Empty;
        Commit();
        OnPropertyChanged(nameof(Items));
    }

    [RelayCommand]
    private void RemoveItem(string? item)
    {
        if (item is null) return;
        Items.Remove(item);
        Commit();
        OnPropertyChanged(nameof(Items));
    }

    public override void Commit() => PathBinder.SetValue(Target, Path, Items.ToList());
}

public partial class ObjectListItemViewModel : ObservableObject
{
    public ObjectListItemViewModel(
        object item,
        IEnumerable<FieldDefinition> itemSchema,
        Action onItemEdited,
        string helpPathPrefix)
    {
        Item = item;
        Fields = new ObservableCollection<FieldViewModelBase>(
            FieldViewModelFactory.CreateMany(itemSchema, item, onItemEdited, helpPathPrefix));
    }

    public object Item { get; }
    public ObservableCollection<FieldViewModelBase> Fields { get; }

    public void CommitFields()
    {
        foreach (var field in Fields)
            field.Commit();
    }
}

public partial class ObjectListFieldViewModel : FieldViewModelBase
{
    private readonly Type _itemType;
    private bool _isCommitting;

    public ObservableCollection<ObjectListItemViewModel> Items { get; } = new();

    public ObjectListFieldViewModel(
        FieldDefinition definition,
        object target,
        Type itemType,
        string? helpPathPrefix = null)
        : base(definition, target, helpPathPrefix)
    {
        _itemType = itemType;
        Reload();
    }

    private void Reload()
    {
        Items.Clear();
        if (PathBinder.GetValue(Target, Path) is not IList list) return;
        foreach (var item in list)
        {
            if (item is null) continue;
            Items.Add(CreateItemViewModel(item));
        }
    }

    private ObjectListItemViewModel CreateItemViewModel(object item) =>
        new(item, Definition.ItemSchema ?? new List<FieldDefinition>(), OnNestedFieldEdited, HelpKey);

    private void OnNestedFieldEdited()
    {
        if (_isCommitting) return;
        Commit();
        OnPropertyChanged(nameof(Items));
    }

    [RelayCommand]
    private void AddItem()
    {
        var list = EnsureList();
        var item = CreateItemInstance();
        list.Add(item);
        Items.Add(CreateItemViewModel(item));
        Commit();
        OnPropertyChanged(nameof(Items));
    }

    [RelayCommand]
    private void RemoveItem(ObjectListItemViewModel? itemVm)
    {
        if (itemVm is null) return;
        var list = EnsureList();
        list.Remove(itemVm.Item);
        Items.Remove(itemVm);
        Commit();
        OnPropertyChanged(nameof(Items));
    }

    public override void Commit()
    {
        if (_isCommitting) return;
        _isCommitting = true;
        try
        {
            foreach (var item in Items)
                item.CommitFields();
            EnsureList();
        }
        finally
        {
            _isCommitting = false;
        }
    }

    private object CreateItemInstance()
    {
        if (_itemType == typeof(Dictionary<string, object>))
            return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        return Activator.CreateInstance(_itemType)
               ?? throw new InvalidOperationException($"Cannot create {_itemType.Name}.");
    }

    private IList EnsureList()
    {
        var existing = PathBinder.GetValue(Target, Path);
        if (existing is IList list) return list;

        var listType = typeof(List<>).MakeGenericType(_itemType);
        list = (IList)(Activator.CreateInstance(listType)
                       ?? throw new InvalidOperationException("Cannot create list."));
        PathBinder.SetValue(Target, Path, list);
        return list;
    }
}

public static class FieldViewModelFactory
{
    public static IEnumerable<FieldViewModelBase> CreateMany(
        IEnumerable<FieldDefinition> fields,
        object target,
        Action? onChanged = null,
        string? helpPathPrefix = null)
    {
        foreach (var field in fields)
        {
            var vm = Create(field, target, helpPathPrefix);
            if (onChanged is not null)
            {
                vm.PropertyChanged += (_, args) =>
                {
                    if (args.PropertyName is null) return;
                    vm.Commit();
                    onChanged();
                };
            }

            yield return vm;
        }
    }

    public static FieldViewModelBase Create(FieldDefinition field, object target, string? helpPathPrefix = null)
    {
        return field.Type.ToLowerInvariant() switch
        {
            "text" => new TextFieldViewModel(field, target, helpPathPrefix),
            "enum" => new EnumFieldViewModel(field, target, helpPathPrefix),
            "boolean" => new BooleanFieldViewModel(field, target, helpPathPrefix),
            "number" => new NumberFieldViewModel(field, target, helpPathPrefix),
            "stringlist" => new StringListFieldViewModel(field, target, helpPathPrefix),
            "objectlist" => new ObjectListFieldViewModel(
                field,
                target,
                ResolveItemType(string.IsNullOrWhiteSpace(helpPathPrefix) ? field.Path : $"{helpPathPrefix}.{field.Path}"),
                helpPathPrefix),
            _ => new StringFieldViewModel(field, target, helpPathPrefix)
        };
    }

    private static Type ResolveItemType(string path) =>
        path.ToLowerInvariant() switch
        {
            "architecture.modules" => typeof(ModuleInfo),
            "ui.screens" => typeof(ScreenInfo),
            "controls" => typeof(ControlInfo),
            "ui.screens.controls" => typeof(ControlInfo),
            "domain.entities" => typeof(EntityInfo),
            "fields" => typeof(EntityFieldInfo),
            "domain.entities.fields" => typeof(EntityFieldInfo),
            "workflows" => typeof(WorkflowInfo),
            "build.targets" => typeof(BuildTargetInfo),
            _ when path.EndsWith(".controls", StringComparison.OrdinalIgnoreCase) => typeof(ControlInfo),
            _ when path.EndsWith(".fields", StringComparison.OrdinalIgnoreCase) => typeof(EntityFieldInfo),
            _ => typeof(Dictionary<string, object>)
        };
}
