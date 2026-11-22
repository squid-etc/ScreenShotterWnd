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
        private const string csEmptyPath = "EMPTY PATH";

        public MainWindow()
        {
            InitializeComponent();
            _notifyIcon = CreateNotifyIcon();
            HotKeyUiSettings settings =  ScreenShotterManager.Init(AppContext.BaseDirectory);

            List<string> keys = ScreenShotterManager.AvailableKeys.Keys.ToList();
            keys.Insert(0, string.Empty);
            cmbxKey.ItemsSource = keys;

            // after deserialize the settings, fill in the UI and validate the UI
            FillInUiBySettingsFromManager(settings);
            ValidateRegisterButtonAccessibility();
            
            // lets try to register the Hot Key as it was on the previous start
            if ((btnRegister.IsEnabled) && (settings.bHotKeyRegistered))
            {
                btnRegister_Click(btnRegister, new RoutedEventArgs());
            }
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

        private void FillInUiBySettingsFromManager(HotKeyUiSettings settings)
        {
            cbAltMod.IsChecked = settings.bModAlt;
            cbCtrlMod.IsChecked = settings.bModCtrl;
            cbShiftMod.IsChecked = settings.bModShift;

            cmbxKey.SelectedItem = ScreenShotterManager.GetKeyName(settings.nKey);

            lblFolder.Content = IsFolderValid(settings.sPathToFolderWithScreenshots) ? settings.sPathToFolderWithScreenshots : csEmptyPath;
        }

        private static bool IsFolderValid(string? sFolderPath)
        {
            return !string.IsNullOrWhiteSpace(sFolderPath)
                   && sFolderPath != csEmptyPath
                   && Directory.Exists(sFolderPath);
        }

        private void cbModificators_CheckedChanged(object sender, RoutedEventArgs e)
        {
            ValidateRegisterButtonAccessibility();
        }

        private void cmbxKey_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ValidateRegisterButtonAccessibility();
        }

        private void btnBrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    lblFolder.Content = dialog.SelectedPath;
                    ValidateRegisterButtonAccessibility();
                }
            }
        }

        private void btnClearFolder_Click(object sender, RoutedEventArgs e)
        {
            lblFolder.Content = csEmptyPath;
            ValidateRegisterButtonAccessibility();
        }

        private void ValidateRegisterButtonAccessibility()
        {
            string? selectedKey = cmbxKey.SelectedItem as string;
            bool isKeySelected = !string.IsNullOrEmpty(selectedKey);

            string? folderPath = lblFolder.Content as string;
            bool isFolderValid = IsFolderValid(folderPath);

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

            settings.bModAlt = cbAltMod.IsChecked == true;
            settings.bModCtrl = cbCtrlMod.IsChecked == true;
            settings.bModShift = cbShiftMod.IsChecked == true;
            string sKey = (string)cmbxKey.SelectedItem;
            settings.nKey = ScreenShotterManager.AvailableKeys[sKey];
            settings.sPathToFolderWithScreenshots = (string)lblFolder.Content;

            if (ScreenShotterManager.HotKeyRegistration(settings))
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
            if (ScreenShotterManager.HotKeyUnregistration())
            {
                // successfully UNregistered Hot Key
                EnableUiControlsOnHotKeyRegistaion(true);
                ValidateRegisterButtonAccessibility();

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

            if (settings.bModAlt)
            {
                hotKeyComponent.Add("Alt");
            }

            if (settings.bModCtrl)
            {
                hotKeyComponent.Add("Ctrl");
            }

            if (settings.bModShift)
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