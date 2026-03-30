// SPDX-License-Identifier: BSD-2-Clause
// Ported from Dust765 reference implementation

using System;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using Microsoft.Xna.Framework;

namespace ClassicUO.Dust765.External
{
    internal class OnCastingGump : Gump
    {
        private const byte _borderSize = 3;

        private uint _startTime;
        private uint _endTime;

        private AlphaBlendControl _background;
        private Label _text;

        // Clilocs que interrompem o casting
        private static readonly int[] _stopAtClilocs =
        {
            500641,  // Your concentration is disturbed
            502625,  // Insufficient mana
            502630,  // More reagents needed
            500946,  // Cannot cast in town
            500015,  // You do not have that spell
            502643,  // Cannot cast while frozen
            1061091, // Cannot cast in this form
            502644,  // Not yet recovered
            1072060, // Cannot cast while calmed
        };

        public OnCastingGump(World world) : base(world, 0, 0)
        {
            CanMove = false;
            AcceptMouseInput = false;
            CanCloseWithEsc = false;
            CanCloseWithRightClick = false;
            GameActions.iscasting = false;
            IsVisible = false;
            BuildGump();
        }

        public void Start()
        {
            if (!ProfileManager.CurrentProfile.OnCastingGump)
            {
                return;
            }

            _startTime = Time.Ticks;
            int spellIndex = GameActions.LastSpellIndex;
            int circle = GetCastingCircle(spellIndex);
            uint protectionDelay = 0;
            PlayerMobile player = World.Player;
            if (player != null)
            {
                SpellDefinition def = SpellDefinition.FullIndexGetSpell(spellIndex);
                bool ignoreProtectionDelay =
                    def.Name != null
                    && def.Name.IndexOf("protection", StringComparison.OrdinalIgnoreCase) >= 0;
                if (
                    (
                        player.IsBuffIconExists(BuffIconType.Protection)
                        && !ignoreProtectionDelay
                    )
                    || player.IsBuffIconExists(BuffIconType.EssenceOfWind)
                )
                {
                    protectionDelay = 2;
                }
            }

            _endTime = _startTime + 400 + (uint)(circle + protectionDelay) * 250;
            GameActions.iscasting = true;

            if (!ProfileManager.CurrentProfile.OnCastingGump_hidden)
            {
                IsVisible = true;
            }
        }

        public void Stop()
        {
            GameActions.iscasting = false;
            IsVisible = false;
        }

        public void OnCliloc(uint cliloc)
        {
            if (!GameActions.iscasting)
            {
                return;
            }

            for (int i = 0; i < _stopAtClilocs.Length; i++)
            {
                if (_stopAtClilocs[i] == cliloc)
                {
                    Stop();
                    return;
                }
            }
        }

        public override void Dispose()
        {
            Stop();
            base.Dispose();
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

            if (!ProfileManager.CurrentProfile.OnCastingGump)
            {
                IsVisible = false;
                return;
            }

            if (GameActions.iscasting && Time.Ticks >= _endTime)
            {
                Stop();
            }
            else if (!GameActions.iscasting && IsVisible)
            {
                Stop();
            }

            if (IsVisible)
            {
                // Posicionar: se OnCastingUnderPlayerBar = true, fica abaixo do player; senão segue o mouse
                if (ProfileManager.CurrentProfile.OnCastingUnderPlayerBar)
                {
                    int gx = ProfileManager.CurrentProfile.GameWindowPosition.X;
                    int gy = ProfileManager.CurrentProfile.GameWindowPosition.Y;
                    int px = gx + World.Player.RealScreenPosition.X + (int)World.Player.Offset.X;
                    int py = gy + World.Player.RealScreenPosition.Y + (int)(World.Player.Offset.Y - World.Player.Offset.Z);

                    int w = _borderSize * 2 + _text.Width;
                    int h = _borderSize * 2 + _text.Height;
                    _background.Width  = w;
                    _background.Height = h;
                    Width  = w;
                    Height = h;

                    X = px - (w >> 1);
                    Y = py + 4; // 4px abaixo do char
                }
                else
                {
                    int w = _borderSize * 2 + _text.Width;
                    int h = _borderSize * 2 + _text.Height;
                    _background.Width  = w;
                    _background.Height = h;
                    Width  = w;
                    Height = h;

                    X = Mouse.Position.X;
                    Y = Mouse.Position.Y;
                }
            }
        }

        private void BuildGump()
        {
            _background = new AlphaBlendControl { Alpha = 0.6f };

            _text = new Label("Casting...", true, 0x35, 0, 1, FontStyle.BlackBorder)
            {
                X = _borderSize,
                Y = _borderSize - 2
            };

            Add(_background);
            Add(_text);
        }

        private static int GetCastingCircle(int spellIndex)
        {
            if (spellIndex >= 1 && spellIndex < 100)
            {
                return Math.Min(8, (spellIndex - 1) / 8 + 1);
            }

            return 4;
        }
    }
}
