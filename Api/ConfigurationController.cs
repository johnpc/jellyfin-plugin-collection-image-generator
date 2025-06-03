using System;
using Jellyfin.Plugin.CollectionImageGenerator.Configuration;
using MediaBrowser.Common.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.CollectionImageGenerator.Api
{
    /// <summary>
    /// The configuration controller for the Collection Image Generator plugin.
    /// </summary>
    [ApiController]
    [Authorize(Policy = "DefaultAuthorization")]
    [Route("Plugins/CollectionImageGenerator/Configuration")]
    public class ConfigurationController : ControllerBase
    {
        private readonly IConfigurationManager _configurationManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationController"/> class.
        /// </summary>
        /// <param name="configurationManager">Instance of the <see cref="IConfigurationManager"/> interface.</param>
        public ConfigurationController(IConfigurationManager configurationManager)
        {
            _configurationManager = configurationManager;
        }

        /// <summary>
        /// Gets the plugin configuration.
        /// </summary>
        /// <returns>The plugin configuration.</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetConfiguration()
        {
            var config = Plugin.Instance?.Configuration;
            return Ok(config);
        }

        /// <summary>
        /// Updates the plugin configuration.
        /// </summary>
        /// <param name="configuration">The updated configuration.</param>
        /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public IActionResult UpdateConfiguration([FromBody] PluginConfiguration configuration)
        {
            if (Plugin.Instance == null)
            {
                return BadRequest("Plugin instance is not available");
            }

            Plugin.Instance.Configuration = configuration;
            _configurationManager.SaveConfiguration(Plugin.Instance.ConfigurationFileName, configuration);

            return NoContent();
        }
    }
}
