using FluentAssertions;
using Jellyfin.Plugin.CollectionImageGenerator.ImageProcessor;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace Jellyfin.Plugin.CollectionImageGenerator.Tests;

/// <summary>
/// Tests for the color analysis methods in <see cref="CollageGenerator"/>.
/// </summary>
public class ColorAnalysisTests
{
    /// <summary>
    /// Tests that ColorDistance returns zero for identical colors.
    /// </summary>
    [Fact]
    public void ColorDistance_IdenticalColors_ReturnsZero()
    {
        var color = new Rgba32(100, 150, 200);

        var distance = ColorAnalyzer.ColorDistance(color, 100, 150, 200);

        distance.Should().Be(0.0);
    }

    /// <summary>
    /// Tests that ColorDistance returns correct value for known colors.
    /// </summary>
    [Fact]
    public void ColorDistance_BlackToWhite_ReturnsCorrectDistance()
    {
        var black = new Rgba32(0, 0, 0);

        var distance = ColorAnalyzer.ColorDistance(black, 255, 255, 255);

        // sqrt(255^2 + 255^2 + 255^2) = sqrt(195075) ~= 441.67
        distance.Should().BeApproximately(441.67, 0.01);
    }

    /// <summary>
    /// Tests that ColorDistance is symmetric in effect.
    /// </summary>
    [Fact]
    public void ColorDistance_IsSymmetric()
    {
        var color1 = new Rgba32(50, 100, 150);
        var color2 = new Rgba32(200, 50, 75);

        var dist1 = ColorAnalyzer.ColorDistance(color1, color2.R, color2.G, color2.B);
        var dist2 = ColorAnalyzer.ColorDistance(color2, color1.R, color1.G, color1.B);

        dist1.Should().BeApproximately(dist2, 0.001);
    }

    /// <summary>
    /// Tests that ColorDistance handles single-channel differences.
    /// </summary>
    [Theory]
    [InlineData(100, 0, 0, 200, 0, 0, 100.0)]
    [InlineData(0, 100, 0, 0, 200, 0, 100.0)]
    [InlineData(0, 0, 100, 0, 0, 200, 100.0)]
    public void ColorDistance_SingleChannelDifference_ReturnsExpected(
        byte r1, byte g1, byte b1, byte r2, byte g2, byte b2, double expected)
    {
        var color = new Rgba32(r1, g1, b1);

        var distance = ColorAnalyzer.ColorDistance(color, r2, g2, b2);

        distance.Should().BeApproximately(expected, 0.001);
    }

    /// <summary>
    /// Tests that SampleImageColors returns colors from a solid-color image.
    /// </summary>
    [Fact]
    public void SampleImageColors_SolidImage_ReturnsConsistentColors()
    {
        using var image = new Image<Rgba32>(100, 150, new Rgba32(128, 64, 32, 255));

        var colors = ColorAnalyzer.SampleImageColors(image, 3);

        colors.Should().NotBeEmpty();
        colors.Should().AllSatisfy(c =>
        {
            c.R.Should().Be(128);
            c.G.Should().Be(64);
            c.B.Should().Be(32);
        });
    }

    /// <summary>
    /// Tests that SampleImageColors skips transparent pixels.
    /// </summary>
    [Fact]
    public void SampleImageColors_TransparentImage_ReturnsEmpty()
    {
        using var image = new Image<Rgba32>(100, 150, new Rgba32(128, 64, 32, 0));

        var colors = ColorAnalyzer.SampleImageColors(image, 3);

        colors.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that SampleImageColors with low alpha below threshold returns empty.
    /// </summary>
    [Fact]
    public void SampleImageColors_LowAlpha_ReturnsEmpty()
    {
        using var image = new Image<Rgba32>(100, 150, new Rgba32(128, 64, 32, 100));

        var colors = ColorAnalyzer.SampleImageColors(image, 3);

        colors.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that SampleImageColors with alpha above threshold returns colors.
    /// </summary>
    [Fact]
    public void SampleImageColors_HighAlpha_ReturnsColors()
    {
        using var image = new Image<Rgba32>(100, 150, new Rgba32(128, 64, 32, 200));

        var colors = ColorAnalyzer.SampleImageColors(image, 3);

        colors.Should().NotBeEmpty();
    }

    /// <summary>
    /// Tests that higher sample rate returns fewer colors.
    /// </summary>
    [Fact]
    public void SampleImageColors_HigherSampleRate_ReturnsFewerColors()
    {
        using var image = new Image<Rgba32>(100, 150, new Rgba32(128, 64, 32, 255));

        var colorsRate3 = ColorAnalyzer.SampleImageColors(image, 3);
        var colorsRate6 = ColorAnalyzer.SampleImageColors(image, 6);

        colorsRate3.Count.Should().BeGreaterThan(colorsRate6.Count);
    }

    /// <summary>
    /// Tests that PerformKMeansClustering with empty colors returns empty.
    /// </summary>
    [Fact]
    public void PerformKMeansClustering_EmptyColors_ReturnsEmpty()
    {
        var colors = new List<Rgba32>();

        var clusters = ColorAnalyzer.PerformKMeansClustering(colors, 4);

        clusters.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that PerformKMeansClustering with zero k returns empty.
    /// </summary>
    [Fact]
    public void PerformKMeansClustering_ZeroK_ReturnsEmpty()
    {
        var colors = new List<Rgba32> { new Rgba32(100, 100, 100) };

        var clusters = ColorAnalyzer.PerformKMeansClustering(colors, 0);

        clusters.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that PerformKMeansClustering with negative k returns empty.
    /// </summary>
    [Fact]
    public void PerformKMeansClustering_NegativeK_ReturnsEmpty()
    {
        var colors = new List<Rgba32> { new Rgba32(100, 100, 100) };

        var clusters = ColorAnalyzer.PerformKMeansClustering(colors, -1);

        clusters.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that PerformKMeansClustering with single color returns one cluster.
    /// </summary>
    [Fact]
    public void PerformKMeansClustering_SingleColor_ReturnsOneCluster()
    {
        var colors = new List<Rgba32> { new Rgba32(100, 150, 200) };

        var clusters = ColorAnalyzer.PerformKMeansClustering(colors, 1);

        clusters.Should().HaveCount(1);
        clusters[0].CentroidR.Should().Be(100);
        clusters[0].CentroidG.Should().Be(150);
        clusters[0].CentroidB.Should().Be(200);
        clusters[0].Colors.Should().HaveCount(1);
    }

    /// <summary>
    /// Tests that PerformKMeansClustering separates distinct colors into clusters.
    /// </summary>
    [Fact]
    public void PerformKMeansClustering_DistinctColors_SeparatesIntoClusters()
    {
        var colors = new List<Rgba32>();

        // Add 50 red-ish colors
        for (var i = 0; i < 50; i++)
        {
            colors.Add(new Rgba32(200, 20, 20));
        }

        // Add 50 blue-ish colors
        for (var i = 0; i < 50; i++)
        {
            colors.Add(new Rgba32(20, 20, 200));
        }

        var clusters = ColorAnalyzer.PerformKMeansClustering(colors, 2);

        clusters.Should().HaveCount(2);

        // One cluster should be red-ish, other blue-ish
        var redCluster = clusters.First(c => c.CentroidR > c.CentroidB);
        var blueCluster = clusters.First(c => c.CentroidB > c.CentroidR);

        redCluster.CentroidR.Should().Be(200);
        blueCluster.CentroidB.Should().Be(200);
    }

    /// <summary>
    /// Tests that PerformKMeansClustering converges with uniform colors.
    /// </summary>
    [Fact]
    public void PerformKMeansClustering_UniformColors_ConvergesQuickly()
    {
        var colors = new List<Rgba32>();
        for (var i = 0; i < 100; i++)
        {
            colors.Add(new Rgba32(128, 128, 128));
        }

        var clusters = ColorAnalyzer.PerformKMeansClustering(colors, 3);

        // All colors are the same, so some clusters may end up empty
        clusters.Should().NotBeEmpty();
        clusters.Should().AllSatisfy(c =>
        {
            c.CentroidR.Should().Be(128);
            c.CentroidG.Should().Be(128);
            c.CentroidB.Should().Be(128);
        });
    }

    /// <summary>
    /// Tests that PerformKMeansClustering with more clusters than colors still works.
    /// </summary>
    [Fact]
    public void PerformKMeansClustering_MoreClustersThanColors_HandlesGracefully()
    {
        var colors = new List<Rgba32>
        {
            new Rgba32(100, 100, 100),
            new Rgba32(200, 200, 200),
        };

        var clusters = ColorAnalyzer.PerformKMeansClustering(colors, 5);

        // Should still work - some clusters may be empty and filtered out
        clusters.Should().NotBeEmpty();
        clusters.Count.Should().BeLessThanOrEqualTo(5);
    }

    /// <summary>
    /// Tests SelectBackgroundColor with all dark clusters falls through to adjustment.
    /// </summary>
    [Fact]
    public void SelectBackgroundColor_AllDarkClusters_ReturnsAdjustedSecondCluster()
    {
        var clusters = new List<ColorCluster>
        {
            new ColorCluster
            {
                CentroidR = 10,
                CentroidG = 10,
                CentroidB = 10,
                Colors = CreateColorList(100, new Rgba32(10, 10, 10)),
            },
            new ColorCluster
            {
                CentroidR = 20,
                CentroidG = 20,
                CentroidB = 20,
                Colors = CreateColorList(50, new Rgba32(20, 20, 20)),
            },
        };

        var result = ColorAnalyzer.SelectBackgroundColor(clusters);

        // Second cluster centroid is (20,20,20) which is clamped to (60,60,60)
        var pixel = result.ToPixel<Rgba32>();
        pixel.R.Should().Be(60);
        pixel.G.Should().Be(60);
        pixel.B.Should().Be(60);
    }

    /// <summary>
    /// Tests SelectBackgroundColor with all bright clusters falls through to adjustment.
    /// </summary>
    [Fact]
    public void SelectBackgroundColor_AllBrightClusters_ReturnsAdjustedSecondCluster()
    {
        var clusters = new List<ColorCluster>
        {
            new ColorCluster
            {
                CentroidR = 250,
                CentroidG = 250,
                CentroidB = 250,
                Colors = CreateColorList(100, new Rgba32(250, 250, 250)),
            },
            new ColorCluster
            {
                CentroidR = 240,
                CentroidG = 240,
                CentroidB = 240,
                Colors = CreateColorList(50, new Rgba32(240, 240, 240)),
            },
        };

        var result = ColorAnalyzer.SelectBackgroundColor(clusters);

        // Second cluster centroid is (240,240,240) clamped to (180,180,180)
        var pixel = result.ToPixel<Rgba32>();
        pixel.R.Should().Be(180);
        pixel.G.Should().Be(180);
        pixel.B.Should().Be(180);
    }

    /// <summary>
    /// Tests SelectBackgroundColor with single extreme cluster returns fallback.
    /// </summary>
    [Fact]
    public void SelectBackgroundColor_SingleDarkCluster_ReturnsFallback()
    {
        var clusters = new List<ColorCluster>
        {
            new ColorCluster
            {
                CentroidR = 5,
                CentroidG = 5,
                CentroidB = 5,
                Colors = CreateColorList(100, new Rgba32(5, 5, 5)),
            },
        };

        var result = ColorAnalyzer.SelectBackgroundColor(clusters);

        // With only one cluster that's too dark, falls to final fallback
        var pixel = result.ToPixel<Rgba32>();
        pixel.R.Should().Be(75);
        pixel.G.Should().Be(75);
        pixel.B.Should().Be(85);
    }

    /// <summary>
    /// Tests SelectBackgroundColor selects by cluster size ordering.
    /// </summary>
    [Fact]
    public void SelectBackgroundColor_MultipleClusters_SelectsLargestValidCluster()
    {
        var clusters = new List<ColorCluster>
        {
            new ColorCluster
            {
                CentroidR = 80,
                CentroidG = 120,
                CentroidB = 60,
                Colors = CreateColorList(30, new Rgba32(80, 120, 60)),
            },
            new ColorCluster
            {
                CentroidR = 150,
                CentroidG = 100,
                CentroidB = 50,
                Colors = CreateColorList(70, new Rgba32(150, 100, 50)),
            },
        };

        var result = ColorAnalyzer.SelectBackgroundColor(clusters);

        // The second cluster has more colors (70 > 30), so it's first after sort
        var pixel = result.ToPixel<Rgba32>();
        pixel.R.Should().Be(150);
        pixel.G.Should().Be(100);
        pixel.B.Should().Be(50);
    }

    /// <summary>
    /// Tests GetBorderColor boundary case exactly at 0.5 luminance.
    /// </summary>
    [Fact]
    public void GetBorderColor_ExactlyAtBoundary_ReturnsBlack()
    {
        // luminance = (0.299*128 + 0.587*128 + 0.114*128)/255 = 128/255 = ~0.502
        var midColor = Color.FromRgb(128, 128, 128);

        var borderColor = ColorAnalyzer.GetBorderColor(midColor);

        // 0.502 >= 0.5 so should return black
        var pixel = borderColor.ToPixel<Rgba32>();
        pixel.R.Should().Be(0);
        pixel.G.Should().Be(0);
        pixel.B.Should().Be(0);
    }

    /// <summary>
    /// Tests GetBorderColor with red-dominant color.
    /// </summary>
    [Fact]
    public void GetBorderColor_RedDominant_ReturnsBasedOnLuminance()
    {
        // Pure red: luminance = (0.299*255 + 0.587*0 + 0.114*0)/255 = 76.245/255 ~= 0.299
        var red = Color.FromRgb(255, 0, 0);

        var borderColor = ColorAnalyzer.GetBorderColor(red);

        // 0.299 < 0.5 so white
        var pixel = borderColor.ToPixel<Rgba32>();
        pixel.R.Should().Be(255);
        pixel.G.Should().Be(255);
        pixel.B.Should().Be(255);
    }

    /// <summary>
    /// Tests GetBorderColor with green-dominant color.
    /// </summary>
    [Fact]
    public void GetBorderColor_GreenDominant_ReturnsBasedOnLuminance()
    {
        // Pure green: luminance = (0.299*0 + 0.587*255 + 0.114*0)/255 = 149.685/255 ~= 0.587
        var green = Color.FromRgb(0, 255, 0);

        var borderColor = ColorAnalyzer.GetBorderColor(green);

        // 0.587 >= 0.5 so black
        var pixel = borderColor.ToPixel<Rgba32>();
        pixel.R.Should().Be(0);
        pixel.G.Should().Be(0);
        pixel.B.Should().Be(0);
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
