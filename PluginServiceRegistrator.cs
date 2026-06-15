using System.Diagnostics.CodeAnalysis;
using Jellyfin.Plugin.CollectionImageGenerator.Services;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.CollectionImageGenerator
{
    /// <summary>
    /// Registers plugin services with the Jellyfin DI container.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class PluginServiceRegistrator : IPluginServiceRegistrator
    {
        /// <inheritdoc />
        public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
        {
            serviceCollection.AddSingleton<ICollageGeneratorService, CollageGeneratorService>();
            serviceCollection.AddSingleton<IImagePersistenceService, ImagePersistenceService>();
        }
    }
}
