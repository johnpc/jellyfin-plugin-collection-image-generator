# Jellyfin Collection Image Generator

A Jellyfin plugin that automatically generates collage images for collections without images.

## Features

- Automatically scans for collections without images
- Creates collage images from movie/show posters within each collection
- Runs as a scheduled task once per day
- Configurable through the Jellyfin admin dashboard
- Supports manual execution through the configuration page

## Installation

1. Download the latest release from the [GitHub releases page](https://github.com/jellyfin-collection-image-generator/jellyfin-collection-image-generator/releases)
2. In the Jellyfin dashboard, go to Dashboard → Plugins → Catalog
3. Click on "..." and select "Install from file"
4. Select the downloaded .zip file
5. Restart Jellyfin

## Configuration

After installation, you can configure the plugin by going to Dashboard → Plugins → Collection Image Generator:

- **Maximum Images in Collage**: Set the maximum number of images to include in the collage (1-9)
- **Scheduled Task Time**: Set the time of day to run the scheduled task (24-hour format)
- **Enable Scheduled Task**: Toggle to enable or disable the daily scheduled task
- **Generate Collection Images Now**: Button to manually trigger the image generation process

## How It Works

The plugin:
1. Scans your Jellyfin library for collections without primary images
2. For each collection without an image, it selects a random sample of items from the collection
3. Creates a collage from the poster images of those items
4. Sets the collage as the collection's primary image

## Building from Source

### Prerequisites

- .NET 6.0 SDK or later
- Visual Studio 2022 or other compatible IDE

### Build Steps

1. Clone the repository
2. Open the solution in Visual Studio
3. Build the solution in Release mode
4. The plugin DLL will be in the `bin/Release/net6.0` directory

### Creating a Release

The repository includes scripts to help create releases:

- For Windows: `.\build.ps1 [version]`
- For macOS/Linux: `./build.sh [version]`

These scripts will:
1. Build the plugin
2. Create a zip file with the necessary files
3. Generate a manifest.json file for the Jellyfin plugin catalog

Alternatively, you can create a GitHub release by:
1. Tagging a commit with your version number (e.g., `1.0.0.0`)
2. Pushing the tag to GitHub
3. The GitHub Action will automatically build and create a release

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Acknowledgments

- Built for the Jellyfin community
- Uses SixLabors.ImageSharp for image processing
