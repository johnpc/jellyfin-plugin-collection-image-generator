using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.CollectionImageGenerator.ImageProcessor;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.CollectionImageGenerator
{
    /// <summary>
    /// Simple test program for the CollageGenerator.
    /// </summary>
    public class TestCollageGenerator
    {
        public static async Task Main(string[] args)
        {
            // Create a simple logger
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var logger = loggerFactory.CreateLogger<CollageGenerator>();
            
            var collageGenerator = new CollageGenerator(logger);
            var cancellationToken = CancellationToken.None;
            
            // Get the project root directory
            var projectRoot = Directory.GetCurrentDirectory();
            var testFolder = Path.Combine(projectRoot, "test", "grid");
            var outputFolder = Path.Combine(projectRoot, "test", "output");
            
            // Create output directory if it doesn't exist
            Directory.CreateDirectory(outputFolder);
            
            Console.WriteLine("Testing CollageGenerator with different grid layouts...");
            Console.WriteLine($"Test images folder: {testFolder}");
            Console.WriteLine($"Output folder: {outputFolder}");
            Console.WriteLine();
            
            // Test 1 image
            await TestLayout(collageGenerator, testFolder, outputFolder, "1", 1, cancellationToken);
            
            // Test 3 images
            await TestLayout(collageGenerator, testFolder, outputFolder, "3", 3, cancellationToken);
            
            // Test 4 images
            await TestLayout(collageGenerator, testFolder, outputFolder, "4", 4, cancellationToken);
            
            // Test 2 images (using first 2 from the 3-image folder)
            await TestLayoutCustom(collageGenerator, testFolder, outputFolder, "2", "3", 2, cancellationToken);
            
            // Test 5 images (using 4 images + 1 from 3-image folder)
            await TestLayout5Images(collageGenerator, testFolder, outputFolder, cancellationToken);
            
            // Test 6 images
            await TestLayoutMultipleImages(collageGenerator, testFolder, outputFolder, "6", 6, cancellationToken);
            
            // Test 7 images
            await TestLayoutMultipleImages(collageGenerator, testFolder, outputFolder, "7", 7, cancellationToken);
            
            // Test 8 images
            await TestLayoutMultipleImages(collageGenerator, testFolder, outputFolder, "8", 8, cancellationToken);
            
            // Test 9 images
            await TestLayoutMultipleImages(collageGenerator, testFolder, outputFolder, "9", 9, cancellationToken);
            
            Console.WriteLine();
            Console.WriteLine("All tests completed! Check the output folder for results.");
        }
        
        private static async Task TestLayout(CollageGenerator generator, string testFolder, string outputFolder, 
            string folderName, int imageCount, CancellationToken cancellationToken)
        {
            try
            {
                var imageFolder = Path.Combine(testFolder, folderName);
                if (!Directory.Exists(imageFolder))
                {
                    Console.WriteLine($"⚠️  Skipping {imageCount} images test - folder {imageFolder} not found");
                    return;
                }
                
                var imageFiles = Directory.GetFiles(imageFolder, "*.jpg");
                if (imageFiles.Length < imageCount)
                {
                    Console.WriteLine($"⚠️  Skipping {imageCount} images test - only {imageFiles.Length} images found");
                    return;
                }
                
                var imagePaths = new List<string>();
                for (int i = 0; i < imageCount; i++)
                {
                    imagePaths.Add(imageFiles[i]);
                }
                
                var outputPath = Path.Combine(outputFolder, $"collage_{imageCount}_images.jpg");
                
                Console.WriteLine($"🎬 Testing {imageCount} image(s) layout...");
                await generator.CreateCollageAsync(imagePaths, outputPath, cancellationToken);
                Console.WriteLine($"✅ Generated: {outputPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error testing {imageCount} images: {ex.Message}");
            }
        }
        
        private static async Task TestLayoutCustom(CollageGenerator generator, string testFolder, string outputFolder, 
            string outputName, string sourceFolder, int imageCount, CancellationToken cancellationToken)
        {
            try
            {
                var imageFolder = Path.Combine(testFolder, sourceFolder);
                if (!Directory.Exists(imageFolder))
                {
                    Console.WriteLine($"⚠️  Skipping {outputName} images test - folder {imageFolder} not found");
                    return;
                }
                
                var imageFiles = Directory.GetFiles(imageFolder, "*.jpg");
                if (imageFiles.Length < imageCount)
                {
                    Console.WriteLine($"⚠️  Skipping {outputName} images test - only {imageFiles.Length} images found");
                    return;
                }
                
                var imagePaths = new List<string>();
                for (int i = 0; i < imageCount; i++)
                {
                    imagePaths.Add(imageFiles[i]);
                }
                
                var outputPath = Path.Combine(outputFolder, $"collage_{outputName}_images.jpg");
                
                Console.WriteLine($"🎬 Testing {outputName} image(s) layout...");
                await generator.CreateCollageAsync(imagePaths, outputPath, cancellationToken);
                Console.WriteLine($"✅ Generated: {outputPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error testing {outputName} images: {ex.Message}");
            }
        }
        
        private static async Task TestLayout5Images(CollageGenerator generator, string testFolder, string outputFolder, CancellationToken cancellationToken)
        {
            try
            {
                var imagePaths = new List<string>();
                
                // Get 4 images from the '4' folder
                var folder4 = Path.Combine(testFolder, "4");
                if (Directory.Exists(folder4))
                {
                    var images4 = Directory.GetFiles(folder4, "*.jpg");
                    foreach (var img in images4)
                    {
                        imagePaths.Add(img);
                    }
                }
                
                // Get 1 image from the '3' folder (if we need more)
                if (imagePaths.Count < 5)
                {
                    var folder3 = Path.Combine(testFolder, "3");
                    if (Directory.Exists(folder3))
                    {
                        var images3 = Directory.GetFiles(folder3, "*.jpg");
                        for (int i = 0; i < Math.Min(images3.Length, 5 - imagePaths.Count); i++)
                        {
                            imagePaths.Add(images3[i]);
                        }
                    }
                }
                
                if (imagePaths.Count >= 5)
                {
                    // Take only first 5
                    imagePaths = imagePaths.GetRange(0, 5);
                    
                    var outputPath = Path.Combine(outputFolder, "collage_5_images.jpg");
                    
                    Console.WriteLine("🎬 Testing 5 images layout...");
                    await generator.CreateCollageAsync(imagePaths, outputPath, cancellationToken);
                    Console.WriteLine($"✅ Generated: {outputPath}");
                }
                else
                {
                    Console.WriteLine($"⚠️  Skipping 5 images test - only {imagePaths.Count} images found");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error testing 5 images: {ex.Message}");
            }
        }
        
        private static async Task TestLayoutMultipleImages(CollageGenerator generator, string testFolder, string outputFolder, 
            string outputName, int imageCount, CancellationToken cancellationToken)
        {
            try
            {
                var imagePaths = new List<string>();
                
                // Collect images from all available folders to reach the desired count
                var folders = new[] { "4", "3", "2", "1" };
                
                foreach (var folder in folders)
                {
                    var folderPath = Path.Combine(testFolder, folder);
                    if (Directory.Exists(folderPath))
                    {
                        var images = Directory.GetFiles(folderPath, "*.jpg");
                        foreach (var img in images)
                        {
                            if (imagePaths.Count < imageCount)
                            {
                                imagePaths.Add(img);
                            }
                        }
                    }
                    
                    if (imagePaths.Count >= imageCount)
                        break;
                }
                
                // If we still don't have enough images, duplicate some
                while (imagePaths.Count < imageCount && imagePaths.Count > 0)
                {
                    imagePaths.Add(imagePaths[imagePaths.Count % imagePaths.Count]);
                }
                
                if (imagePaths.Count >= imageCount)
                {
                    // Take only the number we need
                    imagePaths = imagePaths.GetRange(0, imageCount);
                    
                    var outputPath = Path.Combine(outputFolder, $"collage_{outputName}_images.jpg");
                    
                    Console.WriteLine($"🎬 Testing {outputName} images layout...");
                    await generator.CreateCollageAsync(imagePaths, outputPath, cancellationToken);
                    Console.WriteLine($"✅ Generated: {outputPath}");
                }
                else
                {
                    Console.WriteLine($"⚠️  Skipping {outputName} images test - only {imagePaths.Count} images found");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error testing {outputName} images: {ex.Message}");
            }
        }
    }
}