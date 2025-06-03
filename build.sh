#!/bin/bash
# Build script for local testing of the release process on Unix-like systems

# Get version from command line or use default
VERSION=${1:-"1.0.0.0"}

# Build the project
dotnet build --configuration Release /p:TreatWarningsAsErrors=false

# Create release directory
RELEASE_DIR="release"
rm -rf "$RELEASE_DIR"
mkdir -p "$RELEASE_DIR"

# Copy files to release directory
cp "bin/Release/net6.0/Jellyfin.Plugin.CollectionImageGenerator.dll" "$RELEASE_DIR/"
cp "Plugin.xml" "$RELEASE_DIR/"

# Create zip file
ZIP_FILE="collection-image-generator-$VERSION.zip"
rm -f "$ZIP_FILE"

# Compress the release directory
cd "$RELEASE_DIR"
zip -r "../$ZIP_FILE" .
cd ..

# Calculate checksum
if [[ "$OSTYPE" == "darwin"* ]]; then
    # macOS
    CHECKSUM=$(md5 -q "$ZIP_FILE")
else
    # Linux
    CHECKSUM=$(md5sum "$ZIP_FILE" | awk '{ print $1 }')
fi

# Create manifest.json
TIMESTAMP=$(date -u +"%Y-%m-%dT%H:%M:%SZ")
cat > manifest.json << EOF
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
        "version": "$VERSION",
        "changelog": "- Initial release\n",
        "targetAbi": "10.8.0.0",
        "sourceUrl": "https://github.com/jellyfin-collection-image-generator/jellyfin-collection-image-generator/releases/download/$VERSION/collection-image-generator-$VERSION.zip",
        "checksum": "$CHECKSUM",
        "timestamp": "$TIMESTAMP"
      }
    ]
  }
]
EOF

echo "Build completed successfully!"
echo "Zip file: $ZIP_FILE"
echo "Manifest: manifest.json"
echo "Checksum: $CHECKSUM"
