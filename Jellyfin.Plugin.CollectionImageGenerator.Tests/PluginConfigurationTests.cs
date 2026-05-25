using FluentAssertions;
using Jellyfin.Plugin.CollectionImageGenerator.Configuration;
using Xunit;

namespace Jellyfin.Plugin.CollectionImageGenerator.Tests;

/// <summary>
/// Tests for the <see cref="PluginConfiguration"/> class.
/// </summary>
public class PluginConfigurationTests
{
    /// <summary>
    /// Tests that default MaxImagesInCollage is 4.
    /// </summary>
    [Fact]
    public void DefaultMaxImagesInCollage_IsFour()
    {
        var config = new PluginConfiguration();

        config.MaxImagesInCollage.Should().Be(4);
    }

    /// <summary>
    /// Tests that default ScheduledTaskTimeOfDay is 03:00.
    /// </summary>
    [Fact]
    public void DefaultScheduledTaskTimeOfDay_IsThreeAM()
    {
        var config = new PluginConfiguration();

        config.ScheduledTaskTimeOfDay.Should().Be("03:00");
    }

    /// <summary>
    /// Tests that default EnableScheduledTask is true.
    /// </summary>
    [Fact]
    public void DefaultEnableScheduledTask_IsTrue()
    {
        var config = new PluginConfiguration();

        config.EnableScheduledTask.Should().BeTrue();
    }

    /// <summary>
    /// Tests that MaxImagesInCollage can be updated.
    /// </summary>
    [Fact]
    public void MaxImagesInCollage_CanBeSet()
    {
        var config = new PluginConfiguration
        {
            MaxImagesInCollage = 9,
        };

        config.MaxImagesInCollage.Should().Be(9);
    }

    /// <summary>
    /// Tests that ScheduledTaskTimeOfDay can be updated.
    /// </summary>
    [Fact]
    public void ScheduledTaskTimeOfDay_CanBeSet()
    {
        var config = new PluginConfiguration
        {
            ScheduledTaskTimeOfDay = "14:30",
        };

        config.ScheduledTaskTimeOfDay.Should().Be("14:30");
    }

    /// <summary>
    /// Tests that EnableScheduledTask can be disabled.
    /// </summary>
    [Fact]
    public void EnableScheduledTask_CanBeDisabled()
    {
        var config = new PluginConfiguration
        {
            EnableScheduledTask = false,
        };

        config.EnableScheduledTask.Should().BeFalse();
    }
}
