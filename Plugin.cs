using System;
using System.Collections.Generic;
using Jellyfin.Plugin.CollectionImageGenerator.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.CollectionImageGenerator
{
    /// <summary>
    /// The main plugin class.
    /// </summary>
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Plugin"/> class.
        /// </summary>
        /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
        /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
        }

        /// <inheritdoc />
        public override string Name => "Collection Image Generator";

        /// <inheritdoc />
        public override Guid Id => Guid.Parse("e29b0e3d-f15e-47b9-9b3d-ed3df892e33d");

        /// <summary>
        /// Gets the current plugin instance.
        /// </summary>
        public static Plugin? Instance { get; private set; }

        /// <summary>
        /// Gets or sets the plugin configuration.
        /// </summary>
        public new PluginConfiguration Configuration
        {
            get => base.Configuration;
            set => base.Configuration = value;
        }

        /// <inheritdoc />
        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = Name,
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.configPage.html",
                }
            };
        }
    }
}
