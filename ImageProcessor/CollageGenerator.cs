using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Jellyfin.Plugin.CollectionImageGenerator.ImageProcessor
{
    /// <summary>
    /// Composes collage images from a set of source poster images.
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
        [ExcludeFromCodeCoverage]
        public async Task CreateCollageAsync(List<string> imagePaths, string outputPath, CancellationToken cancellationToken)
        {
            try
            {
                var imageCount = imagePaths.Count;

                const int targetWidth = 1000;
                const int targetHeight = 1500;
                const int padding = 20;

                using var outputImage = new Image<Rgba32>(targetWidth, targetHeight);

                var backgroundColor = await GetDynamicBackgroundColorAsync(imagePaths, cancellationToken).ConfigureAwait(false);
                outputImage.Mutate(x => x.BackgroundColor(backgroundColor));

                var positions = LayoutCalculator.GetCustomPositions(imageCount, targetWidth, targetHeight, padding);

                for (var i = 0; i < imageCount; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    var imagePath = imagePaths[i];
                    var position = positions[i];

                    try
                    {
                        using var posterImage = await Image.LoadAsync<Rgba32>(imagePath, cancellationToken).ConfigureAwait(false);

                        var gridWidth = position.Width - (padding * 2);
                        var gridHeight = position.Height - (padding * 2);

                        posterImage.Mutate(x => x.Resize(new ResizeOptions
                        {
                            Size = new Size(gridWidth, gridHeight),
                            Mode = ResizeMode.Max,
                            Position = AnchorPositionMode.Center,
                        }));

                        var centeredX = position.X + padding + ((gridWidth - posterImage.Width) / 2);
                        var centeredY = position.Y + padding + ((gridHeight - posterImage.Height) / 2);

                        outputImage.Mutate(ctx => ctx.DrawImage(posterImage, new Point(centeredX, centeredY), 1f));

                        var borderColor = ColorAnalyzer.GetBorderColor(backgroundColor);
                        var borderThickness = 6f;
                        var borderRect = new RectangleF(
                            centeredX - (borderThickness / 2),
                            centeredY - (borderThickness / 2),
                            posterImage.Width + borderThickness,
                            posterImage.Height + borderThickness);
                        outputImage.Mutate(ctx => ctx.Draw(borderColor, borderThickness, borderRect));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing image {Path}", imagePath);
                    }
                }

                var outputDir = Path.GetDirectoryName(outputPath) ?? string.Empty;
                Directory.CreateDirectory(outputDir);

                await outputImage.SaveAsJpegAsync(outputPath, cancellationToken).ConfigureAwait(false);

                _logger.LogInformation("Successfully generated collage at {Path}", outputPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating collage");
            }
        }

        [ExcludeFromCodeCoverage]
        private async Task<Color> GetDynamicBackgroundColorAsync(List<string> imagePaths, CancellationToken cancellationToken)
        {
            try
            {
                var allColors = new List<Rgba32>();
                var pixelSampleRate = imagePaths.Count > 6 ? 6 : 3;

                foreach (var imagePath in imagePaths)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    try
                    {
                        using var image = await Image.LoadAsync<Rgba32>(imagePath, cancellationToken).ConfigureAwait(false);
                        var sampledColors = ColorAnalyzer.SampleImageColors(image, pixelSampleRate);
                        allColors.AddRange(sampledColors);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error sampling colors from image {Path}", imagePath);
                    }
                }

                if (allColors.Count == 0)
                {
                    return Color.FromRgb(45, 45, 45);
                }

                var clusters = ColorAnalyzer.PerformKMeansClustering(allColors, k: 4);
                return ColorAnalyzer.SelectBackgroundColor(clusters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting dynamic background color");
                return Color.FromRgb(45, 45, 45);
            }
        }
    }
}
