// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Dust765;
using ClassicUO.Game.UI.Controls;

namespace ClassicUO.Game.UI.Gumps
{
    internal partial class OptionsGump
    {
        private Checkbox _dust765AvoidObstacles;
        private Checkbox _dust765UccBuffbar;
        private Checkbox _dust765UccSwing;
        private Checkbox _dust765UccLocked;
        private Checkbox _dust765NamePlateHealthBar;
        private Checkbox _dust765UseOldHealthBars;
        private Checkbox _dust765MultiUnderlinesParty;
        private Checkbox _dust765MultiUnderlinesBigBars;
        private Checkbox _dust765BandageGump, _dust765BandageGumpUpDown;
        private Checkbox _dust765OnCastingGump, _dust765OnCastingGumpHidden, _dust765OnCastingUnderPlayerBar;
        private Checkbox _dust765TransparentHouses;
        private Checkbox _dust765InvisibleHouses;
        private Checkbox _dust765ShowDeathOnWorldmap;
        private Checkbox _dust765GridContainer;
        private HSliderBar _dust765NamePlateOpacity;
        private HSliderBar _dust765MultiUnderlinesTransparency;
        private HSliderBar _dust765TransparentHousesZ;
        private HSliderBar _dust765TransparentHousesTransparency;
        private HSliderBar _dust765InvisibleHousesZ;
        private HSliderBar _dust765DontRemoveHouseBelowZ;
        private Checkbox _dust765ColorStealth;
        private Combobox _dust765StealthNeonType;
        private ClickableColorBox _dust765StealthHueBox;
        private Checkbox _dust765PreviewFields;
        private Checkbox _dust765BlockWoS,
            _dust765BlockWoSFelOnly,
            _dust765BlockWoSArtForceAoS,
            _dust765BlockEnergyF,
            _dust765BlockEnergyFFelOnly,
            _dust765BlockEnergyFArtForceAoS;
        private InputField _dust765BlockWoSArt, _dust765BlockEnergyFArt;
        private Combobox _dust765HighlightLastTargetType;
        private Combobox _dust765HighlightLastTargetPoison;
        private Combobox _dust765HighlightLastTargetPara;
        private ClickableColorBox _dust765HighlightLastTargetHue;
        private ClickableColorBox _dust765HighlightLastTargetPoisonHue;
        private ClickableColorBox _dust765HighlightLastTargetParaHue;
        private Checkbox _dust765LTHighlightRangeOnCast, _dust765LTHighlightRangeOnActivated;
        private HSliderBar _dust765LTHighlightRangeOnCastRange, _dust765LTHighlightRangeOnActivatedRange;
        private ClickableColorBox _dust765LTHighlightRangeOnCastHue, _dust765LTHighlightRangeOnActivatedHue;
        private Checkbox _dust765ShowHPLineInNOH;
        private Checkbox _dust765NameOverheadBgToggled;
        private Checkbox _dust765NamePlateHideAtFullHealth;
        private HSliderBar _dust765NamePlateBgOpacity;

        internal void BuildDust765()
        {
            const int PAGE = 13;

            ScrollArea rightArea = new ScrollArea
            (
                190,
                20,
                WIDTH - 210,
                420,
                true
            );

            int startX = 5;
            int startY = 5;

            DataBox box = new DataBox(startX, startY, rightArea.Width - 15, 1);
            box.WantUpdateSize = true;
            rightArea.Add(box);

            SettingsSection sectionMove = AddSettingsSection(box, "Movement");

            sectionMove.Add
            (
                _dust765AvoidObstacles = AddCheckBox
                (
                    null,
                    "Avoid obstacles",
                    _currentProfile.AvoidObstacles,
                    startX,
                    startY
                )
            );

            SettingsSection sectionCombatUi = AddSettingsSection(box, "UOCC swing");
            sectionCombatUi.Y = sectionMove.Bounds.Bottom + 40;

            sectionCombatUi.Add
            (
                _dust765UccBuffbar = AddCheckBox
                (
                    null,
                    "Show UOCC gump",
                    _currentProfile.UOClassicCombatBuffbar,
                    startX,
                    startY
                )
            );

            sectionCombatUi.Add
            (
                _dust765UccSwing = AddCheckBox
                (
                    null,
                    "Show swing timer",
                    _currentProfile.UOClassicCombatBuffbar_SwingEnabled,
                    startX,
                    startY
                )
            );

            sectionCombatUi.Add
            (
                _dust765UccLocked = AddCheckBox
                (
                    null,
                    "Lock combat bar",
                    _currentProfile.UOClassicCombatBuffbar_Locked,
                    startX,
                    startY
                )
            );

            SettingsSection sectionBars = AddSettingsSection(box, "HP bars & names");
            sectionBars.Y = sectionCombatUi.Bounds.Bottom + 40;

            sectionBars.Add
            (
                _dust765NamePlateHealthBar = AddCheckBox
                (
                    null,
                    "HP/Mana/Stam under your name",
                    _currentProfile.NamePlateHealthBar,
                    startX,
                    startY
                )
            );

            sectionBars.Add(AddLabel(null, "Bar opacity (0-100)", startX, startY));
            sectionBars.AddRight
            (
                _dust765NamePlateOpacity = AddHSlider
                (
                    null,
                    0,
                    100,
                    Math.Clamp(_currentProfile.NamePlateHealthBarOpacity, (byte)0, (byte)100),
                    startX,
                    startY,
                    200
                )
            );

            sectionBars.Add
            (
                _dust765UseOldHealthBars = AddCheckBox
                (
                    null,
                    "Thin HP lines under mobiles",
                    _currentProfile.UseOldHealthBars,
                    startX,
                    startY
                )
            );

            sectionBars.Add(AddLabel(null, "Floating names", startX, startY));

            sectionBars.Add
            (
                _dust765ShowHPLineInNOH = AddCheckBox
                (
                    null,
                    "HP line on names",
                    _currentProfile.ShowHPLineInNOH,
                    startX,
                    startY
                )
            );
            sectionBars.Add(AddLabel(null, string.Empty, startX, startY));

            sectionBars.Add
            (
                _dust765NameOverheadBgToggled = AddCheckBox
                (
                    null,
                    "Name bg on hover only",
                    _currentProfile.NameOverheadBackgroundToggled,
                    startX,
                    startY
                )
            );
            sectionBars.Add(AddLabel(null, string.Empty, startX, startY));

            sectionBars.Add
            (
                _dust765NamePlateHideAtFullHealth = AddCheckBox
                (
                    null,
                    "Hide name at full HP (war)",
                    _currentProfile.NamePlateHideAtFullHealth,
                    startX,
                    startY
                )
            );

            sectionBars.Add(AddLabel(null, "Name bg opacity (0-100)", startX, startY));
            sectionBars.AddRight
            (
                _dust765NamePlateBgOpacity = AddHSlider
                (
                    null,
                    0,
                    100,
                    Math.Clamp(_currentProfile.NamePlateOpacity, (byte)0, (byte)100),
                    startX,
                    startY,
                    200
                )
            );

            sectionBars.Add
            (
                _dust765MultiUnderlinesParty = AddCheckBox
                (
                    null,
                    "Stam underlines (you/party)",
                    _currentProfile.MultipleUnderlinesSelfParty,
                    startX,
                    startY
                )
            );

            sectionBars.Add
            (
                _dust765MultiUnderlinesBigBars = AddCheckBox
                (
                    null,
                    "Bigger underlines",
                    _currentProfile.MultipleUnderlinesSelfPartyBigBars,
                    startX,
                    startY
                )
            );

            sectionBars.Add(AddLabel(null, "Underline fade (1-10)", startX, startY));
            sectionBars.AddRight
            (
                _dust765MultiUnderlinesTransparency = AddHSlider
                (
                    null,
                    1,
                    10,
                    Math.Clamp(_currentProfile.MultipleUnderlinesSelfPartyTransparency, 1, 10),
                    startX,
                    startY,
                    200
                )
            );

            SettingsSection sectionBandage = AddSettingsSection(box, "Bandage");
            sectionBandage.Y = sectionBars.Bounds.Bottom + 40;

            sectionBandage.Add
            (
                _dust765BandageGump = AddCheckBox
                (
                    null,
                    "Bandage timer gump",
                    _currentProfile.BandageGump,
                    startX,
                    startY
                )
            );

            sectionBandage.Add
            (
                _dust765BandageGumpUpDown = AddCheckBox
                (
                    null,
                    "Count up",
                    _currentProfile.BandageGumpUpDownToggle,
                    startX,
                    startY
                )
            );

            SettingsSection sectionCasting = AddSettingsSection(box, "Casting");
            sectionCasting.Y = sectionBandage.Bounds.Bottom + 40;

            sectionCasting.Add
            (
                _dust765OnCastingGump = AddCheckBox
                (
                    null,
                    "Casting timer gump",
                    _currentProfile.OnCastingGump,
                    startX,
                    startY
                )
            );

            sectionCasting.Add
            (
                _dust765OnCastingGumpHidden = AddCheckBox
                (
                    null,
                    "Hidden (track only)",
                    _currentProfile.OnCastingGump_hidden,
                    startX,
                    startY
                )
            );

            sectionCasting.Add
            (
                _dust765OnCastingUnderPlayerBar = AddCheckBox
                (
                    null,
                    "Below status bar",
                    _currentProfile.OnCastingUnderPlayerBar,
                    startX,
                    startY
                )
            );

            SettingsSection sectionHouse = AddSettingsSection(box, "Houses & map");
            sectionHouse.Y = sectionCasting.Bounds.Bottom + 40;

            sectionHouse.Add
            (
                _dust765TransparentHouses = AddCheckBox
                (
                    null,
                    "Transparent roofs (Z)",
                    _currentProfile.TransparentHousesEnabled,
                    startX,
                    startY
                )
            );

            sectionHouse.PushIndent();
            sectionHouse.Add(AddLabel(null, "Transparent Z range", startX, startY));
            sectionHouse.AddRight
            (
                _dust765TransparentHousesZ = AddHSlider
                (
                    null,
                    1,
                    100,
                    Math.Clamp(_currentProfile.TransparentHousesZ, 1, 100),
                    startX,
                    startY,
                    200
                )
            );
            sectionHouse.Add(AddLabel(null, "See-through amount (1-9)", startX, startY));
            sectionHouse.AddRight
            (
                _dust765TransparentHousesTransparency = AddHSlider
                (
                    null,
                    1,
                    9,
                    Math.Clamp(_currentProfile.TransparentHousesTransparency, 1, 9),
                    startX,
                    startY,
                    200
                )
            );
            sectionHouse.PopIndent();

            sectionHouse.Add
            (
                _dust765InvisibleHouses = AddCheckBox
                (
                    null,
                    "Hide statics above (Z)",
                    _currentProfile.InvisibleHousesEnabled,
                    startX,
                    startY
                )
            );

            sectionHouse.PushIndent();
            sectionHouse.Add(AddLabel(null, "Invisible Z range", 0, 0));
            sectionHouse.Add
            (
                _dust765InvisibleHousesZ = AddHSlider
                (
                    null,
                    1,
                    100,
                    Math.Clamp(_currentProfile.InvisibleHousesZ, 1, 100),
                    0,
                    0,
                    200
                )
            );
            sectionHouse.Add
            (
                AddLabel
                (
                    null,
                    "Min floor Z gap (1-100)",
                    0,
                    0
                )
            );
            sectionHouse.Add
            (
                _dust765DontRemoveHouseBelowZ = AddHSlider
                (
                    null,
                    1,
                    100,
                    Math.Clamp(_currentProfile.DontRemoveHouseBelowZ, 1, 100),
                    0,
                    0,
                    200
                )
            );
            sectionHouse.PopIndent();

            sectionHouse.Add
            (
                _dust765ShowDeathOnWorldmap = AddCheckBox
                (
                    null,
                    "Death on map (~5 min)",
                    _currentProfile.ShowDeathOnWorldmap,
                    startX,
                    startY
                )
            );

            SettingsSection sectionGrid = AddSettingsSection(box, "Grid");
            sectionGrid.Y = sectionHouse.Bounds.Bottom + 40;

            sectionGrid.Add
            (
                _dust765GridContainer = AddCheckBox
                (
                    null,
                    "Grid containers",
                    _currentProfile.GridContainerEnabled,
                    startX,
                    startY
                )
            );

            SettingsSection sectionArt = AddSettingsSection(box, "Stealth");
            sectionArt.Y = sectionGrid.Bounds.Bottom + 40;

            sectionArt.Add
            (
                _dust765ColorStealth = AddCheckBox
                (
                    null,
                    "Tint stealth sprite",
                    _currentProfile.ColorStealth,
                    startX,
                    startY
                )
            );

            sectionArt.Add(AddLabel(null, "Neon style", startX, startY));
            sectionArt.AddRight
            (
                _dust765StealthNeonType = AddCombobox
                (
                    null,
                    new[] { "Off", "White", "Pink", "Ice", "Fire" },
                    _currentProfile.StealthNeonType,
                    startX,
                    startY,
                    150
                ),
                2
            );
            sectionArt.Add(AddLabel(null, "Stealth hue", startX, startY));
            sectionArt.AddRight
            (
                _dust765StealthHueBox = AddColorBox
                (
                    null,
                    startX,
                    startY,
                    _currentProfile.StealthHue,
                    string.Empty
                ),
                2
            );

            SettingsSection sectionVisual = AddSettingsSection(box, "Fields & target");
            sectionVisual.Y = sectionArt.Bounds.Bottom + 40;

            sectionVisual.Add
            (
                _dust765PreviewFields = AddCheckBox
                (
                    null,
                    "Preview field tiles",
                    _currentProfile.PreviewFields,
                    startX,
                    startY
                )
            );

            sectionVisual.Add(AddLabel(null, "Last target", startX, startY));
            sectionVisual.AddRight
            (
                _dust765HighlightLastTargetType = AddCombobox
                (
                    null,
                    new[] { "Off", "White", "Pink", "Ice", "Fire", "Custom" },
                    _currentProfile.HighlightLastTargetType,
                    startX,
                    startY,
                    150
                ),
                2
            );
            sectionVisual.Add(AddLabel(null, "Custom hue", startX, startY));
            sectionVisual.AddRight
            (
                _dust765HighlightLastTargetHue = AddColorBox
                (
                    null,
                    startX,
                    startY,
                    _currentProfile.HighlightLastTargetTypeHue,
                    string.Empty
                ),
                2
            );

            sectionVisual.Add(AddLabel(null, "Target (poison)", startX, startY));
            sectionVisual.AddRight
            (
                _dust765HighlightLastTargetPoison = AddCombobox
                (
                    null,
                    new[] { "Off", "White", "Pink", "Ice", "Fire", "Custom", "Special (green)" },
                    _currentProfile.HighlightLastTargetTypePoison,
                    startX,
                    startY,
                    180
                ),
                2
            );
            sectionVisual.Add(AddLabel(null, "Poison hue", startX, startY));
            sectionVisual.AddRight
            (
                _dust765HighlightLastTargetPoisonHue = AddColorBox
                (
                    null,
                    startX,
                    startY,
                    _currentProfile.HighlightLastTargetTypePoisonHue,
                    string.Empty
                ),
                2
            );

            sectionVisual.Add(AddLabel(null, "Target (para)", startX, startY));
            sectionVisual.AddRight
            (
                _dust765HighlightLastTargetPara = AddCombobox
                (
                    null,
                    new[] { "Off", "White", "Pink", "Ice", "Fire", "Custom", "Special (purple)" },
                    _currentProfile.HighlightLastTargetTypePara,
                    startX,
                    startY,
                    180
                ),
                2
            );
            sectionVisual.Add(AddLabel(null, "Para hue", startX, startY));
            sectionVisual.AddRight
            (
                _dust765HighlightLastTargetParaHue = AddColorBox
                (
                    null,
                    startX,
                    startY,
                    _currentProfile.HighlightLastTargetTypeParaHue,
                    string.Empty
                ),
                2
            );


            sectionVisual.Add
            (
                _dust765LTHighlightRangeOnCast = AddCheckBox
                (
                    null,
                    "Highlight terrain ring at range while casting",
                    _currentProfile.LTHighlightRangeOnCast,
                    startX,
                    startY
                )
            );
            sectionVisual.Add(AddLabel(null, "Cast range (tiles)", startX, startY));
            sectionVisual.AddRight
            (
                _dust765LTHighlightRangeOnCastRange = AddHSlider
                (
                    null,
                    1,
                    18,
                    Math.Clamp(_currentProfile.LTHighlightRangeOnCastRange, 1, 18),
                    startX,
                    startY,
                    150
                )
            );
            sectionVisual.Add(AddLabel(null, "Cast range hue", startX, startY));
            sectionVisual.AddRight
            (
                _dust765LTHighlightRangeOnCastHue = AddColorBox
                (
                    null,
                    startX,
                    startY,
                    _currentProfile.LTHighlightRangeOnCastHue,
                    string.Empty
                ),
                2
            );

            sectionVisual.Add
            (
                _dust765LTHighlightRangeOnActivated = AddCheckBox
                (
                    null,
                    "Highlight terrain ring at range (always when enabled)",
                    _currentProfile.LTHighlightRangeOnActivated,
                    startX,
                    startY
                )
            );
            sectionVisual.Add(AddLabel(null, "Terrain ring distance (tiles)", startX, startY));
            sectionVisual.AddRight
            (
                _dust765LTHighlightRangeOnActivatedRange = AddHSlider
                (
                    null,
                    1,
                    18,
                    Math.Clamp(_currentProfile.LTHighlightRangeOnActivatedRange, 1, 18),
                    startX,
                    startY,
                    150
                )
            );
            sectionVisual.Add(AddLabel(null, "Terrain ring hue", startX, startY));
            sectionVisual.AddRight
            (
                _dust765LTHighlightRangeOnActivatedHue = AddColorBox
                (
                    null,
                    startX,
                    startY,
                    _currentProfile.LTHighlightRangeOnActivatedHue,
                    string.Empty
                ),
                2
            );
            SettingsSection sectionFieldBlock = AddSettingsSection(
                box,
                "WoS & Energy Field"
            );
            sectionFieldBlock.Y = sectionVisual.Bounds.Bottom + 40;

            sectionFieldBlock.Add
            (
                _dust765BlockWoS = AddCheckBox
                (
                    null,
                    "Block WoS",
                    _currentProfile.BlockWoS,
                    startX,
                    startY
                )
            );

            sectionFieldBlock.Add
            (
                _dust765BlockWoSFelOnly = AddCheckBox
                (
                    null,
                    "WoS Fel only",
                    _currentProfile.BlockWoSFelOnly,
                    startX,
                    startY
                )
            );

            sectionFieldBlock.Add(AddLabel(null, "WoS graphic id", 0, 0));
            sectionFieldBlock.AddRight
            (
                _dust765BlockWoSArt = new InputField
                (
                    0x0BB8,
                    FONT,
                    HUE_FONT,
                    true,
                    50,
                    TEXTBOX_HEIGHT,
                    80,
                    50000
                )
                {
                    NumbersOnly = true
                },
                2
            );
            _dust765BlockWoSArt.SetText(_currentProfile.BlockWoSArt.ToString());

            sectionFieldBlock.Add
            (
                _dust765BlockWoSArtForceAoS = AddCheckBox
                (
                    null,
                    "Remap AoS WoS (945)",
                    _currentProfile.BlockWoSArtForceAoS,
                    startX,
                    startY
                )
            );

            sectionFieldBlock.Add
            (
                _dust765BlockEnergyF = AddCheckBox
                (
                    null,
                    "Block Energy Field",
                    _currentProfile.BlockEnergyF,
                    startX,
                    startY
                )
            );

            sectionFieldBlock.Add
            (
                _dust765BlockEnergyFFelOnly = AddCheckBox
                (
                    null,
                    "EF Fel only",
                    _currentProfile.BlockEnergyFFelOnly,
                    startX,
                    startY
                )
            );

            sectionFieldBlock.Add(AddLabel(null, "EF graphic id", 0, 0));
            sectionFieldBlock.AddRight
            (
                _dust765BlockEnergyFArt = new InputField
                (
                    0x0BB8,
                    FONT,
                    HUE_FONT,
                    true,
                    50,
                    TEXTBOX_HEIGHT,
                    80,
                    50000
                )
                {
                    NumbersOnly = true
                },
                2
            );
            _dust765BlockEnergyFArt.SetText(_currentProfile.BlockEnergyFArt.ToString());

            sectionFieldBlock.Add
            (
                _dust765BlockEnergyFArtForceAoS = AddCheckBox
                (
                    null,
                    "Remap EF arts (293)",
                    _currentProfile.BlockEnergyFArtForceAoS,
                    startX,
                    startY
                )
            );

            Add(rightArea, PAGE);
        }

        internal void ApplyDust765Profile()
        {
            _currentProfile.AvoidObstacles = _dust765AvoidObstacles.IsChecked;
            _currentProfile.UOClassicCombatBuffbar = _dust765UccBuffbar.IsChecked;
            _currentProfile.UOClassicCombatBuffbar_SwingEnabled = _dust765UccSwing.IsChecked;
            _currentProfile.UOClassicCombatBuffbar_Locked = _dust765UccLocked.IsChecked;
            _currentProfile.NamePlateHealthBar = _dust765NamePlateHealthBar.IsChecked;
            _currentProfile.NamePlateHealthBarOpacity = (byte)Math.Clamp(_dust765NamePlateOpacity.Value, 0, 100);
            _currentProfile.UseOldHealthBars = _dust765UseOldHealthBars.IsChecked;
            _currentProfile.MultipleUnderlinesSelfParty = _dust765MultiUnderlinesParty.IsChecked;
            _currentProfile.MultipleUnderlinesSelfPartyBigBars = _dust765MultiUnderlinesBigBars.IsChecked;
            _currentProfile.MultipleUnderlinesSelfPartyTransparency = Math.Clamp(_dust765MultiUnderlinesTransparency.Value, 1, 10);
            UOClassicCombatSwingGump.RefreshOpenGump(World);
            _currentProfile.BandageGump = _dust765BandageGump.IsChecked;
            _currentProfile.BandageGumpUpDownToggle = _dust765BandageGumpUpDown.IsChecked;
            _currentProfile.OnCastingGump = _dust765OnCastingGump.IsChecked;
            _currentProfile.OnCastingGump_hidden = _dust765OnCastingGumpHidden.IsChecked;
            _currentProfile.OnCastingUnderPlayerBar = _dust765OnCastingUnderPlayerBar.IsChecked;
            _currentProfile.TransparentHousesEnabled = _dust765TransparentHouses.IsChecked;
            _currentProfile.TransparentHousesZ = Math.Clamp(_dust765TransparentHousesZ.Value, 1, 100);
            _currentProfile.TransparentHousesTransparency = Math.Clamp(_dust765TransparentHousesTransparency.Value, 1, 9);
            _currentProfile.InvisibleHousesEnabled = _dust765InvisibleHouses.IsChecked;
            _currentProfile.InvisibleHousesZ = Math.Clamp(_dust765InvisibleHousesZ.Value, 1, 100);
            _currentProfile.DontRemoveHouseBelowZ = Math.Clamp(_dust765DontRemoveHouseBelowZ.Value, 1, 100);
            _currentProfile.ShowDeathOnWorldmap = _dust765ShowDeathOnWorldmap.IsChecked;
            _currentProfile.GridContainerEnabled = _dust765GridContainer.IsChecked;

            // Art / Hue Changes
            _currentProfile.ColorStealth = _dust765ColorStealth.IsChecked;
            _currentProfile.StealthNeonType = _dust765StealthNeonType.SelectedIndex;
            _currentProfile.StealthHue = _dust765StealthHueBox.Hue;

            // Visual Helpers
            _currentProfile.PreviewFields = _dust765PreviewFields.IsChecked;

            _currentProfile.BlockWoSArtForceAoS = _dust765BlockWoSArtForceAoS.IsChecked;
            if (uint.TryParse(_dust765BlockWoSArt.Text, out uint wosArtId))
            {
                _currentProfile.BlockWoSArt = wosArtId;
            }

            ushort wosGraphic = (ushort)Math.Min(_currentProfile.BlockWoSArt, ushort.MaxValue);

            if (_currentProfile.BlockWoS != _dust765BlockWoS.IsChecked)
            {
                if (_dust765BlockWoS.IsChecked)
                {
                    FieldBlockTileData.SetImpassable(wosGraphic, true);
                }
                else
                {
                    FieldBlockTileData.SetImpassable(wosGraphic, false);
                }

                _currentProfile.BlockWoS = _dust765BlockWoS.IsChecked;
            }

            if (_currentProfile.BlockWoSFelOnly != _dust765BlockWoSFelOnly.IsChecked)
            {
                if (_dust765BlockWoSFelOnly.IsChecked && World.MapIndex == 0)
                {
                    FieldBlockTileData.SetImpassable(wosGraphic, true);
                }
                else
                {
                    FieldBlockTileData.SetImpassable(wosGraphic, false);
                }

                _currentProfile.BlockWoSFelOnly = _dust765BlockWoSFelOnly.IsChecked;
            }

            _currentProfile.BlockEnergyFArtForceAoS = _dust765BlockEnergyFArtForceAoS.IsChecked;
            if (uint.TryParse(_dust765BlockEnergyFArt.Text, out uint eFieldArtId))
            {
                _currentProfile.BlockEnergyFArt = eFieldArtId;
            }

            ushort eFieldGraphic = (ushort)Math.Min(_currentProfile.BlockEnergyFArt, ushort.MaxValue);

            if (_currentProfile.BlockEnergyF != _dust765BlockEnergyF.IsChecked)
            {
                if (_dust765BlockEnergyF.IsChecked)
                {
                    FieldBlockTileData.SetImpassable(eFieldGraphic, true);
                }
                else
                {
                    FieldBlockTileData.SetImpassable(eFieldGraphic, false);
                }

                _currentProfile.BlockEnergyF = _dust765BlockEnergyF.IsChecked;
            }

            if (_currentProfile.BlockEnergyFFelOnly != _dust765BlockEnergyFFelOnly.IsChecked)
            {
                if (_dust765BlockEnergyFFelOnly.IsChecked && World.MapIndex == 0)
                {
                    FieldBlockTileData.SetImpassable(eFieldGraphic, true);
                }
                else
                {
                    FieldBlockTileData.SetImpassable(eFieldGraphic, false);
                }

                _currentProfile.BlockEnergyFFelOnly = _dust765BlockEnergyFFelOnly.IsChecked;
            }

            _currentProfile.HighlightLastTargetType = _dust765HighlightLastTargetType.SelectedIndex;
            _currentProfile.HighlightLastTargetTypeHue = _dust765HighlightLastTargetHue.Hue;
            _currentProfile.HighlightLastTargetTypePoison = _dust765HighlightLastTargetPoison.SelectedIndex;
            _currentProfile.HighlightLastTargetTypePoisonHue = _dust765HighlightLastTargetPoisonHue.Hue;
            _currentProfile.HighlightLastTargetTypePara = _dust765HighlightLastTargetPara.SelectedIndex;
            _currentProfile.HighlightLastTargetTypeParaHue = _dust765HighlightLastTargetParaHue.Hue;
            _currentProfile.LTHighlightRangeOnCast = _dust765LTHighlightRangeOnCast.IsChecked;
            _currentProfile.LTHighlightRangeOnCastRange = _dust765LTHighlightRangeOnCastRange.Value;
            _currentProfile.LTHighlightRangeOnCastHue = _dust765LTHighlightRangeOnCastHue.Hue;
            _currentProfile.LTHighlightRangeOnActivated = _dust765LTHighlightRangeOnActivated.IsChecked;
            _currentProfile.LTHighlightRangeOnActivatedRange = _dust765LTHighlightRangeOnActivatedRange.Value;
            _currentProfile.LTHighlightRangeOnActivatedHue = _dust765LTHighlightRangeOnActivatedHue.Hue;

            _currentProfile.ShowHPLineInNOH = _dust765ShowHPLineInNOH.IsChecked;
            _currentProfile.NameOverheadBackgroundToggled = _dust765NameOverheadBgToggled.IsChecked;
            _currentProfile.NamePlateHideAtFullHealth = _dust765NamePlateHideAtFullHealth.IsChecked;
            _currentProfile.NamePlateOpacity = (byte)Math.Clamp(_dust765NamePlateBgOpacity.Value, 0, 100);
        }
    }
}
