using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jellyfin.Plugin.CollectionImageGenerator.Configuration;
using Jellyfin.Plugin.CollectionImageGenerator.Services;
using Jellyfin.Plugin.CollectionImageGenerator.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Jellyfin.Plugin.CollectionImageGenerator.Tests;

/// <summary>
/// Tests for the <see cref="CollectionImageGeneratorTask"/> class.
/// </summary>
public class CollectionImageGeneratorTaskTests : IDisposable
{
    private readonly ILogger<CollectionImageGeneratorTask> _logger;
    private readonly ILibraryManager _libraryManager;
    private readonly ICollageGeneratorService _collageService;
    private readonly IImagePersistenceService _persistenceService;
    private readonly CollectionImageGeneratorTask _task;

    /// <summary>
    /// Initializes a new instance of the <see cref="CollectionImageGeneratorTaskTests"/> class.
    /// </summary>
    public CollectionImageGeneratorTaskTests()
    {
        _logger = Substitute.For<ILogger<CollectionImageGeneratorTask>>();
        _libraryManager = Substitute.For<ILibraryManager>();
        _collageService = Substitute.For<ICollageGeneratorService>();
        _persistenceService = Substitute.For<IImagePersistenceService>();
        _task = new CollectionImageGeneratorTask(_logger, _libraryManager, _collageService, _persistenceService);

        SetupPluginInstance();
    }

    /// <summary>
    /// Tests that SelectSampleItems returns at most maxImages items.
    /// </summary>
    [Fact]
    public void SelectSampleItems_LimitsToMaxImages()
    {
        var items = CreateItemList(10);

        var result = CollectionImageGeneratorTask.SelectSampleItems(items, 4);

        result.Should().HaveCount(4);
    }

    /// <summary>
    /// Tests that SelectSampleItems pads to 4 when between 2 and 3 items.
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public void SelectSampleItems_PadsToFourWhenBetweenTwoAndThree(int count)
    {
        var items = CreateItemList(count);

        var result = CollectionImageGeneratorTask.SelectSampleItems(items, 9);

        result.Should().HaveCount(4);
    }

    /// <summary>
    /// Tests that SelectSampleItems returns single item as-is.
    /// </summary>
    [Fact]
    public void SelectSampleItems_SingleItem_ReturnsOne()
    {
        var items = CreateItemList(1);

        var result = CollectionImageGeneratorTask.SelectSampleItems(items, 9);

        result.Should().HaveCount(1);
    }

    /// <summary>
    /// Tests that SelectSampleItems with 5+ items does not pad.
    /// </summary>
    [Theory]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(9)]
    public void SelectSampleItems_FiveOrMore_NoPadding(int count)
    {
        var items = CreateItemList(count);

        var result = CollectionImageGeneratorTask.SelectSampleItems(items, count);

        result.Should().HaveCount(count);
    }

    /// <summary>
    /// Tests that ExecuteAsync skips collections that already have images.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_SkipsCollectionsWithExistingImages()
    {
        var boxSet = CreateBoxSet("Test Collection", "/tmp/test-image.jpg");
        _libraryManager.GetItemList(Arg.Any<InternalItemsQuery>())
            .Returns(new List<BaseItem> { boxSet });

        await _task.ExecuteAsync(new Progress<double>(), CancellationToken.None);

        await _collageService.DidNotReceive()
            .GenerateCollageAsync(Arg.Any<List<string>>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Tests that ExecuteAsync skips collections with no linked children that have images.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_SkipsCollectionsWithNoItemImages()
    {
        var boxSet = new BoxSet { Name = "Empty Collection", Path = Path.GetTempPath() };
        _libraryManager.GetItemList(Arg.Any<InternalItemsQuery>())
            .Returns(new List<BaseItem> { boxSet });

        await _task.ExecuteAsync(new Progress<double>(), CancellationToken.None);

        await _collageService.DidNotReceive()
            .GenerateCollageAsync(Arg.Any<List<string>>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Tests that ExecuteAsync respects cancellation.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_RespectsCancellation()
    {
        var boxSet1 = CreateBoxSetWithChildren("Collection 1", 3);
        var boxSet2 = CreateBoxSetWithChildren("Collection 2", 3);
        _libraryManager.GetItemList(Arg.Any<InternalItemsQuery>())
            .Returns(new List<BaseItem> { boxSet1, boxSet2 });

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await _task.ExecuteAsync(new Progress<double>(), cts.Token);

        await _collageService.DidNotReceive()
            .GenerateCollageAsync(Arg.Any<List<string>>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Tests that GetDefaultTriggers returns daily trigger with configured time.
    /// </summary>
    [Fact]
    public void GetDefaultTriggers_EnabledWithValidTime_ReturnsDailyTrigger()
    {
        var triggers = _task.GetDefaultTriggers().ToList();

        triggers.Should().HaveCount(1);
        triggers[0].Type.Should().Be(TaskTriggerInfoType.DailyTrigger);
        triggers[0].TimeOfDayTicks.Should().Be(TimeSpan.FromHours(3).Ticks);
    }

    /// <summary>
    /// Tests that GetDefaultTriggers returns empty when disabled.
    /// </summary>
    [Fact]
    public void GetDefaultTriggers_Disabled_ReturnsEmpty()
    {
        Plugin.Instance!.Configuration.EnableScheduledTask = false;

        var triggers = _task.GetDefaultTriggers().ToList();

        triggers.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that GetDefaultTriggers falls back to 3 AM on invalid time.
    /// </summary>
    [Fact]
    public void GetDefaultTriggers_InvalidTime_FallsBackTo3AM()
    {
        Plugin.Instance!.Configuration.ScheduledTaskTimeOfDay = "not-a-time";

        var triggers = _task.GetDefaultTriggers().ToList();

        triggers.Should().HaveCount(1);
        triggers[0].TimeOfDayTicks.Should().Be(TimeSpan.FromHours(3).Ticks);
    }

    /// <summary>
    /// Tests that the task has the correct name.
    /// </summary>
    [Fact]
    public void Name_ReturnsExpectedValue()
    {
        _task.Name.Should().Be("Generate Collection Images");
    }

    /// <summary>
    /// Tests that the task has the correct key.
    /// </summary>
    [Fact]
    public void Key_ReturnsExpectedValue()
    {
        _task.Key.Should().Be("CollectionImageGeneratorTask");
    }

    /// <summary>
    /// Tests that the task has the correct description.
    /// </summary>
    [Fact]
    public void Description_ReturnsExpectedValue()
    {
        _task.Description.Should().Be("Generates collage images for collections without images");
    }

    /// <summary>
    /// Tests that the task has the correct category.
    /// </summary>
    [Fact]
    public void Category_ReturnsLibrary()
    {
        _task.Category.Should().Be("Library");
    }

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    private static void SetupPluginInstance()
    {
        if (Plugin.Instance != null)
        {
            return;
        }

        var appPaths = Substitute.For<IApplicationPaths>();
        appPaths.PluginConfigurationsPath.Returns(Path.GetTempPath());
        var xmlSerializer = Substitute.For<IXmlSerializer>();
        xmlSerializer.DeserializeFromFile(Arg.Any<Type>(), Arg.Any<string>())
            .Returns(new PluginConfiguration());

        _ = new Plugin(appPaths, xmlSerializer);
    }

    private static List<BaseItem> CreateItemList(int count)
    {
        var items = new List<BaseItem>();
        for (var i = 0; i < count; i++)
        {
            items.Add(new Movie { Name = $"Movie {i}" });
        }

        return items;
    }

    private static BoxSet CreateBoxSet(string name, string? primaryImagePath)
    {
        var boxSet = new BoxSet { Name = name };
        if (!string.IsNullOrEmpty(primaryImagePath))
        {
            boxSet.SetImage(
                new ItemImageInfo
                {
                    Path = primaryImagePath,
                    Type = MediaBrowser.Model.Entities.ImageType.Primary,
                },
                0);
        }

        return boxSet;
    }

    private static BoxSet CreateBoxSetWithChildren(string name, int childCount)
    {
        var boxSet = new BoxSet { Name = name, Path = Path.GetTempPath() };
        return boxSet;
    }
}
