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
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
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
                        _logger.LogInformation("Generating image for collection: {Name} (ID: {Id})", boxSet.Name, boxSet.Id);
                        _logger.LogInformation("Collection path: {Path}", boxSet.Path);
                        
                        // Get items in the collection
                        var collectionItems = boxSet.GetLinkedChildren();
                        _logger.LogInformation("Collection {Name} has {Count} items", boxSet.Name, collectionItems.Count());
                        
                        var itemsWithImages = collectionItems
                            .Where(i => !string.IsNullOrEmpty(i.PrimaryImagePath) && File.Exists(i.PrimaryImagePath))
                            .ToList();

                        _logger.LogInformation("Collection {Name} has {Count} items with valid images", boxSet.Name, itemsWithImages.Count);
                        
                        if (itemsWithImages.Count > 0)
                        {
                            // Log the first few items with their image paths
                            foreach (var item in itemsWithImages.Take(3))
                            {
                                _logger.LogInformation("Item in collection: {ItemName}, Image path: {ImagePath}", 
                                    item.Name, item.PrimaryImagePath);
                            }
                            
                            // Take a sample of items for the collage
                            var sampleSize = Math.Min(config.MaxImagesInCollage, itemsWithImages.Count);
                            var sampleItems = itemsWithImages
                                .OrderBy(_ => Guid.NewGuid()) // Randomize the order
                                .Take(sampleSize)
                                .ToList();
                            
                            _logger.LogInformation("Selected {Count} items for collage in collection {Name}", 
                                sampleItems.Count, boxSet.Name);

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
                
                _logger.LogInformation("Creating collage with {Count} images in a {Rows}x{Cols} grid for collection {Name}", 
                    imageCount, rows, cols, collection.Name);
                
                // Create a new image with appropriate dimensions
                const int targetWidth = 1000;
                const int targetHeight = 1500;
                
                _logger.LogInformation("Creating output image with dimensions {Width}x{Height}", targetWidth, targetHeight);
                
                using var outputImage = new Image<Rgba32>(targetWidth, targetHeight);
                
                // Calculate the size of each poster in the grid
                var posterWidth = targetWidth / cols;
                var posterHeight = targetHeight / rows;
                
                _logger.LogInformation("Each poster will be sized {Width}x{Height}", posterWidth, posterHeight);
                
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
                        _logger.LogInformation("Loading image for item {ItemName} from {Path}", item.Name, item.PrimaryImagePath);
                        
                        using var posterImage = await Image.LoadAsync<Rgba32>(item.PrimaryImagePath, cancellationToken);
                        
                        // Resize the poster to fit in the grid
                        posterImage.Mutate(x => x.Resize(posterWidth, posterHeight));
                        
                        // Calculate position
                        var x = col * posterWidth;
                        var y = row * posterHeight;
                        
                        _logger.LogInformation("Placing image for {ItemName} at position ({X},{Y})", item.Name, x, y);
                        
                        // Draw the poster onto the output image
                        outputImage.Mutate(ctx => ctx.DrawImage(posterImage, new Point(x, y), 1f));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing image for item {Name}", item.Name);
                    }
                }
                
                // First save the collage to a temporary file
                var tempFile = Path.Combine(Path.GetTempPath(), $"collage_{collection.Id}.jpg");
                _logger.LogInformation("Saving temporary collage to {Path}", tempFile);
                await outputImage.SaveAsJpegAsync(tempFile, cancellationToken);
                
                if (File.Exists(tempFile))
                {
                    _logger.LogInformation("Temporary collage file successfully created at {Path}", tempFile);
                    
                    try
                    {
                        // Save to the file system first
                        var directory = collection.Path;
                        var filename = $"folder{Path.DirectorySeparatorChar}poster.jpg";
                        var outputPath = Path.Combine(directory, filename);
                        
                        _logger.LogInformation("Saving collage to file system at {Path}", outputPath);
                        
                        // Ensure the directory exists
                        var folderPath = Path.GetDirectoryName(outputPath);
                        Directory.CreateDirectory(folderPath!);
                        
                        // Copy the temp file to the final location
                        File.Copy(tempFile, outputPath, true);
                        
                        // Use Jellyfin's provider manager to set the image
                        _logger.LogInformation("Setting primary image for collection {Name} using provider manager", collection.Name);
                        
                        // Read the image file into a byte array
                        byte[] imageBytes = await File.ReadAllBytesAsync(tempFile, cancellationToken);
                        
                        // Set the image using the provider manager
                        await SetCollectionImageAsync(collection, imageBytes, cancellationToken);
                        
                        // Force a refresh of the collection
                        _logger.LogInformation("Refreshing metadata for collection {Name}", collection.Name);
                        await collection.RefreshMetadata(cancellationToken).ConfigureAwait(false);
                        
                        // Try to force the collection to reload its images
                        _logger.LogInformation("Forcing image refresh for collection {Name}", collection.Name);
                        await collection.UpdateToRepositoryAsync(ItemUpdateType.ImageUpdate, cancellationToken).ConfigureAwait(false);
                        
                        _logger.LogInformation("Successfully generated and set collage for collection: {Name}", collection.Name);
                        
                        // Clean up the temp file
                        try
                        {
                            File.Delete(tempFile);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to delete temporary file {Path}", tempFile);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error setting image for collection {Name}", collection.Name);
                    }
                }
                else
                {
                    _logger.LogError("Failed to create temporary collage file at {Path}", tempFile);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating collage for collection {Name}", collection.Name);
            }
        }
        
        private async Task SetCollectionImageAsync(BoxSet collection, byte[] imageData, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Setting primary image for collection {Name} (ID: {Id})", collection.Name, collection.Id);
                
                // Save the image to the standard location
                var directory = collection.Path;
                var filename = $"folder{Path.DirectorySeparatorChar}poster.jpg";
                var outputPath = Path.Combine(directory, filename);
                
                // Ensure the directory exists
                var folderPath = Path.GetDirectoryName(outputPath);
                Directory.CreateDirectory(folderPath!);
                
                // Save the image
                File.WriteAllBytes(outputPath, imageData);
                
                _logger.LogInformation("Saved image to {Path}", outputPath);
                
                // Force a refresh of the collection
                _logger.LogInformation("Refreshing metadata for collection {Name}", collection.Name);
                
                // First, try to clear any existing image cache
                try
                {
                    // We can't directly set PrimaryImagePath as it's read-only
                    // Instead, force a metadata refresh and image update
                    await collection.RefreshMetadata(cancellationToken).ConfigureAwait(false);
                    _logger.LogInformation("Refreshed metadata");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error refreshing metadata, continuing anyway");
                }
                
                // Force image update
                await collection.UpdateToRepositoryAsync(ItemUpdateType.ImageUpdate, cancellationToken).ConfigureAwait(false);
                
                // Additional image refresh to ensure it's picked up
                await collection.UpdateToRepositoryAsync(ItemUpdateType.MetadataImport, cancellationToken).ConfigureAwait(false);
                
                _logger.LogInformation("Successfully set primary image for collection {Name}", collection.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting primary image for collection {Name}", collection.Name);
                throw;
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
