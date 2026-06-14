param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$Project = Join-Path $Root "jellyfin-btttr-plugin\BtttrPosterPlugin.csproj"
$Artifacts = Join-Path $Root "artifacts"
$PackageDir = Join-Path $Artifacts "package"
$PublishDir = Join-Path $Artifacts "publish"
$ZipPath = Join-Path $Artifacts "EvenBetterPosters_2.1.1.zip"
$BuildDir = Join-Path $Root "jellyfin-btttr-plugin\bin\$Configuration\net8.0"

if (-not (Test-Path $Artifacts)) {
    New-Item -ItemType Directory -Path $Artifacts | Out-Null
}

if (Test-Path $PackageDir) {
    Remove-Item -LiteralPath $PackageDir -Recurse -Force
}

if (Test-Path $PublishDir) {
    Remove-Item -LiteralPath $PublishDir -Recurse -Force
}

dotnet build $Project -c $Configuration
New-Item -ItemType Directory -Path $PackageDir | Out-Null

Copy-Item -LiteralPath (Join-Path $BuildDir "Jellyfin.Plugin.BtttrPosters.dll") -Destination $PackageDir
Copy-Item -LiteralPath (Join-Path $BuildDir "Jellyfin.Plugin.BtttrPosters.deps.json") -Destination $PackageDir
Copy-Item -LiteralPath (Join-Path $BuildDir "Jellyfin.Plugin.BtttrPosters.pdb") -Destination $PackageDir

if (Test-Path $ZipPath) {
    Remove-Item -LiteralPath $ZipPath -Force
}

Compress-Archive -Path (Join-Path $PackageDir "*") -DestinationPath $ZipPath -Force
Write-Host "Created $ZipPath"
