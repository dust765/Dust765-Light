// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using ClassicUO.Assets;
using ClassicUO.Configuration;
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

            AddLabel("Hotkey:", 0, 0);

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
                    "None",
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
                    "All",
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
            const int row = 21;
            const int head = 26;
            int y = 58;
            AddLabel("Items", 75, y);
            y += head;

            AddCheckbox("Containers", NameOverheadOptions.Containers, 0, y);
            AddCheckbox("Gold", NameOverheadOptions.Gold, 150, y);
            y += row;
            AddCheckbox("Stackable", NameOverheadOptions.Stackable, 0, y);
            AddCheckbox("Locked", NameOverheadOptions.LockedDown, 150, y);
            y += row;
            AddCheckbox("Props", NameOverheadOptions.Properties, 0, y);
            AddCheckbox("Name list", NameOverheadOptions.Nameslist, 150, y);
            y += head;

            AddLabel("Corpses", 75, y);
            y += head;

            AddCheckbox("Mob corpses", NameOverheadOptions.MonsterCorpses, 0, y);
            AddCheckbox("Human corpses", NameOverheadOptions.HumanoidCorpses, 150, y);
            y += row;
            AddCheckbox("Your corpses", NameOverheadOptions.OwnCorpses, 0, y);
            y += head;

            AddLabel("By type", 75, y);
            y += head;

            AddCheckbox("Humanoid", NameOverheadOptions.Humanoid, 0, y);
            AddCheckbox("Monster", NameOverheadOptions.Monster, 150, y);
            y += row;
            AddCheckbox("Own followers", NameOverheadOptions.OwnFollowers, 0, y);
            y += head;

            AddLabel("By noto", 75, y);
            y += head;

            AddCheckbox("Innocent", NameOverheadOptions.Innocent, 0, y);
            AddCheckbox("Ally", NameOverheadOptions.Ally, 150, y);
            y += row;
            AddCheckbox("Gray", NameOverheadOptions.Gray, 0, y);
            AddCheckbox("Criminal", NameOverheadOptions.Criminal, 150, y);
            y += row;
            AddCheckbox("Enemy", NameOverheadOptions.Enemy, 0, y);
            AddCheckbox("Murderer", NameOverheadOptions.Murderer, 150, y);
            y += row;
            Checkbox hideMannequinNameplate = new Checkbox(0x00D2, 0x00D3, "Mannequin (yellow)", 0xFF, 0xFFFF)
            {
                IsChecked = ProfileManager.CurrentProfile.HideInvulnerableMannequinNameplates,
                X = 150,
                Y = y
            };
            hideMannequinNameplate.ValueChanged += (_, _) =>
            {
                ProfileManager.CurrentProfile.HideInvulnerableMannequinNameplates =
                    hideMannequinNameplate.IsChecked;
            };
            Add(hideMannequinNameplate);
            AddCheckbox("Invuln", NameOverheadOptions.Invulnerable, 0, y);
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
