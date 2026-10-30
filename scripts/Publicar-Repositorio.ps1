param(
    [string]$RepositoryUrl = 'https://github.com/KarlSett/Crown-Sovering.git'
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot

function Invoke-ProjectGit {
    param([string[]]$GitArguments)
    & git -C $projectRoot @GitArguments
    if ($LASTEXITCODE -ne 0) {
        throw "Git no pudo completar: $($GitArguments -join ' ')"
    }
}

Get-Command git -ErrorAction Stop | Out-Null
if (-not (Test-Path -LiteralPath (Join-Path $projectRoot '.git'))) {
    Invoke-ProjectGit -GitArguments @('init', '-b', 'main')
}

$currentBranch = & git -C $projectRoot symbolic-ref --short HEAD
if ($LASTEXITCODE -ne 0 -or $currentBranch -ne 'main') {
    throw 'La entrega debe publicarse desde main. No se ha renombrado ni sobrescrito otra rama.'
}

$remoteNames = @(& git -C $projectRoot remote)
if ($LASTEXITCODE -ne 0) { throw 'No se pudieron consultar los remotos.' }
if ($remoteNames -contains 'origin') {
    $currentRemote = & git -C $projectRoot remote get-url origin
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo consultar origin.' }
    if ($currentRemote -ne $RepositoryUrl) {
        throw "origin apunta a $currentRemote. Se conserva; revisar antes de publicar."
    }
}
else {
    Invoke-ProjectGit -GitArguments @('remote', 'add', 'origin', $RepositoryUrl)
}

# Only source, project files, documentation and this publisher are staged.
$sourcePaths = @(
    '.gitignore', '.gitattributes', 'README.md', 'ARQUITECTURA.md', 'DESIGN.md',
    'Crown &.sln', 'Crown &.csproj', 'App.config', 'App.xaml', 'App.xaml.cs',
    'MainWindow.xaml', 'MainWindow.xaml.cs', 'Properties', 'Frontend', 'Backend',
    'docs', 'scripts/Publicar-Repositorio.ps1'
)
Invoke-ProjectGit -GitArguments (@('add', '--') + $sourcePaths)
& git -C $projectRoot diff --cached --quiet
$stagedDiffStatus = $LASTEXITCODE
if ($stagedDiffStatus -eq 1) {
    Invoke-ProjectGit -GitArguments @('commit', '-m', 'Entrega inicial: frontend WPF del login y guia para backend')
}
elseif ($stagedDiffStatus -ne 0) {
    throw 'No se pudo revisar el contenido del commit.'
}

# Git uses your normal credential manager if GitHub authentication is needed.
# No force push: existing remote history is never overwritten.
Invoke-ProjectGit -GitArguments @('push', '-u', 'origin', 'main')
Write-Host "Publicado en $RepositoryUrl"