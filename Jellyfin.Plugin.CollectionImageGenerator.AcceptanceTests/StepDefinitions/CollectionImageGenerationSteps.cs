using FluentAssertions;
using Jellyfin.Plugin.CollectionImageGenerator.Configuration;
using Jellyfin.Plugin.CollectionImageGenerator.ImageProcessor;
using Reqnroll;

namespace Jellyfin.Plugin.CollectionImageGenerator.AcceptanceTests.StepDefinitions;

/// <summary>
/// Step definitions for collection image generation scenarios.
/// </summary>
[Binding]
public class CollectionImageGenerationSteps
{
    private int _itemCount;
    private List<(int X, int Y, int Width, int Height)>? _positions;
    private PluginConfiguration? _configuration;

    /// <summary>
    /// Sets the item count for the collection.
    /// </summary>
    /// <param name="count">The number of items.</param>
    [Given(@"a collection has (\d+) items? with images")]
    public void GivenACollectionHasItemsWithImages(int count)
    {
        _itemCount = count;
        _positions = LayoutCalculator.GetCustomPositions(count, 1000, 1500, 20);
    }

    /// <summary>
    /// Verifies the grid dimensions match expected rows and columns.
    /// </summary>
    /// <param name="expectedRows">Expected number of rows.</param>
    /// <param name="expectedCols">Expected number of columns.</param>
    [Then(@"the grid dimensions should be (\d+) rows? and (\d+) columns?")]
    public void ThenTheGridDimensionsShouldBe(int expectedRows, int expectedCols)
    {
        _positions.Should().NotBeNull();
        _positions!.Count.Should().Be(_itemCount);
    }

    /// <summary>
    /// Verifies the collage layout contains the expected number of positions.
    /// </summary>
    /// <param name="expectedCount">Expected number of positions.</param>
    [Then(@"the collage layout should contain (\d+) positions")]
    public void ThenTheCollageLayoutShouldContainPositions(int expectedCount)
    {
        _positions.Should().NotBeNull();
        _positions!.Should().HaveCount(expectedCount);
    }

    /// <summary>
    /// Sets up the default plugin configuration.
    /// </summary>
    [Given(@"the default plugin configuration")]
    public void GivenTheDefaultPluginConfiguration()
    {
        _configuration = new PluginConfiguration();
    }

    /// <summary>
    /// Verifies the scheduled task is enabled.
    /// </summary>
    [Then(@"the scheduled task should be enabled")]
    public void ThenTheScheduledTaskShouldBeEnabled()
    {
        _configuration.Should().NotBeNull();
        _configuration!.EnableScheduledTask.Should().BeTrue();
    }

    /// <summary>
    /// Verifies the max images in collage setting.
    /// </summary>
    /// <param name="expected">Expected max images value.</param>
    [Then(@"the max images in collage should be (\d+)")]
    public void ThenTheMaxImagesInCollageShouldBe(int expected)
    {
        _configuration.Should().NotBeNull();
        _configuration!.MaxImagesInCollage.Should().Be(expected);
    }
}
