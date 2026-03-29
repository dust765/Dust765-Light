// SPDX-License-Identifier: BSD-2-Clause
// Ported from Dust765 reference implementation

using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using System;

namespace ClassicUO.Dust765.External
{
    internal class BandageGump : Gump
    {
        private const byte _iconSize  = 16;
        private const byte _spaceSize = 3;
        private const byte _borderSize = 3;

        public uint Timer { get; set; }

        private bool _useTime;
        private uint _startTime;
        private uint _initialTimer;

        private static bool _upDownToggle => ProfileManager.CurrentProfile.BandageGumpUpDownToggle;

        private AlphaBlendControl _background;
        private Label _text;
        private StaticPic _icon;

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
            CanMove = false;
            AcceptMouseInput = false;
            CanCloseWithEsc = false;
            CanCloseWithRightClick = false;
            _useTime = false;
            IsVisible = false;

            BuildGump();
        }

        public void Start()
        {
            _useTime = true;
            _startTime = Time.Ticks;
            IsVisible = true;

            if (World.Player.Dexterity >= 80)
            {
                ushort dex = World.Player.Dexterity;
                if (dex >= 181) dex = 180;
                _initialTimer = Convert.ToUInt32(8 - Math.Floor((dex - 80) * 1.0) / 20) - 1;
            }
            else
            {
                _initialTimer = 8;
            }
        }

        public void Stop()
        {
            _useTime = false;
            IsVisible = false;
            Timer = 0;
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

        public override void Update()
        {
            base.Update();

            if (IsDisposed) return;

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

            if (_useTime)
            {
                if (_upDownToggle)
                {
                    // COUNT UP
                    IsVisible = true;
                    Timer = (Time.Ticks - _startTime) / 1000;
                    if (Timer > 10)
                        Stop();
                }
                else
                {
                    // COUNT DOWN
                    IsVisible = true;
                    uint delta = (Time.Ticks - _startTime) / 1000;
                    Timer = _initialTimer > delta ? _initialTimer - delta : 0;
                    if (Timer == 0 || delta > 10)
                        Stop();
                }
            }

            if (IsVisible)
            {
                _text.Text = $"{Timer}";

                // Position next to the player
                int gx = ProfileManager.CurrentProfile.GameWindowPosition.X;
                int gy = ProfileManager.CurrentProfile.GameWindowPosition.Y;
                int px = gx + World.Player.RealScreenPosition.X + (int)World.Player.Offset.X;
                int py = gy + World.Player.RealScreenPosition.Y + (int)(World.Player.Offset.Y - World.Player.Offset.Z);

                Width  = _borderSize * 2 + _iconSize + _spaceSize + _text.Width;
                Height = _borderSize * 2 + _iconSize;

                _background.Width  = Width;
                _background.Height = Height;

                X = px - (Width >> 1) + 5 + ProfileManager.CurrentProfile.BandageGumpOffset.X;
                Y = py + 10 + ProfileManager.CurrentProfile.BandageGumpOffset.Y;
            }
        }

        private void BuildGump()
        {
            _background = new AlphaBlendControl { Alpha = 0.6f };

            _text = new Label($"{Timer}", true, 0x35, 0, 1, FontStyle.BlackBorder)
            {
                X = _borderSize + _iconSize + _spaceSize + 3,
                Y = _borderSize - 2
            };

            _icon = new StaticPic(0x0E21, 0)
            {
                X = _borderSize - _iconSize,
                Y = _borderSize - 1,
                AcceptMouseInput = false
            };

            Add(_background);
            Add(_text);
            Add(_icon);
        }
    }
}
