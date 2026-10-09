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
        private const string csEmptyPath = "EMPTY PATH";

        public MainWindow()
        {
            InitializeComponent();
            _notifyIcon = CreateNotifyIcon();
            _csManager = new ScreenShotterManager(AppContext.BaseDirectory);

            List<string> keys = ScreenShotterManager.AvailableKeys.Keys.ToList();
            keys.Insert(0, string.Empty);
            cmbxKey.ItemsSource = keys;

            // TODO: we will remove it once we load Hot Key from the JSON
            ValidateUIControlsBeforeHotKeyRegisteration();
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

        private void cbModificators_CheckedChanged(object sender, RoutedEventArgs e)
        {
            ValidateUIControlsBeforeHotKeyRegisteration();
        }

        private void cmbxKey_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ValidateUIControlsBeforeHotKeyRegisteration();
        }

        private void btnBrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    lblFolder.Content = dialog.SelectedPath;
                    ValidateUIControlsBeforeHotKeyRegisteration();
                }
            }
        }

        private void btnClearFolder_Click(object sender, RoutedEventArgs e)
        {
            lblFolder.Content = csEmptyPath;
            ValidateUIControlsBeforeHotKeyRegisteration();
        }

        private void ValidateUIControlsBeforeHotKeyRegisteration()
        {
            string? selectedKey = cmbxKey.SelectedItem as string;
            bool isKeySelected = !string.IsNullOrEmpty(selectedKey);

            string? folderPath = lblFolder.Content as string;
            bool isFolderValid = !string.IsNullOrWhiteSpace(folderPath)
                                  && folderPath != csEmptyPath
                                  && Directory.Exists(folderPath);
            bool isModifierUsed = cbAltMod.IsChecked == true
                                  || cbCtrlMod.IsChecked == true
                                  || cbShiftMod.IsChecked == true;

            List<string> problems = new List<string>();

            if (!isModifierUsed)
            {
                problems.Add("Modifier is not used.");
            }

            if (!isKeySelected)
            {
                problems.Add("Key is not selected.");
            }

            if (!isFolderValid)
            {
                problems.Add("Folder for screenshots is not set.");
            }

            if (isModifierUsed && isKeySelected && isFolderValid)
            {
                problems.Add("Everything is ready for the Hot Key registration.");
                btnRegister.IsEnabled = true;
                btnUnregister.IsEnabled = false;
            }
            else
            {
                btnRegister.IsEnabled = false;
            }

            lblStatus.Content = "status: " + string.Join(" | ", problems);
        }

        private void btnRegister_Click(object sender, RoutedEventArgs e)
        {
            HotKeyUiSettings settings = new HotKeyUiSettings();

            settings.bAltMod = cbAltMod.IsChecked == true;
            settings.bCtrlMod = cbCtrlMod.IsChecked == true;
            settings.bShiftMod = cbShiftMod.IsChecked == true;
            string sKey = (string)cmbxKey.SelectedItem;
            settings.nKey = ScreenShotterManager.AvailableKeys[sKey];
            settings.sPathForScreenshots = (string)lblFolder.Content;

            if (_csManager.HotKeyRegistration(settings))
            {
                // successfully registered Hot Key
                lblHotKey.Content = PrepareHotKeyStrForStatus(settings, sKey);
                lblStatus.Content = "status: " + lblHotKey.Content;

                EnableUiControlsOnHotKeyRegistaion(false);
            }
            else
            {
                lblHotKey.Content = "Hot Key registeration failed";

                EnableUiControlsOnHotKeyRegistaion(true);
            }
        }

        private void btnUnregister_Click(object sender, RoutedEventArgs e)
        {
            if (_csManager.HotKeyUnregistration())
            {
                // successfully UNregistered Hot Key
                EnableUiControlsOnHotKeyRegistaion(true);
                ValidateUIControlsBeforeHotKeyRegisteration();

                lblHotKey.Content = "Registered Hot Key: N/A";
            }
            else
            {
                lblHotKey.Content = "Hot Key UNregisteration failed";

                EnableUiControlsOnHotKeyRegistaion(false);
            }
        }

        private string PrepareHotKeyStrForStatus(HotKeyUiSettings settings, string sKey)
        {
            List<string> hotKeyComponent = new List<string>();

            if (settings.bAltMod)
            {
                hotKeyComponent.Add("Alt");
            }

            if (settings.bCtrlMod)
            {
                hotKeyComponent.Add("Ctrl");
            }

            if (settings.bShiftMod)
            {
                hotKeyComponent.Add("Shift");
            }

            hotKeyComponent.Add(sKey);

            string result = "Registered Hot Key: " + string.Join(" + ", hotKeyComponent);

            return result;
        }

        private void EnableUiControlsOnHotKeyRegistaion(bool bHotKeyRegisteredSok)
        {
            cbAltMod.IsEnabled = bHotKeyRegisteredSok;
            cbCtrlMod.IsEnabled = bHotKeyRegisteredSok;
            cbShiftMod.IsEnabled = bHotKeyRegisteredSok;

            lblKey.IsEnabled = bHotKeyRegisteredSok;
            cmbxKey.IsEnabled = bHotKeyRegisteredSok;

            lblFolder.IsEnabled = bHotKeyRegisteredSok;
            btnBrowseFolder.IsEnabled = bHotKeyRegisteredSok;
            btnClearFolder.IsEnabled = bHotKeyRegisteredSok;

            btnRegister.IsEnabled = bHotKeyRegisteredSok;
            btnUnregister.IsEnabled = !bHotKeyRegisteredSok;
        }
    }
}