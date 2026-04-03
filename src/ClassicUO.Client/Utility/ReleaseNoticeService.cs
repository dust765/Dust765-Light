using System;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Utility.Logging;
using ClassicUO.Utility.Platforms;

namespace ClassicUO.Utility
{
    internal static class ReleaseNoticeService
    {
        private const string DefaultReleasesLatestApi =
            "https://api.github.com/repos/dust765/Dust765-Light/releases/latest";

        private static readonly object _sync = new object();
        private static readonly HttpClient _http = CreateHttpClient();

        private static ReleaseNotifyCache _cache;
        private static bool _cacheLoaded;
        private static bool _fetchStarted;
        private static bool _initializeCheckRanForProcess;
        private static bool _noticeShownThisSession;

        private static HttpClient CreateHttpClient()
        {
            HttpClient c = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            c.DefaultRequestHeaders.TryAddWithoutValidation(
                "User-Agent",
                $"Dust765-Light/{CUOEnviroment.Version}"
            );
            c.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/vnd.github+json");
            return c;
        }

        private static string CacheFilePath =>
            Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Client", "release_notify.json");

        private static string GetReleasesLatestApiUrl()
        {
            string o = Settings.GlobalSettings.ReleaseDebugApiUrl?.Trim();
            return string.IsNullOrEmpty(o) ? DefaultReleasesLatestApi : o;
        }

        private static string GetEffectiveLocalVersion()
        {
            string o = Settings.GlobalSettings.ReleaseDebugLocalVersion?.Trim();
            return string.IsNullOrEmpty(o) ? CUOEnviroment.Version : o;
        }

        private static bool ReleaseDebugAnyActive()
        {
            return Settings.GlobalSettings.ReleaseDebugForceShowReleaseNotice
                || !string.IsNullOrWhiteSpace(Settings.GlobalSettings.ReleaseDebugApiUrl?.Trim())
                || !string.IsNullOrWhiteSpace(Settings.GlobalSettings.ReleaseDebugLocalVersion?.Trim());
        }

        internal static void InitializeCheck()
        {
            if (_initializeCheckRanForProcess)
            {
                return;
            }

            _initializeCheckRanForProcess = true;

            if (ReleaseDebugAnyActive())
            {
                Log.Trace(
                    $"Release notice debug: api_url_override={!string.IsNullOrWhiteSpace(Settings.GlobalSettings.ReleaseDebugApiUrl?.Trim())}, local_version_override={!string.IsNullOrWhiteSpace(Settings.GlobalSettings.ReleaseDebugLocalVersion?.Trim())}, force_show_notice={Settings.GlobalSettings.ReleaseDebugForceShowReleaseNotice}, effective_local_for_compare={GetEffectiveLocalVersion()}, fetch_url={GetReleasesLatestApiUrl()}"
                );
            }

            EnsureCacheLoaded();
            if (!ShouldFetchRemote())
            {
                return;
            }

            lock (_sync)
            {
                if (_fetchStarted)
                {
                    return;
                }

                _fetchStarted = true;
            }

            _ = Task.Run(RunFetchAsync);
        }

        private static void EnsureCacheLoaded()
        {
            lock (_sync)
            {
                if (_cacheLoaded)
                {
                    return;
                }

                _cacheLoaded = true;
                _cache =
                    ConfigurationResolver.Load(CacheFilePath, ProfileJsonContext.DefaultToUse.ReleaseNotifyCache)
                    ?? new ReleaseNotifyCache();
            }
        }

        private static bool ShouldFetchRemote()
        {
            lock (_sync)
            {
                if (!string.IsNullOrWhiteSpace(Settings.GlobalSettings.ReleaseDebugApiUrl?.Trim())
                    || Settings.GlobalSettings.ReleaseDebugForceShowReleaseNotice)
                {
                    return true;
                }

                if (_cache == null)
                {
                    return true;
                }

                if (_cache.LastCheckUtcTicks <= 0)
                {
                    return true;
                }

                long elapsed = DateTime.UtcNow.Ticks - _cache.LastCheckUtcTicks;

                return elapsed > TimeSpan.FromHours(6).Ticks;
            }
        }

        private static async Task RunFetchAsync()
        {
            try
            {
                string json = await _http.GetStringAsync(GetReleasesLatestApiUrl()).ConfigureAwait(false);

                lock (_sync)
                {
                    if (_cache == null)
                    {
                        _cache = new ReleaseNotifyCache();
                    }

                    ApplyReleaseJsonToCache(json, _cache);
                    _cache.LastCheckUtcTicks = DateTime.UtcNow.Ticks;
                    SaveCacheLocked();
                }
            }
            catch (Exception ex)
            {
                Log.Warn($"release check failed ({GetReleasesLatestApiUrl()}): {ex.Message}");
                lock (_sync)
                {
                    if (_cache == null)
                    {
                        _cache = new ReleaseNotifyCache();
                    }

                    _cache.LastCheckUtcTicks = DateTime.UtcNow.Ticks;
                    SaveCacheLocked();
                }
            }
            finally
            {
                lock (_sync)
                {
                    _fetchStarted = false;
                }
            }
        }

        private static void SaveCacheLocked()
        {
            try
            {
                FileSystemHelper.CreateFolderIfNotExists(
                    Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Client")
                );
                ConfigurationResolver.Save(_cache, CacheFilePath, ProfileJsonContext.DefaultToUse.ReleaseNotifyCache);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }

        internal static void TryShowNotice(World world)
        {
            if (world == null)
            {
                return;
            }

            EnsureCacheLoaded();

            if (_noticeShownThisSession)
            {
                return;
            }

            if (UIManager.GetGump<ReleaseNoticeGump>() != null)
            {
                return;
            }

            string tag;
            string url;
            if (!ShouldDisplayNotice(out tag, out url))
            {
                return;
            }

            _noticeShownThisSession = true;
            UIManager.Add(new ReleaseNoticeGump(world, tag, url), true);
        }

        private static bool ShouldDisplayNotice(out string tag, out string url)
        {
            tag = null;
            url = null;
            lock (_sync)
            {
                if (_cache == null || string.IsNullOrWhiteSpace(_cache.LatestTag))
                {
                    return false;
                }

                if (Settings.GlobalSettings.ReleaseDebugForceShowReleaseNotice)
                {
                    tag = _cache.LatestTag.Trim();
                    url = string.IsNullOrWhiteSpace(_cache.LatestUrl)
                        ? "https://github.com/dust765/Dust765-Light/releases/latest"
                        : _cache.LatestUrl.Trim();
                    return true;
                }

                if (!TryParseVersionNormalized(_cache.LatestTag, out Version remoteVer))
                {
                    return false;
                }

                if (
                    TryParseVersionNormalized(GetEffectiveLocalVersion(), out Version localVer)
                    && remoteVer <= localVer
                )
                {
                    return false;
                }

                if (
                    !string.IsNullOrEmpty(_cache.DismissedTag)
                    && string.Equals(
                        _cache.DismissedTag.Trim(),
                        _cache.LatestTag.Trim(),
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    return false;
                }

                tag = _cache.LatestTag;
                url = string.IsNullOrWhiteSpace(_cache.LatestUrl)
                    ? "https://github.com/dust765/Dust765-Light/releases/latest"
                    : _cache.LatestUrl;
                return true;
            }
        }

        private static bool TryParseVersionNormalized(string s, out Version v)
        {
            v = null;
            if (string.IsNullOrWhiteSpace(s))
            {
                return false;
            }

            string t = s.Trim().TrimStart('v', 'V');
            int dash = t.IndexOf('-');
            if (dash > 0)
            {
                t = t.Substring(0, dash);
            }

            return Version.TryParse(t, out v);
        }

        internal static void DismissCurrentRelease()
        {
            EnsureCacheLoaded();
            lock (_sync)
            {
                if (_cache == null)
                {
                    _cache = new ReleaseNotifyCache();
                }

                if (!string.IsNullOrWhiteSpace(_cache.LatestTag))
                {
                    _cache.DismissedTag = _cache.LatestTag.Trim();
                }

                SaveCacheLocked();
            }
        }

        internal static void OpenReleasePage(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                url = "https://github.com/dust765/Dust765-Light/releases/latest";
            }

            PlatformHelper.LaunchBrowser(url);
        }

        internal static async Task<string> FetchLatestZipAssetUrlAsync()
        {
            EnsureCacheLoaded();
            string json = await _http.GetStringAsync(GetReleasesLatestApiUrl()).ConfigureAwait(false);

            lock (_sync)
            {
                if (_cache == null)
                {
                    _cache = new ReleaseNotifyCache();
                }

                ApplyReleaseJsonToCache(json, _cache);
                _cache.LastCheckUtcTicks = DateTime.UtcNow.Ticks;
                SaveCacheLocked();
                return string.IsNullOrWhiteSpace(_cache.LatestZipAssetUrl) ? null : _cache.LatestZipAssetUrl;
            }
        }

        internal static bool TryGetCachedZipAssetUrl(out string zipUrl)
        {
            zipUrl = null;
            EnsureCacheLoaded();
            lock (_sync)
            {
                if (string.IsNullOrWhiteSpace(_cache?.LatestZipAssetUrl))
                {
                    return false;
                }

                zipUrl = _cache.LatestZipAssetUrl;
                return true;
            }
        }

        private static void ApplyReleaseJsonToCache(string json, ReleaseNotifyCache cache)
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;
            string tag = root.TryGetProperty("tag_name", out JsonElement t) ? t.GetString() : null;
            string pageUrl = root.TryGetProperty("html_url", out JsonElement h) ? h.GetString() : null;

            if (!string.IsNullOrWhiteSpace(tag))
            {
                cache.LatestTag = tag.Trim();
            }

            if (!string.IsNullOrWhiteSpace(pageUrl))
            {
                cache.LatestUrl = pageUrl.Trim();
            }

            cache.LatestZipAssetUrl = "";
            cache.LatestZipAssetName = "";
            if (root.TryGetProperty("assets", out JsonElement assets) && TryPickZipAssetUrl(assets, out string zUrl, out string zName))
            {
                cache.LatestZipAssetUrl = zUrl;
                cache.LatestZipAssetName = zName ?? "";
            }
        }

        private static bool TryPickZipAssetUrl(JsonElement assets, out string downloadUrl, out string assetName)
        {
            downloadUrl = null;
            assetName = null;
            if (assets.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            bool isWin = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
            bool isOsx = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

            string bestUrl = null;
            string bestName = null;
            int bestScore = int.MinValue;

            foreach (JsonElement el in assets.EnumerateArray())
            {
                if (!el.TryGetProperty("name", out JsonElement nEl) || !el.TryGetProperty("browser_download_url", out JsonElement uEl))
                {
                    continue;
                }

                string nm = nEl.GetString();
                string ur = uEl.GetString();
                if (string.IsNullOrEmpty(nm) || string.IsNullOrEmpty(ur))
                {
                    continue;
                }

                string lower = nm.ToLowerInvariant();
                if (!lower.EndsWith(".zip", StringComparison.Ordinal))
                {
                    continue;
                }

                if (lower.Contains("symbols") || lower.Contains("pdb") || lower.Contains("debug"))
                {
                    continue;
                }

                int score = 0;
                if (isWin)
                {
                    if (lower.Contains("linux") || lower.Contains("osx") || lower.Contains("macos") || lower.Contains("darwin"))
                    {
                        score -= 20;
                    }

                    if (lower.Contains("win"))
                    {
                        score += 30;
                    }

                    score += 5;
                }
                else if (isLinux)
                {
                    if (lower.Contains("win") || lower.Contains("osx") || lower.Contains("macos"))
                    {
                        score -= 20;
                    }

                    if (lower.Contains("linux") || lower.Contains("unix") || lower.Contains("ubuntu"))
                    {
                        score += 30;
                    }

                    score += 5;
                }
                else if (isOsx)
                {
                    if (lower.Contains("win") || lower.Contains("linux"))
                    {
                        score -= 20;
                    }

                    if (lower.Contains("osx") || lower.Contains("mac") || lower.Contains("darwin"))
                    {
                        score += 30;
                    }

                    score += 5;
                }
                else
                {
                    score += 1;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestUrl = ur;
                    bestName = nm;
                }
            }

            if (bestUrl != null)
            {
                downloadUrl = bestUrl;
                assetName = bestName;
                return true;
            }

            foreach (JsonElement el in assets.EnumerateArray())
            {
                if (!el.TryGetProperty("name", out JsonElement nEl) || !el.TryGetProperty("browser_download_url", out JsonElement uEl))
                {
                    continue;
                }

                string nm = nEl.GetString();
                string ur = uEl.GetString();
                if (string.IsNullOrEmpty(nm) || string.IsNullOrEmpty(ur))
                {
                    continue;
                }

                if (!nm.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (nm.Contains("symbols", StringComparison.OrdinalIgnoreCase) || nm.Contains("pdb", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                downloadUrl = ur;
                assetName = nm;
                return true;
            }

            return false;
        }
    }
}
