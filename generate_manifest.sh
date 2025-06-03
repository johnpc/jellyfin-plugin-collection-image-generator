#!/bin/bash

# Configuration
REPO="johnpc/jellyfin-plugin-collection-image-generator"
OUTPUT_FILE="manifest.json"
TEMP_DIR=$(mktemp -d)
GITHUB_API_URL="https://api.github.com/repos/$REPO/releases"

echo "Fetching releases from $REPO..."
RELEASES=$(curl -s "$GITHUB_API_URL")

# Check if GitHub API request was successful
if [[ $RELEASES == *"API rate limit exceeded"* ]]; then
  echo "Error: GitHub API rate limit exceeded. Try again later or use a token."
  exit 1
fi

# Extract release tags
echo "Processing releases..."
RELEASE_TAGS=$(echo "$RELEASES" | grep -o '"tag_name": "[^"]*' | sed 's/"tag_name": "//')

# Download the first manifest to get the base structure
FIRST_TAG=$(echo "$RELEASE_TAGS" | head -n 1)
FIRST_MANIFEST_URL="https://github.com/$REPO/releases/download/$FIRST_TAG/manifest.json"
FIRST_MANIFEST=$(curl -s -L -H "Accept: application/json" "$FIRST_MANIFEST_URL")

if [[ -z "$FIRST_MANIFEST" || "$FIRST_MANIFEST" == "Not Found" ]]; then
  echo "Error: Could not fetch first manifest from $FIRST_MANIFEST_URL"
  exit 1
fi

# Extract the base structure (everything except versions)
BASE_MANIFEST=$(echo "$FIRST_MANIFEST" | jq '.[0] | del(.versions)')

# Process each release to collect all versions
ALL_VERSIONS="[]"
for TAG in $RELEASE_TAGS; do
  echo "Processing release $TAG..."
  MANIFEST_URL="https://github.com/$REPO/releases/download/$TAG/manifest.json"
  
  # Download the manifest for this release
  RELEASE_MANIFEST=$(curl -s -L -H "Accept: application/json" "$MANIFEST_URL")
  
  if [[ -z "$RELEASE_MANIFEST" || "$RELEASE_MANIFEST" == "Not Found" ]]; then
    echo "Warning: Could not fetch manifest for $TAG, skipping..."
    continue
  fi
  
  # Extract the version entry from the release manifest
  VERSION_ENTRY=$(echo "$RELEASE_MANIFEST" | jq '.[0].versions[0]')
  
  if [[ "$VERSION_ENTRY" == "null" || -z "$VERSION_ENTRY" ]]; then
    echo "Warning: No valid version entry found in manifest for $TAG, skipping..."
    continue
  fi
  
  # Add this version to our collection
  if [[ "$ALL_VERSIONS" == "[]" ]]; then
    ALL_VERSIONS="[$VERSION_ENTRY]"
  else
    ALL_VERSIONS=$(echo "$ALL_VERSIONS" | jq ". + [$VERSION_ENTRY]")
  fi
done

# Sort versions by semver (highest to lowest)
echo "Sorting versions by semantic versioning (highest to lowest)..."
SORTED_VERSIONS=$(echo "$ALL_VERSIONS" | jq 'sort_by(.version | split(".") | map(tonumber)) | reverse')

# Create the final manifest by combining the base with sorted versions
FINAL_MANIFEST=$(echo "$BASE_MANIFEST" | jq --argjson versions "$SORTED_VERSIONS" '. + {versions: $versions}')

# Format as an array with one object (to match the example format)
FINAL_MANIFEST="[$FINAL_MANIFEST]"

# Pretty print the JSON
echo "$FINAL_MANIFEST" | jq '.' > "$OUTPUT_FILE"

echo "Generated manifest.json with $(echo "$SORTED_VERSIONS" | jq 'length') versions (sorted by semver)"
echo "Output saved to $OUTPUT_FILE"

# Clean up
rm -rf "$TEMP_DIR"
