// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using Microsoft.Xna.Framework;

namespace ClassicUO.Dust765
{
    internal sealed class AllyRangeIndicatorGump : Gump
    {
        private const int BUCKET_SIZE = 26;
        private const int BUCKET_GAP = 5;
        private const int PANEL_PADDING = 4;
        private const int BORDER_SIZE = 4;
        private const uint ScanIntervalMs = 100;

        private readonly RangeBucketGumpControl _green;
        private readonly RangeBucketGumpControl _yellow;
        private readonly RangeBucketGumpControl _red;
        private uint _nextScanTick;

        public AllyRangeIndicatorGump(World world)
            : base(world, 0, 0)
        {
            CanMove = true;
            CanCloseWithEsc = false;
            CanCloseWithRightClick = false;
            AcceptMouseInput = true;
            LayerOrder = UILayer.Over;
            IsVisible = true;

            Profile profile = ProfileManager.CurrentProfile;

            if (profile != null && profile.AllyRangeIndicator_Locked)
            {
                CanMove = false;
                AcceptMouseInput = false;
            }

            Width = PANEL_PADDING * 2 + BUCKET_SIZE * 3 + BUCKET_GAP * 2;
            Height = PANEL_PADDING * 2 + BUCKET_SIZE;

            Add(
                new AlphaBlendControl(0.55f)
                {
                    X = 0,
                    Y = 0,
                    Width = Width,
                    Height = Height,
                    AcceptMouseInput = false
                }
            );

            int bucketY = PANEL_PADDING;
            int greenX = PANEL_PADDING;

            _green = new RangeBucketGumpControl(
                Color.FromNonPremultiplied(70, 145, 220, 255),
                0x3F,
                BUCKET_SIZE
            );
            _green.X = greenX;
            _green.Y = bucketY;
            Add(_green);

            _yellow = new RangeBucketGumpControl(
                Color.FromNonPremultiplied(90, 185, 210, 255),
                0x35,
                BUCKET_SIZE
            );
            _yellow.X = greenX + BUCKET_SIZE + BUCKET_GAP;
            _yellow.Y = bucketY;
            Add(_yellow);

            _red = new RangeBucketGumpControl(
                Color.FromNonPremultiplied(120, 100, 200, 255),
                0x26,
                BUCKET_SIZE
            );
            _red.X = greenX + (BUCKET_SIZE + BUCKET_GAP) * 2;
            _red.Y = bucketY;
            Add(_red);

            Add(
                new BorderControl(0, 0, Width, Height, BORDER_SIZE)
                {
                    AcceptMouseInput = false,
                    CanMove = false
                }
            );

            WantUpdateSize = false;
            RefreshBucketCounts();
        }

        public override GumpType GumpType => GumpType.None;

        protected override void OnDragEnd(int x, int y)
        {
            base.OnDragEnd(x, y);

            Profile profile = ProfileManager.CurrentProfile;

            if (profile != null)
            {
                profile.AllyRangeIndicatorLocation = new Point(ScreenCoordinateX, ScreenCoordinateY);
            }
        }

        public override void Update()
        {
            base.Update();

            if (World.Player == null || World.Player.IsDestroyed)
            {
                return;
            }

            uint now = Time.Ticks;

            if (now < _nextScanTick)
            {
                return;
            }

            _nextScanTick = now + ScanIntervalMs;
            RefreshBucketCounts();
        }

        private void RefreshBucketCounts()
        {
            AllyRangeBucketHelper.CountBuckets(
                World,
                out int greenCount,
                out int yellowCount,
                out int redCount,
                out ushort greenHue,
                out ushort yellowHue,
                out ushort redHue
            );

            _green.SetCount(greenCount);
            _yellow.SetCount(yellowCount);
            _red.SetCount(redCount);
            _green.SetBorderHue(greenHue);
            _yellow.SetBorderHue(yellowHue);
            _red.SetBorderHue(redHue);
        }

        internal static void RefreshOpenGump(World world)
        {
            AllyRangeIndicatorGump existing = UIManager.GetGump<AllyRangeIndicatorGump>();

            if (existing != null)
            {
                existing.Dispose();
            }

            Profile profile = ProfileManager.CurrentProfile;

            if (profile == null || !profile.AllyRangeIndicator || world == null)
            {
                return;
            }

            Point location = profile.AllyRangeIndicatorLocation;
            int maxX = Math.Max(0, Client.Game.ClientBounds.Width - 90);
            int maxY = Math.Max(0, Client.Game.ClientBounds.Height - 40);
            location.X = Math.Clamp(location.X, 0, maxX);
            location.Y = Math.Clamp(location.Y, 0, maxY);

            AllyRangeIndicatorGump gump = new AllyRangeIndicatorGump(world)
            {
                X = location.X,
                Y = location.Y
            };

            UIManager.Add(gump);
            UIManager.MakeTopMostGump(gump);
        }
    }
}
