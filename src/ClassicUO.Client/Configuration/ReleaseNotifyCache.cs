namespace ClassicUO.Configuration
{
    internal sealed class ReleaseNotifyCache
    {
        public long LastCheckUtcTicks { get; set; }

        public string LatestTag { get; set; } = "";

        public string LatestUrl { get; set; } = "";

        public string LatestZipAssetUrl { get; set; } = "";

        public string LatestZipAssetName { get; set; } = "";

        public string DismissedTag { get; set; } = "";
    }
}
