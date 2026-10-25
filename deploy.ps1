param(
    [string]$Sts2GameDir = $env:STS2_GAME_DIR,
    [string]$UploaderRoot = "",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$projectDir = $PSScriptRoot
$workspaceRoot = Split-Path -Parent $projectDir

if ([string]::IsNullOrWhiteSpace($Sts2GameDir)) {
    $candidates = @(
        "F:\SteamLibrary\steamapps\common\Slay the Spire 2",
        "D:\Steam\steamapps\common\Slay the Spire 2",
        "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2"
    )
    $Sts2GameDir = $candidates | Where-Object {
        Test-Path (Join-Path $_ "data_sts2_windows_x86_64\sts2.dll")
    } | Select-Object -First 1
}

if ([string]::IsNullOrWhiteSpace($Sts2GameDir)) {
    throw "STS2 install not found. Pass -Sts2GameDir or set STS2_GAME_DIR."
}

if ([string]::IsNullOrWhiteSpace($UploaderRoot)) {
    $UploaderRoot = Join-Path $workspaceRoot "ModUploader"
}

$resolvedGameDir = (Resolve-Path -LiteralPath $Sts2GameDir).Path
$resolvedUploaderRoot = (Resolve-Path -LiteralPath $UploaderRoot).Path
$gameExe = Join-Path $resolvedGameDir "SlayTheSpire2.exe"
$uploaderExe = Join-Path $resolvedUploaderRoot "ModUploader.exe"
if (-not (Test-Path -LiteralPath $gameExe)) {
    throw "Invalid STS2 directory: $resolvedGameDir"
}
if (-not (Test-Path -LiteralPath $uploaderExe)) {
    throw "Invalid ModUploader directory: $resolvedUploaderRoot"
}

& (Join-Path $projectDir "build.ps1") -Sts2GameDir $resolvedGameDir -Configuration $Configuration

$packageDir = Join-Path $projectDir "build\MysteriousMap"
$gameTarget = Join-Path $resolvedGameDir "mods\MysteriousMap"
$uploaderWorkspace = Join-Path $resolvedUploaderRoot "MysteriousMap"
$uploaderContent = Join-Path $uploaderWorkspace "content"

New-Item -ItemType Directory -Force -Path $gameTarget | Out-Null
New-Item -ItemType Directory -Force -Path $uploaderContent | Out-Null

$obsoleteFiles = @("MysteriousMap.deps.json")
foreach ($target in @($gameTarget, $uploaderContent)) {
    foreach ($file in $obsoleteFiles) {
        $obsoletePath = Join-Path $target $file
        if (Test-Path -LiteralPath $obsoletePath) {
            Remove-Item -LiteralPath $obsoletePath -Force
        }
    }
}

$files = @("MysteriousMap.dll", "MysteriousMap.json", "MysteriousMap.pck")
foreach ($file in $files) {
    $source = Join-Path $packageDir $file
    if (-not (Test-Path -LiteralPath $source)) {
        throw "Build output missing: $source"
    }
    Copy-Item -LiteralPath $source -Destination (Join-Path $gameTarget $file) -Force
    Copy-Item -LiteralPath $source -Destination (Join-Path $uploaderContent $file) -Force
}

$templateImage = Join-Path $resolvedUploaderRoot "template\image.png"
$workspaceImage = Join-Path $uploaderWorkspace "image.png"
if (-not (Test-Path -LiteralPath $workspaceImage) -and (Test-Path -LiteralPath $templateImage)) {
    Copy-Item -LiteralPath $templateImage -Destination $workspaceImage
}

Write-Output "Deployed game mod: $gameTarget"
Write-Output "Updated uploader content: $uploaderContent"
Write-Output "Workshop metadata: $(Join-Path $uploaderWorkspace 'workshop.json')"
