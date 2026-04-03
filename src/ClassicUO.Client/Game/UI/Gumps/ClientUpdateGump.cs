using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal sealed class ClientUpdateGump : Gump
    {
        private readonly object _uiLock = new object();
        private readonly string _initialZipUrl;
        private readonly string _zipPath;
        private readonly Label _mainLabel;
        private readonly Label _pctLabel;
        private readonly NiceButton _closeBtn;
        private volatile bool _exitRequested;
        private volatile bool _failed;
        private string _lineText = "Preparing…";
        private int _pct = -1;

        public ClientUpdateGump(World world, string initialZipUrlOrNull) : base(world, 0, 0)
        {
            _initialZipUrl = initialZipUrlOrNull;
            _zipPath = Path.Combine(Path.GetTempPath(), "dust765_update_" + Guid.NewGuid().ToString("N") + ".zip");

            CanMove = true;
            CanCloseWithRightClick = false;
            AcceptMouseInput = true;

            Width = 400;
            Height = 118;

            int screenW = Math.Max(Client.Game.ClientBounds.Width, 640);
            X = (screenW - Width) / 2;
            Y = 40;

            Add(new AlphaBlendControl(0.88f) { X = 0, Y = 0, Width = Width, Height = Height });

            Add
            (
                _mainLabel = new Label(_lineText, true, 0x0035, Width - 16, font: 1)
                {
                    X = 8,
                    Y = 10
                }
            );

            Add
            (
                _pctLabel = new Label("", true, 0x0030, Width - 16, font: 1)
                {
                    X = 8,
                    Y = 34,
                    IsVisible = false
                }
            );

            Add
            (
                _closeBtn = new NiceButton(Width - 88, Height - 32, 80, 22, ButtonAction.Activate, "Close", 0)
                {
                    ButtonParameter = 99,
                    IsVisible = false
                }
            );

            Task.Run(RunAsync);
        }

        public override GumpType GumpType => GumpType.None;

        public override void Update()
        {
            base.Update();

            string line;
            int p;
            bool fail;
            bool exit;
            lock (_uiLock)
            {
                line = _lineText;
                p = _pct;
                fail = _failed;
                exit = _exitRequested;
            }

            _mainLabel.Text = line;
            if (p >= 0)
            {
                _pctLabel.IsVisible = true;
                _pctLabel.Text = $"{p}%";
            }
            else
            {
                _pctLabel.IsVisible = false;
            }

            _closeBtn.IsVisible = fail;

            if (exit)
            {
                Client.Game.Exit();
            }
        }

        public override void OnButtonClick(int buttonID)
        {
            if (buttonID == 99)
            {
                Dispose();
            }
        }

        private void SetUi(string message, int percentOrNegative)
        {
            lock (_uiLock)
            {
                _lineText = message ?? "";
                _pct = percentOrNegative;
            }
        }

        private async Task RunAsync()
        {
            try
            {
                string url = _initialZipUrl;
                if (string.IsNullOrWhiteSpace(url))
                {
                    SetUi("Checking release…", -1);
                    url = await ReleaseNoticeService.FetchLatestZipAssetUrlAsync().ConfigureAwait(false);
                }

                if (string.IsNullOrWhiteSpace(url))
                {
                    SetUi("No .zip download is attached to the latest release. Open the release page to update manually.", -1);
                    _failed = true;
                    return;
                }

                SetUi("Downloading update…", 0);
                await DownloadToFileAsync(url, _zipPath).ConfigureAwait(false);

                SetUi("Closing client and applying update…", 100);
                if (!ClientUpdateApplier.TryStartPostExitUpdate(_zipPath, out string err))
                {
                    SetUi("Could not start updater: " + err, -1);
                    _failed = true;
                    return;
                }

                Thread.Sleep(500);
                _exitRequested = true;
            }
            catch (Exception ex)
            {
                SetUi("Update failed: " + ex.Message, -1);
                _failed = true;
            }
        }

        private async Task DownloadToFileAsync(string url, string path)
        {
            using HttpClient c = new HttpClient { Timeout = TimeSpan.FromMinutes(60) };
            c.DefaultRequestHeaders.TryAddWithoutValidation(
                "User-Agent",
                $"Dust765-Light/{CUOEnviroment.Version}"
            );

            using HttpResponseMessage r = await c.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
            r.EnsureSuccessStatusCode();
            long? total = r.Content.Headers.ContentLength;
            await using Stream src = await r.Content.ReadAsStreamAsync().ConfigureAwait(false);
            await using FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            byte[] buf = new byte[81920];
            long read = 0;
            int n;
            int lastPct = -1;
            while ((n = await src.ReadAsync(buf.AsMemory(0, buf.Length)).ConfigureAwait(false)) > 0)
            {
                await fs.WriteAsync(buf.AsMemory(0, n)).ConfigureAwait(false);
                read += n;
                if (total is > 0)
                {
                    int pct = (int)(100 * read / total.Value);
                    if (pct != lastPct)
                    {
                        lastPct = pct;
                        SetUi("Downloading update…", pct);
                    }
                }
            }
        }
    }
}
