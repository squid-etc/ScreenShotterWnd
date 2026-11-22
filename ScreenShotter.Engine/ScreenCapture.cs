using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices.ActiveDirectory;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using static System.Net.Mime.MediaTypeNames;

namespace ScreenShotter.Engine
{
    public static class ScreenCapture
    {
        private const int SM_CXSCREEN = 0;
        private const int SM_CYSCREEN = 1;

        private static string _sFolderPath = "";
        private static int _screenWidth = 0; 
        private static int _screenHeight = 0;

        [DllImport("user32.dll")]
        static extern int GetSystemMetrics(int nIndex);

        public static void RegisterHotKey(string sPathToFolderWithScreenshots)
        {
            _sFolderPath = sPathToFolderWithScreenshots;
            _screenWidth = GetSystemMetrics(SM_CXSCREEN);
            _screenHeight = GetSystemMetrics(SM_CYSCREEN);
        }

        public static void UnregisterHotKey()
        {
            _sFolderPath = "";
            _screenWidth = 0;
            _screenHeight = 0;
        }

        public static Task MakeScreenShotAsync()
        {
            return Task.Run(() => MakeScreenShot());
        }

        private static void MakeScreenShot()
        {
            try
            {
                using (Bitmap bmp = new Bitmap(_screenWidth, _screenHeight))
                {
                    using (Graphics graph = Graphics.FromImage(bmp))
                    {
                        graph.CopyFromScreen(0, 0, 0, 0, bmp.Size);

                        // TODO: throw an error and write to logs if screen resolution was changed and
                        // allocated bitmap siza is not enough for the new screen resolution
                    }

                    if (!Directory.Exists(_sFolderPath))
                    {
                        // creates folder anyway
                        Directory.CreateDirectory(_sFolderPath);
                        // TODO: write to logs about this
                    }

                    string fullPath = Path.Combine(_sFolderPath, GenerateFileName());

                    bmp.Save(fullPath, ImageFormat.Jpeg);
                }
            }
            catch (Exception ex)
            {
                // TODO: write to logs about this
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        private static string GenerateFileName()
        {
            return $"scsh_{DateTime.Now:yyyy-MM-dd_HH-mm-ss-fff}.jpg";
        }
    }
}
