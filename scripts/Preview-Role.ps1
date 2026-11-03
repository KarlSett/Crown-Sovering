param(
    [ValidateSet('Administrador','Gerente','Recepcionista','Huesped')][string]$Role = 'Recepcionista',
    [ValidateSet('Light','Dark','Brand')][string]$Theme = 'Light'
)
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
    $previewOutput = '.design-preview/verified/RolePreview/'
    & $msbuild 'Crown &.csproj' /t:Build /p:Configuration=Debug "/p:OutputPath=$previewOutput" '/p:TargetPlatformSdkPath=C:/Program Files (x86)/Windows Kits/8.1/' /p:TargetPlatformDisplayName=Windows /nologo /verbosity:minimal
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo compilar el frontend.' }
    $env:CROWN_FRONTEND_ASSEMBLY = Join-Path $projectRoot ($previewOutput + 'Crown &.exe')
    $frameworkDirectory = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319'
    $wpfDirectory = Join-Path $frameworkDirectory 'WPF'
    $previewRunner = Join-Path $projectRoot '.design-preview/RolePreview.exe'
    & (Join-Path $frameworkDirectory 'csc.exe') /nologo /target:exe "/out:$previewRunner" "/reference:$wpfDirectory/PresentationCore.dll" "/reference:$wpfDirectory/PresentationFramework.dll" "/reference:$wpfDirectory/WindowsBase.dll" /reference:System.Xaml.dll (Join-Path $PSScriptRoot 'RolePreview.cs')
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo compilar la vista de roles.' }
    & $previewRunner $Role $Theme
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo abrir la vista de roles.' }
} finally { Pop-Location }
