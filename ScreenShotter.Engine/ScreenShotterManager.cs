using System.IO;
using System.Reflection.Metadata.Ecma335;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows.Interop;

namespace ScreenShotter.Engine
{
    public partial class ScreenShotterManager
    {
        private HotKeyUiSettings _settings;
        private readonly string _uiSettingsJsonFilePath;
        private HwndSource _hwndHotKeyFakeWindow;

        public ScreenShotterManager(string baseDirectory)
        {
            _uiSettingsJsonFilePath = Path.Combine(baseDirectory, JSON_FILE_NAME);

            if (File.Exists(_uiSettingsJsonFilePath))
            {
                string json = File.ReadAllText(_uiSettingsJsonFilePath);
                _settings = JsonSerializer.Deserialize<HotKeyUiSettings>(json) ?? new HotKeyUiSettings();
            }
            else
            {
                _settings = new HotKeyUiSettings();
            }

            _hwndHotKeyFakeWindow = CreateHwndForFakeMessageWindow();
        }

        private HwndSource CreateHwndForFakeMessageWindow()
        {
            HwndSourceParameters parameters = new("ScreenShotterMessageWindow")
            {
                Width = 0,
                Height = 0,
                ParentWindow = new IntPtr(-3)
            };

            HwndSource source = new HwndSource(parameters);
            source.AddHook(WndProc);

            return source;
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if ((msg == WM_HOTKEY) && (HOTKEY_SCREENSHOT == wParam.ToInt32()))
            {
                // OnHotkeyPressed();
                handled = true;
            }

            return IntPtr.Zero;
        }

        public bool HotKeyRegistration(HotKeyUiSettings settings)
        {
            bool result = false;
            uint modifiers = (settings.bAltMod ? MOD_ALT : 0)
                            | (settings.bCtrlMod ? MOD_CONTROL : 0)
                            | (settings.bShiftMod ? MOD_SHIFT : 0);

            bool bSuccessRegistration = RegisterHotKey(_hwndHotKeyFakeWindow.Handle, HOTKEY_SCREENSHOT, modifiers, settings.nKey);

            if (bSuccessRegistration)
            {
                result = true;
                _settings = settings;
                _settings.bHotKeyRegistered = true;
                //SerializeSettings();
            }

            return result;
        }

        public bool HotKeyUnregistration()
        {
            bool result = false;

            bool bSuccessUnregistration = UnregisterHotKey(_hwndHotKeyFakeWindow.Handle, HOTKEY_SCREENSHOT);
            if (bSuccessUnregistration)
            {
                result = true;
                _settings.bHotKeyRegistered = false;
                //SerializeSettings();
            }
            return result;
        }

        private void SerializeSettings()
        {
            string json = JsonSerializer.Serialize(_settings);
            File.WriteAllText(_uiSettingsJsonFilePath, json);
        }

        public void Shutdown()
        {
            UnregisterHotKey(_hwndHotKeyFakeWindow.Handle, HOTKEY_SCREENSHOT);
            _hwndHotKeyFakeWindow.RemoveHook(WndProc);
            _hwndHotKeyFakeWindow.Dispose();
        }
    }
}
