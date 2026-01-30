# === SETTINGS ===
$Version = "1.0.0"                      # Change this for new releases
$AppId = "ProjectK3PDB"
$ProjectFile = "ProjectK.3PDB.Standalone.API\ProjectK.3PDB.Standalone.API.csproj"
$MainExe = "ProjectK.3PDB.Standalone.API.exe"
$PublishDir = ".\publish"
$ReleaseDir = ".\Releases"

# === CHECK TOOLS ===
if (-not (Get-Command vpk -ErrorAction SilentlyContinue)) {
    Write-Host "Warning: Velopack tool (vpk) not found. Installing..." -ForegroundColor Yellow
    dotnet tool install -g vpk
}

# === CLEANUP ===
Write-Host "Cleaning up old files..." -ForegroundColor Cyan
if (Test-Path $PublishDir) { Remove-Item -Recurse -Force $PublishDir }
# Uncomment next line if you want a clean build every time (removes history)
# if (Test-Path $ReleaseDir) { Remove-Item -Recurse -Force $ReleaseDir }

# === PUBLISH (COMPILE) ===
Write-Host "Compiling version $Version..." -ForegroundColor Cyan

# Using explicit array for arguments to avoid parsing issues
$buildArgs = @(
    "publish", $ProjectFile,
    "-c", "Release",
    "-r", "win-x64",
    "--self-contained",
    "-p:PublishSingleFile=true",
    "-p:Version=$Version",
    "-o", $PublishDir
)

dotnet @buildArgs

if ($LASTEXITCODE -ne 0) {
    Write-Error "Build Failed!"
    exit
}

# === PACK (VELOPACK) ===
Write-Host "Packing Setup.exe..." -ForegroundColor Cyan

# Add -i "app.ico" if you have an icon
vpk pack -u $AppId -v $Version -p $PublishDir -e $MainExe -o $ReleaseDir

if ($LASTEXITCODE -ne 0) {
    Write-Error "Velopack Packing Failed!"
    exit
}

Write-Host "DONE! Installer is located at: $ReleaseDir\Setup.exe" -ForegroundColor Green
Invoke-Item $ReleaseDir