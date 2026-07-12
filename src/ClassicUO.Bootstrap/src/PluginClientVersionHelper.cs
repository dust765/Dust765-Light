using System.IO;

internal static class PluginClientVersionHelper
{
    private const byte ClilocBwtMagic = 0x8E;

    public static uint GetPluginClientVersion(string assetsPath, uint clientVersion)
    {
        if (!string.IsNullOrWhiteSpace(assetsPath) && !ClilocUsesBwtCompression(assetsPath))
        {
            return 0x05000061;
        }

        return clientVersion;
    }

    static bool ClilocUsesBwtCompression(string uoPath)
    {
        if (string.IsNullOrWhiteSpace(uoPath))
        {
            return false;
        }

        string cliloc = Path.Combine(uoPath, "Cliloc.enu");

        if (!File.Exists(cliloc) || new FileInfo(cliloc).Length <= 3)
        {
            return false;
        }

        using FileStream stream = File.OpenRead(cliloc);
        stream.Seek(3, SeekOrigin.Begin);

        return stream.ReadByte() == ClilocBwtMagic;
    }
}
