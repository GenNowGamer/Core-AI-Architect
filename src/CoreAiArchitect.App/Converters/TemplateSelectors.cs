using System.Windows;
using System.Windows.Controls;
using CoreAiArchitect.App.ViewModels.Fields;

namespace CoreAiArchitect.App.Converters;

public sealed class FieldTemplateSelector : DataTemplateSelector
{
    public const string ObjectListTemplateKey = "ObjectListFieldTemplate";

    public DataTemplate? StringTemplate { get; set; }
    public DataTemplate? TextTemplate { get; set; }
    public DataTemplate? EnumTemplate { get; set; }
    public DataTemplate? BooleanTemplate { get; set; }
    public DataTemplate? NumberTemplate { get; set; }
    public DataTemplate? StringListTemplate { get; set; }
    public DataTemplate? ObjectListTemplate { get; set; }

    public override DataTemplate? SelectTemplate(object item, DependencyObject container) =>
        item switch
        {
            TextFieldViewModel => TextTemplate,
            StringFieldViewModel => StringTemplate,
            EnumFieldViewModel => EnumTemplate,
            BooleanFieldViewModel => BooleanTemplate,
            NumberFieldViewModel => NumberTemplate,
            StringListFieldViewModel => StringListTemplate,
            // Resolve recursively via resources so nested objectLists (Modules, Entity Fields, etc.)
            // do not need DynamicResource on CLR properties (not supported on DataTemplateSelector).
            ObjectListFieldViewModel => ObjectListTemplate ?? FindTemplate(container, ObjectListTemplateKey),
            _ => base.SelectTemplate(item, container)
        };

    private static DataTemplate? FindTemplate(DependencyObject container, string key)
    {
        if (container is FrameworkElement element)
        {
            var local = element.TryFindResource(key);
            if (local is DataTemplate template)
                return template;
        }

        return Application.Current?.TryFindResource(key) as DataTemplate;
    }
}

public sealed class EditorTemplateSelector : DataTemplateSelector
{
    public DataTemplate? SectionTemplate { get; set; }
    public DataTemplate? WorkflowTemplate { get; set; }
    public DataTemplate? UiTemplate { get; set; }

    public override DataTemplate? SelectTemplate(object item, DependencyObject container) =>
        item switch
        {
            ViewModels.WorkflowBuilderViewModel => WorkflowTemplate,
            ViewModels.UiLayoutBuilderViewModel => UiTemplate,
            ViewModels.SectionViewModel => SectionTemplate,
            _ => base.SelectTemplate(item, container)
        };
}
