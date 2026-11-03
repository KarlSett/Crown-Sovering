param([string]$Configuration = 'Debug')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
Push-Location $projectRoot
try {
    $msbuildCmd = Get-Command MSBuild.exe -ErrorAction SilentlyContinue
    $msbuild = if ($msbuildCmd) { $msbuildCmd.Source } else {
        @(
            'C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe',
            'C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe',
            'C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe',
            'C:\Program Files\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe',
            'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe'
        ) | Where-Object { Test-Path $_ } | Select-Object -First 1
    }
    # An isolated output also works while a developer has bin/Debug running.
    $buildOutput = ".design-preview/verified/$Configuration/"
    & $msbuild 'Crown &.csproj' /t:Build "/p:Configuration=$Configuration" "/p:OutputPath=$buildOutput" '/p:TargetPlatformSdkPath=C:/Program Files (x86)/Windows Kits/8.1/' /p:TargetPlatformDisplayName=Windows /nologo /verbosity:minimal
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
    $preview = Join-Path $projectRoot '.design-preview'
    New-Item -ItemType Directory -Force -Path $preview | Out-Null
    $assembly = Join-Path $projectRoot "$buildOutput/Crown &.exe"
    $framework = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319'
    $wpf = Join-Path $framework 'WPF'
    $runner = Join-Path $preview 'FrontendSmoke.exe'
    & (Join-Path $framework 'csc.exe') /nologo /target:exe /platform:anycpu "/out:$runner" "/reference:$assembly" "/reference:$wpf/PresentationCore.dll" "/reference:$wpf/PresentationFramework.dll" "/reference:$wpf/WindowsBase.dll" /reference:System.Xaml.dll /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll (Join-Path $PSScriptRoot 'FrontendSmoke.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Smoke runner compilation failed.' }
    # Resolve the application assembly without copying or changing the installed application.
    $env:CROWN_FRONTEND_ASSEMBLY = $assembly
    & $runner
    if ($LASTEXITCODE -ne 0) { throw 'Frontend verification failed.' }
} finally { Pop-Location }
