// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Dust765
{
    internal sealed class EnemyRangeIndicatorGump : Gump
    {
        private const byte FONT = 0xFF;
        private const int BUCKET_SIZE = 26;
        private const int BUCKET_GAP = 5;
        private const int PANEL_PADDING = 4;
        private const uint ScanIntervalMs = 100;
        private const float IDLE_ALPHA = 0.50f;
        private const float ACTIVE_ALPHA = 1.0f;

        private readonly AlphaBlendControl _panel;
        private readonly RangeBucketControl _green;
        private readonly RangeBucketControl _yellow;
        private readonly RangeBucketControl _red;
        private uint _nextScanTick;

        public EnemyRangeIndicatorGump(World world)
            : base(world, 0, 0)
        {
            CanMove = true;
            CanCloseWithEsc = false;
            CanCloseWithRightClick = false;
            AcceptMouseInput = true;
            LayerOrder = UILayer.Over;
            IsVisible = true;

            Profile profile = ProfileManager.CurrentProfile;

            if (profile != null && profile.EnemyRangeIndicator_Locked)
            {
                CanMove = false;
                AcceptMouseInput = false;
            }

            Width = PANEL_PADDING * 2 + BUCKET_SIZE * 3 + BUCKET_GAP * 2;
            Height = PANEL_PADDING * 2 + BUCKET_SIZE;

            Add(
                _panel = new AlphaBlendControl(0.55f)
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

            _green = new RangeBucketControl(Color.FromNonPremultiplied(80, 185, 75, 255), 0x3F);
            _green.X = greenX;
            _green.Y = bucketY;
            Add(_green);

            _yellow = new RangeBucketControl(Color.FromNonPremultiplied(210, 175, 55, 255), 0x35);
            _yellow.X = greenX + BUCKET_SIZE + BUCKET_GAP;
            _yellow.Y = bucketY;
            Add(_yellow);

            _red = new RangeBucketControl(Color.FromNonPremultiplied(200, 70, 55, 255), 0x26);
            _red.X = greenX + (BUCKET_SIZE + BUCKET_GAP) * 2;
            _red.Y = bucketY;
            Add(_red);

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
                profile.EnemyRangeIndicatorLocation = new Point(ScreenCoordinateX, ScreenCoordinateY);
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
            Profile profile = ProfileManager.CurrentProfile;
            bool lastTargetOnly = profile != null && profile.EnemyRangeIndicator_LastTargetOnly;

            EnemyRangeBucketHelper.CountBuckets(World, lastTargetOnly, out int greenCount, out int yellowCount, out int redCount);

            _green.SetCount(greenCount);
            _yellow.SetCount(yellowCount);
            _red.SetCount(redCount);
        }

        internal static void RefreshOpenGump(World world)
        {
            EnemyRangeIndicatorGump existing = UIManager.GetGump<EnemyRangeIndicatorGump>();

            if (existing != null)
            {
                existing.Dispose();
            }

            Profile profile = ProfileManager.CurrentProfile;

            if (profile == null || !profile.EnemyRangeIndicator || world == null)
            {
                return;
            }

            Point location = profile.EnemyRangeIndicatorLocation;
            int maxX = Math.Max(0, Client.Game.ClientBounds.Width - 90);
            int maxY = Math.Max(0, Client.Game.ClientBounds.Height - 40);
            location.X = Math.Clamp(location.X, 0, maxX);
            location.Y = Math.Clamp(location.Y, 0, maxY);

            EnemyRangeIndicatorGump gump = new EnemyRangeIndicatorGump(world)
            {
                X = location.X,
                Y = location.Y
            };

            UIManager.Add(gump);
            UIManager.MakeTopMostGump(gump);
        }

        private sealed class RangeBucketControl : Control
        {
            private readonly Texture2D _fillTexture;
            private readonly Texture2D _ringTexture;
            private readonly Label _countLabel;
            private int _count;

            public RangeBucketControl(Color fillColor, ushort countHue)
            {
                Width = BUCKET_SIZE;
                Height = BUCKET_SIZE;
                AcceptMouseInput = false;

                _fillTexture = SolidColorTextureCache.GetTexture(fillColor);
                _ringTexture = SolidColorTextureCache.GetTexture(fillColor);

                _countLabel = new Label(string.Empty, true, countHue, font: FONT, style: FontStyle.BlackBorder)
                {
                    X = 0,
                    Y = 5,
                    Width = BUCKET_SIZE,
                    Height = BUCKET_SIZE,
                    AcceptMouseInput = false,
                    IsVisible = false
                };

                Add(_countLabel);
            }

            public void SetCount(int count)
            {
                _count = count;
                _countLabel.IsVisible = count > 0;
                _countLabel.Text = count > 99 ? "99" : count.ToString();
            }

            public override bool AddToRenderLists(RenderLists renderLists, int x, int y, ref float layerDepthRef)
            {
                float layerDepth = layerDepthRef;
                bool active = _count > 0;
                float alpha = active ? ACTIVE_ALPHA : IDLE_ALPHA;
                Vector3 fillHue = ShaderHueTranslator.GetHueVector(0, false, Alpha * alpha);
                Vector3 ringHue = ShaderHueTranslator.GetHueVector(0, false, Alpha * (active ? alpha : alpha * 0.85f));

                renderLists.AddGumpNoAtlas(batcher =>
                {
                    int inner = active ? 14 : 10;
                    int ix = x + (BUCKET_SIZE - inner) / 2;
                    int iy = y + (BUCKET_SIZE - inner) / 2;
                    batcher.Draw(_fillTexture, new Rectangle(ix, iy, inner, inner), fillHue, layerDepth);
                    batcher.DrawRectangle(_ringTexture, x + 2, y + 2, BUCKET_SIZE - 4, BUCKET_SIZE - 4, ringHue, layerDepth);

                    if (active)
                    {
                        batcher.DrawRectangle(_ringTexture, x, y, BUCKET_SIZE, BUCKET_SIZE, ringHue, layerDepth);
                    }

                    return true;
                });

                return base.AddToRenderLists(renderLists, x, y, ref layerDepthRef);
            }
        }
    }
}
