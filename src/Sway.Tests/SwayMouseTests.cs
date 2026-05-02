using System.IO;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Sway.Tests;

public class SwayMouseTests
{
    static SettingsService CreateSettingsService(bool autoStart = false, int runAfterSeconds = 20, int swayLength = 1)
    {
        var path = Path.Combine(Path.GetTempPath(), $"sway_test_{Guid.NewGuid()}.json");
        var settings = new SwaySettings
        {
            AutoStart = autoStart,
            RunWhenWindowsLocked = false,
            RunAfterSeconds = runAfterSeconds,
            SwayLength = swayLength
        };
        var json = System.Text.Json.JsonSerializer.Serialize(
            new { SwaySettings = settings },
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
        return new SettingsService(path);
    }

    [Fact]
    public void Constructor_LoadsSettingsFromService()
    {
        var settingsService = CreateSettingsService(runAfterSeconds: 30, swayLength: 5);
        var registryMock = new Mock<IRegistryService>();
        var loggerMock = new Mock<ILogger<SwayMouse>>();
        var mouseMock = new Mock<IMouseService>();

        var vm = new SwayMouse(settingsService, registryMock.Object, loggerMock.Object, mouseMock.Object);

        Assert.Equal(30, vm.RunAfterSeconds);
        Assert.Equal(5, vm.SwayLength);
        Assert.False(vm.AutoStart);
    }

    [Fact]
    public void RunAfterSeconds_ClampsToDefault_WhenOutOfRange()
    {
        var settingsService = CreateSettingsService();
        var registryMock = new Mock<IRegistryService>();
        var loggerMock = new Mock<ILogger<SwayMouse>>();
        var mouseMock = new Mock<IMouseService>();
        var vm = new SwayMouse(settingsService, registryMock.Object, loggerMock.Object, mouseMock.Object);

        vm.RunAfterSeconds = 0;
        Assert.Equal(20, vm.RunAfterSeconds);

        vm.RunAfterSeconds = 1001;
        Assert.Equal(20, vm.RunAfterSeconds);

        vm.RunAfterSeconds = 50;
        Assert.Equal(50, vm.RunAfterSeconds);
    }

    [Fact]
    public void SwayLength_ClampsToDefault_WhenOutOfRange()
    {
        var settingsService = CreateSettingsService();
        var registryMock = new Mock<IRegistryService>();
        var loggerMock = new Mock<ILogger<SwayMouse>>();
        var mouseMock = new Mock<IMouseService>();
        var vm = new SwayMouse(settingsService, registryMock.Object, loggerMock.Object, mouseMock.Object);

        vm.SwayLength = -1;
        Assert.Equal(1, vm.SwayLength);

        vm.SwayLength = 51;
        Assert.Equal(1, vm.SwayLength);

        vm.SwayLength = 25;
        Assert.Equal(25, vm.SwayLength);
    }

    [Fact]
    public void IsRunning_TogglesWithStartStopCommand()
    {
        var settingsService = CreateSettingsService();
        var registryMock = new Mock<IRegistryService>();
        var loggerMock = new Mock<ILogger<SwayMouse>>();
        var mouseMock = new Mock<IMouseService>();
        var vm = new SwayMouse(settingsService, registryMock.Object, loggerMock.Object, mouseMock.Object);

        var initialStatus = vm.IsRunning;

        vm.StartStopCommand.Execute(null);

        Assert.NotEqual(initialStatus, vm.IsRunning);
    }

    [Fact]
    public void RunningStatus_ReflectsIsRunning()
    {
        var settingsService = CreateSettingsService();
        var registryMock = new Mock<IRegistryService>();
        var loggerMock = new Mock<ILogger<SwayMouse>>();
        var mouseMock = new Mock<IMouseService>();
        var vm = new SwayMouse(settingsService, registryMock.Object, loggerMock.Object, mouseMock.Object);

        Assert.Equal(vm.IsRunning ? "Running" : "Stopped", vm.RunningStatus);

        vm.StartStopCommand.Execute(null);

        Assert.Equal(vm.IsRunning ? "Running" : "Stopped", vm.RunningStatus);
    }

    [Fact]
    public void AutoStart_CallsRegistryService()
    {
        var settingsService = CreateSettingsService();
        var registryMock = new Mock<IRegistryService>();
        registryMock.Setup(r => r.TrySetAutoRun(It.IsAny<bool>())).Returns(true);
        var loggerMock = new Mock<ILogger<SwayMouse>>();
        var mouseMock = new Mock<IMouseService>();
        var vm = new SwayMouse(settingsService, registryMock.Object, loggerMock.Object, mouseMock.Object);

        vm.AutoStart = true;

        registryMock.Verify(r => r.TrySetAutoRun(true), Times.AtLeastOnce);
    }

    [Fact]
    public void Dispose_CleansUpSessionSwitchAndCancellation()
    {
        var settingsService = CreateSettingsService();
        var registryMock = new Mock<IRegistryService>();
        var loggerMock = new Mock<ILogger<SwayMouse>>();
        var mouseMock = new Mock<IMouseService>();
        var vm = new SwayMouse(settingsService, registryMock.Object, loggerMock.Object, mouseMock.Object);

        var ex = Record.Exception(() => vm.Dispose());

        Assert.Null(ex);
    }
}
