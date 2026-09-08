param()

function Get-CommonSections {
    param(
        [string[]]$ArchPatterns,
        [string[]]$UiThemes,
        [string[]]$NavPatterns,
        [string[]]$ControlTypes,
        [string[]]$StorageTypes,
        [string[]]$OutputTypes,
        [string[]]$Packaging,
        [string]$UiEditor = "uiBuilder",
        [string]$UiLabel = "UI",
        [string[]]$TargetOsValues = @("Windows 11", "Windows 10", "Windows Server 2022", "Cross-Platform"),
        [string[]]$RuntimeValues = @(".NET 8", ".NET 9", "PowerShell 7", "Native"),
        [string[]]$UiFrameworkValues = @("WPF", "WinUI", "None", "ASP.NET", "Console", "PowerShell"),
        [string[]]$LanguageValues = @("C#", "PowerShell", "F#"),
        [string[]]$RuntimeIdentifiers = @("win-x64", "win-x86", "win-arm64", "portable"),
        [string[]]$UnitTestFrameworks = @("xUnit", "NUnit", "MSTest", "Pester", "None"),
        [string[]]$UiTestFrameworks = @("None", "FlaUI", "WinAppDriver", "Playwright")
    )

    function Convert-StringArray([string[]]$Items) {
        $escaped = $Items | ForEach-Object { '"' + ($_ -replace '\\', '\\' -replace '"', '\"') + '"' }
        return '[' + ($escaped -join ',') + ']'
    }

    $archJson = Convert-StringArray $ArchPatterns
    $themesJson = Convert-StringArray $UiThemes
    $navJson = Convert-StringArray $NavPatterns
    $controlsJson = Convert-StringArray $ControlTypes
    $storageJson = Convert-StringArray $StorageTypes
    $outputJson = Convert-StringArray $OutputTypes
    $packJson = Convert-StringArray $Packaging
    $targetOsJson = Convert-StringArray $TargetOsValues
    $runtimeJson = Convert-StringArray $RuntimeValues
    $uiFrameworkJson = Convert-StringArray $UiFrameworkValues
    $languageJson = Convert-StringArray $LanguageValues
    $ridJson = Convert-StringArray $RuntimeIdentifiers
    $unitTestJson = Convert-StringArray $UnitTestFrameworks
    $uiTestJson = Convert-StringArray $UiTestFrameworks

    return @"
    {
      "id": "project",
      "label": "Project",
      "editor": "default",
      "fields": [
        { "id": "projectId", "label": "Project Id", "path": "project.id", "type": "string", "required": true },
        { "id": "projectName", "label": "Project Name", "path": "project.name", "type": "string", "required": true },
        { "id": "description", "label": "Description", "path": "project.description", "type": "text", "required": true },
        { "id": "owner", "label": "Owner", "path": "project.owner", "type": "string", "required": false },
        { "id": "tags", "label": "Tags", "path": "project.tags", "type": "stringList", "required": false }
      ]
    },
    {
      "id": "environment",
      "label": "Environment",
      "editor": "default",
      "fields": [
        { "id": "targetOs", "label": "Target OS", "path": "environment.targetOs", "type": "enum", "required": true, "values": $targetOsJson },
        { "id": "runtime", "label": "Runtime", "path": "environment.runtime", "type": "enum", "required": true, "values": $runtimeJson },
        { "id": "uiFramework", "label": "UI Framework", "path": "environment.uiFramework", "type": "enum", "required": true, "values": $uiFrameworkJson },
        { "id": "language", "label": "Language", "path": "environment.language", "type": "enum", "required": true, "values": $languageJson }
      ]
    },
    {
      "id": "architecture",
      "label": "Architecture",
      "editor": "default",
      "fields": [
        { "id": "pattern", "label": "Pattern", "path": "architecture.pattern", "type": "enum", "required": true, "values": $archJson },
        { "id": "layers", "label": "Layers", "path": "architecture.layers", "type": "stringList", "required": false },
        {
          "id": "modules",
          "label": "Modules",
          "path": "architecture.modules",
          "type": "objectList",
          "required": false,
          "itemSchema": [
            { "id": "name", "label": "Name", "path": "name", "type": "string", "required": true },
            { "id": "description", "label": "Description", "path": "description", "type": "text", "required": false },
            { "id": "dependencies", "label": "Dependencies", "path": "dependencies", "type": "stringList", "required": false }
          ]
        }
      ]
    },
    {
      "id": "ui",
      "label": "$UiLabel",
      "editor": "$UiEditor",
      "fields": [
        { "id": "theme", "label": "Theme", "path": "ui.theme", "type": "enum", "required": true, "values": $themesJson },
        { "id": "navigationPattern", "label": "Navigation Pattern", "path": "ui.navigationPattern", "type": "enum", "required": true, "values": $navJson },
        {
          "id": "screens",
          "label": "Screens / Surfaces",
          "path": "ui.screens",
          "type": "objectList",
          "required": false,
          "itemSchema": [
            { "id": "id", "label": "Id", "path": "id", "type": "string", "required": true },
            { "id": "title", "label": "Title", "path": "title", "type": "string", "required": true },
            { "id": "route", "label": "Route", "path": "route", "type": "string", "required": false },
            {
              "id": "controls",
              "label": "Controls",
              "path": "controls",
              "type": "objectList",
              "required": false,
              "itemSchema": [
                { "id": "id", "label": "Id", "path": "id", "type": "string", "required": true },
                { "id": "type", "label": "Type", "path": "type", "type": "enum", "required": true, "values": $controlsJson },
                { "id": "label", "label": "Label", "path": "label", "type": "string", "required": false },
                { "id": "action", "label": "Action", "path": "action", "type": "string", "required": false }
              ]
            }
          ]
        }
      ]
    },
    {
      "id": "domain",
      "label": "Domain",
      "editor": "default",
      "fields": [
        {
          "id": "entities",
          "label": "Entities",
          "path": "domain.entities",
          "type": "objectList",
          "required": false,
          "itemSchema": [
            { "id": "name", "label": "Name", "path": "name", "type": "string", "required": true },
            { "id": "description", "label": "Description", "path": "description", "type": "text", "required": false },
            {
              "id": "fields",
              "label": "Fields",
              "path": "fields",
              "type": "objectList",
              "required": false,
              "itemSchema": [
                { "id": "name", "label": "Name", "path": "name", "type": "string", "required": true },
                { "id": "type", "label": "Type", "path": "type", "type": "enum", "required": true, "values": ["string", "int", "bool", "decimal", "DateTime", "Guid"] },
                { "id": "required", "label": "Required", "path": "required", "type": "boolean", "required": false }
              ]
            }
          ]
        }
      ]
    },
    {
      "id": "workflows",
      "label": "Workflows",
      "editor": "workflowBuilder",
      "fields": [
        {
          "id": "workflows",
          "label": "Workflows",
          "path": "workflows",
          "type": "objectList",
          "required": false,
          "itemSchema": [
            { "id": "id", "label": "Id", "path": "id", "type": "string", "required": true },
            { "id": "name", "label": "Name", "path": "name", "type": "string", "required": true },
            { "id": "trigger", "label": "Trigger", "path": "trigger", "type": "string", "required": false },
            { "id": "steps", "label": "Steps", "path": "steps", "type": "stringList", "required": false },
            { "id": "errorStrategy", "label": "Error Strategy", "path": "errorHandling.strategy", "type": "enum", "required": false, "values": ["ShowMessageBox", "Throw", "LogAndContinue", "Retry"] },
            { "id": "logLevel", "label": "Log Level", "path": "errorHandling.logLevel", "type": "enum", "required": false, "values": ["Trace", "Debug", "Information", "Warning", "Error"] }
          ]
        }
      ]
    },
    {
      "id": "constraints",
      "label": "Constraints",
      "editor": "default",
      "fields": [
        { "id": "allowedLibraries", "label": "Allowed Libraries", "path": "constraints.allowedLibraries", "type": "stringList", "required": false },
        { "id": "forbiddenLibraries", "label": "Forbidden Libraries", "path": "constraints.forbiddenLibraries", "type": "stringList", "required": false },
        { "id": "loggingRequired", "label": "Logging Required", "path": "constraints.loggingRequired", "type": "boolean", "required": true },
        { "id": "telemetryAllowed", "label": "Telemetry Allowed", "path": "constraints.telemetryAllowed", "type": "boolean", "required": true },
        { "id": "thirdPartyBootloadersAllowed", "label": "Third-Party Bootloaders Allowed", "path": "constraints.thirdPartyBootloadersAllowed", "type": "boolean", "required": true }
      ]
    },
    {
      "id": "data",
      "label": "Data",
      "editor": "default",
      "fields": [
        { "id": "storageType", "label": "Storage Type", "path": "data.storage.type", "type": "enum", "required": true, "values": $storageJson },
        { "id": "connectionStringMode", "label": "Connection String Mode", "path": "data.storage.connectionStringMode", "type": "enum", "required": true, "values": ["None", "ConfigFile", "EnvironmentVariable", "UserSecrets"] },
        { "id": "migrationsRequired", "label": "Migrations Required", "path": "data.storage.migrationsRequired", "type": "boolean", "required": true }
      ]
    },
    {
      "id": "build",
      "label": "Build",
      "editor": "default",
      "fields": [
        { "id": "outputType", "label": "Output Type", "path": "build.outputType", "type": "enum", "required": true, "values": $outputJson },
        { "id": "configuration", "label": "Configurations", "path": "build.configuration", "type": "stringList", "required": true },
        {
          "id": "targets",
          "label": "Targets",
          "path": "build.targets",
          "type": "objectList",
          "required": false,
          "itemSchema": [
            { "id": "name", "label": "Name", "path": "name", "type": "string", "required": true },
            { "id": "runtimeIdentifier", "label": "Runtime Identifier", "path": "runtimeIdentifier", "type": "enum", "required": true, "values": $ridJson }
          ]
        }
      ]
    },
    {
      "id": "deployment",
      "label": "Deployment",
      "editor": "default",
      "fields": [
        { "id": "packaging", "label": "Packaging", "path": "deployment.packaging", "type": "enum", "required": true, "values": $packJson },
        { "id": "autoUpdate", "label": "Auto Update", "path": "deployment.autoUpdate", "type": "boolean", "required": true }
      ]
    },
    {
      "id": "testing",
      "label": "Testing",
      "editor": "default",
      "fields": [
        { "id": "unitTestFramework", "label": "Unit Test Framework", "path": "testing.unitTestFramework", "type": "enum", "required": true, "values": $unitTestJson },
        { "id": "uiTestFramework", "label": "UI Test Framework", "path": "testing.uiTestFramework", "type": "enum", "required": true, "values": $uiTestJson },
        { "id": "coverageTarget", "label": "Coverage Target", "path": "testing.coverageTarget", "type": "number", "required": true }
      ]
    },
    {
      "id": "documentation",
      "label": "Documentation",
      "editor": "default",
      "fields": [
        { "id": "generateReadme", "label": "Generate README", "path": "documentation.generateReadme", "type": "boolean", "required": true },
        { "id": "generateArchitectureDoc", "label": "Generate Architecture Doc", "path": "documentation.generateArchitectureDoc", "type": "boolean", "required": true },
        { "id": "sections", "label": "Sections", "path": "documentation.sections", "type": "stringList", "required": false }
      ]
    }
"@
}

$root = Split-Path -Parent $PSScriptRoot
if (-not (Test-Path (Join-Path $root "schemas"))) {
    $root = Get-Location
}

$schemasDir = Join-Path $root "schemas"
New-Item -ItemType Directory -Force -Path $schemasDir | Out-Null

$profiles = @(
    @{
        id = "wpf_desktop"; label = "WPF Desktop App"
        env = @{ targetOs = "Windows 11"; runtime = ".NET 8"; uiFramework = "WPF"; language = "C#" }
        arch = @("MVVM", "MVC", "CleanArchitecture"); themes = @("Default", "Fluent", "Custom"); nav = @("Window+Dialogs", "NavigationFrame", "Tabbed")
        controls = @("Button", "TextBox", "DataGrid", "ListView", "MenuItem", "ComboBox", "CheckBox"); storage = @("None", "SQLite", "SQLServer", "LocalJson")
        output = @("WinExe", "Exe"); packaging = @("None", "MSIX", "Zip"); uiEditor = "uiBuilder"; uiLabel = "UI"
    },
    @{
        id = "android_app"; label = "Android App"
        env = @{ targetOs = "Android"; runtime = "Android Runtime"; uiFramework = "Jetpack Compose"; language = "Kotlin" }
        targetOsValues = @("Android")
        runtimeValues = @("Android Runtime", ".NET 8", ".NET 9")
        uiFrameworkValues = @("Jetpack Compose", "Android Views", ".NET MAUI")
        languageValues = @("Kotlin", "Java", "C#")
        arch = @("MVVM", "CleanArchitecture", "MVI"); themes = @("Material3", "Material", "Custom"); nav = @("BottomNavigation", "NavigationCompose", "SingleActivity", "Tabbed")
        controls = @("Button", "TextField", "OutlinedTextField", "DropdownMenu", "LazyColumn", "RecyclerView", "CheckBox", "FloatingActionButton"); storage = @("None", "SQLite", "Room", "DataStore", "LocalJson")
        output = @("Aab", "Apk", "Library"); packaging = @("None", "AAB", "APK", "PlayStore")
        runtimeIdentifiers = @("android-arm64", "android-x64", "android-x86", "portable")
        unitTestFrameworks = @("JUnit", "xUnit", "None"); uiTestFrameworks = @("None", "Espresso", "ComposeUiTest")
        uiEditor = "uiBuilder"; uiLabel = "UI"
    },
    @{
        id = "winpe_tool"; label = "WinPE Tool"
        env = @{ targetOs = "Windows 11"; runtime = ".NET 8"; uiFramework = "None"; language = "C#" }
        arch = @("Modular", "Pipeline", "CleanArchitecture"); themes = @("Default", "HighContrast"); nav = @("Wizard", "CommandLine", "Menu")
        controls = @("Button", "TextBox", "ListView", "ProgressBar", "MenuItem"); storage = @("None", "LocalJson", "Registry")
        output = @("Exe", "WinExe"); packaging = @("None", "Zip", "WinPEMedia"); uiEditor = "uiBuilder"; uiLabel = "UI / Wizard"
    },
    @{
        id = "web_api"; label = "Web API"
        env = @{ targetOs = "Windows 11"; runtime = ".NET 8"; uiFramework = "ASP.NET"; language = "C#" }
        arch = @("CleanArchitecture", "VerticalSlice", "MinimalApi"); themes = @("Default"); nav = @("HttpRoutes")
        controls = @("Endpoint", "Middleware", "Controller"); storage = @("None", "SQLite", "SQLServer", "PostgreSQL")
        output = @("Exe"); packaging = @("None", "MSIX", "Docker", "Zip"); uiEditor = "uiBuilder"; uiLabel = "API Surfaces"
    },
    @{
        id = "cli_tool"; label = "CLI Tool"
        env = @{ targetOs = "Windows 11"; runtime = ".NET 8"; uiFramework = "Console"; language = "C#" }
        arch = @("CommandHandler", "Modular", "CleanArchitecture"); themes = @("Default"); nav = @("CommandLine")
        controls = @("Command", "Option", "Argument"); storage = @("None", "LocalJson", "SQLite")
        output = @("Exe"); packaging = @("None", "Zip", "DotnetTool"); uiEditor = "uiBuilder"; uiLabel = "Commands"
    },
    @{
        id = "powershell_module"; label = "PowerShell Module"
        env = @{ targetOs = "Windows 11"; runtime = "PowerShell 7"; uiFramework = "PowerShell"; language = "PowerShell" }
        arch = @("CmdletModule", "BinaryModule", "ScriptModule"); themes = @("Default"); nav = @("CmdletPipeline")
        controls = @("Cmdlet", "Parameter", "PipelineInput"); storage = @("None", "LocalJson")
        output = @("Library"); packaging = @("None", "Zip", "PSGallery"); uiEditor = "uiBuilder"; uiLabel = "Cmdlets"
    },
    @{
        id = "deployment_automation"; label = "Deployment Automation"
        env = @{ targetOs = "Windows 11"; runtime = ".NET 8"; uiFramework = "None"; language = "C#" }
        arch = @("Pipeline", "Orchestrator", "Modular"); themes = @("Default"); nav = @("Pipeline")
        controls = @("Stage", "Task", "Gate"); storage = @("None", "LocalJson", "SQLServer")
        output = @("Exe"); packaging = @("None", "Zip", "MSIX"); uiEditor = "uiBuilder"; uiLabel = "Pipeline Surfaces"
    },
    @{
        id = "custom"; label = "Custom"
        env = @{ targetOs = "Windows 11"; runtime = ".NET 8"; uiFramework = "WPF"; language = "C#" }
        targetOsValues = @("Windows 11", "Windows 10", "Windows Server 2022", "Android", "Cross-Platform")
        runtimeValues = @(".NET 8", ".NET 9", "PowerShell 7", "Android Runtime", "Native")
        uiFrameworkValues = @("WPF", "WinUI", "Jetpack Compose", "Android Views", ".NET MAUI", "None", "ASP.NET", "Console", "PowerShell")
        languageValues = @("C#", "PowerShell", "F#", "Kotlin", "Java")
        arch = @("MVVM", "MVC", "CleanArchitecture", "MVI", "Modular", "Custom"); themes = @("Default", "Fluent", "Material3", "Material", "Custom")
        nav = @("Window+Dialogs", "NavigationFrame", "Tabbed", "BottomNavigation", "NavigationCompose", "SingleActivity", "HttpRoutes", "CommandLine", "Custom")
        controls = @("Button", "TextBox", "TextField", "DataGrid", "ListView", "LazyColumn", "MenuItem", "ComboBox", "DropdownMenu", "CheckBox", "Endpoint", "Command", "Cmdlet", "Custom")
        storage = @("None", "SQLite", "SQLServer", "PostgreSQL", "Room", "DataStore", "LocalJson", "Registry")
        output = @("WinExe", "Exe", "Library", "Aab", "Apk"); packaging = @("None", "MSIX", "Zip", "Docker", "DotnetTool", "PSGallery", "WinPEMedia", "AAB", "APK", "PlayStore")
        runtimeIdentifiers = @("win-x64", "win-x86", "win-arm64", "android-arm64", "android-x64", "portable")
        unitTestFrameworks = @("xUnit", "NUnit", "MSTest", "Pester", "JUnit", "None")
        uiTestFrameworks = @("None", "FlaUI", "WinAppDriver", "Playwright", "Espresso", "ComposeUiTest")
        uiEditor = "uiBuilder"; uiLabel = "UI"
    }
)

foreach ($p in $profiles) {
    $sectionArgs = @{
        ArchPatterns = $p.arch
        UiThemes = $p.themes
        NavPatterns = $p.nav
        ControlTypes = $p.controls
        StorageTypes = $p.storage
        OutputTypes = $p.output
        Packaging = $p.packaging
        UiEditor = $p.uiEditor
        UiLabel = $p.uiLabel
    }
    if ($p.ContainsKey("targetOsValues")) { $sectionArgs.TargetOsValues = $p.targetOsValues }
    if ($p.ContainsKey("runtimeValues")) { $sectionArgs.RuntimeValues = $p.runtimeValues }
    if ($p.ContainsKey("uiFrameworkValues")) { $sectionArgs.UiFrameworkValues = $p.uiFrameworkValues }
    if ($p.ContainsKey("languageValues")) { $sectionArgs.LanguageValues = $p.languageValues }
    if ($p.ContainsKey("runtimeIdentifiers")) { $sectionArgs.RuntimeIdentifiers = $p.runtimeIdentifiers }
    if ($p.ContainsKey("unitTestFrameworks")) { $sectionArgs.UnitTestFrameworks = $p.unitTestFrameworks }
    if ($p.ContainsKey("uiTestFrameworks")) { $sectionArgs.UiTestFrameworks = $p.uiTestFrameworks }

    $sections = Get-CommonSections @sectionArgs

    $json = @"
{
  "id": "$($p.id)",
  "version": "1.0.0",
  "targetSsotVersion": "1.0.0",
  "label": "$($p.label)",
  "environmentDefaults": {
    "targetOs": "$($p.env.targetOs)",
    "runtime": "$($p.env.runtime)",
    "uiFramework": "$($p.env.uiFramework)",
    "language": "$($p.env.language)"
  },
  "sections": [
$sections
  ]
}
"@

    $out = Join-Path $schemasDir "$($p.id).json"
    # Avoid ConvertTo-Json collapsing single-element arrays on re-serialize; write raw UTF8 JSON.
    [System.IO.File]::WriteAllText($out, $json)
    # Validate parse
    $null = Get-Content -Raw $out | ConvertFrom-Json
    Write-Host "Wrote $out"
}

Get-ChildItem $schemasDir | Format-Table Name, Length
