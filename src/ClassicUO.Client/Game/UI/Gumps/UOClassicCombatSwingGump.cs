// SPDX-License-Identifier: BSD-2-Clause
// Swing timer bar (Dust765 / UO Classic Combat) — adapted for ClassicUO-light render pipeline

using System;
using System.Collections.Generic;
using System.IO;
using ClassicUO;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.UI.Gumps
{
    internal sealed class UccSwingFillLine : Control
    {
        private readonly Texture2D _texture;

        public UccSwingFillLine(int x, int y, int maxWidth, int height, uint colorPacked)
        {
            X = x;
            Y = y;
            Width = maxWidth;
            Height = height;
            _texture = SolidColorTextureCache.GetTexture(new Color { PackedValue = colorPacked });
        }

        public int FillWidth { get; set; }

        public override bool AddToRenderLists(RenderLists renderLists, int px, int py, ref float layerDepthRef)
        {
            float layerDepth = layerDepthRef;
            int w = Math.Max(0, Math.Min(FillWidth, Width));
            Vector3 hueVector = ShaderHueTranslator.GetHueVector(0, false, Alpha);

            renderLists.AddGumpNoAtlas(batcher =>
            {
                batcher.Draw(_texture, new Rectangle(px, py, w, Height), hueVector, layerDepth);
                return true;
            });

            return true;
        }
    }

    internal sealed class UOClassicCombatSwingGump : Gump
    {
        public static readonly List<ushort> WeaponsList = new List<ushort>();

        private const byte FONT = 0xFF;
        private const ushort HUE_LABEL = 999;
        private const ushort HUE_YELLOW = 0x35;
        private const ushort HUE_RED = 0x26;
        private const ushort HUE_GREEN = 0x3F;

        private readonly AlphaBlendControl _background;
        private readonly Label _titleLabel;
        private readonly UccSwingFillLine _fillLine;
        private readonly Label _timerLabel;

        private bool _triggerSwing;
        private uint _timerSwing;
        private uint _tickSwing;

        public UOClassicCombatSwingGump(World world)
            : base(world, 0, 0)
        {
            CanMove = true;
            CanCloseWithEsc = false;
            CanCloseWithRightClick = false;
            AcceptMouseInput = true;
            LayerOrder = UILayer.Over;

            if (ProfileManager.CurrentProfile.UOClassicCombatBuffbar_Locked)
            {
                CanMove = false;
                AcceptMouseInput = false;
            }

            Width = 141;
            Height = 24;

            Add(
                _background = new AlphaBlendControl(0.6f)
                {
                    Width = Width,
                    Height = Height
                }
            );

            _titleLabel = new Label("Swing", true, HUE_YELLOW, font: FONT, style: FontStyle.BlackBorder)
            {
                X = 0,
                Y = 0,
                Width = 40,
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
            LoadSwingTimerFile();
        }

        public override GumpType GumpType => GumpType.None;

        internal static void NotifyPlayerAnimation(World world, ushort action)
        {
            if (world?.Player == null)
            {
                return;
            }

            if (
                action >= 9 && action <= 15
                || action == 18
                || action == 19
                || action >= 26 && action <= 29
                || action == 31
            )
            {
                UIManager.GetGump<UOClassicCombatSwingGump>()?.ClilocTriggerSwing();
            }
        }

        internal static void NotifyPlayerNewAnimation(World world)
        {
            if (world?.Player == null)
            {
                return;
            }

            UIManager.GetGump<UOClassicCombatSwingGump>()?.ClilocTriggerSwing();
        }

        public void ClilocTriggerSwing()
        {
            if (!ProfileManager.CurrentProfile.UOClassicCombatBuffbar_SwingEnabled)
            {
                return;
            }

            _tickSwing = Time.Ticks;
            _triggerSwing = true;
        }

        protected override void OnDragEnd(int x, int y)
        {
            base.OnDragEnd(x, y);
            ProfileManager.CurrentProfile.UOClassicCombatBuffbarLocation = Location;
        }

        public override void Update()
        {
            base.Update();

            if (World.Player == null || World.Player.IsDestroyed)
            {
                return;
            }

            if (_timerSwing == 0)
            {
                _timerLabel.Hue = HUE_GREEN;
            }
            else
            {
                _timerLabel.Hue = HUE_RED;
            }

            if (_triggerSwing)
            {
                RunSwingCooldown();
            }
        }

        private void RunSwingCooldown()
        {
            uint swingCooldown = 0;

            Item weapon = World.Player.FindItemByLayer(Layer.TwoHanded)
                ?? World.Player.FindItemByLayer(Layer.OneHanded);

            if (weapon != null)
            {
                int index = WeaponsList.IndexOf(weapon.Graphic);

                if (index >= 0 && index + 1 < WeaponsList.Count)
                {
                    swingCooldown = WeaponsList[index + 1];
                }
                else
                {
                    swingCooldown = 2000;
                }

                double input = 60000.0 / ((World.Player.Stamina + 100) * swingCooldown);
                double final = Math.Min(Math.Max(Math.Floor(input) * 0.25, 1.25), 10) * 1000;
                swingCooldown = Convert.ToUInt32(final);
            }

            if (_tickSwing != 0)
            {
                _timerSwing = swingCooldown / 100 - (Time.Ticks - _tickSwing) / 100;
            }

            if (_tickSwing != 0 && _tickSwing + swingCooldown <= Time.Ticks)
            {
                _timerSwing = 0;
                _tickSwing = 0;
                _triggerSwing = false;
                _fillLine.FillWidth = 0;
            }

            _timerLabel.Text = $"{_timerSwing}";

            if (_tickSwing != 0 && swingCooldown > 0)
            {
                uint w = 100 / (swingCooldown / 100) * _timerSwing;
                _fillLine.FillWidth = (int)w;
            }
        }

        private static void LoadSwingTimerFile()
        {
            if (WeaponsList.Count > 0)
            {
                return;
            }

            string path = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Client");

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            string swingPath = Path.Combine(path, "swingtimer.txt");

            if (!File.Exists(swingPath))
            {
                CreateDefaultSwingTimerFile(swingPath);
            }

            TextFileParser parser = new TextFileParser(
                File.ReadAllText(swingPath),
                new[] { ' ', '\t', ',', '=' },
                new[] { '#', ';' },
                new[] { '"', '"' }
            );

            while (!parser.IsEOF())
            {
                List<string> tokens = parser.ReadTokens();

                if (tokens == null || tokens.Count == 0)
                {
                    continue;
                }

                if (tokens.Count > 0 && ushort.TryParse(tokens[0], out ushort graphic))
                {
                    WeaponsList.Add(graphic);
                }

                if (tokens.Count > 1 && ushort.TryParse(tokens[1], out ushort spd))
                {
                    WeaponsList.Add(spd);
                }
            }
        }

        private static void CreateDefaultSwingTimerFile(string swingPath)
        {
            ushort[] weapons =
            {
                0x1400, 0x1401, 0x13FE, 0x13FF, 0x1440, 0x1441, 0x13B5, 0x13B6,
                0x13B3, 0x13B4, 0x143C, 0x143D, 0x13AF, 0x13B0, 0x1404, 0x1405,
                0x13B7, 0x13B8, 0x0F60, 0x0F61, 0x0F5E, 0x0F5F, 0x13B9, 0x13BA,
                0x0F5C, 0x0F5D, 0x143A, 0x143B, 0x1406, 0x1407, 0x13B1, 0x13B2,
                0x1402, 0x1403, 0x0E87, 0x0E88, 0x0F49, 0x0F4A, 0x0F47, 0x0F48,
                0x0F4B, 0x0F4C, 0x0F45, 0x0F46, 0x13FA, 0x13FB, 0x1442, 0x1443,
                0x13F8, 0x13F9, 0x0DF0, 0x0DF1, 0x0E89, 0x0E8A, 0x0F4F, 0x0F50,
                0x0F62, 0x0F63, 0x143E, 0x143F, 0x0F4D, 0x0F4E, 0x1438, 0x1439,
                0x13FC, 0x13FD
            };

            ushort[] speeds =
            {
                53, 53, 58, 58, 45, 45, 43, 43, 40, 40, 30, 30, 40, 40, 45, 45,
                35, 35, 35, 35, 45, 45, 30, 30, 30, 30, 30, 30, 32, 32, 20, 20,
                50, 50, 45, 45, 37, 37, 30, 30, 37, 37, 37, 37, 30, 30, 30, 30,
                33, 33, 35, 35, 48, 48, 18, 18, 46, 46, 25, 25, 26, 26, 31, 31,
                10, 10
            };

            using StreamWriter w = new StreamWriter(swingPath);

            for (int i = 0; i < weapons.Length && i < speeds.Length; i++)
            {
                w.WriteLine($"{weapons[i]}={speeds[i]}");
            }
        }

        internal static void RefreshOpenGump(World world)
        {
            UOClassicCombatSwingGump existing = UIManager.GetGump<UOClassicCombatSwingGump>();

            if (existing != null)
            {
                existing.Dispose();
            }

            var p = ProfileManager.CurrentProfile;

            if (p != null && p.UOClassicCombatBuffbar && p.UOClassicCombatBuffbar_SwingEnabled)
            {
                UIManager.Add(
                    new UOClassicCombatSwingGump(world)
                    {
                        X = p.UOClassicCombatBuffbarLocation.X,
                        Y = p.UOClassicCombatBuffbarLocation.Y
                    }
                );
            }
        }
    }
}
