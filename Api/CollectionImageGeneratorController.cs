using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Jellyfin.Plugin.CollectionImageGenerator.Tasks;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.CollectionImageGenerator.Api
{
    /// <summary>
    /// The collection image generator controller.
    /// </summary>
    [ApiController]
    [Authorize(Policy = "DefaultAuthorization")]
    [Route("CollectionImageGenerator")]
    public class CollectionImageGeneratorController : ControllerBase
    {
        private readonly ILibraryManager _libraryManager;
        private readonly CollectionImageGeneratorTask _task;

        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionImageGeneratorController"/> class.
        /// </summary>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        /// <param name="task">Instance of the <see cref="CollectionImageGeneratorTask"/> class.</param>
        public CollectionImageGeneratorController(ILibraryManager libraryManager, CollectionImageGeneratorTask task)
        {
            _libraryManager = libraryManager;
            _task = task;
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
            await _task.ExecuteAsync(new Progress<double>(), default).ConfigureAwait(false);
            return NoContent();
        }
    }
}
