// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using SDL3;

namespace ClassicUO.Game.Managers
{
    [Flags]
    internal enum NameOverheadTypeAllowed
    {
        None = 0,
        Items = 1 << 0,
        Corpses = 1 << 1,
        Innocent = 1 << 2,
        Ally = 1 << 3,
        Gray = 1 << 4,
        Criminal = 1 << 5,
        Enemy = 1 << 6,
        Murderer = 1 << 7,
        Invulnerable = 1 << 8,
        AllMobiles = Innocent | Ally | Gray | Criminal | Enemy | Murderer | Invulnerable,
        All = Items | Corpses | AllMobiles
    }

    [Flags]
    internal enum NameOverheadOptions
    {
        None = 0,
        Containers = 1 << 0,
        Gold = 1 << 1,
        Stackable = 1 << 2,
        LockedDown = 1 << 3,
        Properties = 1 << 4,
        Nameslist = 1 << 5,
        MonsterCorpses = 1 << 6,
        HumanoidCorpses = 1 << 7,
        OwnCorpses = 1 << 8,
        Humanoid = 1 << 9,
        Monster = 1 << 10,
        OwnFollowers = 1 << 11,
        Innocent = 1 << 12,
        Ally = 1 << 13,
        Gray = 1 << 14,
        Criminal = 1 << 15,
        Enemy = 1 << 16,
        Murderer = 1 << 17,
        Invulnerable = 1 << 18,
        AllItems = Containers | Gold | Stackable | LockedDown | Properties | Nameslist,
        AllMobiles = Humanoid | Monster,
        MobilesAndCorpses = AllMobiles | MonsterCorpses | HumanoidCorpses,
        NameList = Nameslist,
        PropsList = Properties,
    }

    internal sealed class NameOverHeadManager
    {
        private NameOverHeadHandlerGump _gump;
        private readonly World _world;
        private SDL.SDL_Keycode _lastKeySym = SDL.SDL_Keycode.SDLK_UNKNOWN;
        private SDL.SDL_Keymod _lastKeyMod = SDL.SDL_Keymod.SDL_KMOD_NONE;

        private readonly List<NameOverheadOption> _options = new List<NameOverheadOption>();
        private readonly List<string> _compareNames = new List<string>();
        private readonly List<string> _propertyList = new List<string>();

        public NameOverHeadManager(World world)
        {
            _world = world;
        }

        public NameOverheadOptions ActiveOverheadOptions { get; private set; }

        public bool IsTemporarilyShowing { get; private set; }

        public string LastActiveNameOverheadOption
        {
            get
            {
                string value = ProfileManager.CurrentProfile.LastActiveNameOverheadOption ?? string.Empty;
                return Regex.Unescape(value);
            }
            private set
            {
                ProfileManager.CurrentProfile.LastActiveNameOverheadOption = Regex.Unescape(value ?? string.Empty);
            }
        }

        public bool IsHealthLinesToggled
        {
            get => ProfileManager.CurrentProfile.ShowHPLineInNOH;
            private set => ProfileManager.CurrentProfile.ShowHPLineInNOH = value;
        }

        public bool IsBackgroundToggled
        {
            get => ProfileManager.CurrentProfile.NameOverheadBackgroundToggled;
            private set => ProfileManager.CurrentProfile.NameOverheadBackgroundToggled = value;
        }

        public bool IsPinnedToggled
        {
            get => ProfileManager.CurrentProfile.NameOverheadPinnedToggled;
            private set => ProfileManager.CurrentProfile.NameOverheadPinnedToggled = value;
        }

        public bool IsPermaToggled
        {
            get => ProfileManager.CurrentProfile.NameOverheadToggled;
            private set => ProfileManager.CurrentProfile.NameOverheadToggled = value;
        }

        public bool IsShowing =>
            IsPermaToggled || IsTemporarilyShowing || (Keyboard.Ctrl && Keyboard.Shift);

        public NameOverheadTypeAllowed TypeAllowed
        {
            get => ProfileManager.CurrentProfile.NameOverheadTypeAllowed;
            set => ProfileManager.CurrentProfile.NameOverheadTypeAllowed = value;
        }

        public bool IsToggled
        {
            get => ProfileManager.CurrentProfile.NameOverheadToggled;
            set => ProfileManager.CurrentProfile.NameOverheadToggled = value;
        }

        public bool IsAllowed(Entity entity)
        {
            if (entity == null)
            {
                return false;
            }

            if (SerialHelper.IsItem(entity.Serial))
            {
                return HandleItemOverhead(entity);
            }

            if (SerialHelper.IsMobile(entity.Serial))
            {
                return HandleMobileOverhead(entity);
            }

            return false;
        }

        private bool HandleMobileOverhead(Entity serial)
        {
            var mobile = serial as Mobile;

            if (mobile == null)
            {
                return false;
            }

            if (ActiveOverheadOptions.HasFlag(NameOverheadOptions.Humanoid) && mobile.IsHuman)
            {
                return true;
            }

            if (ActiveOverheadOptions.HasFlag(NameOverheadOptions.Monster) && !mobile.IsHuman)
            {
                return true;
            }

            if (
                ActiveOverheadOptions.HasFlag(NameOverheadOptions.OwnFollowers)
                && mobile.IsRenamable
                && mobile.NotorietyFlag != NotorietyFlag.Invulnerable
                && mobile.NotorietyFlag != NotorietyFlag.Enemy
            )
            {
                return true;
            }

            if (ActiveOverheadOptions.HasFlag(NameOverheadOptions.Innocent) && mobile.NotorietyFlag == NotorietyFlag.Innocent)
            {
                return true;
            }

            if (ActiveOverheadOptions.HasFlag(NameOverheadOptions.Ally) && mobile.NotorietyFlag == NotorietyFlag.Ally)
            {
                return true;
            }

            if (ActiveOverheadOptions.HasFlag(NameOverheadOptions.Gray) && mobile.NotorietyFlag == NotorietyFlag.Gray)
            {
                return true;
            }

            if (ActiveOverheadOptions.HasFlag(NameOverheadOptions.Criminal) && mobile.NotorietyFlag == NotorietyFlag.Criminal)
            {
                return true;
            }

            if (ActiveOverheadOptions.HasFlag(NameOverheadOptions.Enemy) && mobile.NotorietyFlag == NotorietyFlag.Enemy)
            {
                return true;
            }

            if (ActiveOverheadOptions.HasFlag(NameOverheadOptions.Murderer) && mobile.NotorietyFlag == NotorietyFlag.Murderer)
            {
                return true;
            }

            if (ActiveOverheadOptions.HasFlag(NameOverheadOptions.Invulnerable) && mobile.NotorietyFlag == NotorietyFlag.Invulnerable)
            {
                return true;
            }

            return false;
        }

        private bool HandleItemOverhead(Entity serial)
        {
            var item = serial as Item;

            if (item == null)
            {
                return false;
            }

            if (SerialHelper.IsItem(serial) && ActiveOverheadOptions.HasFlag(NameOverheadOptions.AllItems))
            {
                return true;
            }

            if (item.IsCorpse)
            {
                return HandleCorpseOverhead(item);
            }

            if (item.ItemData.IsContainer && ActiveOverheadOptions.HasFlag(NameOverheadOptions.Containers))
            {
                return true;
            }

            if (item.IsCoin && ActiveOverheadOptions.HasFlag(NameOverheadOptions.Gold))
            {
                return true;
            }

            if (item.ItemData.IsStackable && ActiveOverheadOptions.HasFlag(NameOverheadOptions.Stackable))
            {
                return true;
            }

            if (item.IsLocked && ActiveOverheadOptions.HasFlag(NameOverheadOptions.LockedDown))
            {
                return true;
            }

            if (ActiveOverheadOptions.HasFlag(NameOverheadOptions.Properties))
            {
                string texto = string.Empty;

                if (
                    SerialHelper.IsValid(serial.Serial)
                    && _world.OPL.TryGetNameAndData(serial.Serial, out string name, out string data)
                )
                {
                    using (ValueStringBuilder sbHTML = new ValueStringBuilder())
                    {
                        using (ValueStringBuilder sb = new ValueStringBuilder())
                        {
                            bool hasStartColor = false;

                            if (!string.IsNullOrEmpty(name))
                            {
                                if (SerialHelper.IsItem(serial.Serial))
                                {
                                    sbHTML.Append(' ');
                                    hasStartColor = true;
                                }
                                else
                                {
                                    Mobile mob = _world.Mobiles.Get(serial.Serial);

                                    if (mob != null)
                                    {
                                        sbHTML.Append(Notoriety.GetHTMLHue(mob.NotorietyFlag));
                                        hasStartColor = true;
                                    }
                                }

                                sb.Append(name);
                                sbHTML.Append(name);

                                if (hasStartColor)
                                {
                                    sbHTML.Append(' ');
                                }
                            }

                            if (!string.IsNullOrEmpty(data))
                            {
                                sb.Append('\n');
                                sb.Append(data);
                                sbHTML.Append('\n');
                                sbHTML.Append(data);
                            }

                            texto = sbHTML.ToString();
                        }
                    }
                }

                if (texto.Length > 0)
                {
                    for (int x = 0; x < _propertyList.Count; x++)
                    {
                        if (texto.Contains(_propertyList[x], StringComparison.Ordinal))
                        {
                            return true;
                        }
                    }
                }
            }

            if (ActiveOverheadOptions.HasFlag(NameOverheadOptions.Nameslist))
            {
                for (int x = 0; x < _compareNames.Count; x++)
                {
                    if (item.Name != null && item.Name.Contains(_compareNames[x], StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsHumanoidCorpseBody(Item item)
        {
            ushort body = item.GetGraphicForAnimation();

            return body >= 0x0190 && body <= 0x0193
                || body >= 0x00B7 && body <= 0x00BA
                || body >= 0x025D && body <= 0x0260
                || body == 0x029A
                || body == 0x029B
                || body == 0x02B6
                || body == 0x02B7
                || body == 0x03DB
                || body == 0x03DF
                || body == 0x03E2
                || body == 0x02E8
                || body == 0x02E9
                || body == 0x04E5;
        }

        private bool HandleCorpseOverhead(Item item)
        {
            bool isHumanCorpse = IsHumanoidCorpseBody(item);

            if (isHumanCorpse && ActiveOverheadOptions.HasFlag(NameOverheadOptions.HumanoidCorpses))
            {
                return true;
            }

            if (!isHumanCorpse && ActiveOverheadOptions.HasFlag(NameOverheadOptions.MonsterCorpses))
            {
                return true;
            }

            return false;
        }

        public void Open()
        {
            if (_gump == null || _gump.IsDisposed)
            {
                _gump = new NameOverHeadHandlerGump(_world);
                UIManager.Add(_gump);
            }

            _gump.IsEnabled = true;
            _gump.IsVisible = true;
        }

        public void SetMenuVisible(bool visible)
        {
            if (_gump != null && !_gump.IsDisposed)
            {
                if (!visible && IsPinnedToggled)
                    return;

                _gump.IsVisible = visible;
            }
        }

        public void Close()
        {
            if (_gump == null)
            {
                _gump = new NameOverHeadHandlerGump(_world);
                UIManager.Add(_gump);
            }

            if (IsPinnedToggled)
                return;

            _gump.IsEnabled = false;
            _gump.IsVisible = false;
        }

        public void ToggleOverheads()
        {
            SetOverheadToggled(!IsPermaToggled);
        }

        public void SetHealthLinesToggled(bool toggled)
        {
            if (IsHealthLinesToggled == toggled)
            {
                return;
            }

            IsHealthLinesToggled = toggled;
            _gump?.UpdateCheckboxes();
        }

        public void SetBackgroundToggled(bool toggled)
        {
            if (IsBackgroundToggled == toggled)
            {
                return;
            }

            IsBackgroundToggled = toggled;
            _gump?.UpdateCheckboxes();
        }

        public void SetPinnedToggled(bool toggled)
        {
            if (IsPinnedToggled == toggled)
            {
                return;
            }

            IsPinnedToggled = toggled;
            _gump?.UpdateCheckboxes();

            // If unpinned while gump is hidden, keep it visible so user can see it
            if (!toggled && _gump != null && !_gump.IsDisposed && !_gump.IsVisible)
            {
                _gump.IsEnabled = true;
                _gump.IsVisible = true;
            }
        }

        public void SetOverheadToggled(bool toggled)
        {
            if (IsPermaToggled == toggled)
            {
                return;
            }

            IsPermaToggled = toggled;
            _gump?.UpdateCheckboxes();
        }

        public void Load()
        {
            string path = Path.Combine(ProfileManager.ProfilePath, "nameoverhead.xml");

            if (!File.Exists(path))
            {
                Log.Trace("No nameoverhead.xml file. Creating a default file.");

                _options.Clear();
                CreateDefaultEntries();
                Save();
                SetActiveOption(_options[0]);
                LoadTxtLists();
                return;
            }

            _options.Clear();
            var doc = new XmlDocument();

            try
            {
                doc.Load(path);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return;
            }

            XmlElement root = doc["nameoverhead"];

            if (root != null)
            {
                bool matched = false;

                foreach (XmlElement xml in root.GetElementsByTagName("nameoverheadoption"))
                {
                    var option = new NameOverheadOption(xml.GetAttribute("name"));
                    option.Load(xml);
                    _options.Add(option);

                    if (option.Name == LastActiveNameOverheadOption)
                    {
                        SetActiveOption(option);
                        matched = true;
                    }
                }

                if (!matched && _options.Count > 0)
                {
                    SetActiveOption(_options[0]);
                }
            }

            LoadTxtLists();
        }

        private void LoadTxtLists()
        {
            string path = Path.Combine(ProfileManager.ProfilePath, "OverheadNamesList.txt");

            if (!File.Exists(path))
            {
                Log.Trace("No OverheadNamesList.txt. Creating a default file.");

                _compareNames.Clear();
                using (StreamWriter sw = File.CreateText(path))
                {
                    sw.WriteLine("Bag");
                    sw.WriteLine("bag");
                }
            }

            try
            {
                _compareNames.Clear();
                using (StreamReader reader = new StreamReader(path))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        _compareNames.Add(line);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }

            path = Path.Combine(ProfileManager.ProfilePath, "OverheadPropertiesList.txt");

            if (!File.Exists(path))
            {
                Log.Trace("No OverheadPropertiesList.txt. Creating a default file.");
                _propertyList.Clear();
                using (StreamWriter sw = File.CreateText(path))
                {
                    sw.WriteLine("Artifact");
                    sw.WriteLine("artifact");
                }
            }

            try
            {
                _propertyList.Clear();
                using (StreamReader reader = new StreamReader(path))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        _propertyList.Add(line);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }

        public void Save()
        {
            string path = Path.Combine(ProfileManager.ProfilePath, "nameoverhead.xml");

            using (var xml = new XmlTextWriter(path, Encoding.UTF8))
            {
                xml.Formatting = Formatting.Indented;
                xml.IndentChar = '\t';
                xml.Indentation = 1;

                xml.WriteStartDocument(true);
                xml.WriteStartElement("nameoverhead");

                foreach (NameOverheadOption option in _options)
                {
                    option.Save(xml);
                }

                xml.WriteEndElement();
                xml.WriteEndDocument();
            }
        }

        private void CreateDefaultEntries()
        {
            _options.AddRange(
                new[]
                {
                    new NameOverheadOption("All", int.MaxValue),
                    new NameOverheadOption("Mobiles only", (int)NameOverheadOptions.AllMobiles),
                    new NameOverheadOption("Items only", (int)NameOverheadOptions.AllItems),
                    new NameOverheadOption("Mobiles & Corpses only", (int)NameOverheadOptions.MobilesAndCorpses),
                    new NameOverheadOption("Names list", (int)NameOverheadOptions.NameList),
                    new NameOverheadOption("Properties List", (int)NameOverheadOptions.PropsList),
                }
            );
        }

        public NameOverheadOption FindOption(string name)
        {
            return _options.Find(o => o.Name == name);
        }

        public void AddOption(NameOverheadOption option)
        {
            _options.Add(option);
            _gump?.RedrawOverheadOptions();
        }

        public void RemoveOption(NameOverheadOption option)
        {
            _options.Remove(option);
            _gump?.RedrawOverheadOptions();
        }

        public NameOverheadOption FindOptionByHotkey(SDL.SDL_Keycode key, bool alt, bool ctrl, bool shift)
        {
            return _options.FirstOrDefault(o =>
                o.Key == key && o.Alt == alt && o.Ctrl == ctrl && o.Shift == shift
            );
        }

        public IReadOnlyList<NameOverheadOption> GetAllOptions() => _options;

        public void RegisterKeyDown(SDL.SDL_KeyboardEvent e)
        {
            if (_lastKeySym == (SDL.SDL_Keycode)e.key && _lastKeyMod == e.mod)
            {
                return;
            }

            _lastKeySym = (SDL.SDL_Keycode)e.key;
            _lastKeyMod = e.mod;

            bool shift = (e.mod & SDL.SDL_Keymod.SDL_KMOD_SHIFT) != SDL.SDL_Keymod.SDL_KMOD_NONE;
            bool alt = (e.mod & SDL.SDL_Keymod.SDL_KMOD_ALT) != SDL.SDL_Keymod.SDL_KMOD_NONE;
            bool ctrl = (e.mod & SDL.SDL_Keymod.SDL_KMOD_CTRL) != SDL.SDL_Keymod.SDL_KMOD_NONE;

            NameOverheadOption option = FindOptionByHotkey(_lastKeySym, alt, ctrl, shift);

            if (option == null)
            {
                return;
            }

            SetActiveOption(option);

            IsTemporarilyShowing = true;
        }

        public void RegisterKeyUp(SDL.SDL_KeyboardEvent e)
        {
            if ((SDL.SDL_Keycode)e.key != _lastKeySym)
            {
                return;
            }

            _lastKeySym = SDL.SDL_Keycode.SDLK_UNKNOWN;

            IsTemporarilyShowing = false;
        }

        public void SetActiveOption(NameOverheadOption option)
        {
            if (option == null)
            {
                return;
            }

            ActiveOverheadOptions = (NameOverheadOptions)option.NameOverheadOptionFlags;
            LastActiveNameOverheadOption = option.Name;
            _gump?.UpdateCheckboxes();
        }

        public void SyncActiveOverheadFlagsIfCurrent(NameOverheadOption option)
        {
            if (option == null)
            {
                return;
            }

            if (string.Equals(LastActiveNameOverheadOption, option.Name, StringComparison.Ordinal))
            {
                ActiveOverheadOptions = (NameOverheadOptions)option.NameOverheadOptionFlags;
            }
        }
    }

    internal class NameOverheadOption
    {
        public NameOverheadOption(string name, SDL.SDL_Keycode key, bool alt, bool ctrl, bool shift, int optionflagscode)
            : this(name)
        {
            Key = key;
            Alt = alt;
            Ctrl = ctrl;
            Shift = shift;
            NameOverheadOptionFlags = optionflagscode;
        }

        public NameOverheadOption(string name)
        {
            Name = name;
        }

        public NameOverheadOption(string name, int optionflagcode)
        {
            Name = name;
            NameOverheadOptionFlags = optionflagcode;
        }

        public string Name { get; }

        public SDL.SDL_Keycode Key { get; set; }

        public bool Alt { get; set; }

        public bool Ctrl { get; set; }

        public bool Shift { get; set; }

        public int NameOverheadOptionFlags { get; set; }

        public bool Equals(NameOverheadOption other)
        {
            if (other == null)
            {
                return false;
            }

            return Key == other.Key
                && Alt == other.Alt
                && Ctrl == other.Ctrl
                && Shift == other.Shift
                && Name == other.Name;
        }

        public void Save(XmlTextWriter writer)
        {
            writer.WriteStartElement("nameoverheadoption");
            writer.WriteAttributeString("name", Name);
            writer.WriteAttributeString("key", ((int)Key).ToString());
            writer.WriteAttributeString("alt", Alt.ToString());
            writer.WriteAttributeString("ctrl", Ctrl.ToString());
            writer.WriteAttributeString("shift", Shift.ToString());
            writer.WriteAttributeString("optionflagscode", NameOverheadOptionFlags.ToString());
            writer.WriteEndElement();
        }

        public void Load(XmlElement xml)
        {
            if (xml == null)
            {
                return;
            }

            Key = (SDL.SDL_Keycode)int.Parse(xml.GetAttribute("key"));
            Alt = bool.Parse(xml.GetAttribute("alt"));
            Ctrl = bool.Parse(xml.GetAttribute("ctrl"));
            Shift = bool.Parse(xml.GetAttribute("shift"));
            NameOverheadOptionFlags = int.Parse(xml.GetAttribute("optionflagscode"));
        }
    }
}
