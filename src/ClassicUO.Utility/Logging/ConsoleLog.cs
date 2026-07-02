using System;
using System.Runtime.InteropServices;

namespace ClassicUO.Utility.Logging
{
    public static class ConsoleLog
    {
        public static bool IsEnabled()
        {
#if CONSOLE_LOG
            return true;
#else
            if (string.Equals(Environment.GetEnvironmentVariable("CUO_CONSOLE_LOG"), "1", StringComparison.Ordinal))
            {
                return true;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return GetConsoleWindow() != IntPtr.Zero;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return Isatty(1) == 1;
            }

            return false;
#endif
        }

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("libc", SetLastError = true)]
        private static extern int Isatty(int fd);
    }
}
