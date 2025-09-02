// WPF-адаптована версія Settings
using System;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows;
using WatchedAnimeList.Helpers;

namespace WatchedAnimeList.Logic
{
    public static class Settings
    {
        public static IniFile iniFile = null!;
        private static Window window = null!;

        public static void Initialize()
        {
            window = MainWindow.Global;
            Load();
        }

        public static void Save()
        {
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string folderPath = Path.Combine(documentsPath, "RE ZERO", "WachedAnimeList");
            Directory.CreateDirectory(folderPath);

            string filePath = Path.Combine(folderPath, "Settings.ini");
            var config = new IniFile(filePath);

            if (window.WindowState != WindowState.Maximized)
            {
                var width = (int)window.Width;
                var height = (int)window.Height;
                config.Write("Size", $"{width}/{height}", "MainWindow");
            }

            if (window.WindowState != WindowState.Minimized)
            {
                config.Write("WindowState", window.WindowState.ToString(), "MainWindow");
            }
        }

        public static void Load()
        {
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string filePath = Path.Combine(documentsPath, "RE ZERO", "WachedAnimeList", "Settings.ini");

            if (!File.Exists(filePath)) return;

            var config = new IniFile(filePath);

            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;

            string size = config.Read("Size", "MainWindow");
            var s = size.Split('/');
            int width = 0;
            int height = 0;
            if (!string.IsNullOrWhiteSpace(size))
            {
                if (s.Length == 2 && int.TryParse(s[0], out int _width) && int.TryParse(s[1], out int _height))
                {
                    width = _width;
                    height = _height;
                }
            }
            else
            {
                width = 1280;
                height = 720;
            }
            window.Width = Math.Min(width, screenWidth);
            window.Height = Math.Min(height, screenHeight);

            if (width <= screenWidth)
                {
                    var deltaWidth = screenWidth - width;
                        window.Left = deltaWidth/2;
            }

            if (height <= screenHeight)
                {
                    var deltaHeight = screenHeight - height;
                    window.Top = deltaHeight / 2;
                }
            
            if (width == screenWidth)
                window.Left = 0;
            if(height == screenHeight)
                window.Top = 0;

            string windowStateStr = config.Read("WindowState", "MainWindow");
            if (Enum.TryParse(windowStateStr, out WindowState state))
            {
                window.WindowState = state;
            }
            else
            {
                window.WindowState = WindowState.Maximized;
            }
        }
    }

    public class IniFile
    {
        readonly string Path;
        readonly string? EXE = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        static extern long WritePrivateProfileString(string Section, string Key, string Value, string FilePath);

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        static extern int GetPrivateProfileString(string Section, string Key, string Default, StringBuilder RetVal, int Size, string FilePath);

        public IniFile(string? iniPath = null)
        {
            Path = new FileInfo(iniPath ?? EXE + ".ini").FullName;
        }

        public string Read(string Key, string? Section = null)
        {
            if (EXE is null)
                Debug.Ex("EXE is null");

            var RetVal = new StringBuilder(255);
            GetPrivateProfileString(Section ?? EXE, Key, "", RetVal, 255, Path);
            return RetVal.ToString();
        }

        public void Write(string Key, string Value, string? Section = null)
        {
            if (EXE is null)
                Debug.Ex("EXE is null");

            WritePrivateProfileString(Section ?? EXE, Key, Value, Path);
        }

        public void DeleteKey(string Key, string? Section = null)
        {
            if (EXE is null)
                Debug.Ex("EXE is null");

            Write(Key, string.Empty, Section ?? EXE);
        }

        public void DeleteSection(string? Section = null)
        {
            if (EXE is null)
                Debug.Ex("EXE is null");

            Write(string.Empty, string.Empty, Section ?? EXE);
        }

        public bool KeyExists(string Key, string? Section = null)
        {
            return Read(Key, Section).Length > 0;
        }
    }
}
