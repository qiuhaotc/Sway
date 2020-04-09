using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Win32;

namespace Sway
{
    public class SwayMouse : BaseViewModel, IDisposable
    {
        public SwayMouse()
        {
            autoStart = bool.Parse(ConfigurationManager.AppSettings["AutoStart"]);
            runWhenWindowsLocked = bool.Parse(ConfigurationManager.AppSettings["RunWhenWindowsLocked"]);
            runAfterSeconds = int.Parse(ConfigurationManager.AppSettings["RunAfterSeconds"]);
            swayLength = int.Parse(ConfigurationManager.AppSettings["SwayLength"]);
            IsRunning = autoStart;

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
                var appPath = Assembly.GetExecutingAssembly().Location;
                var path = Path.Combine(new FileInfo(appPath).DirectoryName, "AutoStart.exe");

                try
                {
                    using (var process = Process.Start(new ProcessStartInfo(path, $"{value} Sway {appPath}")
                    {
                        Verb = "runas"
                    }))
                    {
                        process?.WaitForExit();
                        autoStart = value;
                        SetConfigValue("AutoStart", autoStart.ToString());
                    }
                }
                catch
                {
                }
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
