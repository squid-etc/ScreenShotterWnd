using System.IO;
using System.Reflection.Metadata.Ecma335;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows.Interop;

namespace ScreenShotter.Engine
{
    public static partial class ScreenShotterManager
    {
        private static bool _bInitialized = false;
        private static HotKeyUiSettings? _settings;
        private static string _uiSettingsJsonFilePath = string.Empty;
        private static HwndSource? _hwndHotKeyFakeWindow;

        /// <summary>
        /// Initialized ScreenShotterManager static class with the path where exe-file stored
        /// Init can be called only once.
        /// </summary>
        /// <param name="baseDirectory"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public static HotKeyUiSettings Init(string baseDirectory)
        {
            if (_bInitialized)
            {
                throw new InvalidOperationException("ScreenShotterManager is already initialized.");
            }

            _uiSettingsJsonFilePath = Path.Combine(baseDirectory, JSON_FILE_NAME);

            _settings = DeserializeSettings(_uiSettingsJsonFilePath);

            _hwndHotKeyFakeWindow = CreateHwndForFakeMessageWindow();
            _bInitialized = true;

            return _settings;
        }

        private static HotKeyUiSettings DeserializeSettings(string sJsonFilePath)
        {
            HotKeyUiSettings? settings = null;

            if (File.Exists(sJsonFilePath))
            {
                try
                {
                    string json = File.ReadAllText(sJsonFilePath);
                    settings = JsonSerializer.Deserialize<HotKeyUiSettings>(json);
                    // TODO: process case when one of class members or all of them are null
                }
                catch (Exception)
                {
                    // just ignore all the exceptions. the empty settings are already created
                    // TODO: write to log file when it will be implemened
                }
            }

            return settings ?? new HotKeyUiSettings();
        }

        private static HwndSource CreateHwndForFakeMessageWindow()
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

        private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if ((msg == WM_HOTKEY) && (HOTKEY_SCREENSHOT == wParam.ToInt32()))
            {
                _ = ScreenCapture.MakeScreenShotAsync();
                handled = true;
            }

            return IntPtr.Zero;
        }

        private static HwndSource GetHotKeyWindow()
        {
            if (_hwndHotKeyFakeWindow == null)
            {
                throw new InvalidOperationException("ScreenShotterManager.Init() was not called.");
            }

            return _hwndHotKeyFakeWindow;
        }

        public static bool HotKeyRegistration(HotKeyUiSettings settings)
        {
            bool result = false;
            uint modifiers = (settings.bModAlt ? MOD_ALT : 0)
                            | (settings.bModCtrl ? MOD_CONTROL : 0)
                            | (settings.bModShift ? MOD_SHIFT : 0);

            bool bSuccessRegistration = RegisterHotKey(GetHotKeyWindow().Handle, HOTKEY_SCREENSHOT, modifiers, settings.nKey);

            if (bSuccessRegistration)
            {
                if (_settings is not null)
                {
                    ScreenCapture.RegisterHotKey(_settings.sPathToFolderWithScreenshots);
                }
                result = true;
                _settings = settings;
                _settings.bHotKeyRegistered = true;
                SerializeSettings();
            }

            return result;
        }

        public static bool HotKeyUnregistration()
        {
            bool result = false;

            bool bSuccessUnregistration = UnregisterHotKey(GetHotKeyWindow().Handle, HOTKEY_SCREENSHOT);
            if (bSuccessUnregistration)
            {
                result = true;
                _settings!.bHotKeyRegistered = false;
                SerializeSettings();
            }
            return result;
        }

        private static void SerializeSettings()
        {
            try
            {
                string json = JsonSerializer.Serialize(_settings);
                File.WriteAllText(_uiSettingsJsonFilePath, json);
            }
            catch (Exception ex)
            {
                // TODO: write to log file when it will be implemened
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        public static void Shutdown()
        {
            UnregisterHotKey(GetHotKeyWindow().Handle, HOTKEY_SCREENSHOT);
            GetHotKeyWindow().RemoveHook(WndProc);
            GetHotKeyWindow().Dispose();
        }
    }
}
