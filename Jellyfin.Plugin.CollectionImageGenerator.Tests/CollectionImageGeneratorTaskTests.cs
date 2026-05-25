using FluentAssertions;
using Jellyfin.Plugin.CollectionImageGenerator.Tasks;
using Xunit;

namespace Jellyfin.Plugin.CollectionImageGenerator.Tests;

/// <summary>
/// Tests for the <see cref="CollectionImageGeneratorTask"/> class.
/// </summary>
public class CollectionImageGeneratorTaskTests
{
    /// <summary>
    /// Tests that GetGridDimensions returns 1x1 for a single image.
    /// </summary>
    [Fact]
    public void GetGridDimensions_SingleImage_Returns1x1()
    {
        var (rows, cols) = CollectionImageGeneratorTask.GetGridDimensions(1);

        rows.Should().Be(1);
        cols.Should().Be(1);
    }

    /// <summary>
    /// Tests that GetGridDimensions returns 1x2 for two images.
    /// </summary>
    [Fact]
    public void GetGridDimensions_TwoImages_Returns1x2()
    {
        var (rows, cols) = CollectionImageGeneratorTask.GetGridDimensions(2);

        rows.Should().Be(1);
        cols.Should().Be(2);
    }

    /// <summary>
    /// Tests that GetGridDimensions returns 1x3 for three images.
    /// </summary>
    [Fact]
    public void GetGridDimensions_ThreeImages_Returns1x3()
    {
        var (rows, cols) = CollectionImageGeneratorTask.GetGridDimensions(3);

        rows.Should().Be(1);
        cols.Should().Be(3);
    }

    /// <summary>
    /// Tests that GetGridDimensions returns 2x2 for four images.
    /// </summary>
    [Fact]
    public void GetGridDimensions_FourImages_Returns2x2()
    {
        var (rows, cols) = CollectionImageGeneratorTask.GetGridDimensions(4);

        rows.Should().Be(2);
        cols.Should().Be(2);
    }

    /// <summary>
    /// Tests that GetGridDimensions returns 2x3 for five images.
    /// </summary>
    [Fact]
    public void GetGridDimensions_FiveImages_Returns2x3()
    {
        var (rows, cols) = CollectionImageGeneratorTask.GetGridDimensions(5);

        rows.Should().Be(2);
        cols.Should().Be(3);
    }

    /// <summary>
    /// Tests that GetGridDimensions returns 2x3 for six images.
    /// </summary>
    [Fact]
    public void GetGridDimensions_SixImages_Returns2x3()
    {
        var (rows, cols) = CollectionImageGeneratorTask.GetGridDimensions(6);

        rows.Should().Be(2);
        cols.Should().Be(3);
    }

    /// <summary>
    /// Tests that GetGridDimensions returns 3x3 for seven images.
    /// </summary>
    [Fact]
    public void GetGridDimensions_SevenImages_Returns3x3()
    {
        var (rows, cols) = CollectionImageGeneratorTask.GetGridDimensions(7);

        rows.Should().Be(3);
        cols.Should().Be(3);
    }

    /// <summary>
    /// Tests that GetGridDimensions returns 3x3 for eight images.
    /// </summary>
    [Fact]
    public void GetGridDimensions_EightImages_Returns3x3()
    {
        var (rows, cols) = CollectionImageGeneratorTask.GetGridDimensions(8);

        rows.Should().Be(3);
        cols.Should().Be(3);
    }

    /// <summary>
    /// Tests that GetGridDimensions returns 3x3 for nine images.
    /// </summary>
    [Fact]
    public void GetGridDimensions_NineImages_Returns3x3()
    {
        var (rows, cols) = CollectionImageGeneratorTask.GetGridDimensions(9);

        rows.Should().Be(3);
        cols.Should().Be(3);
    }

    /// <summary>
    /// Tests that GetGridDimensions returns 3x3 for more than nine images.
    /// </summary>
    [Theory]
    [InlineData(10)]
    [InlineData(15)]
    [InlineData(20)]
    public void GetGridDimensions_MoreThanNine_Returns3x3(int count)
    {
        var (rows, cols) = CollectionImageGeneratorTask.GetGridDimensions(count);

        rows.Should().Be(3);
        cols.Should().Be(3);
    }

    /// <summary>
    /// Tests that the task has the correct name.
    /// </summary>
    [Fact]
    public void Name_ReturnsExpectedValue()
    {
        var task = CreateTask();

        task.Name.Should().Be("Generate Collection Images");
    }

    /// <summary>
    /// Tests that the task has the correct key.
    /// </summary>
    [Fact]
    public void Key_ReturnsExpectedValue()
    {
        var task = CreateTask();

        task.Key.Should().Be("CollectionImageGeneratorTask");
    }

    /// <summary>
    /// Tests that the task has the correct description.
    /// </summary>
    [Fact]
    public void Description_ReturnsExpectedValue()
    {
        var task = CreateTask();

        task.Description.Should().Be("Generates collage images for collections without images");
    }

    /// <summary>
    /// Tests that the task has the correct category.
    /// </summary>
    [Fact]
    public void Category_ReturnsLibrary()
    {
        var task = CreateTask();

        task.Category.Should().Be("Library");
    }

    private static CollectionImageGeneratorTask CreateTask()
    {
        var logger = NSubstitute.Substitute.For<Microsoft.Extensions.Logging.ILogger<CollectionImageGeneratorTask>>();
        var libraryManager = NSubstitute.Substitute.For<MediaBrowser.Controller.Library.ILibraryManager>();
        var collectionManager = NSubstitute.Substitute.For<MediaBrowser.Controller.Collections.ICollectionManager>();

        return new CollectionImageGeneratorTask(logger, libraryManager, collectionManager);
    }
}
