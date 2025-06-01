@{
    # Script module or binary module file associated with this manifest.
    RootModule = 'ConsoleInk.psm1'

    # Version number of this module.
    ModuleVersion = '0.1.4'

    # ID used to uniquely identify this module
    GUID = 'b1c3e7b3-0a1f-4f7b-9f8a-000000000001'

    # Author of this module
    Author = 'Jon Davis'

    # Company or vendor of this module
    CompanyName = 'Jon Davis'

    # Copyright statement for this module
    Copyright = '(c) 2025 Jon Davis. All rights reserved.'

    # Description of the functionality provided by this module
    Description = 'PowerShell module for rendering Markdown in the console using ConsoleInk.Net. Supports pipeline, string, and file input, with theming and color options.'


    # Minimum version of the Windows PowerShell engine required by this module
    PowerShellVersion = '5.1'

    # Compatible PowerShell Editions
    CompatiblePSEditions = @('Desktop', 'Core')

    # Functions to export from this module
    FunctionsToExport = @('ConvertTo-Markdown', 'Show-Markdown')

    # Cmdlets to export from this module
    CmdletsToExport = @()

    # Variables to export from this module
    VariablesToExport = @()

    # Aliases to export from this module
    AliasesToExport = @()

    # PrivateData for gallery and additional metadata
    PrivateData = @{ 
        PSData = @{ 
            Tags = @('markdown','console','ansi','color','powershell','render','formatting','text','cli')
            ProjectUri = 'https://github.com/stimpy77/ConsoleInk.NET'
            LicenseUri = 'https://github.com/stimpy77/ConsoleInk.NET/blob/main/LICENSE'
            HelpInfoURI = 'https://github.com/stimpy77/ConsoleInk.NET#powershell-module'
        }
    }
}
