using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.CollectionImageGenerator.Tasks;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.CollectionImageGenerator.ScheduledTasks
{
    /// <summary>
    /// Scheduled task for generating collection images.
    /// </summary>
    public class CollectionImageGeneratorScheduledTask : IScheduledTask, IConfigurableScheduledTask
    {
        private readonly CollectionImageGeneratorTask _task;
        private readonly ILogger<CollectionImageGeneratorScheduledTask> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionImageGeneratorScheduledTask"/> class.
        /// </summary>
        /// <param name="task">Instance of the <see cref="CollectionImageGeneratorTask"/> class.</param>
        /// <param name="logger">Instance of the <see cref="ILogger{CollectionImageGeneratorScheduledTask}"/> interface.</param>
        public CollectionImageGeneratorScheduledTask(
            CollectionImageGeneratorTask task,
            ILogger<CollectionImageGeneratorScheduledTask> logger)
        {
            _task = task;
            _logger = logger;
        }

        /// <inheritdoc />
        public string Name => _task.Name;

        /// <inheritdoc />
        public string Key => _task.Key;

        /// <inheritdoc />
        public string Description => _task.Description;

        /// <inheritdoc />
        public string Category => _task.Category;

        /// <inheritdoc />
        public bool IsHidden => false;

        /// <inheritdoc />
        public bool IsEnabled => Plugin.Instance!.Configuration.EnableScheduledTask;

        /// <inheritdoc />
        public bool IsLogged => true;

        /// <inheritdoc />
        public Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            return _task.ExecuteAsync(progress, cancellationToken);
        }

        /// <inheritdoc />
        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return _task.GetDefaultTriggers();
        }
    }
}
