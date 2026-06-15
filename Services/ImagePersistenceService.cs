using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.CollectionImageGenerator.Services
{
    /// <summary>
    /// Saves collage images to the filesystem and updates Jellyfin collection metadata.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ImagePersistenceService : IImagePersistenceService
    {
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger<ImagePersistenceService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImagePersistenceService"/> class.
        /// </summary>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        /// <param name="logger">Instance of the <see cref="ILogger{ImagePersistenceService}"/> interface.</param>
        public ImagePersistenceService(
            ILibraryManager libraryManager,
            ILogger<ImagePersistenceService> logger)
        {
            _libraryManager = libraryManager;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task SaveCollectionImageAsync(BoxSet collection, Stream imageData, CancellationToken cancellationToken)
        {
            var outputPath = Path.Combine(collection.Path, "folder", "poster.jpg");
            var folderPath = Path.GetDirectoryName(outputPath) ?? string.Empty;
            Directory.CreateDirectory(folderPath);

            using (var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
            {
                await imageData.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
            }

            _logger.LogDebug("Saved collage image to {Path}", outputPath);

            collection.SetImage(
                new ItemImageInfo
                {
                    Path = outputPath,
                    Type = ImageType.Primary,
                },
                0);

            await _libraryManager.UpdateItemAsync(
                collection,
                collection.GetParent(),
                ItemUpdateType.ImageUpdate,
                cancellationToken).ConfigureAwait(false);

            await collection.RefreshMetadata(cancellationToken).ConfigureAwait(false);
            await collection.UpdateToRepositoryAsync(ItemUpdateType.ImageUpdate, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Set primary image for collection {Name}", collection.Name);
        }
    }
}
