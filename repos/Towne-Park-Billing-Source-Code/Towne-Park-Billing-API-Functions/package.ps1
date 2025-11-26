#Requires -Version 5.0 # Compress-Archive needs 5.0+, Base script structure assumed >= 5.0

# CONFIGURATION
$FunctionSourceDir = ".\src"         # Relative path to the directory containing the .csproj
$BaseOutputDirName = "output"      # Name for the base output directory
$ZipName = "function.zip"          # Name of the final zip file
$BuildConfig = "Release"           # Build configuration (Release/Debug)
# Optional: Specify runtime if needed (e.g., "win-x64", "linux-x64"). Leave empty for framework-dependent.
$RuntimeIdentifier = ""            # Example: $RuntimeIdentifier = "win-x64"

# --- SCRIPT START ---
Clear-Host
Write-Host "Starting build and packaging process..." -ForegroundColor Yellow
Write-Host "Date and Time: $(Get-Date)"
$InitialCWD = Get-Location
Write-Host "Initial Current Working Directory (CWD): $($InitialCWD.Path)"
Write-Host "--------------------------------------------------"

# --- Resolve Paths based on Initial CWD ---
# Use Resolve-Path to get absolute paths reliably
try {
    $ProjectRoot = Resolve-Path -Path $InitialCWD.Path -ErrorAction Stop
    $AbsoluteFunctionSourceDir = Resolve-Path -Path (Join-Path -Path $ProjectRoot -ChildPath $FunctionSourceDir) -ErrorAction Stop
    $OutputDir = Join-Path -Path $ProjectRoot -ChildPath $BaseOutputDirName
    $PublishDir = Join-Path -Path $OutputDir -ChildPath "app" # Staging directory for published files
    $ZipPath = Join-Path -Path $OutputDir -ChildPath $ZipName
}
catch {
    Write-Error "CRITICAL Error resolving initial paths. Check CWD and configuration."
    Write-Error $_.Exception.Message
    exit 1
}

Write-Host "SCRIPT IS USING THE FOLLOWING ABSOLUTE PATHS:"
Write-Host "Project Root:               $ProjectRoot"
Write-Host "Function Source Directory:  $AbsoluteFunctionSourceDir"
Write-Host "Output Directory Target:    $OutputDir"
Write-Host "Publish Directory Target:   $PublishDir" # This is where dotnet publish outputs files
Write-Host "Zip Path Target:            $ZipPath"
Write-Host "--------------------------------------------------"

# *** WARNING ABOUT ONEDRIVE/SYNC FOLDERS ***
if ($ProjectRoot -like "*OneDrive*" -or $ProjectRoot -like "*Dropbox*" -or $ProjectRoot -like "*Google Drive*") {
    Write-Warning "Project is located within a cloud sync folder (OneDrive, Dropbox, etc.)."
    Write-Warning "File locking during synchronization can sometimes interfere with build/packaging scripts."
    Write-Warning "If you encounter errors (especially during file deletion, creation, or zipping), try pausing synchronization or moving the project temporarily outside the sync folder."
    Write-Host "--------------------------------------------------"
}

# VERIFY .NET SDK
Write-Host "Verifying .NET SDK installation..."
dotnet --version
if ($LASTEXITCODE -ne 0) {
    Write-Error "Error: .NET SDK does not seem to be installed or is not in the PATH. Please install it."
    exit 1
}
Write-Host ".NET SDK found."
Write-Host "--------------------------------------------------"

# VERIFY SOURCE DIRECTORY
Write-Host "Verifying source directory: '$AbsoluteFunctionSourceDir'..."
if (-not (Test-Path -Path $AbsoluteFunctionSourceDir -PathType Container)) {
    Write-Error "Error: The source directory '$AbsoluteFunctionSourceDir' does not exist. Check the '$FunctionSourceDir' variable."
    exit 1
}
Write-Host "Source directory found."
Write-Host "--------------------------------------------------"

# CLEAN OUTPUT DIRECTORY
Write-Host "Cleaning output directory '$OutputDir'..."
if (Test-Path -Path $OutputDir) {
    Write-Host "Removing existing directory: '$OutputDir'"
    try {
        Remove-Item -Path $OutputDir -Recurse -Force -ErrorAction Stop
        Write-Host "Directory '$OutputDir' removed."
    }
    catch {
        Write-Warning "Warning: Could not completely remove the directory '$OutputDir'. Some files might be locked (e.g., by an editor, terminal, or cloud sync). Close applications using the folder and try again. Pausing sync might help."
        # Consider exiting if a clean state is absolutely mandatory
        # exit 1
    }
}
Write-Host "--------------------------------------------------"

# ENSURE OUTPUT DIRECTORIES EXIST
Write-Host "Ensuring output directories exist..."
try {
    # Create Base Output Directory ($OutputDir)
    if (-not (Test-Path -Path $OutputDir -PathType Container)) {
        Write-Host "Creating base output directory: '$OutputDir'"
        New-Item -ItemType Directory -Path $OutputDir -ErrorAction Stop | Out-Null
        Write-Host "Base output directory '$OutputDir' created successfully."
    } else {
        Write-Host "Base output directory '$OutputDir' already exists (might occur if cleanup failed)."
    }

    # Create Publish Subdirectory ($PublishDir) where dotnet publish outputs files
    if (-not (Test-Path -Path $PublishDir -PathType Container)) {
         Write-Host "Creating publish subdirectory: '$PublishDir'"
        New-Item -ItemType Directory -Path $PublishDir -ErrorAction Stop | Out-Null
        Write-Host "Publish subdirectory '$PublishDir' created successfully."
    } else {
         Write-Host "Publish subdirectory '$PublishDir' already exists." # Should not happen after clean, but check anyway
    }
}
catch {
    Write-Error "CRITICAL Error: Could not create output directory structure ('$OutputDir' or '$PublishDir'). Check permissions or locking issues (e.g., cloud sync)."
    Write-Error $_.Exception.Message
    exit 1
}
Write-Host "--------------------------------------------------"

# FIND .csproj
Write-Host "Searching for .csproj file in '$AbsoluteFunctionSourceDir'..."
$csprojFiles = Get-ChildItem -Path $AbsoluteFunctionSourceDir -Filter *.csproj -File
if ($csprojFiles.Count -eq 0) {
    Write-Error "Error: No .csproj file found in '$AbsoluteFunctionSourceDir'. Ensure the project exists at that path."
    exit 1
}
if ($csprojFiles.Count -gt 1) {
    Write-Warning "Warning: Multiple .csproj files found in '$AbsoluteFunctionSourceDir'. Using the first one: $($csprojFiles[0].FullName)"
}
$csprojPath = $csprojFiles[0]
Write-Host ".csproj file found: $($csprojPath.FullName)"
Write-Host "--------------------------------------------------"

# .NET PUBLISH
Write-Host "Publishing .NET project..."
Write-Host "Source:  $($csprojPath.FullName)"
Write-Host "Target:  $PublishDir"
Write-Host "Config:  $BuildConfig"
if ($RuntimeIdentifier) { Write-Host "Runtime: $RuntimeIdentifier" }

# Construct dotnet publish command arguments (still useful)
$publishArgs = @(
    "publish",
    "`"$($csprojPath.FullName)`"", # Quote path in case of spaces
    "-c", $BuildConfig,
    "-o", "`"$PublishDir`"",       # Quote path
    "-v", "minimal" # Use minimal verbosity unless debugging (-v n for normal)
)
if ($RuntimeIdentifier) {
    $publishArgs += "-r", $RuntimeIdentifier
}

$commandString = "dotnet $($publishArgs -join ' ')" # For display purposes
$publishLogPath = Join-Path -Path $OutputDir -ChildPath "publish.log"
$exitCode = -1 # Initialize with a non-zero value

# *** Execute dotnet publish directly using PowerShell redirection ***
Write-Host "Executing: $commandString"
Write-Host "Redirecting Standard Output and Standard Error to: $publishLogPath"

# Ensure the log directory exists (should exist from earlier steps, but double-check)
if (-not (Test-Path (Split-Path $publishLogPath) -PathType Container)) {
     Write-Error "CRITICAL Error: Log directory '$(Split-Path $publishLogPath)' does not exist before executing dotnet publish."
     exit 1
}

try {
    # Use the '*' redirection operator to send both stdout (1) and stderr (2) to the log file.
    # Make sure $publishLogPath is quoted in case it contains spaces.
    dotnet $publishArgs *>"$publishLogPath"
    $exitCode = $LASTEXITCODE # Capture the exit code IMMEDIATELY after the command finishes
} catch {
    # This catch block will now primarily catch PowerShell script errors,
    # not necessarily errors *within* the dotnet process itself (those go to the log).
    Write-Error "A PowerShell script error occurred while attempting to run 'dotnet publish'."
    Write-Error "This might indicate a problem launching the process or with the redirection setup."
    Write-Error $_.Exception.Message
    # $LASTEXITCODE might still be relevant here if dotnet started but PS had an issue
    $exitCode = $LASTEXITCODE
    # Attempt to show partial log if it exists
    if(Test-Path $publishLogPath) {
         Write-Host "--- Attempting to display potentially incomplete log: $publishLogPath ---"
         Get-Content $publishLogPath -ErrorAction SilentlyContinue
         Write-Host "--- End of log ---"
     }
     # Exit here as the process likely didn't complete as expected
     exit 1
}

# --- Check the Exit Code ---
Write-Host "'dotnet publish' command completed. Checking exit code..."
Write-Host "Exit Code captured via `$LASTEXITCODE: $exitCode"
Write-Host "Full publish log should be at: $publishLogPath"

if ($exitCode -ne 0) {
    Write-Error "Error: 'dotnet publish' failed (Exit Code: $exitCode). Check the log file for build errors or details: $publishLogPath" -ForegroundColor Red
    # Display log content on error
    if (Test-Path $publishLogPath) {
        Write-Host "--- Displaying publish log ($publishLogPath) ---"
        # Use -Raw to load faster, or remove if log is huge and you only want the start/end
        Get-Content $publishLogPath -ErrorAction SilentlyContinue #-Raw
        Write-Host "--- End of publish log ---"
    } else {
        Write-Warning "Publish log file '$publishLogPath' was not found, even though the process reported an error (Exit Code: $exitCode)."
    }
    exit 1
}

# If exit code is 0, proceed
Write-Host ".NET project published successfully to '$PublishDir' (according to exit code)."
Write-Host "--------------------------------------------------"

# VERIFY PUBLISHED FILES
Write-Host "Verifying content of the publish directory '$PublishDir'..."
# Add -ErrorAction SilentlyContinue in case $PublishDir doesn't exist after a failed publish
$publishedItems = Get-ChildItem -Path $PublishDir -ErrorAction SilentlyContinue
if (-not $publishedItems) {
    # This check is important if dotnet exited with 0 but produced no output (unlikely but possible)
    Write-Error "Error: The publish directory '$PublishDir' IS EMPTY or does not exist after 'dotnet publish', despite a reported success exit code (0)."
    Write-Error "This is unexpected. Check the build process and log: $publishLogPath"
    exit 1
}
$fileCount = ($publishedItems | Where-Object { -not $_.PSIsContainer }).Count
$dirCount = ($publishedItems | Where-Object { $_.PSIsContainer }).Count
Write-Host "Found $fileCount files and $dirCount directories in '$PublishDir'."
$hostJsonPath = Join-Path -Path $PublishDir -ChildPath "host.json"
if (Test-Path $hostJsonPath) {
    Write-Host "Found '$hostJsonPath'."
} else {
     Write-Warning "Warning: 'host.json' not found in the root of '$PublishDir'."
}
# Add more specific checks if needed

Write-Host "--- Listing top-level contents of '$PublishDir' ---"
Get-ChildItem -Path $PublishDir | Select-Object Name, Mode, Length, LastWriteTime
Write-Host "--- End of listing ---"
Write-Host "--------------------------------------------------"

# CREATE ZIP PACKAGE *** USING Compress-Archive ***
Write-Host "Creating ZIP package..."
Write-Host "Source (content of): $PublishDir"
Write-Host "Destination ZIP:     $ZipPath"

$ZipParentDir = Split-Path $ZipPath -Parent
if (-not (Test-Path -Path $ZipParentDir -PathType Container)) {
    Write-Error "CRITICAL Error: The parent directory '$ZipParentDir' for the ZIP file does not exist right before zipping."
    exit 1
}

if (Test-Path -Path $ZipPath) {
    Write-Host "Deleting existing ZIP: '$ZipPath'..."
    try {
        Remove-Item -Path $ZipPath -Force -ErrorAction Stop
    } catch {
        Write-Error "Error: Could not delete existing ZIP file '$ZipPath'. It might be locked."
        Write-Error $_.Exception.Message
        exit 1
    }
}

try {
    Compress-Archive -Path "$PublishDir\*" -DestinationPath $ZipPath -CompressionLevel Optimal -ErrorAction Stop
    Write-Host "ZIP package created successfully at '$ZipPath'."
}
catch {
    Write-Error "Error: Could not create ZIP file '$ZipPath' from the contents of '$PublishDir'."
    Write-Error "Underlying Exception: $($_.Exception.Message)"
    Write-Error "Source Directory ('$PublishDir') Exists: $(Test-Path -Path $PublishDir -PathType Container)"
    $sourceItemsCount = @(Get-ChildItem -Path $PublishDir -ErrorAction SilentlyContinue).Count
    Write-Error "Items inside '$PublishDir' count: $sourceItemsCount"
    if ($sourceItemsCount -eq 0) { Write-Warning "Source directory appears empty!" }
    Write-Error "Destination Parent Directory ('$ZipParentDir') Exists: $(Test-Path -Path $ZipParentDir -PathType Container)"
    exit 1
}
Write-Host "--------------------------------------------------"

# FINAL VERIFICATION
if (Test-Path -Path $ZipPath) {
    Write-Host "Process completed successfully!" -ForegroundColor Green
    Write-Host "Package created at: $ZipPath"
    try {
        Add-Type -AssemblyName System.IO.Compression.FileSystem -ErrorAction Stop
        $zip = [System.IO.Compression.ZipFile]::OpenRead($ZipPath)
        Write-Host "--- Verifying ZIP root contents ---"
        $rootEntries = $zip.Entries | Where-Object { $_.FullName -notmatch '[\\/]' } | Select-Object -ExpandProperty FullName
        if ($rootEntries) {
            $rootEntries
        } else {
            Write-Host "(No files found directly in the root of the ZIP)"
        }
        $zip.Dispose()
        Write-Host "--- End ZIP root contents ---"
    } catch {
        Write-Warning "Could not list ZIP contents."
        Write-Warning "$($_.Exception.Message)"
    }
} else {
    Write-Error "CRITICAL Error: The final ZIP file '$ZipPath' was not found after the compression step." -ForegroundColor Red
}
Write-Host "--------------------------------------------------"

# Set-Location $InitialCWD