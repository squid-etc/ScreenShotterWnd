using System.Text;
using System;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MessageBox = System.Windows.MessageBox;
using Application = System.Windows.Application;

namespace UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly NotifyIcon _notifyIcon;
        private bool _isExiting;

        public MainWindow()
        {
            InitializeComponent();
            _notifyIcon = CreateNotifyIcon();
        }
        private NotifyIcon CreateNotifyIcon()
        {
            var icon = new NotifyIcon
            {
                Icon = new System.Drawing.Icon("App.ico"),
                Visible = true,
                Text = "ScreenshotterWnd"
            };

            var contextMenu = new ContextMenuStrip();
            var closeItem = new ToolStripMenuItem("Close ScreenShotter");
            closeItem.Click += (s, e) => ExitApplication();
            contextMenu.Items.Add(closeItem);

            icon.ContextMenuStrip = contextMenu;
            icon.DoubleClick += (s, e) => RestoreFromTray();

            return icon;
        }

        private void RestoreFromTray()
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        }

        private void ExitApplication()
        {
            _isExiting = true;
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            Application.Current.Shutdown();
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                Hide(); // hide from task bar
            }
            base.OnStateChanged(e);
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (!_isExiting)
            {
                e.Cancel = true;                     // cancel closing
                WindowState = WindowState.Minimized; // call OnStateChanged -> Hide()
            }
            else
            {
                _notifyIcon?.Dispose();
            }
            base.OnClosing(e);
        }
    }
}