using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenShotter.Engine
{
    /// <summary>
    /// class for storing the UI settings and serialize these settings to the JSON file
    /// </summary>
    [Serializable]
    public class HotKeyUiSettings
    {
        public bool bAltMod { get; set; } = false;
        public bool bCtrlMod { get; set; } = false;
        public bool bShiftMod { get; set; } = false;
        public uint nKey {  get; set; }
        public string? sPathForScreenshots { get; set; } = "";
        /// <summary>
        /// property about was Hot Key registered successfully when App ran last time.
        /// </summary>
        public bool bHotKeyRegistered { get; set; } = false;

    }
}
