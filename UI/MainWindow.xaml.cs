using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
 using ScreenShotter.Engine;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly NotifyIcon _notifyIcon;
        private bool _isExiting;
        private ScreenShotterManager _csManager;

        public MainWindow()
        {
            InitializeComponent();
            _notifyIcon = CreateNotifyIcon();
            _csManager = new ScreenShotterManager(AppContext.BaseDirectory);

            List<string> keys = ScreenShotterManager.AvailableKeys.Keys.ToList();
            keys.Insert(0, string.Empty);
            cmbxKey.ItemsSource = keys;

            // we will remove it once we load HotKey from the JSON
            ValidateUiControls();
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

        private void cmbxKey_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ValidateUiControls();
        }

        private void btnBrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    lblFolder.Content = dialog.SelectedPath;
                    ValidateUiControls();
                }
            }
        }

        private void btnClearFolder_Click(object sender, RoutedEventArgs e)
        {
            lblFolder.Content = "PATH";
            ValidateUiControls();
        }

        private void ValidateUiControls()
        {
            string? folderPath = lblFolder.Content as string;
            bool isFolderValid = !string.IsNullOrWhiteSpace(folderPath)
                                  && folderPath != "PATH"
                                  && Directory.Exists(folderPath);

            string? selectedKey = cmbxKey.SelectedItem as string;
            bool isKeySelected = !string.IsNullOrEmpty(selectedKey);

            btnRegister.IsEnabled = isFolderValid && isKeySelected;
        }
    }
}