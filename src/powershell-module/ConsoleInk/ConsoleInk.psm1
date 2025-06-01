$ErrorActionPreference = 'Stop'

# Path to DLL (relative to module root)
$dllPath = Join-Path $PSScriptRoot 'lib/ConsoleInk.Net.dll'
if (!(Test-Path $dllPath)) {
    Write-Error "ConsoleInk.Net.dll not found in 'lib/'. Please run the provided fetch script or see README.md."
    return
}

# Print loaded assemblies

# Try to load the DLL if not already loaded from the expected path
$alreadyLoaded = [AppDomain]::CurrentDomain.GetAssemblies() | Where-Object { $_.Location -eq $dllPath }
if (-not $alreadyLoaded) {
    try {
                Add-Type -Path $dllPath -ErrorAction Stop
            } catch {
        Write-Error "[ConsoleInk] Add-Type FAILED: $_"
        throw
    }
} else {
    }


function ConvertTo-Markdown {
    [CmdletBinding(DefaultParameterSetName='Text')]
    param(
        [Parameter(Position=0, ValueFromPipeline=$true, ValueFromPipelineByPropertyName=$true, ParameterSetName='Text')]
        [string[]]$MarkdownText,
        [Parameter(Position=0, Mandatory=$true, ParameterSetName='Path')]
        [string]$Path,
        [Parameter()]
        [int]$Width = 0,
        [Parameter()]
        [switch]$NoColor,
        [Parameter()]
        [ValidateSet('Default','Monochrome')]
        [string]$Theme = 'Default'
    )
    begin {
        $renderOptions = [ConsoleInk.MarkdownRenderOptions]::new()
        if ($Width -gt 0) { $renderOptions.ConsoleWidth = $Width }
        $renderOptions.EnableColors = -not $NoColor.IsPresent
        $renderOptions.Theme = if ($Theme -eq 'Monochrome') { [ConsoleInk.ConsoleTheme]::Monochrome } else { [ConsoleInk.ConsoleTheme]::Default }
        $outputWriter = [System.Console]::Out
        $pipelineBuffer = [System.Text.StringBuilder]::new()
    }
    process {
        if ($PSCmdlet.ParameterSetName -eq 'Path') {
            # File input, defer to end block
        } else {
            foreach ($line in $MarkdownText) {
                $pipelineBuffer.AppendLine($line) | Out-Null
            }
        }
    }
    end {
        try {
            if ($PSCmdlet.ParameterSetName -eq 'Path') {
                if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
                    Throw "File not found: $Path"
                }
                $reader = [System.IO.StreamReader]::new($Path)
                [ConsoleInk.MarkdownConsole]::Render($reader, $outputWriter, $renderOptions)
                $reader.Dispose()
            } elseif ($pipelineBuffer.Length -gt 0) {
                [ConsoleInk.MarkdownConsole]::Render($pipelineBuffer.ToString(), $outputWriter, $renderOptions)
            }
        } catch {
            Write-Error $_
        }
    }
}

function Show-Markdown {
    [CmdletBinding(DefaultParameterSetName='Text')]
    param(
        [Parameter(Position=0, ValueFromPipeline=$true, ValueFromPipelineByPropertyName=$true, ParameterSetName='Text')]
        [string[]]$MarkdownText,
        [Parameter(Position=0, Mandatory=$true, ParameterSetName='Path')]
        [string]$Path,
        [Parameter()]
        [int]$Width = 0,
        [Parameter()]
        [switch]$NoColor,
        [Parameter()]
        [ValidateSet('Default','Monochrome')]
        [string]$Theme = 'Default'
    )
    process {
        if ($PSCmdlet.ParameterSetName -eq 'Path') {
            ConvertTo-Markdown -Path $Path -Width $Width -NoColor:$NoColor.IsPresent -Theme $Theme
        } else {
            ConvertTo-Markdown -MarkdownText $MarkdownText -Width $Width -NoColor:$NoColor.IsPresent -Theme $Theme
        }
    }
}

Export-ModuleMember -Function ConvertTo-Markdown, Show-Markdown
