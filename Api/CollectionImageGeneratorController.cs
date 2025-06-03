using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Jellyfin.Plugin.CollectionImageGenerator.Tasks;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.CollectionImageGenerator.Api
{
    /// <summary>
    /// The collection image generator controller.
    /// </summary>
    [ApiController]
    [Route("CollectionImageGenerator")]
    public class CollectionImageGeneratorController : ControllerBase
    {
        private readonly ILibraryManager _libraryManager;
        private readonly ICollectionManager _collectionManager;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IProviderManager _providerManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionImageGeneratorController"/> class.
        /// </summary>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        /// <param name="collectionManager">Instance of the <see cref="ICollectionManager"/> interface.</param>
        /// <param name="loggerFactory">Instance of the <see cref="ILoggerFactory"/> interface.</param>
        /// <param name="providerManager">Instance of the <see cref="IProviderManager"/> interface.</param>
        public CollectionImageGeneratorController(
            ILibraryManager libraryManager,
            ICollectionManager collectionManager,
            ILoggerFactory loggerFactory,
            IProviderManager providerManager)
        {
            _libraryManager = libraryManager;
            _collectionManager = collectionManager;
            _loggerFactory = loggerFactory;
            _providerManager = providerManager;
        }

        /// <summary>
        /// Run the collection image generator task.
        /// </summary>
        /// <response code="204">Task started successfully.</response>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpPost("Run")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<ActionResult> RunTask()
        {
            // Create a new instance of the task directly
            var logger = _loggerFactory.CreateLogger<CollectionImageGeneratorTask>();
            var task = new CollectionImageGeneratorTask(logger, _libraryManager, _collectionManager, _providerManager);
            
            await task.ExecuteAsync(new Progress<double>(), default).ConfigureAwait(false);
            return NoContent();
        }
    }
}
