using System.Runtime.Versioning;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace Sway
{
    [SupportedOSPlatform("windows")]
    public class SwayMouse : BaseViewModel, IDisposable
    {
        readonly SettingsService _settingsService;
        readonly IRegistryService _registryService;
        readonly ILogger<SwayMouse> _logger;
        readonly IMouseService _mouseService;

        public SwayMouse(SettingsService settingsService, IRegistryService registryService, ILogger<SwayMouse> logger, IMouseService mouseService)
        {
            _settingsService = settingsService;
            _registryService = registryService;
            _logger = logger;
            _mouseService = mouseService;

            var settings = settingsService.Current;
            autoStart = settings.AutoStart;
            runWhenWindowsLocked = settings.RunWhenWindowsLocked;
            runAfterSeconds = settings.RunAfterSeconds;
            swayLength = settings.SwayLength;
            IsRunning = autoStart;

            _logger.LogDebug("SwayMouse initialized: AutoStart={AutoStart}, RunAfterSeconds={RunAfterSeconds}, SwayLength={SwayLength}",
                autoStart, runAfterSeconds, swayLength);

            if (autoStart)
            {
                _registryService.TrySetAutoRun(true);
            }

            RunOrStopRunning();

            StartStopCommand = new CommandHandler(StartStop, () => !isOperator);

            SystemEvents.SessionSwitch += new SessionSwitchEventHandler(SystemEvents_SessionSwitch);
        }

        public ICommand StartStopCommand { get; set; }

        public bool IsRunning
        {
            get => isRunning;
            private set
            {
                isRunning = value;
                NotifyPropertyChange(nameof(IsRunning));
                NotifyPropertyChange(nameof(RunningStatus));
            }
        }

        public string RunningStatus => IsRunning ? "Running" : "Stopped";

        public int RunAfterSeconds
        {
            get => runAfterSeconds;
            set
            {
                if (value > 1000 || value < 2)
                {
                    value = 20;
                }

                runAfterSeconds = value;
                PersistSettings();
            }
        }

        public int SwayLength
        {
            get => swayLength;
            set
            {
                if (value < 0 || value > 50)
                {
                    value = 1;
                }

                swayLength = value;
                PersistSettings();
            }
        }

        public bool RunWhenWindowsLocked
        {
            get => runWhenWindowsLocked;
            set
            {
                runWhenWindowsLocked = value;
                PersistSettings();
            }
        }

        public bool AutoStart
        {
            get => autoStart;
            set
            {
                try
                {
                    if (_registryService.TrySetAutoRun(value))
                    {
                        autoStart = value;
                        PersistSettings();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to set AutoStart to {Value}", value);
                }
            }
        }

        bool autoStart;
        int runAfterSeconds;
        bool isRunning;
        CancellationTokenSource? tokenSource;
        bool isLocked;
        bool isOperator;
        int swayLength;
        bool runWhenWindowsLocked;

        void PersistSettings()
        {
            try
            {
                _settingsService.Save(new SwaySettings
                {
                    AutoStart = autoStart,
                    RunWhenWindowsLocked = runWhenWindowsLocked,
                    RunAfterSeconds = runAfterSeconds,
                    SwayLength = swayLength
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist settings");
            }
        }

        void StartStop()
        {
            IsRunning = !IsRunning;

            isOperator = true;

            _logger.LogInformation("Sway {Status}", IsRunning ? "started" : "stopped");

            RunOrStopRunning();

            isOperator = false;
        }

        void RunOrStopRunning()
        {
            tokenSource?.Cancel();
            tokenSource?.Dispose();
            tokenSource = null;

            if (IsRunning)
            {
                tokenSource = new CancellationTokenSource();
                var token = tokenSource.Token;

                var task = Task.Run(async () =>
                {
                    while (!token.IsCancellationRequested)
                    {
                        try
                        {
                            if (!isLocked)
                            {
                                var mousePosition = _mouseService.GetMousePosition();
                                await Task.Delay(runAfterSeconds * 1000, token);
                                var nowPosition = _mouseService.GetMousePosition();

                                if (mousePosition == nowPosition && !token.IsCancellationRequested)
                                {
                                    SwayIt(token);
                                }
                            }
                            else
                            {
                                await Task.Delay(1000, token);
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error in sway loop");
                        }
                    }
                }, token);
            }
        }

        void SwayIt(CancellationToken token)
        {
            _mouseService.MoveMouse(SwayLength, SwayLength);
            token.ThrowIfCancellationRequested();
            Thread.Sleep(100);
            _mouseService.MoveMouse(-SwayLength, -SwayLength);
        }

        void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            if (!RunWhenWindowsLocked)
            {
                if (e.Reason == SessionSwitchReason.SessionLock)
                {
                    isLocked = true;
                    _logger.LogDebug("Session locked, sway paused");
                }
                else if (e.Reason == SessionSwitchReason.SessionUnlock)
                {
                    isLocked = false;
                    _logger.LogDebug("Session unlocked, sway resumed");

                    RunOrStopRunning();
                }
            }
        }

        public void Dispose()
        {
            SystemEvents.SessionSwitch -= new SessionSwitchEventHandler(SystemEvents_SessionSwitch);
            tokenSource?.Cancel();
            tokenSource?.Dispose();
            tokenSource = null;
        }
    }
}
