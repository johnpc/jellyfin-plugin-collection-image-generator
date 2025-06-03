using Jellyfin.Plugin.CollectionImageGenerator.Tasks;
using MediaBrowser.Common.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.CollectionImageGenerator
{
    /// <summary>
    /// Register services for the plugin.
    /// </summary>
    public class ServiceRegistration : IPluginServiceRegistrator
    {
        /// <inheritdoc />
        public void RegisterServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<CollectionImageGeneratorTask>();
        }
    }
}
