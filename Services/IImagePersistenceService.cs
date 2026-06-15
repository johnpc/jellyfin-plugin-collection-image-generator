using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities.Movies;

namespace Jellyfin.Plugin.CollectionImageGenerator.Services
{
    /// <summary>
    /// Service for saving collage images and updating Jellyfin metadata.
    /// </summary>
    public interface IImagePersistenceService
    {
        /// <summary>
        /// Saves a collage image to the collection's directory and updates Jellyfin metadata.
        /// </summary>
        /// <param name="collection">The collection to update.</param>
        /// <param name="imageData">The image data stream.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task SaveCollectionImageAsync(BoxSet collection, Stream imageData, CancellationToken cancellationToken);
    }
}
