using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
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
            // TODO: Hide When Auto Start
            foreach (string s in e.Args)
            {
                if (s == "autostart")
                {
                    IsAutoStart = true;
                    break;
                }
            }
        }

        internal static bool IsAutoStart;
    }
}
