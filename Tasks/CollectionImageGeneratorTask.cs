using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.CollectionImageGenerator.Configuration;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Jellyfin.Plugin.CollectionImageGenerator.Tasks
{
    /// <summary>
    /// Task that generates images for collections.
    /// </summary>
    public class CollectionImageGeneratorTask : IScheduledTask
    {
        private readonly ILogger<CollectionImageGeneratorTask> _logger;
        private readonly ILibraryManager _libraryManager;
        private readonly ICollectionManager _collectionManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionImageGeneratorTask"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger{CollectionImageGeneratorTask}"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        /// <param name="collectionManager">Instance of the <see cref="ICollectionManager"/> interface.</param>
        public CollectionImageGeneratorTask(
            ILogger<CollectionImageGeneratorTask> logger,
            ILibraryManager libraryManager,
            ICollectionManager collectionManager)
        {
            _logger = logger;
            _libraryManager = libraryManager;
            _collectionManager = collectionManager;
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
            var collections = _libraryManager.GetItemList(new MediaBrowser.Controller.Entities.InternalItemsQuery
            {
                IncludeItemTypes = new[] { BaseItemKind.BoxSet }
            });

            var totalCollections = collections.Count;
            var processedCount = 0;
            
            foreach (var collection in collections)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    var boxSet = (BoxSet)collection;
                    
                    // Check if collection already has an image
                    if (string.IsNullOrEmpty(boxSet.PrimaryImagePath))
                    {
                        _logger.LogInformation("Generating image for collection: {Name}", boxSet.Name);
                        
                        // Get items in the collection
                        var collectionItems = boxSet.GetLinkedChildren();
                        var itemsWithImages = collectionItems
                            .Where(i => !string.IsNullOrEmpty(i.PrimaryImagePath) && File.Exists(i.PrimaryImagePath))
                            .ToList();

                        if (itemsWithImages.Count > 0)
                        {
                            // Take a sample of items for the collage
                            var sampleSize = Math.Min(config.MaxImagesInCollage, itemsWithImages.Count);
                            var sampleItems = itemsWithImages
                                .OrderBy(_ => Guid.NewGuid()) // Randomize the order
                                .Take(sampleSize)
                                .ToList();

                            // Generate and save the collage
                            await GenerateAndSaveCollageAsync(boxSet, sampleItems, cancellationToken).ConfigureAwait(false);
                        }
                        else
                        {
                            _logger.LogInformation("No items with images found in collection: {Name}", boxSet.Name);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing collection {Name}", collection.Name);
                }

                processedCount++;
                progress.Report(100.0 * processedCount / totalCollections);
            }

            _logger.LogInformation("Collection image generation task completed");
        }

        /// <inheritdoc />
        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            var config = Plugin.Instance!.Configuration;
            
            if (config.EnableScheduledTask)
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

        private async Task GenerateAndSaveCollageAsync(BoxSet collection, List<BaseItem> items, CancellationToken cancellationToken)
        {
            try
            {
                // Determine the layout based on the number of images
                var imageCount = items.Count;
                var (rows, cols) = GetGridDimensions(imageCount);
                
                // Create a new image with appropriate dimensions
                const int targetWidth = 1000;
                const int targetHeight = 1500;
                
                using var outputImage = new Image<Rgba32>(targetWidth, targetHeight);
                
                // Calculate the size of each poster in the grid
                var posterWidth = targetWidth / cols;
                var posterHeight = targetHeight / rows;
                
                // Load and place each poster image
                for (var i = 0; i < imageCount; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }
                    
                    var item = items[i];
                    var row = i / cols;
                    var col = i % cols;
                    
                    try
                    {
                        using var posterImage = await Image.LoadAsync<Rgba32>(item.PrimaryImagePath, cancellationToken);
                        
                        // Resize the poster to fit in the grid
                        posterImage.Mutate(x => x.Resize(posterWidth, posterHeight));
                        
                        // Calculate position
                        var x = col * posterWidth;
                        var y = row * posterHeight;
                        
                        // Draw the poster onto the output image
                        outputImage.Mutate(ctx => ctx.DrawImage(posterImage, new Point(x, y), 1f));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing image for item {Name}", item.Name);
                    }
                }
                
                // Save the collage as the collection's primary image
                var directory = Path.GetDirectoryName(collection.Path);
                var filename = $"folder{Path.DirectorySeparatorChar}poster.jpg";
                var outputPath = Path.Combine(directory!, filename);
                
                // Ensure the directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
                
                // Save the image
                await outputImage.SaveAsJpegAsync(outputPath, cancellationToken);
                
                // Refresh the collection to use the new image
                await collection.RefreshMetadata(cancellationToken).ConfigureAwait(false);
                
                _logger.LogInformation("Successfully generated collage for collection: {Name}", collection.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating collage for collection {Name}", collection.Name);
            }
        }

        private static (int rows, int cols) GetGridDimensions(int count)
        {
            return count switch
            {
                1 => (1, 1),
                2 => (1, 2),
                3 => (1, 3),
                4 => (2, 2),
                5 => (2, 3), // 2x3 with one empty space
                6 => (2, 3),
                7 => (3, 3), // 3x3 with two empty spaces
                8 => (3, 3), // 3x3 with one empty space
                9 => (3, 3),
                _ => (3, 3), // Default to 3x3 for any larger number
            };
        }
    }
}
