using ClassicUO.Configuration;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using System.IO;

namespace ClassicUO
{
    internal static class PluginCompatibility
    {
        private const byte ClilocBwtMagic = 0x8E;

        public static uint GetClientVersionForPlugins(ClientVersion version)
        {
            if (!ClilocUsesBwtCompression())
            {
                uint legacyVersion = (uint)ClientVersion.CV_500A;
                Log.Trace($"Plugin client version downgraded to 5.0.0a (cliloc is legacy, real client {version})");
                return legacyVersion;
            }

            return (uint)version;
        }

        public static uint GetPluginClientVersion(string assetsPath, uint clientVersion)
        {
            if (!string.IsNullOrWhiteSpace(assetsPath) && !ClilocUsesBwtCompression(assetsPath))
            {
                return (uint)ClientVersion.CV_500A;
            }

            return clientVersion;
        }

        static bool ClilocUsesBwtCompression() =>
            ClilocUsesBwtCompression(Settings.GlobalSettings.UltimaOnlineDirectory);

        static bool ClilocUsesBwtCompression(string uoPath)
        {
            if (string.IsNullOrWhiteSpace(uoPath))
            {
                return false;
            }

            string lang = Settings.GlobalSettings.Language;

            if (string.IsNullOrWhiteSpace(lang))
            {
                lang = "enu";
            }

            string localized = Path.Combine(uoPath, $"Cliloc.{lang}");

            if (File.Exists(localized))
            {
                return ClilocFileUsesBwt(localized);
            }

            return ClilocFileUsesBwt(Path.Combine(uoPath, "Cliloc.enu"));
        }

        static bool ClilocFileUsesBwt(string path)
        {
            if (!File.Exists(path) || new FileInfo(path).Length <= 3)
            {
                return false;
            }

            using FileStream stream = File.OpenRead(path);
            stream.Seek(3, SeekOrigin.Begin);

            return stream.ReadByte() == ClilocBwtMagic;
        }
    }
}
