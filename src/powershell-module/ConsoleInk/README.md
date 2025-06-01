# ConsoleInk.PowerShell

A PowerShell module that wraps the ConsoleInk.Net .NET library for rich console output.

## Prerequisites
- PowerShell 7+ (for .NET Core compatibility)
- Internet access (if fetching DLL from NuGet)

## Installation

### 1. Fetch the ConsoleInk.Net DLL

You can fetch the latest DLL from NuGet using the provided script or manually:

```powershell
# Run this in the module root to fetch the DLL
dotnet add package ConsoleInk.Net --package-directory ./lib
# Or manually download and extract ConsoleInk.Net.dll to ./lib/
```

### 2. Import the Module

```powershell
Import-Module ./ConsoleInk.PowerShell.psd1
```

## Usage

```powershell
Invoke-MarkdownConsoleWrite -Markdown '# Hello from ConsoleInk!'
```

## Developer Workflow

To keep the PowerShell module's DLL up to date with the latest build of ConsoleInk.Net, use the provided build project:

```powershell
# From the repo root or src/powershell-module/
dotnet build src/powershell-module/ConsoleInk.PowerShell.Build.csproj
```

This will:
- Build ConsoleInk.Net (if needed)
- Copy the latest ConsoleInk.Net.dll and XML docs into `src/powershell-module/lib/`

You can also build the entire solution, which will update the PowerShell module automatically:

```powershell
dotnet build ConsoleInk.sln
```

## Publishing
- Update the manifest and package the module as per PowerShell Gallery requirements.
- Publish using `Publish-Module`.
