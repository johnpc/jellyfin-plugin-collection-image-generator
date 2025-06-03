# Build script for local testing of the release process

# Get version from command line or use default
param (
    [string]$Version = "1.0.0.0"
)

# Build the project
dotnet build --configuration Release /p:TreatWarningsAsErrors=false

# Create release directory
$releaseDir = "release"
if (Test-Path $releaseDir) {
    Remove-Item -Path $releaseDir -Recurse -Force
}
New-Item -Path $releaseDir -ItemType Directory

# Copy files to release directory
Copy-Item -Path "bin/Release/net6.0/Jellyfin.Plugin.CollectionImageGenerator.dll" -Destination $releaseDir
Copy-Item -Path "bin/Release/net6.0/SixLabors.ImageSharp.dll" -Destination $releaseDir
Copy-Item -Path "bin/Release/net6.0/SixLabors.ImageSharp.Drawing.dll" -Destination $releaseDir
Copy-Item -Path "Plugin.xml" -Destination $releaseDir

# Create zip file
$zipFile = "collection-image-generator-$Version.zip"
if (Test-Path $zipFile) {
    Remove-Item -Path $zipFile -Force
}

# Compress the release directory
Compress-Archive -Path "$releaseDir/*" -DestinationPath $zipFile

# Calculate checksum
$md5 = New-Object -TypeName System.Security.Cryptography.MD5CryptoServiceProvider
$hash = [System.BitConverter]::ToString($md5.ComputeHash([System.IO.File]::ReadAllBytes((Get-Item $zipFile).FullName))).Replace("-", "").ToLower()

# Create manifest.json
$timestamp = [DateTime]::UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
$manifestContent = @"
[
  {
    "guid": "e29b0e3d-f15e-47b9-9b3d-ed3df892e33d",
    "name": "Collection Image Generator",
    "overview": "Automatically generates collage images for collections without images",
    "description": "A Jellyfin plugin that automatically generates collage images for collections without images. It runs as a scheduled task once per day and creates collages from movie/show posters within each collection.",
    "owner": "jellyfin-collection-image-generator",
    "category": "Metadata",
    "versions": [
      {
        "version": "$Version",
        "changelog": "- Initial release\n",
        "targetAbi": "10.8.0.0",
        "sourceUrl": "https://github.com/johnpc/jellyfin-plugin-collection-image-generator/releases/download/$Version/collection-image-generator-$Version.zip",
        "checksum": "$hash",
        "timestamp": "$timestamp"
      }
    ]
  }
]
"@

Set-Content -Path "manifest.json" -Value $manifestContent

Write-Host "Build completed successfully!"
Write-Host "Zip file: $zipFile"
Write-Host "Manifest: manifest.json"
Write-Host "Checksum: $hash"
