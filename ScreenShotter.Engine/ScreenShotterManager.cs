using System.Text.Json;
using System.Text.Json.Nodes;

namespace ScreenShotter.Engine
{
    public partial class ScreenShotterManager
    {
        private SettingsForUI _settings;
        private readonly string _uiSettingsJsonFilePath;

        public ScreenShotterManager(string baseDirectory)
        {
            _uiSettingsJsonFilePath = Path.Combine(baseDirectory, JSON_FILE_NAME);

            if (!File.Exists(_uiSettingsJsonFilePath))
            {
                _settings = new SettingsForUI();
            }
            else
            {
                string json = File.ReadAllText(_uiSettingsJsonFilePath);
                _settings = JsonSerializer.Deserialize<SettingsForUI>(json) ?? new SettingsForUI();
            }
        }

        //public HotkeyRegistrationResult RegisterHotkey(SettingsForUI settings)
        //{
        //    bool success = RegisterHotKey(/* hWnd, id, modifiers, vk из settings */);
        //    RegisterHotKey()

        //    if (success)
        //    {
        //        SaveSettings(settings);
        //        return HotkeyRegistrationResult.Success;
        //    }

        //    return HotkeyRegistrationResult.Failed;
        //}

        private void SaveSettings()
        {
            string json = JsonSerializer.Serialize(_settings);
            File.WriteAllText(_uiSettingsJsonFilePath, json);
        }
    }
}
