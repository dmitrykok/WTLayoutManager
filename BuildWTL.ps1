param(
    [switch]$Clean,
    [switch]$Debug,
    [switch]$Release,
    [switch]$x64,
    [switch]$x86,
    [switch]$ARM64,
    [switch]$NoPackage,
    [switch]$Restore,
    [switch]$Bundle,
    [string]$CertPath
)

# Set default configurations if none are specified
if (-not $Debug -and -not $Release) {
    $Release = $true
}

$Configurations = @()
if ($Release) { $Configurations += "Release" }
if ($Debug) { $Configurations += "Debug" }

# Set default platforms if none are specified
if (-not $x64 -and -not $x86 -and -not $ARM64) {
    $x64 = $true
}

$Platforms = @()
if ($x64) { $Platforms += "x64" }
if ($x86) { $Platforms += "x86" }
if ($ARM64) { $Platforms += "ARM64" }

# Clean builds if the Clean switch is specified
if ($Clean) {
    foreach ($Configuration in $Configurations) {
        foreach ($Platform in $Platforms) {
            MSBuild .\WTLayoutManager.sln -t:Clean -p:Configuration=$Configuration -p:Platform=$Platform
        }
    }
}

if ($Restore) {
    # Check if nuget exists
    if (Get-Command nuget -ErrorAction SilentlyContinue) {
        Write-Host "NuGet exists, using nuget restore..."
        nuget restore .\WTLayoutManager.sln
    }
    else {
        Write-Host "NuGet not found, falling back to dotnet restore..."
        dotnet restore .\WTLayoutManager.sln
    }
}

# Build commands
foreach ($Configuration in $Configurations) {
    foreach ($Platform in $Platforms) {
        MSBuild .\WTLayoutManager.sln -p:Configuration=$Configuration -p:Platform=$Platform

        if (-not $NoPackage) {
            MSBuild .\WTLayoutManager.sln -p:Configuration=$Configuration -p:Platform=$Platform -t:WTLayoutManagerBundle -m
        }
    }
}

# Copy .msix packages to .\msix folder
if (-not $NoPackage -and $Bundle) {
    $msixFolder = ".\msix"
    # Clean the msix folder before copying
    if (Test-Path $msixFolder) {
        Remove-Item -Path "$msixFolder\*" -Recurse -Force
    }
    else {
        New-Item -ItemType Directory -Path $msixFolder | Out-Null
    }

    $version = $null  # Initialize version variable

    foreach ($Platform in $Platforms) {
        # Build output path
        $appPackagesPath = ".\WTLayoutManagerBundle\AppPackages"

        # Get the latest package folder for the platform
        $packageFolders = Get-ChildItem -Path $appPackagesPath -Directory | Where-Object { $_.Name -like "*_$Platform*" }
        if ($packageFolders.Count -eq 0) {
            Write-Warning "No package folders found for platform $Platform."
            continue
        }

        # Assuming the latest folder is the one we just built
        $latestPackageFolder = $packageFolders | Sort-Object LastWriteTime -Descending | Select-Object -First 1

        # Find the .msix file inside the package folder
        $msixFiles = Get-ChildItem -Path $latestPackageFolder.FullName -Filter "*.msix" -File
        if ($msixFiles.Count -eq 0) {
            Write-Warning "No .msix files found in $($latestPackageFolder.FullName) for platform $Platform."
            continue
        }

        # Copy the .msix files to the .\msix folder
        foreach ($msixFile in $msixFiles) {
            Copy-Item -Path $msixFile.FullName -Destination $msixFolder -Force
            Write-Host "Copied $($msixFile.Name) to $msixFolder"

            # Extract version from the msix filename if not already set
            if (-not $version) {
                if ($msixFile.Name -match 'WTLayoutManagerBundle_(?<version>[0-9\.]+)') {
                    $version = $matches['version']
                    Write-Host "Extracted version: $version"
                }
            }
        }
    }

    # Proceed to bundle, sign, and verify if version is found
    if ($version) {
        # Path to the output bundle
        $bundlePath = ".\WTLayoutManagerBundle_$version.msixbundle"

        # Create the bundle using makeappx
        $makeappxCmd = "makeappx bundle /d $msixFolder /p $bundlePath"
        Write-Host "Creating bundle with makeappx..."
        Invoke-Expression $makeappxCmd

        # Sign the bundle using signtool
        # $certPath = ".\Dm17tryK.pfx"
        if (Test-Path $CertPath) {
            # Prompt for the certificate password securely
            $certPassword = Read-Host -AsSecureString "Enter the password for the certificate"

            # Convert the secure string to an encrypted standard string for signtool
            $ptr = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($certPassword)
            $password = [System.Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr)

            # Sign the bundle
            $signtoolCmd = "signtool sign /fd SHA256 /td SHA256 /a /f `"$CertPath`" /p `"$password`" /tr http://timestamp.digicert.com `"$bundlePath`""
            Write-Host "Signing the bundle with signtool..."
            Invoke-Expression $signtoolCmd

            # Verify the signature
            $verifyCmd = "signtool verify /pa /v `"$bundlePath`""
            Write-Host "Verifying the bundle signature..."
            Invoke-Expression $verifyCmd

            # Zero out and dispose of the password for security
            [System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr)
            $certPassword.Dispose()
        }
        else {
            Write-Warning "Certificate file $CertPath not found. Skipping signing."
        }
    }
    else {
        Write-Warning "Version could not be determined. Skipping bundle creation and signing."
    }
}
