using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.CollectionImageGenerator.Services;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.CollectionImageGenerator.Tasks
{
    /// <summary>
    /// Task that generates images for collections.
    /// </summary>
    public class CollectionImageGeneratorTask : IScheduledTask
    {
        private readonly ILogger<CollectionImageGeneratorTask> _logger;
        private readonly ILibraryManager _libraryManager;
        private readonly ICollageGeneratorService _collageGeneratorService;
        private readonly IImagePersistenceService _imagePersistenceService;

        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionImageGeneratorTask"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger{CollectionImageGeneratorTask}"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        /// <param name="collageGeneratorService">Instance of the <see cref="ICollageGeneratorService"/> interface.</param>
        /// <param name="imagePersistenceService">Instance of the <see cref="IImagePersistenceService"/> interface.</param>
        public CollectionImageGeneratorTask(
            ILogger<CollectionImageGeneratorTask> logger,
            ILibraryManager libraryManager,
            ICollageGeneratorService collageGeneratorService,
            IImagePersistenceService imagePersistenceService)
        {
            _logger = logger;
            _libraryManager = libraryManager;
            _collageGeneratorService = collageGeneratorService;
            _imagePersistenceService = imagePersistenceService;
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
        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting collection image generation task");

            var config = Plugin.Instance!.Configuration;
            var collections = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { BaseItemKind.BoxSet },
            });

            var totalCollections = collections.Count;
            var processedCount = 0;
            var skippedCount = 0;
            var generatedCount = 0;

            _logger.LogInformation("Found {Total} collections to evaluate", totalCollections);

            foreach (var collection in collections)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    var boxSet = (BoxSet)collection;

                    if (!string.IsNullOrEmpty(boxSet.PrimaryImagePath))
                    {
                        skippedCount++;
                    }
                    else
                    {
                        await GenerateImageForCollectionAsync(boxSet, config.MaxImagesInCollage, cancellationToken).ConfigureAwait(false);
                        generatedCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing collection {Name}", collection.Name);
                }

                processedCount++;
                progress.Report(100.0 * processedCount / totalCollections);
            }

            _logger.LogInformation(
                "Collection image generation task completed. Found {Total} collections, {Skipped} already have images, {Generated} generated",
                totalCollections,
                skippedCount,
                generatedCount);
        }

        /// <inheritdoc />
        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            var config = Plugin.Instance!.Configuration;

            if (!config.EnableScheduledTask)
            {
                yield break;
            }

            if (TimeSpan.TryParse(config.ScheduledTaskTimeOfDay, out var time))
            {
                yield return new TaskTriggerInfo
                {
                    Type = TaskTriggerInfoType.DailyTrigger,
                    TimeOfDayTicks = time.Ticks,
                };
            }
            else
            {
                yield return new TaskTriggerInfo
                {
                    Type = TaskTriggerInfoType.DailyTrigger,
                    TimeOfDayTicks = TimeSpan.FromHours(3).Ticks,
                };
            }
        }

        /// <summary>
        /// Selects sample items from a collection for collage generation.
        /// </summary>
        /// <param name="itemsWithImages">Items that have valid primary images.</param>
        /// <param name="maxImages">Maximum number of images to include.</param>
        /// <returns>The selected items for the collage.</returns>
        internal static List<BaseItem> SelectSampleItems(List<BaseItem> itemsWithImages, int maxImages)
        {
            var sampleSize = Math.Min(maxImages, itemsWithImages.Count);
            var sampleItems = itemsWithImages
                .OrderBy(_ => Guid.NewGuid())
                .Take(sampleSize)
                .ToList();

            while (sampleItems.Count > 1 && sampleItems.Count < 4)
            {
                sampleItems.Add(sampleItems[sampleItems.Count % itemsWithImages.Count]);
            }

            return sampleItems;
        }

        private async Task GenerateImageForCollectionAsync(BoxSet boxSet, int maxImages, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Generating image for collection: {Name}", boxSet.Name);

            var collectionItems = boxSet.GetLinkedChildren().ToList();
            var itemsWithImages = collectionItems
                .Where(i => !string.IsNullOrEmpty(i.PrimaryImagePath) && File.Exists(i.PrimaryImagePath))
                .ToList();

            _logger.LogDebug("Collection {Name} has {Total} items, {WithImages} with valid images", boxSet.Name, collectionItems.Count, itemsWithImages.Count);

            if (itemsWithImages.Count == 0)
            {
                _logger.LogInformation("No items with images found in collection: {Name}", boxSet.Name);
                return;
            }

            var sampleItems = SelectSampleItems(itemsWithImages, maxImages);
            var imagePaths = sampleItems.Select(i => i.PrimaryImagePath!).ToList();

            _logger.LogDebug("Selected {Count} items for collage in collection {Name}", sampleItems.Count, boxSet.Name);

            using var collageStream = await _collageGeneratorService.GenerateCollageAsync(imagePaths, cancellationToken).ConfigureAwait(false);
            await _imagePersistenceService.SaveCollectionImageAsync(boxSet, collageStream, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Successfully generated collage for collection: {Name}", boxSet.Name);
        }
    }
}
