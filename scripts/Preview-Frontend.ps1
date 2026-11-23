param([string]$OutputDirectory = '.design-preview/modern', [switch]$ThemesOnly)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
Push-Location -LiteralPath $projectRoot
try {
    $msbuild = 'C:/Program Files/Microsoft Visual Studio/2022/Community/MSBuild/Current/Bin/MSBuild.exe'
    & $msbuild 'Crown &.csproj' /t:Build /p:Configuration=Debug '/p:TargetPlatformSdkPath=C:/Program Files (x86)/Windows Kits/8.1/' /p:TargetPlatformDisplayName=Windows /nologo /verbosity:minimal
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
    $framework = 'C:/Windows/Microsoft.NET/Framework64/v4.0.30319'
    $wpf = "$framework/WPF"
    $preview = Join-Path $projectRoot 'bin/Debug/Preview-Frontend.exe'
    & "$framework/csc.exe" /nologo /target:exe "/out:$preview" '/r:bin/Debug/Crown &.exe' "/r:$wpf/PresentationFramework.dll" "/r:$wpf/PresentationCore.dll" "/r:$wpf/WindowsBase.dll" "/r:$framework/System.Xaml.dll" (Join-Path $PSScriptRoot 'Preview-Frontend.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Preview compilation failed.' }
    if ($ThemesOnly) { & $preview $OutputDirectory --themes-only }
    else { & $preview $OutputDirectory }
    if ($LASTEXITCODE -ne 0) { throw 'Preview verification failed.' }
}
finally { Pop-Location }
