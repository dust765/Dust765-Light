using System;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal sealed class ReleaseNoticeGump : Gump
    {
        private readonly string _url;

        public ReleaseNoticeGump(World world, string latestTag, string releaseUrl) : base(world, 0, 0)
        {
            _url = releaseUrl;
            CanMove = true;
            CanCloseWithRightClick = true;
            AcceptMouseInput = true;

            Width = 520;
            Height = 76;

            int screenW = Math.Max(Client.Game.ClientBounds.Width, 640);
            X = (screenW - Width) / 2;
            Y = 10;

            Add(new AlphaBlendControl(0.82f) { X = 0, Y = 0, Width = Width, Height = Height });

            Add
            (
                new Label($"New release available: {latestTag}", true, 0x0035, Width - 16, font: 1)
                {
                    X = 8,
                    Y = 8
                }
            );

            Add
            (
                new NiceButton(8, 44, 118, 22, ButtonAction.Activate, "Open page", 0)
                {
                    ButtonParameter = 1
                }
            );

            Add
            (
                new NiceButton(132, 44, 132, 22, ButtonAction.Activate, "Download update", 0)
                {
                    ButtonParameter = 2
                }
            );

            Add
            (
                new NiceButton(270, 44, 100, 22, ButtonAction.Activate, "Dismiss", 0)
                {
                    ButtonParameter = 3
                }
            );
        }

        public override GumpType GumpType => GumpType.None;

        public override void OnButtonClick(int buttonID)
        {
            switch (buttonID)
            {
                case 1:
                    ReleaseNoticeService.OpenReleasePage(_url);

                    break;

                case 2:
                    ReleaseNoticeService.TryGetCachedZipAssetUrl(out string zipUrl);
                    Dispose();
                    UIManager.Add(new ClientUpdateGump(World, zipUrl), true);

                    break;

                case 3:
                    ReleaseNoticeService.DismissCurrentRelease();
                    Dispose();

                    break;
            }
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Right && CanCloseWithRightClick)
            {
                Dispose();
            }

            base.OnMouseUp(x, y, button);
        }
    }
}
