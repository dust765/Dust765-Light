// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.IO;
using System.Reflection;
using System.Threading;

namespace ClassicUO
{
    internal static class CUOEnviroment
    {
        public static Thread GameThread;
        public static float DPIScaleFactor = 1.0f;
        public static bool NoSound;
        public static string[] Args;
        public static string[] Plugins;
        public static bool Debug;
        public static bool IsHighDPI;
        public static uint CurrentRefreshRate;
        public static bool SkipLoginScreen;
        public static bool NoServerPing;

        public static readonly bool IsWindows = Environment.OSVersion.Platform == PlatformID.Win32NT || Environment.OSVersion.Platform == PlatformID.Win32Windows || Environment.OSVersion.Platform == PlatformID.Win32S || Environment.OSVersion.Platform == PlatformID.WinCE;
        public static readonly bool IsUnix = !IsWindows;

        public const string ProductTitle = "Dust765 - Light";

        public static readonly string Version =
            Assembly.GetExecutingAssembly()?.GetName()?.Version is System.Version ver
                ? ver.ToString(4)
                : "1.0.0.0";
        public static readonly string ExecutablePath = ResolveExecutablePath();

        private static string ResolveExecutablePath()
        {
            string dir = NormalizeDir(AppContext.BaseDirectory);

            if (IsUnusableDir(dir))
            {
                string loc = Assembly.GetEntryAssembly()?.Location;
                if (string.IsNullOrWhiteSpace(loc))
                {
                    loc = Assembly.GetExecutingAssembly()?.Location;
                }

                if (!string.IsNullOrWhiteSpace(loc))
                {
                    dir = NormalizeDir(Path.GetDirectoryName(loc));
                }
            }

            if (IsUnusableDir(dir))
            {
                dir = NormalizeDir(Environment.CurrentDirectory);
            }

            try
            {
                dir = Path.GetFullPath(dir);
            }
            catch
            {
                dir = NormalizeDir(Environment.CurrentDirectory);
            }

            return dir;
        }

        private static string NormalizeDir(string dir)
        {
            if (string.IsNullOrWhiteSpace(dir))
            {
                return dir;
            }

            return dir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        private static bool IsUnusableDir(string dir)
        {
            if (string.IsNullOrWhiteSpace(dir))
            {
                return true;
            }

            string trimmed = dir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return trimmed.Length == 0 || trimmed == "/" || trimmed == "\\";
        }
    }
}