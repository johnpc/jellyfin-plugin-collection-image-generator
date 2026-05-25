using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
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

                var positions = GetCustomPositions(imageCount, targetWidth, targetHeight, padding);

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
                        var posX = centeredX;
                        var posY = centeredY;

                        outputImage.Mutate(ctx => ctx.DrawImage(posterImage, new Point(posX, posY), 1f));

                        var borderColor = GetBorderColor(backgroundColor);
                        var borderThickness = 6f;
                        var borderRect = new RectangleF(
                            posX - (borderThickness / 2),
                            posY - (borderThickness / 2),
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

        /// <summary>
        /// Gets custom positions for each image based on the layout type with consistent padding.
        /// </summary>
        /// <param name="count">The number of images to position.</param>
        /// <param name="canvasWidth">The canvas width in pixels.</param>
        /// <param name="canvasHeight">The canvas height in pixels.</param>
        /// <param name="padding">The padding between images in pixels.</param>
        /// <returns>A list of position tuples (X, Y, Width, Height) for each image.</returns>
        internal static List<(int X, int Y, int Width, int Height)> GetCustomPositions(int count, int canvasWidth, int canvasHeight, int padding)
        {
            return count switch
            {
                1 => GetSingleImageLayout(canvasWidth, canvasHeight, padding),
                2 => GetDiagonalLayout(canvasWidth, canvasHeight, padding),
                3 => GetTriangularLayout(canvasWidth, canvasHeight, padding),
                4 => GetQuadLayout(canvasWidth, canvasHeight, padding),
                5 => GetLayout_2_1_2(canvasWidth, canvasHeight, padding),
                6 => GetLayout_2_2_2(canvasWidth, canvasHeight, padding),
                7 => GetLayout_2_3_2(canvasWidth, canvasHeight, padding),
                8 => GetLayout_3_2_3(canvasWidth, canvasHeight, padding),
                _ => GetStandardGridLayout(count, canvasWidth, canvasHeight, padding)
            };
        }

        /// <summary>
        /// Selects the best background color from color clusters.
        /// Skips dark/black clusters and chooses the second most prominent.
        /// </summary>
        /// <param name="clusters">The list of color clusters to select from.</param>
        /// <returns>The selected background color.</returns>
        internal static Color SelectBackgroundColor(List<ColorCluster> clusters)
        {
            if (clusters.Count == 0)
            {
                return Color.FromRgb(45, 45, 45);
            }

            var sortedClusters = clusters.OrderByDescending(c => c.Colors.Count).ToList();

            foreach (var cluster in sortedClusters)
            {
                var r = cluster.CentroidR;
                var g = cluster.CentroidG;
                var b = cluster.CentroidB;

                var brightness = (r + g + b) / 3.0;

                // Skip very dark colors (common in movie posters)
                if (brightness < 40)
                {
                    continue;
                }

                // Skip very bright colors
                if (brightness > 230)
                {
                    continue;
                }

                return Color.FromRgb(r, g, b);
            }

            // If all clusters are too extreme, use the second most prominent with adjustment
            if (sortedClusters.Count > 1)
            {
                var secondCluster = sortedClusters[1];
                var r = Math.Max(60, Math.Min(180, (int)secondCluster.CentroidR));
                var g = Math.Max(60, Math.Min(180, (int)secondCluster.CentroidG));
                var b = Math.Max(60, Math.Min(180, (int)secondCluster.CentroidB));
                return Color.FromRgb((byte)r, (byte)g, (byte)b);
            }

            return Color.FromRgb(75, 75, 85);
        }

        /// <summary>
        /// Determines the appropriate border color (black or white) based on background brightness.
        /// </summary>
        /// <param name="backgroundColor">The background color to evaluate.</param>
        /// <returns>White for dark backgrounds, black for light backgrounds.</returns>
        internal static Color GetBorderColor(Color backgroundColor)
        {
            var rgba = backgroundColor.ToPixel<Rgba32>();
            var luminance = ((0.299 * rgba.R) + (0.587 * rgba.G) + (0.114 * rgba.B)) / 255.0;
            return luminance < 0.5 ? Color.White : Color.Black;
        }

        /// <summary>
        /// Calculates cell size for a 3x3 grid layout.
        /// </summary>
        /// <param name="canvasWidth">The canvas width.</param>
        /// <param name="canvasHeight">The canvas height.</param>
        /// <param name="padding">The padding between cells.</param>
        /// <returns>A tuple of (Width, Height) for each cell.</returns>
        internal static (int Width, int Height) Get3x3CellSize(int canvasWidth, int canvasHeight, int padding)
        {
            return ((canvasWidth - (padding * 4)) / 3, (canvasHeight - (padding * 4)) / 3);
        }

        /// <summary>
        /// Calculates cell size for a 2x2 grid layout.
        /// </summary>
        /// <param name="canvasWidth">The canvas width.</param>
        /// <param name="canvasHeight">The canvas height.</param>
        /// <param name="padding">The padding between cells.</param>
        /// <returns>A tuple of (Width, Height) for each cell.</returns>
        internal static (int Width, int Height) Get2x2CellSize(int canvasWidth, int canvasHeight, int padding)
        {
            return ((canvasWidth - (padding * 3)) / 2, (canvasHeight - (padding * 3)) / 2);
        }

        /// <summary>
        /// Samples colors from an image at the given rate.
        /// </summary>
        /// <param name="image">The image to sample from.</param>
        /// <param name="sampleRate">The pixel sampling rate (every Nth pixel).</param>
        /// <returns>A list of sampled RGBA colors.</returns>
        internal static List<Rgba32> SampleImageColors(Image<Rgba32> image, int sampleRate = 3)
        {
            var colors = new List<Rgba32>();

            using var resizedImage = image.Clone();
            resizedImage.Mutate(x => x.Resize(100, 150));

            for (var y = 0; y < resizedImage.Height; y += sampleRate)
            {
                for (var x = 0; x < resizedImage.Width; x += sampleRate)
                {
                    var pixel = resizedImage[x, y];

                    if (pixel.A > 128)
                    {
                        colors.Add(pixel);
                    }
                }
            }

            return colors;
        }

        /// <summary>
        /// Performs k-means clustering on a list of colors.
        /// </summary>
        /// <param name="colors">The colors to cluster.</param>
        /// <param name="k">The number of clusters.</param>
        /// <returns>A list of color clusters.</returns>
        internal static List<ColorCluster> PerformKMeansClustering(List<Rgba32> colors, int k)
        {
            if (colors.Count == 0 || k <= 0)
            {
                return new List<ColorCluster>();
            }

            var clusters = new List<ColorCluster>();

            for (var i = 0; i < k; i++)
            {
                var index = i * colors.Count / k;
                var seedColor = colors[index];
                clusters.Add(new ColorCluster
                {
                    CentroidR = seedColor.R,
                    CentroidG = seedColor.G,
                    CentroidB = seedColor.B,
                    Colors = new List<Rgba32>(),
                });
            }

            for (var iteration = 0; iteration < 10; iteration++)
            {
                foreach (var cluster in clusters)
                {
                    cluster.Colors.Clear();
                }

                foreach (var color in colors)
                {
                    var nearestCluster = clusters
                        .OrderBy(c => ColorDistance(color, c.CentroidR, c.CentroidG, c.CentroidB))
                        .First();
                    nearestCluster.Colors.Add(color);
                }

                var hasChanged = false;
                foreach (var cluster in clusters)
                {
                    if (cluster.Colors.Count > 0)
                    {
                        var avgR = (byte)cluster.Colors.Average(c => c.R);
                        var avgG = (byte)cluster.Colors.Average(c => c.G);
                        var avgB = (byte)cluster.Colors.Average(c => c.B);

                        if (avgR != cluster.CentroidR || avgG != cluster.CentroidG || avgB != cluster.CentroidB)
                        {
                            hasChanged = true;
                            cluster.CentroidR = avgR;
                            cluster.CentroidG = avgG;
                            cluster.CentroidB = avgB;
                        }
                    }
                }

                if (!hasChanged)
                {
                    break;
                }
            }

            return clusters.Where(c => c.Colors.Count > 0).ToList();
        }

        /// <summary>
        /// Calculates the Euclidean distance between a color and a centroid.
        /// </summary>
        /// <param name="color">The color to measure from.</param>
        /// <param name="centroidR">The red component of the centroid.</param>
        /// <param name="centroidG">The green component of the centroid.</param>
        /// <param name="centroidB">The blue component of the centroid.</param>
        /// <returns>The Euclidean distance in RGB space.</returns>
        internal static double ColorDistance(Rgba32 color, byte centroidR, byte centroidG, byte centroidB)
        {
            var rDiff = color.R - centroidR;
            var gDiff = color.G - centroidG;
            var bDiff = color.B - centroidB;
            return Math.Sqrt((rDiff * rDiff) + (gDiff * gDiff) + (bDiff * bDiff));
        }

        private static void AddStandardRow(
            List<(int X, int Y, int Width, int Height)> positions,
            int rowIndex,
            int startCol,
            int itemCount,
            int cellWidth,
            int cellHeight,
            int padding)
        {
            var rowY = padding + (rowIndex * (cellHeight + padding));
            for (var i = 0; i < itemCount; i++)
            {
                var colX = padding + ((startCol + i) * (cellWidth + padding));
                positions.Add((colX, rowY, cellWidth, cellHeight));
            }
        }

        private static void AddCenteredRow(
            List<(int X, int Y, int Width, int Height)> positions,
            int rowIndex,
            int itemCount,
            int cellWidth,
            int cellHeight,
            int padding,
            int canvasWidth)
        {
            var rowY = padding + (rowIndex * (cellHeight + padding));
            var centerOffset = (cellWidth + padding) / 2;
            var startX = padding + centerOffset;

            for (var i = 0; i < itemCount; i++)
            {
                var colX = startX + (i * (cellWidth + padding));
                positions.Add((colX, rowY, cellWidth, cellHeight));
            }
        }

        private static List<(int X, int Y, int Width, int Height)> GetSingleImageLayout(int canvasWidth, int canvasHeight, int padding)
        {
            return new List<(int X, int Y, int Width, int Height)>
            {
                (padding, padding, canvasWidth - (padding * 2), canvasHeight - (padding * 2)),
            };
        }

        private static List<(int X, int Y, int Width, int Height)> GetDiagonalLayout(int canvasWidth, int canvasHeight, int padding)
        {
            var (width, height) = Get2x2CellSize(canvasWidth, canvasHeight, padding);
            var eighthWidth = canvasWidth / 8;

            return new List<(int X, int Y, int Width, int Height)>
            {
                (padding + eighthWidth, padding, width, height),
                (canvasWidth - width - padding - eighthWidth, canvasHeight - height - padding, width, height),
            };
        }

        private static List<(int X, int Y, int Width, int Height)> GetTriangularLayout(int canvasWidth, int canvasHeight, int padding)
        {
            var (width, height) = Get2x2CellSize(canvasWidth, canvasHeight, padding);
            var topCenterX = (canvasWidth - width) / 2;

            return new List<(int X, int Y, int Width, int Height)>
            {
                (topCenterX, padding, width, height),
                (padding, (padding * 2) + height, width, height),
                ((padding * 2) + width, (padding * 2) + height, width, height),
            };
        }

        private static List<(int X, int Y, int Width, int Height)> GetQuadLayout(int canvasWidth, int canvasHeight, int padding)
        {
            var (width, height) = Get2x2CellSize(canvasWidth, canvasHeight, padding);
            var positions = new List<(int X, int Y, int Width, int Height)>();

            AddStandardRow(positions, 0, 0, 2, width, height, padding);
            AddStandardRow(positions, 1, 0, 2, width, height, padding);

            return positions;
        }

        private static List<(int X, int Y, int Width, int Height)> GetLayout_2_1_2(int canvasWidth, int canvasHeight, int padding)
        {
            var (width, height) = Get3x3CellSize(canvasWidth, canvasHeight, padding);
            var positions = new List<(int X, int Y, int Width, int Height)>();

            AddCenteredRow(positions, 0, 2, width, height, padding, canvasWidth);

            var centerX = (canvasWidth - width) / 2;
            var middleY = (padding * 2) + height;
            positions.Add((centerX, middleY, width, height));

            AddCenteredRow(positions, 2, 2, width, height, padding, canvasWidth);

            return positions;
        }

        private static List<(int X, int Y, int Width, int Height)> GetLayout_2_2_2(int canvasWidth, int canvasHeight, int padding)
        {
            var (width, height) = Get3x3CellSize(canvasWidth, canvasHeight, padding);
            var positions = new List<(int X, int Y, int Width, int Height)>();

            AddCenteredRow(positions, 0, 2, width, height, padding, canvasWidth);

            var middleRowSpacing = (int)(width * 0.4f);
            var middleStartX = (canvasWidth - ((width * 2) + middleRowSpacing)) / 2;
            var middleY = (padding * 2) + height;
            positions.Add((middleStartX, middleY, width, height));
            positions.Add((middleStartX + width + middleRowSpacing, middleY, width, height));

            AddCenteredRow(positions, 2, 2, width, height, padding, canvasWidth);

            return positions;
        }

        private static List<(int X, int Y, int Width, int Height)> GetLayout_2_3_2(int canvasWidth, int canvasHeight, int padding)
        {
            var (width, height) = Get3x3CellSize(canvasWidth, canvasHeight, padding);
            var positions = new List<(int X, int Y, int Width, int Height)>();

            AddCenteredRow(positions, 0, 2, width, height, padding, canvasWidth);
            AddStandardRow(positions, 1, 0, 3, width, height, padding);
            AddCenteredRow(positions, 2, 2, width, height, padding, canvasWidth);

            return positions;
        }

        private static List<(int X, int Y, int Width, int Height)> GetLayout_3_2_3(int canvasWidth, int canvasHeight, int padding)
        {
            var (width, height) = Get3x3CellSize(canvasWidth, canvasHeight, padding);
            var positions = new List<(int X, int Y, int Width, int Height)>();

            AddStandardRow(positions, 0, 0, 3, width, height, padding);
            AddCenteredRow(positions, 1, 2, width, height, padding, canvasWidth);
            AddStandardRow(positions, 2, 0, 3, width, height, padding);

            return positions;
        }

        private static List<(int X, int Y, int Width, int Height)> GetStandardGridLayout(int count, int canvasWidth, int canvasHeight, int padding)
        {
            var (width, height) = Get3x3CellSize(canvasWidth, canvasHeight, padding);
            var positions = new List<(int X, int Y, int Width, int Height)>();

            for (var i = 0; i < Math.Min(count, 9); i++)
            {
                var row = i / 3;
                var col = i % 3;
                var x = padding + (col * (width + padding));
                var y = padding + (row * (height + padding));
                positions.Add((x, y, width, height));
            }

            return positions;
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
                        var sampledColors = SampleImageColors(image, pixelSampleRate);
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

                var clusters = PerformKMeansClustering(allColors, k: 4);
                return SelectBackgroundColor(clusters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting dynamic background color");
                return Color.FromRgb(45, 45, 45);
            }
        }

        /// <summary>
        /// Represents a color cluster for k-means clustering.
        /// </summary>
        internal class ColorCluster
        {
            /// <summary>
            /// Gets or sets the red component of the cluster centroid.
            /// </summary>
            public byte CentroidR { get; set; }

            /// <summary>
            /// Gets or sets the green component of the cluster centroid.
            /// </summary>
            public byte CentroidG { get; set; }

            /// <summary>
            /// Gets or sets the blue component of the cluster centroid.
            /// </summary>
            public byte CentroidB { get; set; }

            /// <summary>
            /// Gets or sets the colors assigned to this cluster.
            /// </summary>
            public List<Rgba32> Colors { get; set; } = new List<Rgba32>();
        }
    }
}
