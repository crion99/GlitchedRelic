param(
    [string]$Sts2GameDir = $env:STS2_GAME_DIR,
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$projectDir = $PSScriptRoot
$projectFile = Join-Path $projectDir "MysteriousMap.csproj"
$outputDir = Join-Path $projectDir "build\MysteriousMap"
$pckRoot = Join-Path $outputDir "_pck_src"

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

dotnet build $projectFile -c $Configuration "/p:Sts2GameDir=$Sts2GameDir"
if ($LASTEXITCODE -ne 0) {
    throw "dotnet build failed with exit code $LASTEXITCODE."
}

$resolvedProjectDir = (Resolve-Path -LiteralPath $projectDir).Path
if (Test-Path -LiteralPath $outputDir) {
    $resolvedOutputDir = (Resolve-Path -LiteralPath $outputDir).Path
    if (-not $resolvedOutputDir.StartsWith($resolvedProjectDir + [IO.Path]::DirectorySeparatorChar,
        [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to clear build output outside the project: $resolvedOutputDir"
    }
    Remove-Item -LiteralPath $resolvedOutputDir -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $pckRoot | Out-Null
$binaryDir = Join-Path $projectDir "bin\$Configuration\net9.0"
Copy-Item -LiteralPath (Join-Path $binaryDir "MysteriousMap.dll") -Destination $outputDir -Force
Copy-Item -LiteralPath (Join-Path $projectDir "MysteriousMap.json") -Destination $outputDir -Force

$localizationDestination = Join-Path $pckRoot "MysteriousMap\localization"
New-Item -ItemType Directory -Force -Path $localizationDestination | Out-Null
Copy-Item -Path (Join-Path $projectDir "assets\localization\*") `
    -Destination $localizationDestination -Recurse -Force
$relicDestination = Join-Path $pckRoot "MysteriousMap\relics"
New-Item -ItemType Directory -Force -Path $relicDestination | Out-Null
Copy-Item -Path (Join-Path $projectDir "assets\relics\*.png") `
    -Destination $relicDestination -Force

$packer = Join-Path $projectDir "tools\pack_godot_pck.py"
if (-not (Test-Path -LiteralPath $packer)) {
    throw "Godot PCK packer not found: $packer"
}
$pckPath = Join-Path $outputDir "MysteriousMap.pck"
python $packer $pckRoot -o $pckPath --engine-version 4.5.1 --pack-version 3
if ($LASTEXITCODE -ne 0) {
    throw "PCK packing failed with exit code $LASTEXITCODE."
}

$resolvedPckRoot = (Resolve-Path -LiteralPath $pckRoot).Path
$resolvedOutputDir = (Resolve-Path -LiteralPath $outputDir).Path
if (-not $resolvedPckRoot.StartsWith($resolvedOutputDir + [IO.Path]::DirectorySeparatorChar,
    [StringComparison]::OrdinalIgnoreCase)) {
    throw "Refusing to clear PCK staging outside build output: $resolvedPckRoot"
}
Remove-Item -LiteralPath $resolvedPckRoot -Recurse -Force

Write-Output "Built mod package: $outputDir"
