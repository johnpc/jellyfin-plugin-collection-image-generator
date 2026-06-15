using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.CollectionImageGenerator.Services
{
    /// <summary>
    /// Service for generating collage images from a set of source images.
    /// </summary>
    public interface ICollageGeneratorService
    {
        /// <summary>
        /// Generates a collage image from the given image paths.
        /// </summary>
        /// <param name="imagePaths">The paths to the source images.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A stream containing the generated collage image.</returns>
        Task<Stream> GenerateCollageAsync(List<string> imagePaths, CancellationToken cancellationToken);
    }
}
