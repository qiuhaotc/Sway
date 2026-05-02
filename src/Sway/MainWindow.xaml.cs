using System.ComponentModel;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace Sway
{
    [SupportedOSPlatform("windows")]
    public partial class MainWindow : Window
    {
        private NotifyIcon? NotifyIcon { get; set; }
        private ToolStripMenuItem[]? AreaMenuItems { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            InitWindow();

            if (App.IsAutoStart)
            {
                Hide();
            }
        }

        private void InitWindow()
        {
            DataContext = App.Services.GetRequiredService<SwayMouse>();

            NotifyIcon = new NotifyIcon();
            NotifyIcon.Text = "Sway";
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                NotifyIcon.Icon = Properties.Resources.Icon;
            }

            NotifyIcon.Visible = true;

            NotifyIcon.DoubleClick += (sender, e) =>
            {
                if (IsVisible)
                {
                    Hide();
                }
                else
                {
                    Show();
                    Activate();
                }
            };

            AreaMenuItems = new[]
            {
                new ToolStripMenuItem("Close", null, (sender, e) => ExitApp())
            };

            if (AreaMenuItems != null && AreaMenuItems.Length > 0)
            {
                var menu = new ContextMenuStrip();
                menu.Items.AddRange(AreaMenuItems);
                NotifyIcon.ContextMenuStrip = menu;
            }

            Closing += MainWindow_Closing;
        }

        void ExitApp()
        {
            ((SwayMouse)DataContext).Dispose();
            NotifyIcon?.Dispose();
            Environment.Exit(0);
        }

        void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            Hide();
            e.Cancel = true;
        }

        protected override void OnClosed(EventArgs e)
        {
            ((SwayMouse)DataContext).Dispose();
            base.OnClosed(e);
        }

        void Button_Click(object sender, RoutedEventArgs e)
        {
            ExitApp();
        }
    }
}
