using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.CollectionImageGenerator.Tasks;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.CollectionImageGenerator.ScheduledTasks
{
    /// <summary>
    /// Scheduled task for generating collection images.
    /// </summary>
    public class CollectionImageGeneratorScheduledTask : IScheduledTask, IConfigurableScheduledTask
    {
        private readonly ILogger<CollectionImageGeneratorScheduledTask> _logger;
        private readonly ILibraryManager _libraryManager;
        private readonly ICollectionManager _collectionManager;
        private readonly IProviderManager _providerManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionImageGeneratorScheduledTask"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger{CollectionImageGeneratorScheduledTask}"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        /// <param name="collectionManager">Instance of the <see cref="ICollectionManager"/> interface.</param>
        /// <param name="providerManager">Instance of the <see cref="IProviderManager"/> interface.</param>
        public CollectionImageGeneratorScheduledTask(
            ILogger<CollectionImageGeneratorScheduledTask> logger,
            ILibraryManager libraryManager,
            ICollectionManager collectionManager,
            IProviderManager providerManager)
        {
            _logger = logger;
            _libraryManager = libraryManager;
            _collectionManager = collectionManager;
            _providerManager = providerManager;
        }

        /// <inheritdoc />
        public string Name => "Generate Collection Images";

        /// <inheritdoc />
        public string Key => "CollectionImageGeneratorTask";

        /// <inheritdoc />
        public string Description => "Generates collage images for collections without images";

        /// <inheritdoc />
        public string Category => "Library";

        /// <inheritdoc />
        public bool IsHidden => false;

        /// <inheritdoc />
        public bool IsEnabled => Plugin.Instance?.Configuration?.EnableScheduledTask ?? false;

        /// <inheritdoc />
        public bool IsLogged => true;

        /// <inheritdoc />
        public Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            // Create a new logger for the task
            var taskLogger = (ILogger<CollectionImageGeneratorTask>)LoggerFactory.Create(builder => 
                builder.AddConsole()).CreateLogger<CollectionImageGeneratorTask>();
                
            var task = new CollectionImageGeneratorTask(taskLogger, _libraryManager, _collectionManager);
            return task.ExecuteAsync(progress, cancellationToken);
        }

        /// <inheritdoc />
        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            var config = Plugin.Instance?.Configuration;
            
            if (config != null && config.EnableScheduledTask)
            {
                // Parse the time of day from configuration
                if (TimeSpan.TryParse(config.ScheduledTaskTimeOfDay, out var time))
                {
                    yield return new TaskTriggerInfo
                    {
                        Type = TaskTriggerInfo.TriggerDaily,
                        TimeOfDayTicks = time.Ticks
                    };
                }
                else
                {
                    // Default to 3 AM if parsing fails
                    yield return new TaskTriggerInfo
                    {
                        Type = TaskTriggerInfo.TriggerDaily,
                        TimeOfDayTicks = TimeSpan.FromHours(3).Ticks
                    };
                }
            }
        }
    }
}
