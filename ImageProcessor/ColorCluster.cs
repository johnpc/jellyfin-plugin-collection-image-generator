using System.Collections.Generic;
using SixLabors.ImageSharp.PixelFormats;

namespace Jellyfin.Plugin.CollectionImageGenerator.ImageProcessor
{
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
