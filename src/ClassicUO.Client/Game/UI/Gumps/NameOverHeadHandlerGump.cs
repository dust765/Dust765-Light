// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Resources;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal class NameOverHeadHandlerGump : Gump
    {
        public static Point? LastPosition;

        public override GumpType GumpType => GumpType.NameOverHeadHandler;

        private readonly List<RadioButton> _overheadButtons = new List<RadioButton>();
        private Control _alpha;
        private readonly Checkbox _keepOpenCheckbox;
        private readonly Checkbox _keepPinnedCheckbox;
        private readonly Checkbox _noBackgroundCheckbox;
        private readonly Checkbox _healthLinesCheckbox;

        public NameOverHeadHandlerGump(World world) : base(world, 0, 0)
        {
            CanMove = true;
            AcceptMouseInput = true;
            CanCloseWithRightClick = !world.NameOverHeadManager.IsPinnedToggled;

            if (LastPosition == null)
            {
                X = 100;
                Y = 100;
            }
            else
            {
                X = LastPosition.Value.X;
                Y = LastPosition.Value.Y;
            }

            WantUpdateSize = false;

            LayerOrder = UILayer.Over;

            Add(
                _alpha = new AlphaBlendControl(0.7f)
                {
                    Hue = 34
                }
            );

            Add(
                _keepOpenCheckbox = new Checkbox(
                    0x00D2,
                    0x00D3,
                    ResGumps.StayActive,
                    0xFF,
                    0xFFFF
                )
                {
                    IsChecked = world.NameOverHeadManager.IsPermaToggled
                }
            );

            _keepOpenCheckbox.ValueChanged += (_, _) =>
                world.NameOverHeadManager.SetOverheadToggled(_keepOpenCheckbox.IsChecked);

            Add(
                _keepPinnedCheckbox = new Checkbox(
                    0x00D2,
                    0x00D3,
                    "Pin this UI",
                    0xFF,
                    0xFFFF
                )
                {
                    IsChecked = world.NameOverHeadManager.IsPinnedToggled,
                    X = 100
                }
            );

            _keepPinnedCheckbox.ValueChanged += (_, _) =>
                world.NameOverHeadManager.SetPinnedToggled(_keepPinnedCheckbox.IsChecked);

            Add(
                _noBackgroundCheckbox = new Checkbox(
                    0x00D2,
                    0x00D3,
                    "bg on mouse",
                    0xFF,
                    0xFFFF
                )
                {
                    IsChecked = world.NameOverHeadManager.IsBackgroundToggled,
                    Y = 20
                }
            );

            _noBackgroundCheckbox.ValueChanged += (_, _) =>
                world.NameOverHeadManager.SetBackgroundToggled(_noBackgroundCheckbox.IsChecked);

            Add(
                _healthLinesCheckbox = new Checkbox(
                    0x00D2,
                    0x00D3,
                    "HP",
                    0xFF,
                    0xFFFF
                )
                {
                    IsChecked = world.NameOverHeadManager.IsHealthLinesToggled,
                    Y = 20,
                    X = 100
                }
            );

            _healthLinesCheckbox.ValueChanged += (_, _) =>
                world.NameOverHeadManager.SetHealthLinesToggled(_healthLinesCheckbox.IsChecked);

            DrawChoiceButtons();
        }

        protected override void OnDragEnd(int x, int y)
        {
            LastPosition = new Point(ScreenCoordinateX, ScreenCoordinateY);

            SetInScreen();

            base.OnDragEnd(x, y);
        }

        public void UpdateCheckboxes()
        {
            foreach (RadioButton button in _overheadButtons)
            {
                button.IsChecked = World.NameOverHeadManager.LastActiveNameOverheadOption == button.Text;
            }

            _keepOpenCheckbox.IsChecked = World.NameOverHeadManager.IsPermaToggled;
            _keepPinnedCheckbox.IsChecked = World.NameOverHeadManager.IsPinnedToggled;
            _noBackgroundCheckbox.IsChecked = World.NameOverHeadManager.IsBackgroundToggled;
            _healthLinesCheckbox.IsChecked = World.NameOverHeadManager.IsHealthLinesToggled;
        }

        public void RedrawOverheadOptions()
        {
            foreach (RadioButton button in _overheadButtons)
            {
                Remove(button);
            }

            _overheadButtons.Clear();
            DrawChoiceButtons();
        }

        private void DrawChoiceButtons()
        {
            int biggestWidth = 100;
            IReadOnlyList<NameOverheadOption> options = World.NameOverHeadManager.GetAllOptions();

            for (int i = 0; i < options.Count; i++)
            {
                biggestWidth = System.Math.Max(biggestWidth, AddOverheadOptionButton(options[i], i).Width);
            }

            _alpha.Width = biggestWidth;
            _alpha.Height = System.Math.Max(30, options.Count * 20) + 42;

            Width = _alpha.Width;
            Height = _alpha.Height;
        }

        private RadioButton AddOverheadOptionButton(NameOverheadOption option, int index)
        {
            RadioButton button;

            Add(
                button = new RadioButton(
                    0,
                    0x00D0,
                    0x00D1,
                    option.Name,
                    0xFF,
                    0xFFFF
                )
                {
                    Y = 20 * index + 42,
                    IsChecked = World.NameOverHeadManager.LastActiveNameOverheadOption == option.Name,
                }
            );

            button.ValueChanged += (_, _) =>
            {
                if (button.IsChecked)
                {
                    World.NameOverHeadManager.SetActiveOption(option);
                }
            };

            _overheadButtons.Add(button);

            return button;
        }
    }
}
