using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using MediaBrowser.Common.Api;
using MediaBrowser.Model.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.CollectionImageGenerator.Api
{
    /// <summary>
    /// The collection image generator controller.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [ApiController]
    [Route("CollectionImageGenerator")]
    [Authorize(Policy = Policies.RequiresElevation)]
    public class CollectionImageGeneratorController : ControllerBase
    {
        private readonly ITaskManager _taskManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionImageGeneratorController"/> class.
        /// </summary>
        /// <param name="taskManager">Instance of the <see cref="ITaskManager"/> interface.</param>
        public CollectionImageGeneratorController(ITaskManager taskManager)
        {
            _taskManager = taskManager;
        }

        /// <summary>
        /// Run the collection image generator task.
        /// </summary>
        /// <response code="204">Task started successfully.</response>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpPost("Run")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public ActionResult RunTask()
        {
            _taskManager.Execute<Tasks.CollectionImageGeneratorTask>();
            return NoContent();
        }
    }
}
