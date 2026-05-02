using System.Runtime.InteropServices;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

[assembly: ComVisible(false)]
[assembly: ThemeInfo(
    ResourceDictionaryLocation.None,
    ResourceDictionaryLocation.SourceAssembly
)]

namespace Sway
{
    public partial class App : System.Windows.Application
    {
        internal static IServiceProvider Services { get; private set; } = null!;

        void Application_Startup(object sender, StartupEventArgs e)
        {
            foreach (var s in e.Args)
            {
                if (s == AutoStart)
                {
                    IsAutoStart = true;
                }
            }

            var services = new ServiceCollection();

            var jsonPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            services.AddSingleton(new SettingsService(jsonPath));
            services.AddSingleton<IRegistryService, RegistryService>();
            services.AddSingleton<IMouseService, MouseMoveHelper>();
            services.AddSingleton<SwayMouse>();

            services.AddLogging(builder =>
            {
                builder.AddDebug();
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            Services = services.BuildServiceProvider();

            AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
            {
                var logger = Services.GetService<ILogger<App>>();
                logger?.LogCritical((Exception)ex.ExceptionObject, "Unhandled domain exception");
            };

            DispatcherUnhandledException += (s, ex) =>
            {
                var logger = Services.GetService<ILogger<App>>();
                logger?.LogError(ex.Exception, "Unhandled dispatcher exception");
                ex.Handled = true;
            };
        }

        internal static bool IsAutoStart { get; private set; }

        public const string AutoStart = "autostart";
    }
}
