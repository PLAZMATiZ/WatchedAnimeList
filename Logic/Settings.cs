// WPF-адаптована версія Settings
using System;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows;
using WatchedAnimeList.Helpers;

namespace WatchedAnimeList.Logic
{
    public class Settings
    {
        public static Settings Global { get; private set; }
        public IniFile iniFile;
        private readonly Window window;

        public Settings()
        {
            window = MainWindow.Global;
            Global = this;
            LoadSettings();
        }

        public void SaveSettings()
        {
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string folderPath = Path.Combine(documentsPath, "RE ZERO", "WachedAnimeList");
            Directory.CreateDirectory(folderPath);

            string filePath = Path.Combine(folderPath, "Settings.ini");
            var config = new IniFile(filePath);

            if (window.WindowState != WindowState.Maximized)
            {
                var left = (int)window.Left;
                var top = (int)window.Top;
                config.Write("Position", $"{left}/{top}", "MainWindow");

                var width = (int)window.Width;
                var height = (int)window.Height;
                config.Write("Size", $"{width}/{height}", "MainWindow");
            }

            if (window.WindowState != WindowState.Minimized)
            {
                config.Write("WindowState", window.WindowState.ToString(), "MainWindow");
            }
        }

        public void LoadSettings()
        {
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string filePath = Path.Combine(documentsPath, "RE ZERO", "WachedAnimeList", "Settings.ini");

            if (!File.Exists(filePath)) return;

            var config = new IniFile(filePath);

            string position = config.Read("Position", "MainWindow");
            if (!string.IsNullOrWhiteSpace(position))
            {
                var pos = position.Split('/');
                if (pos.Length == 2 && int.TryParse(pos[0], out int left) && int.TryParse(pos[1], out int top))
                {
                    window.Left = left;
                    window.Top = top;
                }
            }

            string size = config.Read("Size", "MainWindow");
            if (!string.IsNullOrWhiteSpace(size))
            {
                var s = size.Split('/');
                if (s.Length == 2 && int.TryParse(s[0], out int width) && int.TryParse(s[1], out int height))
                {
                    window.Width = width;
                    window.Height = height;
                }
            }

            string windowStateStr = config.Read("WindowState", "MainWindow");
            if (Enum.TryParse(windowStateStr, out WindowState state))
            {
                window.WindowState = state;
            }
        }
    }

    public class IniFile
    {
        string Path;
        string EXE = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        static extern long WritePrivateProfileString(string Section, string Key, string Value, string FilePath);

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        static extern int GetPrivateProfileString(string Section, string Key, string Default, StringBuilder RetVal, int Size, string FilePath);

        public IniFile(string iniPath = null)
        {
            Path = new FileInfo(iniPath ?? EXE + ".ini").FullName;
        }

        public string Read(string Key, string Section = null)
        {
            var RetVal = new StringBuilder(255);
            GetPrivateProfileString(Section ?? EXE, Key, "", RetVal, 255, Path);
            return RetVal.ToString();
        }

        public void Write(string Key, string Value, string Section = null)
        {
            WritePrivateProfileString(Section ?? EXE, Key, Value, Path);
        }

        public void DeleteKey(string Key, string Section = null)
        {
            Write(Key, null, Section ?? EXE);
        }

        public void DeleteSection(string Section = null)
        {
            Write(null, null, Section ?? EXE);
        }

        public bool KeyExists(string Key, string Section = null)
        {
            return Read(Key, Section).Length > 0;
        }
    }
}
