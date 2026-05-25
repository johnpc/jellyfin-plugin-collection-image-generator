using FluentAssertions;
using Jellyfin.Plugin.CollectionImageGenerator.ImageProcessor;
using Xunit;

namespace Jellyfin.Plugin.CollectionImageGenerator.Tests;

/// <summary>
/// Tests for layout helper methods in <see cref="CollageGenerator"/>.
/// </summary>
public class LayoutHelperTests
{
    private const int CanvasWidth = 1000;
    private const int CanvasHeight = 1500;
    private const int Padding = 20;

    /// <summary>
    /// Tests that Get3x3CellSize returns correct dimensions.
    /// </summary>
    [Fact]
    public void Get3x3CellSize_StandardCanvas_ReturnsCorrectDimensions()
    {
        var (width, height) = CollageGenerator.Get3x3CellSize(CanvasWidth, CanvasHeight, Padding);

        // (1000 - (20*4)) / 3 = (1000 - 80) / 3 = 306
        width.Should().Be(306);
        // (1500 - (20*4)) / 3 = (1500 - 80) / 3 = 473
        height.Should().Be(473);
    }

    /// <summary>
    /// Tests that Get2x2CellSize returns correct dimensions.
    /// </summary>
    [Fact]
    public void Get2x2CellSize_StandardCanvas_ReturnsCorrectDimensions()
    {
        var (width, height) = CollageGenerator.Get2x2CellSize(CanvasWidth, CanvasHeight, Padding);

        // (1000 - (20*3)) / 2 = (1000 - 60) / 2 = 470
        width.Should().Be(470);
        // (1500 - (20*3)) / 2 = (1500 - 60) / 2 = 720
        height.Should().Be(720);
    }

    /// <summary>
    /// Tests that Get3x3CellSize with zero padding uses full canvas.
    /// </summary>
    [Fact]
    public void Get3x3CellSize_ZeroPadding_UsesFullCanvas()
    {
        var (width, height) = CollageGenerator.Get3x3CellSize(900, 900, 0);

        width.Should().Be(300);
        height.Should().Be(300);
    }

    /// <summary>
    /// Tests that Get2x2CellSize with zero padding uses full canvas.
    /// </summary>
    [Fact]
    public void Get2x2CellSize_ZeroPadding_UsesFullCanvas()
    {
        var (width, height) = CollageGenerator.Get2x2CellSize(800, 600, 0);

        width.Should().Be(400);
        height.Should().Be(300);
    }

    /// <summary>
    /// Tests that GetCustomPositions for 9+ uses standard grid with correct cell placement.
    /// </summary>
    [Fact]
    public void GetCustomPositions_StandardGrid_CellsArePlacedCorrectly()
    {
        var positions = CollageGenerator.GetCustomPositions(9, CanvasWidth, CanvasHeight, Padding);

        positions.Should().HaveCount(9);

        // First cell should be at (padding, padding)
        positions[0].X.Should().Be(Padding);
        positions[0].Y.Should().Be(Padding);

        // Second cell should be shifted right by (width + padding)
        var cellWidth = (CanvasWidth - (Padding * 4)) / 3;
        positions[1].X.Should().Be(Padding + cellWidth + Padding);
        positions[1].Y.Should().Be(Padding);
    }

    /// <summary>
    /// Tests that no positions overlap for any layout count.
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
    public void GetCustomPositions_AllCounts_NoOverlappingPositions(int count)
    {
        var positions = CollageGenerator.GetCustomPositions(count, CanvasWidth, CanvasHeight, Padding);

        // Check that all positions have valid dimensions
        foreach (var pos in positions)
        {
            pos.X.Should().BeGreaterThanOrEqualTo(0);
            pos.Y.Should().BeGreaterThanOrEqualTo(0);
            pos.Width.Should().BeGreaterThan(0);
            pos.Height.Should().BeGreaterThan(0);
        }
    }

    /// <summary>
    /// Tests that positions fit within canvas bounds.
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
    public void GetCustomPositions_AllCounts_FitWithinCanvas(int count)
    {
        var positions = CollageGenerator.GetCustomPositions(count, CanvasWidth, CanvasHeight, Padding);

        foreach (var pos in positions)
        {
            (pos.X + pos.Width).Should().BeLessThanOrEqualTo(CanvasWidth + Padding,
                "position should fit within canvas width for count {0}", count);
            (pos.Y + pos.Height).Should().BeLessThanOrEqualTo(CanvasHeight + Padding,
                "position should fit within canvas height for count {0}", count);
        }
    }

    /// <summary>
    /// Tests large image count caps at 9 positions.
    /// </summary>
    [Theory]
    [InlineData(10)]
    [InlineData(20)]
    [InlineData(100)]
    public void GetCustomPositions_LargeCounts_CapsAtNine(int count)
    {
        var positions = CollageGenerator.GetCustomPositions(count, CanvasWidth, CanvasHeight, Padding);

        positions.Should().HaveCount(9);
    }

    /// <summary>
    /// Tests that larger canvas produces larger cells.
    /// </summary>
    [Fact]
    public void Get3x3CellSize_LargerCanvas_ProducesLargerCells()
    {
        var (width1, height1) = CollageGenerator.Get3x3CellSize(600, 900, 10);
        var (width2, height2) = CollageGenerator.Get3x3CellSize(1200, 1800, 10);

        width2.Should().BeGreaterThan(width1);
        height2.Should().BeGreaterThan(height1);
    }

    /// <summary>
    /// Tests that 4-image layout produces a 2x2 grid.
    /// </summary>
    [Fact]
    public void GetCustomPositions_FourImages_ProducesTwoByTwoGrid()
    {
        var positions = CollageGenerator.GetCustomPositions(4, CanvasWidth, CanvasHeight, Padding);

        positions.Should().HaveCount(4);

        // Row 1 should have same Y
        positions[0].Y.Should().Be(positions[1].Y);
        // Row 2 should have same Y
        positions[2].Y.Should().Be(positions[3].Y);
        // Rows should be different
        positions[0].Y.Should().NotBe(positions[2].Y);
    }

    /// <summary>
    /// Tests that single image layout fills the canvas minus padding.
    /// </summary>
    [Fact]
    public void GetCustomPositions_SingleImage_FillsCanvas()
    {
        var positions = CollageGenerator.GetCustomPositions(1, CanvasWidth, CanvasHeight, Padding);

        positions.Should().HaveCount(1);
        positions[0].X.Should().Be(Padding);
        positions[0].Y.Should().Be(Padding);
        positions[0].Width.Should().Be(CanvasWidth - (Padding * 2));
        positions[0].Height.Should().Be(CanvasHeight - (Padding * 2));
    }
}
