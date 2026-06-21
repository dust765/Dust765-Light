// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Dust765.External
{
    internal class BandageGump : Gump
    {
        private const byte FONT = 0xFF;
        private const ushort HUE_LABEL = 999;
        private const ushort HUE_YELLOW = 0x35;
        private const ushort HUE_RED = 0x26;
        private const ushort HUE_GREEN = 0x3F;

        private readonly AlphaBlendControl _background;
        private readonly Label _titleLabel;
        private readonly UccSwingFillLine _fillLine;
        private readonly Label _timerLabel;

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

            Width = 141;
            Height = 24;

            Add(
                _background = new AlphaBlendControl(0.6f)
                {
                    Width = Width,
                    Height = Height
                }
            );

            _titleLabel = new Label("Bandage", true, HUE_YELLOW, font: FONT, style: FontStyle.BlackBorder)
            {
                X = 0,
                Y = 0,
                Width = 52,
                Height = 20
            };
            Add(_titleLabel);

            _fillLine = new UccSwingFillLine(_titleLabel.Width + 1, 0, 100, 20, Color.Red.PackedValue);
            Add(_fillLine);

            _timerLabel = new Label("0", true, HUE_GREEN, font: FONT, style: FontStyle.BlackBorder)
            {
                X = _titleLabel.Width + 10,
                Y = 0
            };
            Add(_timerLabel);

            WantUpdateSize = false;
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
            _fillLine.FillWidth = 100;
        }

        public void Stop()
        {
            _useTime = false;
            IsVisible = false;
            _fillLine.FillWidth = 0;
            _timerLabel.Text = "0";
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

        protected override void OnDragEnd(int x, int y)
        {
            base.OnDragEnd(x, y);
            ProfileManager.CurrentProfile.BandageGumpLocation = Location;
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
            uint elapsed = (Time.Ticks - _startTime) / 1000;
            bool countUp = ProfileManager.CurrentProfile.BandageGumpUpDownToggle;
            uint display;

            if (countUp)
            {
                display = elapsed;
                if (display > 10)
                {
                    Stop();
                    return;
                }

                _timerLabel.Hue = display >= _maxSeconds ? HUE_GREEN : HUE_RED;
                _fillLine.FillWidth = (int)Math.Min(100, display * 100 / 10);
            }
            else
            {
                display = _initialTimer > elapsed ? _initialTimer - elapsed : 0;
                if (display == 0 || elapsed > 10)
                {
                    Stop();
                    return;
                }

                _timerLabel.Hue = HUE_RED;
                _fillLine.FillWidth = (int)(_maxSeconds > 0 ? display * 100 / _maxSeconds : 0);
            }

            _timerLabel.Text = $"{display}";
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
    }
}
