using System.IO;
using System.Text.Json;
using Xunit;

namespace Sway.Tests;

public class SettingsServiceTests
{
    [Fact]
    public void Load_ReturnsDefaults_WhenFileDoesNotExist()
    {
        var path = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}.json");
        var service = new SettingsService(path);

        var settings = service.Current;

        Assert.False(settings.AutoStart);
        Assert.False(settings.RunWhenWindowsLocked);
        Assert.Equal(20, settings.RunAfterSeconds);
        Assert.Equal(1, settings.SwayLength);
    }

    [Fact]
    public void Save_And_Load_Roundtrip()
    {
        var path = Path.Combine(Path.GetTempPath(), $"sway_roundtrip_{Guid.NewGuid()}.json");
        var service = new SettingsService(path);

        var original = new SwaySettings
        {
            AutoStart = true,
            RunWhenWindowsLocked = true,
            RunAfterSeconds = 60,
            SwayLength = 10
        };
        service.Save(original);

        var loaded = service.Current;

        Assert.True(loaded.AutoStart);
        Assert.True(loaded.RunWhenWindowsLocked);
        Assert.Equal(60, loaded.RunAfterSeconds);
        Assert.Equal(10, loaded.SwayLength);

        try { File.Delete(path); } catch { }
    }

    [Fact]
    public void Save_ClampsOutOfRangeValues()
    {
        var path = Path.Combine(Path.GetTempPath(), $"sway_clamp_{Guid.NewGuid()}.json");
        var service = new SettingsService(path);

        var settings = new SwaySettings
        {
            RunAfterSeconds = 9999,
            SwayLength = 999
        };
        service.Save(settings);

        var loaded = service.Current;

        Assert.Equal(1000, loaded.RunAfterSeconds);
        Assert.Equal(50, loaded.SwayLength);

        try { File.Delete(path); } catch { }
    }

    [Fact]
    public void Load_ClampsInvalidValuesFromFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"sway_invalid_{Guid.NewGuid()}.json");
        var json = JsonSerializer.Serialize(
            new { SwaySettings = new { AutoStart = false, RunWhenWindowsLocked = false, RunAfterSeconds = 1, SwayLength = -5 } },
            new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);

        var service = new SettingsService(path);

        var settings = service.Current;

        Assert.Equal(20, settings.RunAfterSeconds);
        Assert.Equal(1, settings.SwayLength);

        try { File.Delete(path); } catch { }
    }
}
