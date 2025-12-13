using System;
using System.Configuration;
using System.Reflection;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Win32;

namespace Sway
{
    [SupportedOSPlatform("windows")]
    public class SwayMouse : BaseViewModel, IDisposable
    {
        const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        const string RunValueName = "Sway";

        public SwayMouse()
        {
            autoStart = bool.Parse(ConfigurationManager.AppSettings["AutoStart"]);
            runWhenWindowsLocked = bool.Parse(ConfigurationManager.AppSettings["RunWhenWindowsLocked"]);
            runAfterSeconds = int.Parse(ConfigurationManager.AppSettings["RunAfterSeconds"]);
            swayLength = int.Parse(ConfigurationManager.AppSettings["SwayLength"]);
            IsRunning = autoStart;

            if (autoStart)
            {
                TrySetAutoRun(true);
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
                SetConfigValue("RunAfterSeconds", value.ToString());
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
                SetConfigValue("SwayLength", value.ToString());
            }
        }

        public bool RunWhenWindowsLocked
        {
            get => runWhenWindowsLocked;
            set
            {
                runWhenWindowsLocked = value;
                SetConfigValue("RunWhenWindowsLocked", value.ToString());
            }
        }

        public bool AutoStart
        {
            get => autoStart;
            set
            {
                try
                {
                    if (TrySetAutoRun(value))
                    {
                        autoStart = value;
                        SetConfigValue("AutoStart", autoStart.ToString());
                    }
                }
                catch
                {
                }
            }
        }

        static bool TrySetAutoRun(bool enabled)
        {
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(RunKeyPath))
                {
                    if (key == null)
                    {
                        return false;
                    }

                    if (enabled)
                    {
                        var exePath = Assembly.GetExecutingAssembly().Location;
                        var command = $"\"{exePath}\" {App.AutoStart}";
                        key.SetValue(RunValueName, command, RegistryValueKind.String);
                    }
                    else
                    {
                        key.DeleteValue(RunValueName, false);
                    }

                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        bool autoStart;
        int runAfterSeconds;
        bool isRunning;
        CancellationTokenSource tokenSource;
        bool isLocked;
        bool isOperator;
        int swayLength;
        bool runWhenWindowsLocked;

        void StartStop()
        {
            IsRunning = !IsRunning;

            isOperator = true;

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

                var task = Task.Run(() =>
                {
                    while (!token.IsCancellationRequested)
                    {
                        if (!isLocked)
                        {
                            var mousePosition = MouseMoveHelper.GetMousePosition();
                            Thread.Sleep(runAfterSeconds * 1000);
                            var nowPosition = MouseMoveHelper.GetMousePosition();

                            if (mousePosition == nowPosition && !token.IsCancellationRequested)
                            {
                                SwayIt();
                            }
                        }
                    }
                }, token);
            }
        }

        void SwayIt()
        {
            MouseMoveHelper.MoveMouse(SwayLength, SwayLength);
            Thread.Sleep(100);
            MouseMoveHelper.MoveMouse(-SwayLength, -SwayLength);
        }

        void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            if (!RunWhenWindowsLocked)
            {
                if (e.Reason == SessionSwitchReason.SessionLock)
                {
                    isLocked = true;
                }
                else if (e.Reason == SessionSwitchReason.SessionUnlock)
                {
                    isLocked = false;

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

        static bool SetConfigValue(string key, string value)
        {
            try
            {
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                if (config.AppSettings.Settings[key] != null)
                {
                    config.AppSettings.Settings[key].Value = value;
                }
                else
                {
                    config.AppSettings.Settings.Add(key, value);
                }

                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
