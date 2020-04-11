using System.Windows;

namespace Sway
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        void Application_Startup(object sender, StartupEventArgs e)
        {
            foreach (var s in e.Args)
            {
                if (s == "autostart")
                {
                    IsAutoStart = true;
                }
            }
        }

        internal static bool IsAutoStart { get; private set; }

        public const string AutoStart = "autostart";
    }
}
