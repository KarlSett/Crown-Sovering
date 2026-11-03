param([string]$Configuration = 'Debug')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
Push-Location $projectRoot
try {
    # An isolated output also works while a developer has bin/Debug running.
    $buildOutput = ".design-preview/verified/$Configuration/"
    & MSBuild.exe 'Crown &.csproj' /t:Build "/p:Configuration=$Configuration" "/p:OutputPath=$buildOutput" '/p:TargetPlatformSdkPath=C:/Program Files (x86)/Windows Kits/8.1/' /p:TargetPlatformDisplayName=Windows /nologo /verbosity:minimal
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
