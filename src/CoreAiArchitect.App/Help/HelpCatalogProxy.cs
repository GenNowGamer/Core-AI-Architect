namespace CoreAiArchitect.App.Help;

/// <summary>XAML-friendly static summaries for menu ToolTips.</summary>
public static class HelpCatalogProxy
{
    public static string FileNew => HelpCatalog.GetSummary("file.new");
    public static string FileOpen => HelpCatalog.GetSummary("file.open");
    public static string FileSave => HelpCatalog.GetSummary("file.save");
    public static string FileExportJson => HelpCatalog.GetSummary("file.exportJson");
    public static string FileExportMarkdown => HelpCatalog.GetSummary("file.exportMarkdown");
    public static string ToolsValidate => HelpCatalog.GetSummary("tools.validate");
    public static string ToolsCopyPowershell => HelpCatalog.GetSummary("tools.copyPowershell");
}
