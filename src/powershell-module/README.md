# ConsoleInk PowerShell Module

This directory contains the PowerShell module wrapper for ConsoleInk.Net.

## Building the Module

The module requires the ConsoleInk.Net.dll to be present in the `ConsoleInk/lib/` directory. **This DLL is not committed to git** and must be built locally.

### Build Script

Use the provided build script from the repository root:

```powershell
# Build in Release mode (default)
./build-psmodule.ps1

# Build in Debug mode
./build-psmodule.ps1 -Configuration Debug

# Skip build and just copy existing DLL
./build-psmodule.ps1 -SkipBuild
```

### Manual Build

If you prefer to build manually:

```powershell
# Build the library
dotnet build ../../src/ConsoleInk.Net/ConsoleInk.Net.csproj -c Release

# Copy the DLL
cp ../../src/ConsoleInk.Net/bin/Release/netstandard2.0/ConsoleInk.Net.dll ConsoleInk/lib/
cp ../../src/ConsoleInk.Net/bin/Release/netstandard2.0/ConsoleInk.Net.xml ConsoleInk/lib/
```

## Publishing to PowerShell Gallery

After building the module:

```powershell
# Test locally first
Import-Module ./ConsoleInk/ConsoleInk.psd1 -Force
'# Test' | ConvertTo-Markdown

# Publish to PowerShell Gallery
Publish-Module -Path ./ConsoleInk -Repository PSGallery -NuGetApiKey YOUR_API_KEY
```

## Directory Structure

```
powershell-module/
├── ConsoleInk/
│   ├── ConsoleInk.psd1      # Module manifest
│   ├── ConsoleInk.psm1      # Module script
│   └── lib/                 # Build artifacts (not in git)
│       ├── ConsoleInk.Net.dll
│       └── ConsoleInk.Net.xml
└── README.md                # This file
```

## Notes

- The `lib/` directory and its contents are excluded from git via `.gitignore`
- Always build the library before publishing the PowerShell module
- The module version in `ConsoleInk.psd1` should match the library version in `ConsoleInk.Net.csproj`
