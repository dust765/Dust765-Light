// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.Assets;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    internal class PaperDollInteractable : Control
    {
        private static readonly Dictionary<(ushort mobile, ushort item), ushort> _tileArtGumpCache = new(64);

        private readonly PaperDollGump _paperDollGump;

        private bool _updateUI;

        public PaperDollInteractable(int x, int y, uint serial, PaperDollGump paperDollGump)
        {
            X = x;
            Y = y;
            _paperDollGump = paperDollGump;
            AcceptMouseInput = false;
            LocalSerial = serial;
            _updateUI = true;
        }

        public bool HasFakeItem { get; private set; }

        public override void Update()
        {
            base.Update();

            if (_updateUI)
            {
                UpdateUI();

                _updateUI = false;
            }
        }

        public void SetFakeItem(bool value)
        {
            _updateUI = HasFakeItem && !value || !HasFakeItem && value;
            HasFakeItem = value;
        }

        private void UpdateUI()
        {
            if (IsDisposed)
            {
                return;
            }

            Mobile mobile = _paperDollGump.World.Mobiles.Get(LocalSerial);

            if (mobile == null || mobile.IsDestroyed)
            {
                Dispose();

                return;
            }

            Clear();

            // Add the base gump - the semi-naked paper doll.
            ushort body;
            ushort hue = mobile.Hue;

            if (mobile.Graphic == 0x0191 || mobile.Graphic == 0x0193)
            {
                body = 0x000D;
            }
            else if (mobile.Graphic == 0x025D)
            {
                body = 0x000E;
            }
            else if (mobile.Graphic == 0x025E)
            {
                body = 0x000F;
            }
            else if (mobile.Graphic == 0x029A || mobile.Graphic == 0x02B6)
            {
                body = 0x029A;
            }
            else if (mobile.Graphic == 0x029B || mobile.Graphic == 0x02B7)
            {
                body = 0x0299;
            }
            else if (mobile.Graphic == 0x04E5)
            {
                body = 0xC835;
            }
            else if (mobile.Graphic == 0x03DB)
            {
                body = 0x000C;
                hue = 0x03EA;
            }
            else if (mobile.IsFemale)
            {
                body = 0x000D;
            }
            else
            {
                body = 0x000C;
            }

            // body
            Add(new GumpPic(0, 0, body, hue) { IsPartialHue = true });

            if (mobile.Graphic == 0x03DB)
            {
                Add(
                    new GumpPic(0, 0, 0xC72B, mobile.Hue)
                    {
                        AcceptMouseInput = true,
                        IsPartialHue = true
                    }
                );
            }

            Span<ushort> layerGraphics = stackalloc ushort[PaperdollOrder.N];
            PaperdollOrder.GraphicsFromEntity(mobile, layerGraphics);

            if (
                HasFakeItem
                && Client.Game.UO.GameCursor.ItemHold.Enabled
                && !Client.Game.UO.GameCursor.ItemHold.IsFixedPosition
            )
            {
                byte holdLayer = Client.Game.UO.GameCursor.ItemHold.ItemData.Layer;

                if (holdLayer > 0 && holdLayer < layerGraphics.Length && layerGraphics[holdLayer] == 0)
                {
                    layerGraphics[holdLayer] = Client.Game.UO.GameCursor.ItemHold.ItemData.AnimID;
                }
            }

            bool isOwnPaperdoll = _paperDollGump.World.Player != null && LocalSerial == _paperDollGump.World.Player.Serial;
            bool showAllLayersPaperdoll = ProfileManager.CurrentProfile?.ShowAllLayersPaperdoll ?? true;
            Item wornOuterTorso = mobile.FindItemByLayer(Layer.Robe);
            Item wornCloak = mobile.FindItemByLayer(Layer.Cloak);
            bool useParrotPaperdollRules = (ProfileManager.CurrentProfile?.PaperdollParrotOriginalView ?? true)
                && Game.Data.LayerOrder.IsParrotEpaulets(wornOuterTorso);
            bool useQuiverPaperdollRules = wornCloak != null && wornCloak.ItemData.IsContainer;

            Span<Layer> layers = stackalloc Layer[PaperdollOrder.N];
            int layerCount;

            if (useParrotPaperdollRules)
            {
                layerCount = Game.Data.LayerOrder.ParrotLayers.Length;
                Game.Data.LayerOrder.ParrotLayers.AsSpan().CopyTo(layers);
            }
            else if (useQuiverPaperdollRules)
            {
                layerCount = Game.Data.LayerOrder.QuiverLayers.Length;
                Game.Data.LayerOrder.QuiverLayers.AsSpan().CopyTo(layers);
            }
            else
            {
                bool altTorso = mobile.IsFemale || IsGargoyleBody(mobile.Graphic);
                Span<Layer> order = stackalloc Layer[PaperdollOrder.N];
                PaperdollOrder.Build(layerGraphics, altTorso, order);
                layerCount = PaperdollOrder.Filter(order, includeBackpack: false, layers);
            }

            Item equipItem;

            for (int i = 0; i < layerCount; i++)
            {
                Layer layer = layers[i];

                equipItem = mobile.FindItemByLayer(layer);

                if (equipItem != null)
                {
                    bool hideHeadUnderCoveringRobe = isOwnPaperdoll
                        && (ProfileManager.CurrentProfile?.PaperdollHideHeadUnderCoveringRobe ?? false)
                        && IsHelmetOrHairLayer(layer)
                        && IsHeadCoveredByEquipment(mobile);

                    if (hideHeadUnderCoveringRobe)
                    {
                        continue;
                    }

                    bool respectCoveredLayers = !showAllLayersPaperdoll;

                    if (
                        respectCoveredLayers
                        && layer != Layer.Shirt
                        && layer != Layer.Tunic
                        && layer != Layer.Pants
                        && layer != Layer.Robe
                        && Mobile.IsCovered(mobile, layer)
                        && !equipItem.IsSpellbookEquipment()
                    )
                    {
                        continue;
                    }

                    ushort id = GetAnimID(
                        mobile.Graphic,
                        equipItem.Graphic,
                        equipItem.ItemData.AnimID,
                        mobile.IsFemale
                    );

                    if (id == 0 || Client.Game.UO.Gumps.GetGump(id).Texture == null)
                    {
                        continue;
                    }

                    Add(
                        new GumpPicEquipment(
                            _paperDollGump,
                            equipItem.Serial,
                            0,
                            0,
                            id,
                            (ushort)(equipItem.Hue & 0x3FFF),
                            layer
                        )
                        {
                            AcceptMouseInput = true,
                            IsPartialHue = equipItem.ItemData.IsPartialHue,
                            CanLift =
                                _paperDollGump.World.InGame
                                && !_paperDollGump.World.Player.IsDead
                                && layer != Layer.Beard
                                && layer != Layer.Hair
                                && (_paperDollGump.CanLift || LocalSerial == _paperDollGump.World.Player)
                        }
                    );
                }
                else if (
                    HasFakeItem
                    && Client.Game.UO.GameCursor.ItemHold.Enabled
                    && !Client.Game.UO.GameCursor.ItemHold.IsFixedPosition
                    && (byte)layer == Client.Game.UO.GameCursor.ItemHold.ItemData.Layer
                    && Client.Game.UO.GameCursor.ItemHold.ItemData.AnimID != 0
                )
                {
                    ushort id = GetAnimID(
                        mobile.Graphic,
                        Client.Game.UO.GameCursor.ItemHold.Graphic,
                        Client.Game.UO.GameCursor.ItemHold.ItemData.AnimID,
                        mobile.IsFemale
                    );

                    Add(
                        new GumpPicEquipment(
                            _paperDollGump,
                            0,
                            0,
                            0,
                            id,
                            (ushort)(Client.Game.UO.GameCursor.ItemHold.Hue & 0x3FFF),
                            Client.Game.UO.GameCursor.ItemHold.Layer
                        )
                        {
                            AcceptMouseInput = true,
                            IsPartialHue = Client.Game.UO.GameCursor.ItemHold.IsPartialHue,
                            Alpha = 0.5f
                        }
                    );
                }
            }

            equipItem = mobile.FindItemByLayer(Layer.Backpack);

            if (equipItem != null && equipItem.ItemData.AnimID != 0)
            {
                ushort backpackGraphic = (ushort)(
                    equipItem.ItemData.AnimID + Constants.MALE_GUMP_OFFSET
                );

                // If player, apply backpack skin
                if (mobile.Serial == _paperDollGump.World.Player.Serial)
                {
                    var gump = Client.Game.UO.Gumps;

                    switch (ProfileManager.CurrentProfile.BackpackStyle)
                    {
                        case 1:
                            if (gump.GetGump(0x777B).Texture != null)
                            {
                                backpackGraphic = 0x777B; // Suede Backpack
                            }

                            break;
                        case 2:
                            if (gump.GetGump(0x777C).Texture != null)
                            {
                                backpackGraphic = 0x777C; // Polar Bear Backpack
                            }

                            break;
                        case 3:
                            if (gump.GetGump(0x777D).Texture != null)
                            {
                                backpackGraphic = 0x777D; // Ghoul Skin Backpack
                            }

                            break;
                        default:
                            if (gump.GetGump(0xC4F6).Texture != null)
                            {
                                backpackGraphic = 0xC4F6; // Default Backpack
                            }

                            break;
                    }
                }

                int bx = 0;

                if (_paperDollGump.World.ClientFeatures.PaperdollBooks)
                {
                    bx = 14;
                }

                Add(
                    new GumpPicEquipment(
                        _paperDollGump,
                        equipItem.Serial,
                        -bx,
                        0,
                        backpackGraphic,
                        (ushort)(equipItem.Hue & 0x3FFF),
                        Layer.Backpack
                    )
                    {
                        AcceptMouseInput = true
                    }
                );
            }
        }

        public void RequestUpdate()
        {
            _updateUI = true;
        }

        public void RequestRefresh()
        {
            _updateUI = true;
        }

        private static bool IsGargoyleBody(ushort graphic)
        {
            return graphic == 0x029A || graphic == 0x029B
                || graphic == 0x02B6 || graphic == 0x02B7;
        }

        private static bool TryResolveTileArtAppearance(TileArtInfo tileArtInfo, ushort mobileGraphic, out uint appearanceId)
        {
            appearanceId = 0;

            if (tileArtInfo.TryGetAppearance(mobileGraphic, out appearanceId) && appearanceId != 0)
            {
                return true;
            }

            foreach (KeyValuePair<byte, Dictionary<uint, uint>> subtype in tileArtInfo.Appearances)
            {
                if (subtype.Value.TryGetValue(mobileGraphic, out appearanceId) && appearanceId != 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsHelmetOrHairLayer(Layer layer)
        {
            return layer == Layer.Helmet || layer == Layer.Hair;
        }

        private static bool IsHeadCoveredByEquipment(Mobile mobile)
        {
            return Mobile.IsCovered(mobile, Layer.Helmet) || Mobile.IsCovered(mobile, Layer.Hair);
        }

        protected static ushort GetAnimID(ushort mobileGraphic, ushort itemGraphic, ushort animID, bool isfemale)
        {
            int offset = isfemale ? Constants.FEMALE_GUMP_OFFSET : Constants.MALE_GUMP_OFFSET;

            if (
                    Client.Game.UO.Version >= ClientVersion.CV_7000
                    && animID == 0x03CA // graphic for dead shroud
                    && (mobileGraphic == 0x02B7 || mobileGraphic == 0x02B6)
                ) // dead gargoyle graphics
                {
                    animID = 0x0223;
                }

            Client.Game.UO.Animations.ConvertBodyIfNeeded(ref mobileGraphic);

            if (
                Client.Game.UO.FileManager.Animations.EquipConversions.TryGetValue(
                    mobileGraphic,
                    out Dictionary<ushort, EquipConvData> dict
                )
            )
            {
                if (dict.TryGetValue(animID, out EquipConvData data))
                {
                    if (data.Gump > Constants.MALE_GUMP_OFFSET)
                    {
                        animID = (ushort)(
                            data.Gump >= Constants.FEMALE_GUMP_OFFSET
                                ? data.Gump - Constants.FEMALE_GUMP_OFFSET
                                : data.Gump - Constants.MALE_GUMP_OFFSET
                        );
                    }
                    else
                    {
                        animID = data.Gump;
                    }
                }
            }

            int classicOffset = offset;
            bool classicExists = IsAnimExistsInGump(animID, ref classicOffset, isfemale);
            ushort classicGumpId = (ushort)(animID + classicOffset);

            if (classicExists && Client.Game.UO.Gumps.GetGump(classicGumpId).Texture != null)
            {
                return classicGumpId;
            }

            if (itemGraphic != 0 && itemGraphic != animID)
            {
                int altOffset = isfemale ? Constants.FEMALE_GUMP_OFFSET : Constants.MALE_GUMP_OFFSET;
                ushort altAnim = itemGraphic;

                if (IsAnimExistsInGump(altAnim, ref altOffset, isfemale))
                {
                    ushort altGumpId = (ushort)(altAnim + altOffset);

                    if (Client.Game.UO.Gumps.GetGump(altGumpId).Texture != null)
                    {
                        return altGumpId;
                    }
                }
            }

            if (_tileArtGumpCache.TryGetValue((mobileGraphic, itemGraphic), out ushort cachedGump))
            {
                return cachedGump;
            }

            if (Client.Game.UO.FileManager.TileArt.TryGetTileArtInfo(itemGraphic, out var tileArtInfo))
            {
                if (TryResolveTileArtAppearance(tileArtInfo, mobileGraphic, out uint appearanceId) && appearanceId != 0)
                {
                    int primaryOffset = isfemale ? Constants.FEMALE_GUMP_OFFSET : Constants.MALE_GUMP_OFFSET;
                    int fallbackOffset = isfemale ? Constants.MALE_GUMP_OFFSET : Constants.FEMALE_GUMP_OFFSET;

                    foreach (int tileArtOffset in new[] { primaryOffset, fallbackOffset })
                    {
                        var gumpId = (ushort)(tileArtOffset + appearanceId);

                        if (
                            gumpId <= GumpsLoader.MAX_GUMP_DATA_INDEX_COUNT
                            && Client.Game.UO.Gumps.GetGump(gumpId).Texture != null
                        )
                        {
                            _tileArtGumpCache[(mobileGraphic, itemGraphic)] = gumpId;

                            return gumpId;
                        }
                    }
                }
            }

            return classicGumpId;
        }

        private static bool IsAnimExistsInGump(ushort animID, ref int offset, bool isFemale)
        {
            if (
                    animID + offset > GumpsLoader.MAX_GUMP_DATA_INDEX_COUNT
                    || Client.Game.UO.Gumps.GetGump((ushort)(animID + offset)).Texture == null
                )
            {
                // inverse
                offset = isFemale ? Constants.MALE_GUMP_OFFSET : Constants.FEMALE_GUMP_OFFSET;
            }

            if (Client.Game.UO.Gumps.GetGump((ushort)(animID + offset)).Texture == null)
            {
                Log.Error(
                    $"Texture not found in paperdoll: gump_graphic: {(ushort)(animID + offset)}"
                );

                return false;
            }

            return true;
        }

        protected class GumpPicEquipment : GumpPic
        {
            private readonly Layer _layer;
            private readonly Gump _gump;

            public GumpPicEquipment(
                Gump gump,
                uint serial,
                int x,
                int y,
                ushort graphic,
                ushort hue,
                Layer layer
            ) : base(x, y, graphic, hue)
            {
                _gump = gump;
                LocalSerial = serial;
                CanMove = false;
                _layer = layer;

                if (SerialHelper.IsValid(serial) && _gump.World.InGame)
                {
                    SetTooltip(serial);
                }
            }

            public bool CanLift { get; set; }

            protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
            {
                if (button != MouseButtonType.Left)
                {
                    return false;
                }

                // this check is necessary to avoid crashes during character creation
                if (_gump.World.InGame)
                {
                    GameActions.DoubleClick(_gump.World, LocalSerial);
                }

                return true;
            }

            protected override void OnMouseUp(int x, int y, MouseButtonType button)
            {
                SelectedObject.Object = _gump.World.Get(LocalSerial);
                base.OnMouseUp(x, y, button);
            }

            public override void Update()
            {
                base.Update();

                if (_gump.World.InGame)
                {
                    if (
                        CanLift
                        && !Client.Game.UO.GameCursor.ItemHold.Enabled
                        && Mouse.LButtonPressed
                        && UIManager.LastControlMouseDown(MouseButtonType.Left) == this
                        && (
                            Mouse.LastLeftButtonClickTime != 0xFFFF_FFFF
                                && Mouse.LastLeftButtonClickTime != 0
                                && Mouse.LastLeftButtonClickTime + Mouse.MOUSE_DELAY_DOUBLE_CLICK
                                    < Time.Ticks
                            || Mouse.LDragOffset != Point.Zero
                        )
                    )
                    {
                        GameActions.PickUp(_gump.World, LocalSerial, 0, 0);

                        if (_layer == Layer.OneHanded || _layer == Layer.TwoHanded)
                        {
                            _gump.World.Player.UpdateAbilities();
                        }
                    }
                    else if (MouseIsOver)
                    {
                        SelectedObject.Object = _gump.World.Get(LocalSerial);
                    }
                }
            }

            protected override void OnMouseOver(int x, int y)
            {
                SelectedObject.Object = _gump.World.Get(LocalSerial);
            }
        }
    }
}
