using System;
using System.Collections.Generic;
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
        public async Task CreateCollageAsync(List<string> imagePaths, string outputPath, CancellationToken cancellationToken)
        {
            try
            {
                // Determine the layout based on the number of images
                var imageCount = imagePaths.Count;
                
                // Create a new image with appropriate dimensions
                const int targetWidth = 1000;
                const int targetHeight = 1500;
                const int padding = 20; // Padding around each image
                
                using var outputImage = new Image<Rgba32>(targetWidth, targetHeight);
                
                // Get dynamic background color from the input images
                var backgroundColor = await GetDynamicBackgroundColorAsync(imagePaths, cancellationToken).ConfigureAwait(false);
                
                // Fill background with the dynamic color
                outputImage.Mutate(x => x.BackgroundColor(backgroundColor));
                
                // Get custom layout positions for this image count
                var positions = GetCustomPositions(imageCount, targetWidth, targetHeight, padding);
                
                // Load and place each poster image
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
                        
                        // Use consistent grid size for all items (same width and height)
                        var gridWidth = position.Width - (padding * 2);
                        var gridHeight = position.Height - (padding * 2);
                        
                        // Resize the poster to fit within grid size without cropping
                        posterImage.Mutate(x => x.Resize(new ResizeOptions
                        {
                            Size = new Size(gridWidth, gridHeight),
                            Mode = ResizeMode.Max,
                            Position = AnchorPositionMode.Center
                        }));
                        
                        // Center the resized image within the grid space
                        var centeredX = position.X + padding + (gridWidth - posterImage.Width) / 2;
                        var centeredY = position.Y + padding + (gridHeight - posterImage.Height) / 2;
                        var posX = centeredX;
                        var posY = centeredY;
                        
                        // Draw the poster onto the output image
                        outputImage.Mutate(ctx => ctx.DrawImage(posterImage, new Point(posX, posY), 1f));
                        
                        // Add border around the image based on background brightness
                        var borderColor = GetBorderColor(backgroundColor);
                        var borderThickness = 6f;
                        var borderRect = new RectangleF(posX - borderThickness/2, posY - borderThickness/2, 
                                                      posterImage.Width + borderThickness, posterImage.Height + borderThickness);
                        outputImage.Mutate(ctx => ctx.Draw(borderColor, borderThickness, borderRect));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing image {Path}", imagePath);
                    }
                }
                
                // Ensure the directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
                
                // Save the image
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
        private static List<(int X, int Y, int Width, int Height)> GetCustomPositions(int count, int canvasWidth, int canvasHeight, int padding)
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
        /// Gets cell dimensions for a 3x3 grid layout.
        /// </summary>
        private static (int width, int height) Get3x3CellSize(int canvasWidth, int canvasHeight, int padding)
        {
            return ((canvasWidth - (padding * 4)) / 3, (canvasHeight - (padding * 4)) / 3);
        }

        /// <summary>
        /// Gets cell dimensions for a 2x2 grid layout.
        /// </summary>
        private static (int width, int height) Get2x2CellSize(int canvasWidth, int canvasHeight, int padding)
        {
            return ((canvasWidth - (padding * 3)) / 2, (canvasHeight - (padding * 3)) / 2);
        }

        /// <summary>
        /// Adds a row of images with standard grid spacing.
        /// </summary>
        private static void AddStandardRow(List<(int X, int Y, int Width, int Height)> positions, 
            int rowIndex, int startCol, int itemCount, int cellWidth, int cellHeight, int padding)
        {
            var rowY = padding + rowIndex * (cellHeight + padding);
            for (var i = 0; i < itemCount; i++)
            {
                var colX = padding + (startCol + i) * (cellWidth + padding);
                positions.Add((colX, rowY, cellWidth, cellHeight));
            }
        }

        /// <summary>
        /// Adds a row of images centered horizontally.
        /// </summary>
        private static void AddCenteredRow(List<(int X, int Y, int Width, int Height)> positions,
            int rowIndex, int itemCount, int cellWidth, int cellHeight, int padding, int canvasWidth)
        {
            var rowY = padding + rowIndex * (cellHeight + padding);
            var centerOffset = (cellWidth + padding) / 2;
            var startX = padding + centerOffset;
            
            for (var i = 0; i < itemCount; i++)
            {
                var colX = startX + i * (cellWidth + padding);
                positions.Add((colX, rowY, cellWidth, cellHeight));
            }
        }

        /// <summary>
        /// Single image centered with padding.
        /// </summary>
        private static List<(int X, int Y, int Width, int Height)> GetSingleImageLayout(int canvasWidth, int canvasHeight, int padding)
        {
            return new List<(int X, int Y, int Width, int Height)>
            {
                (padding, padding, canvasWidth - (padding * 2), canvasHeight - (padding * 2))
            };
        }

        /// <summary>
        /// Diagonal arrangement with 1/8 canvas width inward spacing.
        /// </summary>
        private static List<(int X, int Y, int Width, int Height)> GetDiagonalLayout(int canvasWidth, int canvasHeight, int padding)
        {
            var (width, height) = Get2x2CellSize(canvasWidth, canvasHeight, padding);
            var eighthWidth = canvasWidth / 8;
            
            return new List<(int X, int Y, int Width, int Height)>
            {
                (padding + eighthWidth, padding, width, height),
                (canvasWidth - width - padding - eighthWidth, canvasHeight - height - padding, width, height)
            };
        }

        /// <summary>
        /// Triangular arrangement - 1 top centered, 2 bottom.
        /// </summary>
        private static List<(int X, int Y, int Width, int Height)> GetTriangularLayout(int canvasWidth, int canvasHeight, int padding)
        {
            var (width, height) = Get2x2CellSize(canvasWidth, canvasHeight, padding);
            var topCenterX = (canvasWidth - width) / 2;
            
            return new List<(int X, int Y, int Width, int Height)>
            {
                (topCenterX, padding, width, height), // Top center
                (padding, padding * 2 + height, width, height), // Bottom left
                (padding * 2 + width, padding * 2 + height, width, height) // Bottom right
            };
        }

        /// <summary>
        /// Standard 2x2 grid layout.
        /// </summary>
        private static List<(int X, int Y, int Width, int Height)> GetQuadLayout(int canvasWidth, int canvasHeight, int padding)
        {
            var (width, height) = Get2x2CellSize(canvasWidth, canvasHeight, padding);
            var positions = new List<(int X, int Y, int Width, int Height)>();
            
            AddStandardRow(positions, 0, 0, 2, width, height, padding);
            AddStandardRow(positions, 1, 0, 2, width, height, padding);
            
            return positions;
        }

        /// <summary>
        /// 2-1-2 arrangement: top row 2 images, middle row 1 centered, bottom row 2 images.
        /// </summary>
        private static List<(int X, int Y, int Width, int Height)> GetLayout_2_1_2(int canvasWidth, int canvasHeight, int padding)
        {
            var (width, height) = Get3x3CellSize(canvasWidth, canvasHeight, padding);
            var positions = new List<(int X, int Y, int Width, int Height)>();
            
            // Top row (2 images centered)
            AddCenteredRow(positions, 0, 2, width, height, padding, canvasWidth);
            
            // Middle row (1 centered image)
            var centerX = (canvasWidth - width) / 2;
            var middleY = padding * 2 + height;
            positions.Add((centerX, middleY, width, height));
            
            // Bottom row (2 images centered)
            AddCenteredRow(positions, 2, 2, width, height, padding, canvasWidth);
            
            return positions;
        }

        /// <summary>
        /// 2-2-2 arrangement (3 rows, 2 columns each) with wider spacing in middle row.
        /// </summary>
        private static List<(int X, int Y, int Width, int Height)> GetLayout_2_2_2(int canvasWidth, int canvasHeight, int padding)
        {
            var (width, height) = Get3x3CellSize(canvasWidth, canvasHeight, padding);
            var positions = new List<(int X, int Y, int Width, int Height)>();
            
            // Top row (2 images centered)
            AddCenteredRow(positions, 0, 2, width, height, padding, canvasWidth);
            
            // Middle row (2 images with wider spacing)
            var middleRowSpacing = (int)(width * 0.4f);
            var middleStartX = (canvasWidth - (width * 2 + middleRowSpacing)) / 2;
            var middleY = padding * 2 + height;
            positions.Add((middleStartX, middleY, width, height));
            positions.Add((middleStartX + width + middleRowSpacing, middleY, width, height));
            
            // Bottom row (2 images centered)
            AddCenteredRow(positions, 2, 2, width, height, padding, canvasWidth);
            
            return positions;
        }

        /// <summary>
        /// 2-3-2 arrangement with consistent cell sizes.
        /// </summary>
        private static List<(int X, int Y, int Width, int Height)> GetLayout_2_3_2(int canvasWidth, int canvasHeight, int padding)
        {
            var (width, height) = Get3x3CellSize(canvasWidth, canvasHeight, padding);
            var positions = new List<(int X, int Y, int Width, int Height)>();
            
            // Top row (2 images centered)
            AddCenteredRow(positions, 0, 2, width, height, padding, canvasWidth);
            
            // Middle row (3 images)
            AddStandardRow(positions, 1, 0, 3, width, height, padding);
            
            // Bottom row (2 images centered)
            AddCenteredRow(positions, 2, 2, width, height, padding, canvasWidth);
            
            return positions;
        }

        /// <summary>
        /// 3-2-3 arrangement with consistent cell sizes.
        /// </summary>
        private static List<(int X, int Y, int Width, int Height)> GetLayout_3_2_3(int canvasWidth, int canvasHeight, int padding)
        {
            var (width, height) = Get3x3CellSize(canvasWidth, canvasHeight, padding);
            var positions = new List<(int X, int Y, int Width, int Height)>();
            
            // Top row (3 images)
            AddStandardRow(positions, 0, 0, 3, width, height, padding);
            
            // Middle row (2 images centered)
            AddCenteredRow(positions, 1, 2, width, height, padding, canvasWidth);
            
            // Bottom row (3 images)
            AddStandardRow(positions, 2, 0, 3, width, height, padding);
            
            return positions;
        }

        /// <summary>
        /// Standard 3x3 grid for 9+ images with consistent padding.
        /// </summary>
        private static List<(int X, int Y, int Width, int Height)> GetStandardGridLayout(int count, int canvasWidth, int canvasHeight, int padding)
        {
            var (width, height) = Get3x3CellSize(canvasWidth, canvasHeight, padding);
            var positions = new List<(int X, int Y, int Width, int Height)>();
            
            for (var i = 0; i < Math.Min(count, 9); i++)
            {
                var row = i / 3;
                var col = i % 3;
                var x = padding + col * (width + padding);
                var y = padding + row * (height + padding);
                positions.Add((x, y, width, height));
            }
            
            return positions;
        }

        /// <summary>
        /// Extracts dynamic background color from input images using k-means clustering.
        /// </summary>
        private async Task<Color> GetDynamicBackgroundColorAsync(List<string> imagePaths, CancellationToken cancellationToken)
        {
            try
            {
                var allColors = new List<Rgba32>();
                
                // Sample colors from all images in the collection
                // For large collections, we'll sample fewer pixels per image to maintain performance
                var pixelSampleRate = imagePaths.Count > 6 ? 6 : 3; // Sample every 6th pixel for large collections, every 3rd for smaller ones
                
                foreach (var imagePath in imagePaths)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                        
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
                    return Color.FromRgb(45, 45, 45); // Fallback neutral dark color
                }
                
                // Perform k-means clustering to find dominant colors
                var clusters = PerformKMeansClustering(allColors, k: 4);
                
                // Choose the best background color from clusters
                var backgroundColor = SelectBackgroundColor(clusters);
                
                return backgroundColor;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting dynamic background color");
                return Color.FromRgb(45, 45, 45); // Fallback neutral dark color
            }
        }

        /// <summary>
        /// Samples colors from an image by resizing and sampling every Nth pixel.
        /// </summary>
        private static List<Rgba32> SampleImageColors(Image<Rgba32> image, int sampleRate = 3)
        {
            var colors = new List<Rgba32>();
            
            // Resize to 100x150px for speed as suggested in TODO
            using var resizedImage = image.Clone();
            resizedImage.Mutate(x => x.Resize(100, 150));
            
            // Sample every Nth pixel to reduce computation (default every 3rd)
            for (var y = 0; y < resizedImage.Height; y += sampleRate)
            {
                for (var x = 0; x < resizedImage.Width; x += sampleRate)
                {
                    var pixel = resizedImage[x, y];
                    
                    // Skip nearly transparent pixels
                    if (pixel.A > 128)
                    {
                        colors.Add(pixel);
                    }
                }
            }
            
            return colors;
        }

        /// <summary>
        /// Performs k-means clustering on color data to find dominant color groups.
        /// </summary>
        private static List<ColorCluster> PerformKMeansClustering(List<Rgba32> colors, int k)
        {
            if (colors.Count == 0 || k <= 0)
                return new List<ColorCluster>();
            
            var clusters = new List<ColorCluster>();
            
            // Initialize cluster centroids deterministically by spreading them evenly
            for (var i = 0; i < k; i++)
            {
                var index = i * colors.Count / k;
                var randomColor = colors[index];
                clusters.Add(new ColorCluster
                {
                    CentroidR = randomColor.R,
                    CentroidG = randomColor.G,
                    CentroidB = randomColor.B,
                    Colors = new List<Rgba32>()
                });
            }
            
            // Perform k-means iterations (max 10 iterations for performance)
            for (var iteration = 0; iteration < 10; iteration++)
            {
                // Clear previous assignments
                foreach (var cluster in clusters)
                {
                    cluster.Colors.Clear();
                }
                
                // Assign each color to the nearest cluster
                foreach (var color in colors)
                {
                    var nearestCluster = clusters
                        .OrderBy(c => ColorDistance(color, c.CentroidR, c.CentroidG, c.CentroidB))
                        .First();
                    nearestCluster.Colors.Add(color);
                }
                
                // Update centroids
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
                
                // If centroids didn't change, we've converged
                if (!hasChanged)
                    break;
            }
            
            return clusters.Where(c => c.Colors.Count > 0).ToList();
        }

        /// <summary>
        /// Calculates the Euclidean distance between a color and centroid values.
        /// </summary>
        private static double ColorDistance(Rgba32 color, byte centroidR, byte centroidG, byte centroidB)
        {
            var rDiff = color.R - centroidR;
            var gDiff = color.G - centroidG;
            var bDiff = color.B - centroidB;
            return Math.Sqrt(rDiff * rDiff + gDiff * gDiff + bDiff * bDiff);
        }

        /// <summary>
        /// Selects the best background color from color clusters.
        /// Skips dark/black clusters and chooses the second most prominent.
        /// </summary>
        private static Color SelectBackgroundColor(List<ColorCluster> clusters)
        {
            if (clusters.Count == 0)
                return Color.FromRgb(45, 45, 45); // Neutral fallback
            
            // Sort clusters by size (most prominent first)
            var sortedClusters = clusters.OrderByDescending(c => c.Colors.Count).ToList();
            
            foreach (var cluster in sortedClusters)
            {
                var r = cluster.CentroidR;
                var g = cluster.CentroidG;
                var b = cluster.CentroidB;
                
                // Skip if the color is too dark/black (common in movie posters)
                var brightness = (r + g + b) / 3.0;
                if (brightness < 40) // Skip very dark colors
                    continue;
                
                // Skip if the color is too bright/white
                if (brightness > 230) // Skip very bright colors
                    continue;
                
                // Found a suitable color
                return Color.FromRgb(r, g, b);
            }
            
            // If all clusters are too extreme, use the second most prominent with adjustment
            if (sortedClusters.Count > 1)
            {
                var secondCluster = sortedClusters[1];
                var r = Math.Max(60, Math.Min(180, (int)secondCluster.CentroidR)); // Clamp to moderate range
                var g = Math.Max(60, Math.Min(180, (int)secondCluster.CentroidG));
                var b = Math.Max(60, Math.Min(180, (int)secondCluster.CentroidB));
                return Color.FromRgb((byte)r, (byte)g, (byte)b);
            }
            
            // Final fallback: neutral color
            return Color.FromRgb(75, 75, 85); // Slightly blue-gray neutral
        }

        /// <summary>
        /// Determines the appropriate border color (black or white) based on background brightness.
        /// </summary>
        private static Color GetBorderColor(Color backgroundColor)
        {
            // Convert to Rgba32 to access color components
            var rgba = backgroundColor.ToPixel<Rgba32>();
            
            // Calculate perceived brightness using the standard luminance formula
            var luminance = (0.299 * rgba.R + 0.587 * rgba.G + 0.114 * rgba.B) / 255.0;
            
            // Use white border for dark backgrounds, black border for light backgrounds
            return luminance < 0.5 ? Color.White : Color.Black;
        }

        /// <summary>
        /// Represents a color cluster for k-means clustering.
        /// </summary>
        private class ColorCluster
        {
            public byte CentroidR { get; set; }
            public byte CentroidG { get; set; }
            public byte CentroidB { get; set; }
            public List<Rgba32> Colors { get; set; } = new();
        }
    }
}
