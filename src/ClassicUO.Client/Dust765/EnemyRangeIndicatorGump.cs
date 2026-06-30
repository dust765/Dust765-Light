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
        private const int BUCKET_SIZE = 22;
        private const int BUCKET_GAP = 4;
        private const uint ScanIntervalMs = 100;
        private const float IDLE_ALPHA = 0.30f;
        private const float ACTIVE_ALPHA = 1.0f;

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

            Profile profile = ProfileManager.CurrentProfile;

            if (profile != null && profile.EnemyRangeIndicator_Locked)
            {
                CanMove = false;
                AcceptMouseInput = false;
            }

            Width = BUCKET_SIZE * 3 + BUCKET_GAP * 2;
            Height = BUCKET_SIZE;

            _green = new RangeBucketControl(Color.FromNonPremultiplied(80, 185, 75, 255), 0x3F);
            _green.X = 0;
            _green.Y = 0;
            Add(_green);

            _yellow = new RangeBucketControl(Color.FromNonPremultiplied(210, 175, 55, 255), 0x35);
            _yellow.X = BUCKET_SIZE + BUCKET_GAP;
            _yellow.Y = 0;
            Add(_yellow);

            _red = new RangeBucketControl(Color.FromNonPremultiplied(200, 70, 55, 255), 0x26);
            _red.X = (BUCKET_SIZE + BUCKET_GAP) * 2;
            _red.Y = 0;
            Add(_red);

            WantUpdateSize = false;
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

            if (profile != null && profile.EnemyRangeIndicator)
            {
                UIManager.Add(
                    new EnemyRangeIndicatorGump(world)
                    {
                        X = profile.EnemyRangeIndicatorLocation.X,
                        Y = profile.EnemyRangeIndicatorLocation.Y
                    }
                );
            }
        }

        private sealed class RangeBucketControl : Control
        {
            private const int SIZE = 22;
            private const float CENTER = SIZE / 2f;
            private const float INNER_RADIUS = 7f;
            private const float RING_RADIUS = 9f;
            private const float RING_THICKNESS = 2f;
            private const float ARC_GAP = 0.35f;

            private readonly Texture2D _fillTexture;
            private readonly Texture2D _ringTexture;
            private readonly Texture2D _arcTexture;
            private readonly Label _countLabel;
            private int _count;

            public RangeBucketControl(Color fillColor, ushort countHue)
            {
                Width = SIZE;
                Height = SIZE;
                AcceptMouseInput = false;

                _fillTexture = SolidColorTextureCache.GetTexture(fillColor);
                _ringTexture = SolidColorTextureCache.GetTexture(fillColor);
                _arcTexture = SolidColorTextureCache.GetTexture(Color.FromNonPremultiplied(220, 220, 220, 255));

                _countLabel = new Label(string.Empty, true, countHue, font: FONT, style: FontStyle.BlackBorder)
                {
                    X = 0,
                    Y = 3,
                    Width = SIZE,
                    Height = SIZE,
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
                Vector2 center = new Vector2(x + CENTER, y + CENTER);
                Vector3 fillHue = ShaderHueTranslator.GetHueVector(0, false, Alpha * alpha);
                Vector3 ringHue = ShaderHueTranslator.GetHueVector(0, false, Alpha * alpha);
                Vector3 arcHue = ShaderHueTranslator.GetHueVector(0, false, Alpha * (active ? 0.85f : 0.35f));

                renderLists.AddGumpNoAtlas(batcher =>
                {
                    DrawDecorativeArcs(batcher, center, arcHue, layerDepth);

                    if (active)
                    {
                        FillCircle(batcher, _fillTexture, center, INNER_RADIUS, fillHue, layerDepth);
                    }
                    else
                    {
                        FillCircle(batcher, _fillTexture, center, INNER_RADIUS * 0.75f, fillHue, layerDepth);
                    }

                    DrawArcRing(batcher, _ringTexture, center, RING_RADIUS, 0f, MathHelper.TwoPi, RING_THICKNESS, ringHue, layerDepth);
                    return true;
                });

                return base.AddToRenderLists(renderLists, x, y, ref layerDepthRef);
            }

            private void DrawDecorativeArcs(UltimaBatcher2D batcher, Vector2 center, Vector3 hue, float depth)
            {
                float outer = RING_RADIUS + RING_THICKNESS + 1.5f;
                float arcSpan = MathHelper.PiOver2 - ARC_GAP;

                DrawArcRing(batcher, _arcTexture, center, outer, -MathHelper.PiOver2 + ARC_GAP, arcSpan, 1.2f, hue, depth);
                DrawArcRing(batcher, _arcTexture, center, outer, MathHelper.PiOver2 + ARC_GAP, arcSpan, 1.2f, hue, depth);
                DrawArcRing(batcher, _arcTexture, center, outer, MathHelper.Pi + ARC_GAP, arcSpan, 1.2f, hue, depth);
                DrawArcRing(batcher, _arcTexture, center, outer, MathHelper.Pi + MathHelper.PiOver2 + ARC_GAP, arcSpan, 1.2f, hue, depth);
            }

            private static void FillCircle(UltimaBatcher2D batcher, Texture2D texture, Vector2 center, float radius, Vector3 hue, float depth)
            {
                const int segments = 20;
                float stroke = radius * 0.28f;

                for (int i = 0; i < segments; i++)
                {
                    float angle = MathHelper.TwoPi * i / segments;
                    Vector2 edge = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
                    batcher.DrawLine(texture, center, edge, hue, stroke, depth);
                }
            }

            private static void DrawArcRing(
                UltimaBatcher2D batcher,
                Texture2D texture,
                Vector2 center,
                float radius,
                float startAngle,
                float sweep,
                float thickness,
                Vector3 hue,
                float depth
            )
            {
                if (sweep <= 0f)
                {
                    return;
                }

                int segments = Math.Max(8, (int)(sweep / MathHelper.Pi * 20));
                float step = sweep / segments;

                for (int i = 0; i < segments; i++)
                {
                    float a0 = startAngle + step * i;
                    float a1 = startAngle + step * (i + 1);
                    Vector2 p0 = center + new Vector2(MathF.Cos(a0), MathF.Sin(a0)) * radius;
                    Vector2 p1 = center + new Vector2(MathF.Cos(a1), MathF.Sin(a1)) * radius;
                    batcher.DrawLine(texture, p0, p1, hue, thickness, depth);
                }
            }
        }
    }
}
