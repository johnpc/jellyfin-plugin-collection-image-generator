using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Jellyfin.Plugin.CollectionImageGenerator.ImageProcessor
{
    /// <summary>
    /// Utility class for generating collage images.
    /// </summary>
    public class CollageGenerator
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CollageGenerator"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
        public CollageGenerator(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Creates a collage from a list of image paths.
        /// </summary>
        /// <param name="imagePaths">The list of image paths to include in the collage.</param>
        /// <param name="outputPath">The path where the collage should be saved.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task CreateCollageAsync(List<string> imagePaths, string outputPath, CancellationToken cancellationToken)
        {
            try
            {
                // Determine the layout based on the number of images
                var imageCount = imagePaths.Count;
                var (rows, cols) = GetGridDimensions(imageCount);
                
                // Create a new image with appropriate dimensions
                const int targetWidth = 1000;
                const int targetHeight = 1500;
                
                using var outputImage = new Image<Rgba32>(targetWidth, targetHeight);
                
                // Calculate the size of each poster in the grid
                var posterWidth = targetWidth / cols;
                var posterHeight = targetHeight / rows;
                
                // Load and place each poster image
                for (var i = 0; i < imageCount; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }
                    
                    var imagePath = imagePaths[i];
                    var row = i / cols;
                    var col = i % cols;
                    
                    try
                    {
                        using var posterImage = await Image.LoadAsync<Rgba32>(imagePath, cancellationToken);
                        
                        // Resize the poster to fit in the grid
                        posterImage.Mutate(x => x.Resize(posterWidth, posterHeight));
                        
                        // Calculate position
                        var x = col * posterWidth;
                        var y = row * posterHeight;
                        
                        // Draw the poster onto the output image
                        outputImage.Mutate(ctx => ctx.DrawImage(posterImage, new Point(x, y), 1f));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing image {Path}", imagePath);
                    }
                }
                
                // Ensure the directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
                
                // Save the image
                await outputImage.SaveAsJpegAsync(outputPath, cancellationToken);
                
                _logger.LogInformation("Successfully generated collage at {Path}", outputPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating collage");
            }
        }

        private static (int rows, int cols) GetGridDimensions(int count)
        {
            return count switch
            {
                1 => (1, 1),
                2 => (1, 2),
                3 => (1, 3),
                4 => (2, 2),
                5 => (2, 3), // 2x3 with one empty space
                6 => (2, 3),
                7 => (3, 3), // 3x3 with two empty spaces
                8 => (3, 3), // 3x3 with one empty space
                9 => (3, 3),
                _ => (3, 3), // Default to 3x3 for any larger number
            };
        }
    }
}
