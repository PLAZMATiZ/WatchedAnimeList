using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WatchedAnimeList.ViewModels
{
    public class MpvPlayer : Control, IDisposable
    {
        private IntPtr mpvHandle;
        private IntPtr hwnd;

        [DllImport("libmpv-2.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr mpv_create();

        [DllImport("libmpv-2.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int mpv_initialize(IntPtr ctx);

        [DllImport("libmpv-2.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void mpv_destroy(IntPtr ctx);

        [DllImport("libmpv-2.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int mpv_command(IntPtr ctx, string[] args);

        public void ExecuteCommand(string[] args)
        {
            // Переконайся, що останній елемент = null, як вимагає mpv
            var listWithNull = args.Concat(new string[] {  }).ToArray();
            mpv_command(mpvHandle, listWithNull);
        }

        [DllImport("libmpv-2.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int mpv_command_string(IntPtr ctx, [MarshalAs(UnmanagedType.LPStr)] string cmd);
        [DllImport("libmpv-2.dll")]
        private static extern int mpv_get_property(IntPtr ctx, string name, mpv_format format, out double data);

        public enum mpv_format
        {
            MPV_FORMAT_NONE = 0,
            MPV_FORMAT_STRING = 1,
            MPV_FORMAT_OSD_STRING = 2,
            MPV_FORMAT_FLAG = 3,
            MPV_FORMAT_INT64 = 4,
            MPV_FORMAT_DOUBLE = 5,
            MPV_FORMAT_NODE = 6,
            MPV_FORMAT_NODE_ARRAY = 7,
            MPV_FORMAT_NODE_MAP = 8,
            MPV_FORMAT_BYTE_ARRAY = 9
        }

        public void ExecuteCommand(string cmd)
        {
            if (mpvHandle == IntPtr.Zero)
                throw new InvalidOperationException("mpv context is not initialized");

            int ret = mpv_command_string(mpvHandle, cmd);
            if (ret < 0)
                throw new Exception($"mpv command failed: {cmd}");
        }

        [DllImport("libmpv-2.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int mpv_set_option_string(IntPtr ctx, string name, string value);


        public MpvPlayer()
        {
            this.HandleCreated += MpvPlayer_HandleCreated;
            this.HandleDestroyed += MpvPlayer_HandleDestroyed;
            this.SetStyle(ControlStyles.UserPaint, true);
            this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
        }
        public double GetProperty(string property)
        {
            double value = 0;
            int status = mpv_get_property(mpvHandle, property, mpv_format.MPV_FORMAT_DOUBLE, out value);
            if (status == 0)
                return value;
            return 0;
        }


        private void MpvPlayer_HandleCreated(object? sender, EventArgs e)
        {
            mpvHandle = mpv_create();
            if (mpvHandle == IntPtr.Zero)
                throw new Exception("Failed to create mpv context");

            hwnd = this.Handle;

            mpv_set_option_string(mpvHandle, "log-file", "mpv.log");
            mpv_set_option_string(mpvHandle, "msg-level", "all=v");


            if (mpv_set_option_string(mpvHandle, "wid", hwnd.ToInt64().ToString()) < 0)
                throw new Exception("Failed to set wid");

            if (mpv_set_option_string(mpvHandle, "vo", "gpu") < 0)
                throw new Exception("Failed to set vo");

            if (mpv_set_option_string(mpvHandle, "gpu-context", "d3d11") < 0)
                throw new Exception("Failed to set gpu-context");

            // Додаємо шейдер anime4k
            string shaderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "shaders", "Anime4K_HQ.glsl");
            if (mpv_set_option_string(mpvHandle, "glsl-shaders", shaderPath) < 0)
                throw new Exception("Failed to set glsl-shaders");

            if (mpv_set_option_string(mpvHandle, "mute", "no") < 0)
                throw new Exception("Failed to set mute");

            if (mpv_set_option_string(mpvHandle, "log-file", "mpv.log") < 0)
                throw new Exception("Failed to set log-file");

            if (mpv_set_option_string(mpvHandle, "msg-level", "all=v") < 0)
                throw new Exception("Failed to set msg-level");

            int ret = mpv_initialize(mpvHandle);
            if (ret < 0)
                throw new Exception("mpv initialization failed");
        }
        public double GetTimePosition()
        {
            return GetProperty("time-pos");
        }

        public double GetDuration()
        {
            return GetProperty("duration");
        }

        private void MpvPlayer_HandleDestroyed(object? sender, EventArgs e)
        {
            if (mpvHandle != IntPtr.Zero)
            {
                mpv_destroy(mpvHandle);
                mpvHandle = IntPtr.Zero;
            }
        }

        public void Load(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Video not found", path);

            var args = new string[]
            {
                "loadfile",
                path
            };

            int status = mpv_command(mpvHandle, args);
            if (status < 0)
                throw new Exception("mpv failed to load file");
        }



        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (mpvHandle != IntPtr.Zero)
                {
                    mpv_destroy(mpvHandle);
                    mpvHandle = IntPtr.Zero;
                }
            }
            base.Dispose(disposing);
        }
    }
}
