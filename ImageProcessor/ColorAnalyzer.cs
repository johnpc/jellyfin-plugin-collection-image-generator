using System;
using System.Collections.Generic;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Jellyfin.Plugin.CollectionImageGenerator.ImageProcessor
{
    /// <summary>
    /// Analyzes image colors using k-means clustering for dynamic background selection.
    /// </summary>
    internal static class ColorAnalyzer
    {
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

                if (brightness < 40)
                {
                    continue;
                }

                if (brightness > 230)
                {
                    continue;
                }

                return Color.FromRgb(r, g, b);
            }

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
    }
}
