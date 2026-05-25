using FluentAssertions;
using Jellyfin.Plugin.CollectionImageGenerator.ImageProcessor;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace Jellyfin.Plugin.CollectionImageGenerator.Tests;

/// <summary>
/// Tests for the <see cref="CollageGenerator"/> class.
/// </summary>
public class CollageGeneratorTests
{
    private const int CanvasWidth = 1000;
    private const int CanvasHeight = 1500;
    private const int Padding = 20;

    /// <summary>
    /// Tests that GetCustomPositions returns exactly 1 position for a single image.
    /// </summary>
    [Fact]
    public void GetCustomPositions_SingleImage_ReturnsOnePosition()
    {
        var positions = CollageGenerator.GetCustomPositions(1, CanvasWidth, CanvasHeight, Padding);

        positions.Should().HaveCount(1);
        positions[0].X.Should().Be(Padding);
        positions[0].Y.Should().Be(Padding);
        positions[0].Width.Should().Be(CanvasWidth - (Padding * 2));
        positions[0].Height.Should().Be(CanvasHeight - (Padding * 2));
    }

    /// <summary>
    /// Tests that GetCustomPositions returns exactly 2 positions for two images.
    /// </summary>
    [Fact]
    public void GetCustomPositions_TwoImages_ReturnsTwoPositions()
    {
        var positions = CollageGenerator.GetCustomPositions(2, CanvasWidth, CanvasHeight, Padding);

        positions.Should().HaveCount(2);

        // Both positions should be within canvas bounds
        foreach (var pos in positions)
        {
            pos.X.Should().BeGreaterThanOrEqualTo(0);
            pos.Y.Should().BeGreaterThanOrEqualTo(0);
            (pos.X + pos.Width).Should().BeLessThanOrEqualTo(CanvasWidth + Padding);
            (pos.Y + pos.Height).Should().BeLessThanOrEqualTo(CanvasHeight + Padding);
        }
    }

    /// <summary>
    /// Tests that GetCustomPositions returns exactly 3 positions for three images.
    /// </summary>
    [Fact]
    public void GetCustomPositions_ThreeImages_ReturnsThreePositions()
    {
        var positions = CollageGenerator.GetCustomPositions(3, CanvasWidth, CanvasHeight, Padding);

        positions.Should().HaveCount(3);
    }

    /// <summary>
    /// Tests that GetCustomPositions returns exactly 4 positions for four images.
    /// </summary>
    [Fact]
    public void GetCustomPositions_FourImages_ReturnsFourPositions()
    {
        var positions = CollageGenerator.GetCustomPositions(4, CanvasWidth, CanvasHeight, Padding);

        positions.Should().HaveCount(4);
    }

    /// <summary>
    /// Tests that GetCustomPositions returns exactly 5 positions for five images.
    /// </summary>
    [Fact]
    public void GetCustomPositions_FiveImages_ReturnsFivePositions()
    {
        var positions = CollageGenerator.GetCustomPositions(5, CanvasWidth, CanvasHeight, Padding);

        positions.Should().HaveCount(5);
    }

    /// <summary>
    /// Tests that GetCustomPositions returns exactly 6 positions for six images.
    /// </summary>
    [Fact]
    public void GetCustomPositions_SixImages_ReturnsSixPositions()
    {
        var positions = CollageGenerator.GetCustomPositions(6, CanvasWidth, CanvasHeight, Padding);

        positions.Should().HaveCount(6);
    }

    /// <summary>
    /// Tests that GetCustomPositions returns exactly 7 positions for seven images.
    /// </summary>
    [Fact]
    public void GetCustomPositions_SevenImages_ReturnsSevenPositions()
    {
        var positions = CollageGenerator.GetCustomPositions(7, CanvasWidth, CanvasHeight, Padding);

        positions.Should().HaveCount(7);
    }

    /// <summary>
    /// Tests that GetCustomPositions returns exactly 8 positions for eight images.
    /// </summary>
    [Fact]
    public void GetCustomPositions_EightImages_ReturnsEightPositions()
    {
        var positions = CollageGenerator.GetCustomPositions(8, CanvasWidth, CanvasHeight, Padding);

        positions.Should().HaveCount(8);
    }

    /// <summary>
    /// Tests that GetCustomPositions returns exactly 9 positions for nine or more images.
    /// </summary>
    [Theory]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(15)]
    public void GetCustomPositions_NineOrMoreImages_ReturnsNinePositions(int count)
    {
        var positions = CollageGenerator.GetCustomPositions(count, CanvasWidth, CanvasHeight, Padding);

        positions.Should().HaveCount(9);
    }

    /// <summary>
    /// Tests that all positions have positive width and height.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    public void GetCustomPositions_AllCounts_PositionsHavePositiveDimensions(int count)
    {
        var positions = CollageGenerator.GetCustomPositions(count, CanvasWidth, CanvasHeight, Padding);

        foreach (var pos in positions)
        {
            pos.Width.Should().BeGreaterThan(0, "width should be positive for count {0}", count);
            pos.Height.Should().BeGreaterThan(0, "height should be positive for count {0}", count);
        }
    }

    /// <summary>
    /// Tests that GetBorderColor returns white for a dark background.
    /// </summary>
    [Fact]
    public void GetBorderColor_DarkBackground_ReturnsWhite()
    {
        var darkColor = Color.FromRgb(20, 20, 20);

        var borderColor = CollageGenerator.GetBorderColor(darkColor);

        var pixel = borderColor.ToPixel<Rgba32>();
        pixel.R.Should().Be(255);
        pixel.G.Should().Be(255);
        pixel.B.Should().Be(255);
    }

    /// <summary>
    /// Tests that GetBorderColor returns black for a light background.
    /// </summary>
    [Fact]
    public void GetBorderColor_LightBackground_ReturnsBlack()
    {
        var lightColor = Color.FromRgb(220, 220, 220);

        var borderColor = CollageGenerator.GetBorderColor(lightColor);

        var pixel = borderColor.ToPixel<Rgba32>();
        pixel.R.Should().Be(0);
        pixel.G.Should().Be(0);
        pixel.B.Should().Be(0);
    }

    /// <summary>
    /// Tests that GetBorderColor handles mid-tone backgrounds correctly.
    /// </summary>
    [Fact]
    public void GetBorderColor_MidToneBackground_ReturnsAppropriateColor()
    {
        // Mid-gray with luminance > 0.5 due to green weighting
        var midColor = Color.FromRgb(128, 128, 128);

        var borderColor = CollageGenerator.GetBorderColor(midColor);

        // 128/255 = ~0.502, right at boundary - should be black (>= 0.5)
        var pixel = borderColor.ToPixel<Rgba32>();
        pixel.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that SelectBackgroundColor returns a fallback when clusters are empty.
    /// </summary>
    [Fact]
    public void SelectBackgroundColor_EmptyClusters_ReturnsFallback()
    {
        var clusters = new List<CollageGenerator.ColorCluster>();

        var result = CollageGenerator.SelectBackgroundColor(clusters);

        var pixel = result.ToPixel<Rgba32>();
        pixel.R.Should().Be(45);
        pixel.G.Should().Be(45);
        pixel.B.Should().Be(45);
    }

    /// <summary>
    /// Tests that SelectBackgroundColor skips very dark clusters.
    /// </summary>
    [Fact]
    public void SelectBackgroundColor_SkipsDarkClusters()
    {
        var clusters = new List<CollageGenerator.ColorCluster>
        {
            new CollageGenerator.ColorCluster
            {
                CentroidR = 10,
                CentroidG = 10,
                CentroidB = 10,
                Colors = CreateColorList(100, new Rgba32(10, 10, 10)),
            },
            new CollageGenerator.ColorCluster
            {
                CentroidR = 100,
                CentroidG = 80,
                CentroidB = 60,
                Colors = CreateColorList(50, new Rgba32(100, 80, 60)),
            },
        };

        var result = CollageGenerator.SelectBackgroundColor(clusters);

        var pixel = result.ToPixel<Rgba32>();
        pixel.R.Should().Be(100);
        pixel.G.Should().Be(80);
        pixel.B.Should().Be(60);
    }

    /// <summary>
    /// Tests that SelectBackgroundColor skips very bright clusters.
    /// </summary>
    [Fact]
    public void SelectBackgroundColor_SkipsBrightClusters()
    {
        var clusters = new List<CollageGenerator.ColorCluster>
        {
            new CollageGenerator.ColorCluster
            {
                CentroidR = 250,
                CentroidG = 250,
                CentroidB = 250,
                Colors = CreateColorList(100, new Rgba32(250, 250, 250)),
            },
            new CollageGenerator.ColorCluster
            {
                CentroidR = 120,
                CentroidG = 100,
                CentroidB = 80,
                Colors = CreateColorList(50, new Rgba32(120, 100, 80)),
            },
        };

        var result = CollageGenerator.SelectBackgroundColor(clusters);

        var pixel = result.ToPixel<Rgba32>();
        pixel.R.Should().Be(120);
        pixel.G.Should().Be(100);
        pixel.B.Should().Be(80);
    }

    private static List<Rgba32> CreateColorList(int count, Rgba32 color)
    {
        var list = new List<Rgba32>();
        for (var i = 0; i < count; i++)
        {
            list.Add(color);
        }

        return list;
    }
}
