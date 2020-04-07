using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Sway
{
    public class SwayMouse : BaseViewModel
    {
        public SwayMouse()
        {
            autoStart = bool.Parse(ConfigurationManager.AppSettings["AutoStart"]);
        }

        bool autoStart;

        public bool AutoStart
        {
            get => autoStart;
            set
            {
                autoStart = value;

                var appPath = Assembly.GetExecutingAssembly().Location;
                var path = Path.Combine(new FileInfo(appPath).DirectoryName, "AutoStart.exe");

                try
                {
                    using (var process = Process.Start(new ProcessStartInfo(path, $"{autoStart} Sway {appPath}")
                    {
                        Verb = "runas"
                    }))
                    {
                        process?.WaitForExit();
                        SetConfigValue("AutoStart", autoStart.ToString());
                    }
                }
                catch
                {
                }
            }
        }

        public static bool SetConfigValue(string key, string value)
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
