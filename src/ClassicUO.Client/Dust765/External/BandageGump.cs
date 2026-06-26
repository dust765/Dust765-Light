// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Dust765.External
{
    internal class BandageGump : Gump
    {
        private const byte FONT = 0xFF;
        private const ushort HUE_YELLOW = 0x35;
        private const ushort HUE_RED = 0x26;
        private const ushort HUE_GREEN = 0x3F;
        private const ushort BANDAGE_GRAPHIC = 0x0E21;

        private const int DIAL_SIZE = 58;
        private const int ROUND_ICON_SIZE = 20;
        private const int DIAL_CENTER = DIAL_SIZE / 2;
        private const float RING_RADIUS = 26f;
        private const float RING_THICKNESS = 4f;
        private const float INNER_RADIUS = 19f;
        private const float START_ANGLE = -MathHelper.PiOver2;

        private const int LINE_HEIGHT = 30;
        private const int LINE_ICON_SIZE = 24;
        private const int LINE_ICON_OUTSET = 6;
        private const int LINE_CONTENT_GAP = 8;
        private const int LINE_BAR_OUTER_W = 72;
        private const int LINE_BAR_OUTER_H = 12;
        private const int LINE_BAR_TRACK_W = 52;
        private const int LINE_BAR_TRACK_H = 8;
        private const int LINE_TIMER_SLOT_W = 22;
        private const int LINE_PADDING_RIGHT = 6;

        private static readonly uint COLOR_DISC = Color.FromNonPremultiplied(12, 12, 12, 245).PackedValue;
        private static readonly uint COLOR_TRACK = Color.FromNonPremultiplied(38, 38, 38, 255).PackedValue;
        private static readonly uint COLOR_SHELL = Color.FromNonPremultiplied(18, 18, 18, 255).PackedValue;
        private static readonly uint COLOR_FILL = Color.FromNonPremultiplied(210, 175, 55, 255).PackedValue;
        private static readonly uint COLOR_FILL_WARN = Color.FromNonPremultiplied(220, 120, 40, 255).PackedValue;
        private static readonly uint COLOR_FILL_READY = Color.FromNonPremultiplied(80, 185, 75, 255).PackedValue;

        private readonly bool _roundStyle;
        private int _barX;
        private int _barY;
        private int _timerX;
        private int _lineTrackW;

        private BandageDialControl _dial;
        private AlphaBlendControl _panelBg;
        private BandageFrameControl _frame;
        private BandageFillBar _barShell;
        private BandageFillBar _barTrack;
        private BandageFillBar _barFill;
        private Label _timerLabel;

        private bool _useTime;
        private uint _startTime;
        private uint _initialTimer;
        private uint _maxSeconds;

        private static readonly int[] _startAtClilocs =
        {
            500956, 500957, 500958, 500959, 500960
        };

        private static readonly int[] _stopAtClilocs =
        {
            500955, 500962, 500963, 500964, 500965, 500966, 500967, 500968, 500969,
            503252, 503253, 503254, 503255, 503256, 503257, 503258, 503259, 503260, 503261,
            1010058, 1010648, 1010650, 1060088, 1060167
        };

        public BandageGump(World world) : base(world, 0, 0)
        {
            CanMove = true;
            CanCloseWithEsc = false;
            CanCloseWithRightClick = false;
            AcceptMouseInput = true;
            LayerOrder = UILayer.Over;
            IsVisible = false;

            Profile profile = ProfileManager.CurrentProfile;
            _roundStyle = profile == null || profile.BandageGumpRoundStyle;

            if (_roundStyle)
            {
                BuildRoundLayout();
            }
            else
            {
                BuildLineLayout();
            }

            WantUpdateSize = false;
        }

        private void BuildRoundLayout()
        {
            Width = DIAL_SIZE;
            Height = DIAL_SIZE;

            Add(
                _dial = new BandageDialControl
                {
                    X = 0,
                    Y = 0,
                    Width = DIAL_SIZE,
                    Height = DIAL_SIZE,
                    AcceptMouseInput = false
                }
            );

            Add(
                new BandageIconControl(BANDAGE_GRAPHIC, ROUND_ICON_SIZE)
                {
                    X = DIAL_CENTER - ROUND_ICON_SIZE / 2,
                    Y = DIAL_CENTER - ROUND_ICON_SIZE / 2,
                    AcceptMouseInput = false,
                    CanMove = false
                }
            );

            _timerLabel = new Label(
                "0s",
                true,
                HUE_YELLOW,
                28,
                font: FONT,
                style: FontStyle.BlackBorder,
                align: TEXT_ALIGN_TYPE.TS_CENTER
            )
            {
                X = DIAL_CENTER - 14,
                Y = DIAL_SIZE - 12,
                AcceptMouseInput = false
            };
            Add(_timerLabel);
        }

        private void BuildLineLayout()
        {
            _barY = (LINE_HEIGHT - LINE_BAR_OUTER_H) / 2;
            _lineTrackW = LINE_BAR_TRACK_W;

            int iconX = LINE_ICON_OUTSET - LINE_ICON_SIZE;
            int iconY = (LINE_HEIGHT - LINE_ICON_SIZE) / 2;

            _barX = Math.Max(LINE_ICON_OUTSET + 4, iconX + LINE_ICON_SIZE + LINE_CONTENT_GAP);
            _timerX = _barX + LINE_BAR_OUTER_W + 6;
            int panelWidth = _timerX + LINE_TIMER_SLOT_W + LINE_PADDING_RIGHT;

            Width = panelWidth;
            Height = LINE_HEIGHT;

            int trackX = _barX + (LINE_BAR_OUTER_W - LINE_BAR_TRACK_W) / 2;
            int trackY = _barY + (LINE_BAR_OUTER_H - LINE_BAR_TRACK_H) / 2;

            Add(
                _panelBg = new AlphaBlendControl(0.9f)
                {
                    X = 0,
                    Y = 0,
                    Width = panelWidth,
                    Height = LINE_HEIGHT
                }
            );

            Add(
                _frame = new BandageFrameControl
                {
                    X = 0,
                    Y = 0,
                    Width = panelWidth,
                    Height = LINE_HEIGHT,
                    AcceptMouseInput = false
                }
            );

            _barShell = new BandageFillBar(_barX, _barY, LINE_BAR_OUTER_W, LINE_BAR_OUTER_H, COLOR_SHELL);
            _barShell.AcceptMouseInput = false;
            Add(_barShell);

            _barTrack = new BandageFillBar(trackX, trackY, LINE_BAR_TRACK_W, LINE_BAR_TRACK_H, COLOR_TRACK);
            _barTrack.AcceptMouseInput = false;
            Add(_barTrack);

            _barFill = new BandageFillBar(trackX, trackY, LINE_BAR_TRACK_W, LINE_BAR_TRACK_H, COLOR_FILL);
            _barFill.AcceptMouseInput = false;
            Add(_barFill);

            Add(
                new BandageIconControl(BANDAGE_GRAPHIC, LINE_ICON_SIZE)
                {
                    X = iconX,
                    Y = iconY,
                    AcceptMouseInput = false,
                    CanMove = false
                }
            );

            _timerLabel = new Label(
                "0s",
                true,
                HUE_YELLOW,
                LINE_TIMER_SLOT_W,
                font: FONT,
                style: FontStyle.BlackBorder,
                align: TEXT_ALIGN_TYPE.TS_CENTER
            )
            {
                X = _timerX,
                Y = (LINE_HEIGHT - 12) / 2,
                AcceptMouseInput = false
            };
            Add(_timerLabel);
        }

        public void Start()
        {
            _useTime = true;
            _startTime = Time.Ticks;
            IsVisible = true;

            if (World.Player.Dexterity >= 80)
            {
                ushort dex = World.Player.Dexterity;
                if (dex >= 181)
                {
                    dex = 180;
                }

                _initialTimer = Convert.ToUInt32(8 - Math.Floor((dex - 80) * 1.0) / 20) - 1;
            }
            else
            {
                _initialTimer = 8;
            }

            _maxSeconds = Math.Max(_initialTimer, 1);

            if (_roundStyle)
            {
                _dial.Progress = 1f;
                _dial.ArcColor = COLOR_FILL;
            }
            else
            {
                _barShell.FillWidth = LINE_BAR_OUTER_W;
                _barTrack.FillWidth = LINE_BAR_TRACK_W;
                _barFill.FillWidth = LINE_BAR_TRACK_W;
                _barFill.SetColor(COLOR_FILL);
            }
        }

        public void Stop()
        {
            _useTime = false;
            IsVisible = false;

            if (_roundStyle)
            {
                _dial.Progress = 0f;
            }
            else
            {
                _barShell.FillWidth = 0;
                _barTrack.FillWidth = 0;
                _barFill.FillWidth = 0;
            }

            _timerLabel.Text = "0s";
        }

        public void OnCliloc(uint cliloc)
        {
            for (int i = 0; i < _stopAtClilocs.Length; i++)
            {
                if (_stopAtClilocs[i] == cliloc)
                {
                    Stop();
                    return;
                }
            }

            for (int i = 0; i < _startAtClilocs.Length; i++)
            {
                if (_startAtClilocs[i] == cliloc)
                {
                    Start();
                    return;
                }
            }
        }

        private void PersistLocation()
        {
            Profile p = ProfileManager.CurrentProfile;
            if (p == null)
            {
                return;
            }

            p.BandageGumpLocation = new Point(ScreenCoordinateX, ScreenCoordinateY);
        }

        protected override void OnDragEnd(int x, int y)
        {
            base.OnDragEnd(x, y);
            PersistLocation();
        }

        protected override void OnMove(int x, int y)
        {
            base.OnMove(x, y);
            PersistLocation();
        }

        internal static void PersistLocation(World world)
        {
            BandageGump gump = world?.Player?.BandageTimer ?? UIManager.GetGump<BandageGump>();
            gump?.PersistLocation();
        }

        public override void Update()
        {
            base.Update();

            if (IsDisposed)
            {
                return;
            }

            if (World.Player == null || World.Player.IsDestroyed)
            {
                Dispose();
                return;
            }

            if (!ProfileManager.CurrentProfile.BandageGump)
            {
                IsVisible = false;
                return;
            }

            if (!_useTime)
            {
                return;
            }

            IsVisible = true;

            float elapsedSec = (Time.Ticks - _startTime) / 1000f;
            bool countUp = ProfileManager.CurrentProfile.BandageGumpUpDownToggle;
            uint display;
            uint fillColor;
            float progress;

            if (countUp)
            {
                display = (uint)elapsedSec;
                if (display > 10)
                {
                    Stop();
                    return;
                }

                bool ready = display >= _maxSeconds;
                _timerLabel.Hue = ready ? HUE_GREEN : HUE_RED;
                fillColor = ready ? COLOR_FILL_READY : COLOR_FILL_WARN;
                progress = Math.Min(1f, elapsedSec / 10f);
            }
            else
            {
                float remaining = Math.Max(0f, _initialTimer - elapsedSec);
                display = (uint)Math.Ceiling(remaining);

                if (remaining <= 0f || elapsedSec > 10f)
                {
                    Stop();
                    return;
                }

                _timerLabel.Hue = HUE_YELLOW;
                fillColor = remaining <= 2f ? COLOR_FILL_WARN : COLOR_FILL;
                progress = _maxSeconds > 0 ? remaining / _maxSeconds : 0f;
            }

            _timerLabel.Text = $"{display}s";

            if (_roundStyle)
            {
                _dial.ArcColor = fillColor;
                _dial.Progress = progress;
            }
            else
            {
                _barFill.SetColor(fillColor);
                _barFill.FillWidth = (int)(_lineTrackW * progress);
                _barShell.FillWidth = LINE_BAR_OUTER_W;
                _barTrack.FillWidth = LINE_BAR_TRACK_W;
                _timerLabel.Y = (LINE_HEIGHT - _timerLabel.Height) / 2;
            }
        }

        internal static void RefreshOpenGump(World world)
        {
            BandageGump existing = UIManager.GetGump<BandageGump>();

            if (existing != null)
            {
                if (world.Player?.BandageTimer == existing)
                {
                    world.Player.BandageTimer = null;
                }

                existing.Dispose();
            }

            Profile p = ProfileManager.CurrentProfile;

            if (p == null || !p.BandageGump || world?.Player == null)
            {
                return;
            }

            BandageGump gump = new BandageGump(world)
            {
                X = p.BandageGumpLocation.X,
                Y = p.BandageGumpLocation.Y
            };

            world.Player.BandageTimer = gump;
            UIManager.Add(gump);
        }

        private sealed class BandageFrameControl : Control
        {
            private static readonly Texture2D _texture = SolidColorTextureCache.GetTexture(Color.White);

            public override bool AddToRenderLists(RenderLists renderLists, int x, int y, ref float layerDepthRef)
            {
                float layerDepth = layerDepthRef;
                Vector3 outer = ShaderHueTranslator.GetHueVector(34, false, Alpha * 0.55f);
                Vector3 inner = ShaderHueTranslator.GetHueVector(0, false, Alpha * 0.35f);

                renderLists.AddGumpNoAtlas(batcher =>
                {
                    batcher.DrawRectangle(_texture, x, y, Width, Height, outer, layerDepth);
                    batcher.DrawRectangle(_texture, x + 1, y + 1, Width - 2, Height - 2, inner, layerDepth);
                    return true;
                });

                return true;
            }
        }

        private sealed class BandageFillBar : Control
        {
            private Texture2D _texture;

            public BandageFillBar(int x, int y, int maxWidth, int height, uint colorPacked)
            {
                X = x;
                Y = y;
                Width = maxWidth;
                Height = height;
                SetColor(colorPacked);
            }

            public int FillWidth { get; set; }

            public void SetColor(uint colorPacked)
            {
                _texture = SolidColorTextureCache.GetTexture(new Color { PackedValue = colorPacked });
            }

            public override bool AddToRenderLists(RenderLists renderLists, int x, int y, ref float layerDepthRef)
            {
                float layerDepth = layerDepthRef;
                int w = Math.Max(0, Math.Min(FillWidth, Width));
                Vector3 hueVector = ShaderHueTranslator.GetHueVector(0, false, Alpha);

                renderLists.AddGumpNoAtlas(batcher =>
                {
                    batcher.Draw(_texture, new Rectangle(x, y, w, Height), hueVector, layerDepth);
                    return true;
                });

                return true;
            }
        }

        private sealed class BandageIconControl : Control
        {
            private readonly ushort _graphic;
            private readonly int _displaySize;

            public BandageIconControl(ushort graphic, int displaySize)
            {
                _graphic = graphic;
                _displaySize = displaySize;
                Width = displaySize;
                Height = displaySize;
            }

            public override bool AddToRenderLists(RenderLists renderLists, int x, int y, ref float layerDepthRef)
            {
                float layerDepth = layerDepthRef;
                Vector3 hueVector = ShaderHueTranslator.GetHueVector(0, false, Alpha);
                ref readonly var artInfo = ref Client.Game.UO.Arts.GetArt(_graphic);

                if (artInfo.Texture == null)
                {
                    return true;
                }

                Texture2D texture = artInfo.Texture;
                Rectangle uv = artInfo.UV;
                Rectangle bounds = Client.Game.UO.Arts.GetRealArtBounds(_graphic);

                if (bounds.Width <= 0 || bounds.Height <= 0)
                {
                    bounds = new Rectangle(0, 0, uv.Width, uv.Height);
                }

                int size = _displaySize;
                float scale = Math.Min(size / (float)bounds.Width, size / (float)bounds.Height);
                scale = Math.Min(scale, 1f);
                int drawW = Math.Max(1, (int)Math.Round(bounds.Width * scale));
                int drawH = Math.Max(1, (int)Math.Round(bounds.Height * scale));
                int drawX = x + (size - drawW) / 2;
                int drawY = y + (size - drawH) / 2;

                Rectangle source = new Rectangle(
                    uv.X + bounds.X,
                    uv.Y + bounds.Y,
                    bounds.Width,
                    bounds.Height
                );

                renderLists.AddGumpWithAtlas(batcher =>
                {
                    batcher.Draw(texture, new Rectangle(drawX, drawY, drawW, drawH), source, hueVector, layerDepth);
                    return true;
                });

                return true;
            }
        }

        private sealed class BandageDialControl : Control
        {
            public float Progress { get; set; }
            public uint ArcColor { get; set; } = COLOR_FILL;

            public override bool AddToRenderLists(RenderLists renderLists, int x, int y, ref float layerDepthRef)
            {
                float layerDepth = layerDepthRef;
                Vector2 center = new Vector2(x + DIAL_CENTER, y + DIAL_CENTER);
                Vector3 hue = ShaderHueTranslator.GetHueVector(0, false, Alpha);
                Vector3 borderHue = ShaderHueTranslator.GetHueVector(34, false, Alpha * 0.7f);

                Texture2D discTex = SolidColorTextureCache.GetTexture(new Color { PackedValue = COLOR_DISC });
                Texture2D trackTex = SolidColorTextureCache.GetTexture(new Color { PackedValue = COLOR_TRACK });
                Texture2D fillTex = SolidColorTextureCache.GetTexture(new Color { PackedValue = ArcColor });
                Texture2D borderTex = SolidColorTextureCache.GetTexture(Color.White);

                renderLists.AddGumpNoAtlas(batcher =>
                {
                    FillCircle(batcher, discTex, center, INNER_RADIUS, hue, layerDepth);
                    DrawArcRing(batcher, trackTex, center, RING_RADIUS, START_ANGLE, MathHelper.TwoPi, RING_THICKNESS, hue, layerDepth);

                    float sweep = MathHelper.TwoPi * Math.Clamp(Progress, 0f, 1f);
                    if (sweep > 0.02f)
                    {
                        DrawArcRing(batcher, fillTex, center, RING_RADIUS, START_ANGLE, sweep, RING_THICKNESS, hue, layerDepth);
                    }

                    DrawArcRing(batcher, borderTex, center, RING_RADIUS + RING_THICKNESS * 0.5f + 1f, START_ANGLE, MathHelper.TwoPi, 1.5f, borderHue, layerDepth);
                    return true;
                });

                return true;
            }

            private static void FillCircle(UltimaBatcher2D batcher, Texture2D texture, Vector2 center, float radius, Vector3 hue, float depth)
            {
                const int segments = 24;
                float stroke = radius * 0.22f;

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

                int segments = Math.Max(12, (int)(sweep / MathHelper.Pi * 28));
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
