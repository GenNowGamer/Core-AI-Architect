@{
    RootModule        = 'CoreAiArchitect.PowerShell.dll'
    ModuleVersion     = '1.0.0'
    GUID              = 'a7c3e9d1-4b2f-4e8a-9c1d-6f0e2a8b5d73'
    Author            = 'Coresynth Studios'
    CompanyName       = 'Coresynth Studios'
    Copyright         = '(c) Coresynth Studios. All rights reserved.'
    Description       = 'PowerShell cmdlets for Core AI Architect SSOT documents.'
    PowerShellVersion = '7.0'
    FunctionsToExport = @()
    CmdletsToExport   = @(
        'Get-SsotFormProfile',
        'New-SsotDocument',
        'Test-SsotDocument',
        'Export-SsotMarkdown',
        'ConvertTo-SsotJson'
    )
    VariablesToExport = @()
    AliasesToExport   = @()
}
