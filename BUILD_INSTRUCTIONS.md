# Google Ads Optimizer - Build Instructions

This guide explains how to build the Google Ads Optimizer application and create the MSI installer.

## Prerequisites

### Required Tools:
1. **.NET 8.0 SDK** - Download from [https://dotnet.microsoft.com/download/dotnet/8.0](https://dotnet.microsoft.com/download/dotnet/8.0)
2. **WiX Toolset v3.11+** - Download from [https://wixtoolset.org/releases/](https://wixtoolset.org/releases/)
3. **Visual Studio 2022** (recommended) or compatible .NET IDE
4. **Windows 10/11** - WiX only works on Windows

### Optional:
- Git - for cloning the repository
- GitHub CLI - for creating releases

## Building the Application

### 1. Clone and Setup
```bash
git clone https://github.com/UPB-FLL/google-ads-optimizer.git
cd google-ads-optimizer
```

### 2. Restore NuGet Packages
```bash
dotnet restore GoogleAdsOptimizer.sln
```

### 3. Build the Solution
```bash
# Debug build
dotnet build GoogleAdsOptimizer.sln --configuration Debug

# Release build
dotnet build GoogleAdsOptimizer.sln --configuration Release
```

The compiled executable will be in:
- Debug: `src/GoogleAdsOptimizer/bin/Debug/net8.0-windows/`
- Release: `src/GoogleAdsOptimizer/bin/Release/net8.0-windows/`

### 4. Test the Application
```bash
# Run the application
dotnet run --project src/GoogleAdsOptimizer/GoogleAdsOptimizer.csproj

# Or run the executable directly
src/GoogleAdsOptimizer/bin/Release/net8.0-windows/GoogleAdsOptimizer.exe
```

## Creating the MSI Installer

### 1. Update WiX Configuration

Edit `build/GoogleAdsOptimizer.wxs` and replace placeholder GUIDs:
```xml
UpgradeCode="YOUR-GUID-HERE-PLACEHOLDER"
```

Generate GUIDs using PowerShell:
```powershell
[Guid]::NewGuid()
```

Replace all `YOUR-GUID-HERE` instances with generated GUIDs.

### 2. Update Build Paths

Ensure the WiX file references correct paths:
- `$(var.GoogleAdsOptimizer.TargetPath)` should point to your build output
- Update file paths if your build output location differs

### 3. Build the Installer

```bash
cd build

# Compile WiX source
candle.exe GoogleAdsOptimizer.wxs -ext WixUIExtension

# Link and create MSI
light.exe -out GoogleAdsOptimizer.msi GoogleAdsOptimizer.wixobj -ext WixUIExtension
```

The MSI installer will be created as `build/GoogleAdsOptimizer.msi`

### 4. Test the Installer

```bash
# Install the application
msiexec /i build\GoogleAdsOptimizer.msi

# Uninstall the application
msiexec /x {YOUR-UPGRADE-CODE-GUID}
```

## Automated Build Script

Create `build.bat` in the project root:

```batch
@echo off
echo Building Google Ads Optimizer...

echo.
echo Step 1: Building .NET application...
dotnet build GoogleAdsOptimizer.sln --configuration Release

if %ERRORLEVEL% NEQ 0 (
    echo Build failed!
    exit /b 1
)

echo.
echo Step 2: Creating MSI installer...
cd build
candle.exe GoogleAdsOptimizer.wxs -ext WixUIExtension

if %ERRORLEVEL% NEQ 0 (
    echo WiX compilation failed!
    exit /b 1
)

light.exe -out GoogleAdsOptimizer.msi GoogleAdsOptimizer.wixobj -ext WixUIExtension

if %ERRORLEVEL% NEQ 0 (
    echo MSI creation failed!
    exit /b 1
)

cd ..

echo.
echo Build completed successfully!
echo Installer: build/GoogleAdsOptimizer.msi

pause
```

## Troubleshooting

### Build Issues:

**Issue**: dotnet command not found
**Solution**: Install .NET 8.0 SDK and restart your terminal

**Issue**: candle.exe or light.exe not found
**Solution**: Install WiX Toolset and add it to your PATH

**Issue**: Missing dependencies
**Solution**: Run `dotnet restore` before building

### WiX Issues:

**Issue**: "The system cannot find the file specified"
**Solution**: Update file paths in GoogleAdsOptimizer.wxs to match your build output

**Issue**: GUID-related errors
**Solution**: Replace all placeholder GUIDs with real GUIDs

**Issue**: Missing UI files
**Solution**: Create or remove references to LICENSE.rtf, banner.bmp, dialog.bmp

## Project Structure After Build

```
google-ads-optimizer/
├── build/
│   ├── GoogleAdsOptimizer.wxs
│   ├── GoogleAdsOptimizer.msi (after build)
│   └── GoogleAdsOptimizer.wixobj (temporary)
├── src/
│   └── GoogleAdsOptimizer/
│       └── bin/
│           └── Release/
│               └── net8.0-windows/
│                   ├── GoogleAdsOptimizer.exe
│                   ├── GoogleAdsOptimizer.exe.config
│                   ├── Newtonsoft.Json.dll
│                   ├── MaterialDesignThemes.Wpf.dll
│                   ├── MaterialDesignColors.dll
│                   ├── Google.Ads.GoogleAds.dll
│                   ├── Azure.AI.OpenAI.dll
│                   └── Azure.Core.dll
└── BUILD_INSTRUCTIONS.md (this file)
```

## Distribution

Once built, you can distribute the MSI installer:

1. **GitHub Releases**: Upload MSI to GitHub Releases
2. **Website**: Host on your download page
3. **Email**: Send MSI to stakeholders
4. **Network**: Place on shared network drive

## Version Updates

To update the version:

1. Update `src/GoogleAdsOptimizer/GoogleAdsOptimizer.csproj`: `<Version>1.0.1</Version>`
2. Update `build/GoogleAdsOptimizer.wxs`: `Version="1.0.1.0"`
3. Rebuild the application and installer

## Support

For build issues:
- Check .NET SDK installation: `dotnet --version`
- Check WiX installation: `candle.exe -?`
- Review build logs for specific errors
- Ensure all prerequisites are installed

## Next Steps

After successful build:
1. Test the MSI installer on a clean machine
2. Verify application functionality
3. Test auto-update functionality
4. Create documentation
5. Prepare for distribution