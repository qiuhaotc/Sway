using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Forms;

namespace Sway
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public NotifyIcon NotifyIcon { get; private set; }
        public MenuItem[] AreaMenuItems { get; private set; }

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
            DataContext = new SwayMouse();

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
                new MenuItem("Close", (sender, e)=>{
                    ExitApp();
                })
            };

            if (AreaMenuItems != null && AreaMenuItems.Length > 0)
            {
                NotifyIcon.ContextMenu = new ContextMenu(AreaMenuItems.ToArray());
            }

            Closing += MainWindow_Closing;
        }

        void ExitApp()
        {
            NotifyIcon.Dispose();
            Environment.Exit(0);
        }

        void MainWindow_Closing(object sender, CancelEventArgs e)
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
