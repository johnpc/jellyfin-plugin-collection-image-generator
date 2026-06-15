using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.CollectionImageGenerator.ImageProcessor;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.CollectionImageGenerator.Services
{
    /// <summary>
    /// Service that wraps CollageGenerator to produce collage images.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class CollageGeneratorService : ICollageGeneratorService
    {
        private readonly CollageGenerator _collageGenerator;

        /// <summary>
        /// Initializes a new instance of the <see cref="CollageGeneratorService"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger{CollageGeneratorService}"/> interface.</param>
        public CollageGeneratorService(ILogger<CollageGeneratorService> logger)
        {
            _collageGenerator = new CollageGenerator(logger);
        }

        /// <inheritdoc />
        public async Task<Stream> GenerateCollageAsync(List<string> imagePaths, CancellationToken cancellationToken)
        {
            var tempPath = Path.Combine(Path.GetTempPath(), $"collage_{Path.GetRandomFileName()}.jpg");
            try
            {
                await _collageGenerator.CreateCollageAsync(imagePaths, tempPath, cancellationToken).ConfigureAwait(false);

                var memoryStream = new MemoryStream();
                using (var fileStream = File.OpenRead(tempPath))
                {
                    await fileStream.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);
                }

                memoryStream.Position = 0;
                return memoryStream;
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
        }
    }
}
