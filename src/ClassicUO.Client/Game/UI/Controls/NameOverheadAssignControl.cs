// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using ClassicUO.Assets;
using ClassicUO.Game;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Resources;
using SDL3;

namespace ClassicUO.Game.UI.Controls
{
    internal class NameOverheadAssignControl : Control
    {
        private enum ButtonType
        {
            CheckAll,
            UncheckAll,
        }

        private readonly World _world;
        private readonly HotkeyBox _hotkeyBox;
        private readonly Dictionary<NameOverheadOptions, Checkbox> _checkboxDict = new Dictionary<NameOverheadOptions, Checkbox>();

        public NameOverheadAssignControl(World world, NameOverheadOption option)
        {
            _world = world;
            Option = option;

            CanMove = true;

            AddLabel("Set hotkey:", 0, 0);

            _hotkeyBox = new HotkeyBox
            {
                X = 80
            };

            _hotkeyBox.HotkeyChanged += BoxOnHotkeyChanged;
            _hotkeyBox.HotkeyCancelled += BoxOnHotkeyCancelled;

            Add(_hotkeyBox);

            Add(
                new NiceButton(
                    0,
                    _hotkeyBox.Height + 3,
                    100,
                    25,
                    ButtonAction.Activate,
                    "Uncheck all",
                    0,
                    TEXT_ALIGN_TYPE.TS_LEFT
                )
                {
                    ButtonParameter = (int)ButtonType.UncheckAll,
                    IsSelectable = false
                }
            );

            Add(
                new NiceButton(
                    120,
                    _hotkeyBox.Height + 3,
                    100,
                    25,
                    ButtonAction.Activate,
                    "Check all",
                    0,
                    TEXT_ALIGN_TYPE.TS_LEFT
                )
                {
                    ButtonParameter = (int)ButtonType.CheckAll,
                    IsSelectable = false
                }
            );

            SetupOptionCheckboxes();

            UpdateCheckboxesByCurrentOptionFlags();
            UpdateValueInHotkeyBox();
        }

        public NameOverheadOption Option { get; }

        private void SetupOptionCheckboxes()
        {
            int y = 60;
            AddLabel("Items", 75, y);
            y += 28;

            AddCheckbox("Containers", NameOverheadOptions.Containers, 0, y);
            AddCheckbox("Gold", NameOverheadOptions.Gold, 150, y);
            y += 22;
            AddCheckbox("Stackable", NameOverheadOptions.Stackable, 0, y);
            AddCheckbox("Locked down", NameOverheadOptions.LockedDown, 150, y);
            y += 22;
            AddCheckbox("Propertys", NameOverheadOptions.Properties, 0, y);
            AddCheckbox("Name List", NameOverheadOptions.Nameslist, 150, y);
            y += 28;

            AddLabel("Corpses", 75, y);
            y += 28;

            AddCheckbox("Monster corpses", NameOverheadOptions.MonsterCorpses, 0, y);
            AddCheckbox("Humanoid corpses", NameOverheadOptions.HumanoidCorpses, 150, y);
            y += 22;
            AddCheckbox("Own corpses", NameOverheadOptions.OwnCorpses, 0, y);
            y += 28;

            AddLabel("Mobiles by type", 75, y);
            y += 28;

            AddCheckbox("Humanoid", NameOverheadOptions.Humanoid, 0, y);
            AddCheckbox("Monster", NameOverheadOptions.Monster, 150, y);
            y += 22;
            AddCheckbox("Own Followes", NameOverheadOptions.OwnFollowers, 0, y);
            y += 28;

            AddLabel("Mobiles by notoriety", 75, y);
            y += 28;

            AddCheckbox("Innocent (blue)", NameOverheadOptions.Innocent, 0, y);
            AddCheckbox("Allied (green)", NameOverheadOptions.Ally, 150, y);
            y += 22;
            AddCheckbox("Attackable (gray)", NameOverheadOptions.Gray, 0, y);
            AddCheckbox("Criminal (gray)", NameOverheadOptions.Criminal, 150, y);
            y += 22;
            AddCheckbox("Enemy (orange)", NameOverheadOptions.Enemy, 0, y);
            AddCheckbox("Murderer (red)", NameOverheadOptions.Murderer, 150, y);
            y += 22;
            AddCheckbox("Invulnerable (yellow)", NameOverheadOptions.Invulnerable, 0, y);
        }

        private void AddLabel(string name, int x, int y)
        {
            Add(
                new Label(name, true, 0xFFFF)
                {
                    X = x,
                    Y = y,
                }
            );
        }

        private void AddCheckbox(string checkboxName, NameOverheadOptions optionFlag, int x, int y)
        {
            var checkbox = new Checkbox(0x00D2, 0x00D3, checkboxName, 0xFF, 0xFFFF)
            {
                IsChecked = true,
                X = x,
                Y = y
            };

            checkbox.ValueChanged += (_, _) =>
            {
                if (checkbox.IsChecked)
                {
                    Option.NameOverheadOptionFlags |= (int)optionFlag;
                }
                else
                {
                    Option.NameOverheadOptionFlags &= ~(int)optionFlag;
                }

                _world.NameOverHeadManager.SyncActiveOverheadFlagsIfCurrent(Option);
            };

            _checkboxDict.Add(optionFlag, checkbox);

            Add(checkbox);
        }

        private void UpdateValueInHotkeyBox()
        {
            if (Option == null || _hotkeyBox == null)
            {
                return;
            }

            if (Option.Key != SDL.SDL_Keycode.SDLK_UNKNOWN)
            {
                SDL.SDL_Keymod mod = SDL.SDL_Keymod.SDL_KMOD_NONE;

                if (Option.Alt)
                {
                    mod |= SDL.SDL_Keymod.SDL_KMOD_ALT;
                }

                if (Option.Shift)
                {
                    mod |= SDL.SDL_Keymod.SDL_KMOD_SHIFT;
                }

                if (Option.Ctrl)
                {
                    mod |= SDL.SDL_Keymod.SDL_KMOD_CTRL;
                }

                _hotkeyBox.SetKey(Option.Key, mod);
            }
        }

        private void BoxOnHotkeyChanged(object sender, EventArgs e)
        {
            bool shift = (_hotkeyBox.Mod & SDL.SDL_Keymod.SDL_KMOD_SHIFT) != SDL.SDL_Keymod.SDL_KMOD_NONE;
            bool alt = (_hotkeyBox.Mod & SDL.SDL_Keymod.SDL_KMOD_ALT) != SDL.SDL_Keymod.SDL_KMOD_NONE;
            bool ctrl = (_hotkeyBox.Mod & SDL.SDL_Keymod.SDL_KMOD_CTRL) != SDL.SDL_Keymod.SDL_KMOD_NONE;

            if (_hotkeyBox.Key == SDL.SDL_Keycode.SDLK_UNKNOWN)
            {
                return;
            }

            NameOverheadOption option = _world.NameOverHeadManager.FindOptionByHotkey(_hotkeyBox.Key, alt, ctrl, shift);

            if (option == null)
            {
                Option.Key = _hotkeyBox.Key;
                Option.Shift = shift;
                Option.Alt = alt;
                Option.Ctrl = ctrl;

                return;
            }

            if (Option == option)
            {
                return;
            }

            UpdateValueInHotkeyBox();
            UIManager.Add(
                new MessageBoxGump(
                    _world,
                    250,
                    150,
                    string.Format(ResGumps.ThisKeyCombinationAlreadyExists, option.Name),
                    null
                )
            );
        }

        private void BoxOnHotkeyCancelled(object sender, EventArgs e)
        {
            Option.Alt = Option.Ctrl = Option.Shift = false;
            Option.Key = SDL.SDL_Keycode.SDLK_UNKNOWN;
        }

        public override void OnButtonClick(int buttonID)
        {
            switch ((ButtonType)buttonID)
            {
                case ButtonType.CheckAll:
                    Option.NameOverheadOptionFlags = int.MaxValue;
                    UpdateCheckboxesByCurrentOptionFlags();
                    _world.NameOverHeadManager.SyncActiveOverheadFlagsIfCurrent(Option);

                    break;

                case ButtonType.UncheckAll:
                    Option.NameOverheadOptionFlags = 0;
                    UpdateCheckboxesByCurrentOptionFlags();
                    _world.NameOverHeadManager.SyncActiveOverheadFlagsIfCurrent(Option);

                    break;
            }
        }

        private void UpdateCheckboxesByCurrentOptionFlags()
        {
            foreach (KeyValuePair<NameOverheadOptions, Checkbox> kvp in _checkboxDict)
            {
                kvp.Value.IsChecked = ((NameOverheadOptions)Option.NameOverheadOptionFlags).HasFlag(kvp.Key);
            }
        }
    }
}
