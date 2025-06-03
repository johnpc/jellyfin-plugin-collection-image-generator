using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.CollectionImageGenerator.Configuration
{
    /// <summary>
    /// Plugin configuration.
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
        /// </summary>
        public PluginConfiguration()
        {
            MaxImagesInCollage = 4;
            ScheduledTaskTimeOfDay = "03:00";
            EnableScheduledTask = true;
        }

        /// <summary>
        /// Gets or sets the maximum number of images to include in the collage.
        /// </summary>
        public int MaxImagesInCollage { get; set; }

        /// <summary>
        /// Gets or sets the time of day to run the scheduled task (24-hour format).
        /// </summary>
        public string ScheduledTaskTimeOfDay { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the scheduled task is enabled.
        /// </summary>
        public bool EnableScheduledTask { get; set; }
    }
}
