// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace ClassicUO.Game.UI.Gumps
{
    internal class NameOverheadGump : Gump
    {
        private AlphaBlendControl _background;
        private Point _lockedPosition,
            _lastLeftMousePositionDown;
        private bool _positionLocked,
            _leftMouseIsDown;
        private readonly RenderedText _renderedText;
        private Texture2D _borderColor = BORDER_COLOR_BLACK;
        private string _lastSourceName = string.Empty;
        private ushort _lastDisplayHue;
        private bool _hasDisplayHue;
        private bool _lastIsLastTarget;

        // ## BEGIN - END ## // NAMEOVERHEAD
        private LineCHB _hpLineBorder, _hpLineRed, _hpLine;
        private static readonly Color HPB_COLOR_DRAW_RED   = Color.Red;
        private static readonly Color HPB_COLOR_DRAW_BLUE  = Color.DodgerBlue;
        private static readonly Color HPB_COLOR_DRAW_BLACK = Color.Black;
        private static readonly Texture2D BORDER_COLOR_BLACK = SolidColorTextureCache.GetTexture(Color.Black);
        private static readonly Texture2D BORDER_COLOR_RED   = SolidColorTextureCache.GetTexture(Color.Red);
        private static readonly Texture2D HPB_COLOR_WHITE    = SolidColorTextureCache.GetTexture(Color.White);
        private static readonly Texture2D HPB_COLOR_BLUE   = SolidColorTextureCache.GetTexture(Color.DodgerBlue);
        private static readonly Texture2D HPB_COLOR_YELLOW = SolidColorTextureCache.GetTexture(Color.Orange);
        private static readonly Texture2D HPB_COLOR_POISON = SolidColorTextureCache.GetTexture(Color.LimeGreen);
        private static readonly Texture2D HPB_COLOR_PARA   = SolidColorTextureCache.GetTexture(Color.MediumPurple);

        private class LineCHB : Line
        {
            public LineCHB(int x, int y, int w, int h, uint color)
                : base(x, y, w, h, color)
            {
                LineWidth = w;
                LineColor = SolidColorTextureCache.GetTexture(new Color { PackedValue = color });
                CanMove = true;
            }
            public int LineWidth { get; set; }
            public Texture2D LineColor { get; set; }

            public override bool AddToRenderLists(RenderLists renderLists, int x, int y, ref float layerDepthRef)
            {
                if (LineWidth <= 0 || Height <= 0 || LineColor == null)
                {
                    return false;
                }

                float layerDepth = layerDepthRef;
                Vector3 hv = ShaderHueTranslator.GetHueVector(0, false, Alpha);
                Texture2D tex = LineColor;
                int lw = LineWidth;
                int lh = Height;
                renderLists.AddGumpNoAtlas(batcher =>
                {
                    batcher.Draw(tex, new Rectangle(x, y, lw, lh), hv, layerDepth);
                    return true;
                });
                return true;
            }
        }

        private static int CalculatePercents(int max, int current, int maxValue)
        {
            if (max > 0)
            {
                max = current * 100 / max;
                if (max > 100) max = 100;
                if (max > 1)   max = maxValue * max / 100;
            }
            return max;
        }
        // ## BEGIN - END ## // NAMEOVERHEAD

        public NameOverheadGump(World world, uint serial) : base(world, serial, 0)
        {
            CanMove = false;
            AcceptMouseInput = true;
            CanCloseWithRightClick = true;

            Entity entity = World.Get(serial);

            if (entity == null)
            {
                Dispose();

                return;
            }

            _renderedText = RenderedText.Create(
                string.Empty,
                entity is Mobile m ? Notoriety.GetHue(m.NotorietyFlag) : (ushort)0x0481,
                0xFF,
                true,
                FontStyle.BlackBorder,
                TEXT_ALIGN_TYPE.TS_CENTER,
                100,
                30,
                true
            );

            SetTooltip(entity);

            // ## BEGIN - END ## // NAMEOVERHEAD
            if (entity is Mobile)
            {
                Add(_hpLineBorder = new LineCHB(1, -8, 1, 8, HPB_COLOR_DRAW_BLACK.PackedValue) { LineWidth = 0 });
                Add(_hpLineRed    = new LineCHB(1, -7, 1, 6, HPB_COLOR_DRAW_RED.PackedValue)   { LineWidth = 0 });
                Add(_hpLine       = new LineCHB(1, -7, 1, 6, HPB_COLOR_DRAW_BLUE.PackedValue)  { LineWidth = 0 });
            }
            // ## BEGIN - END ## // NAMEOVERHEAD

            BuildGump();

            WantUpdateSize = false;
        }

        public bool SetName()
        {
            Entity entity = World.Get(LocalSerial);

            if (entity == null)
            {
                return false;
            }

            if (entity is Item item)
            {
                if (!World.OPL.TryGetNameAndData(item, out string t, out _))
                {
                    if (!item.IsCorpse && item.Amount > 1)
                    {
                        t = item.Amount.ToString() + ' ';
                    }

                    if (string.IsNullOrEmpty(item.ItemData.Name))
                    {
                        t += Client.Game.UO.FileManager.Clilocs.GetString(1020000 + item.Graphic, true, t);
                    }
                    else
                    {
                        t += StringHelper.CapitalizeAllWords(
                            StringHelper.GetPluralAdjustedString(
                                item.ItemData.Name,
                                item.Amount > 1
                            )
                        );
                    }
                }

                if (string.IsNullOrEmpty(t))
                {
                    return false;
                }

                return SetRenderedText(t);
            }

            if (!string.IsNullOrEmpty(entity.Name))
            {
                return SetRenderedText(entity.Name);
            }

            return false;
        }

        private bool SetRenderedText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            string sourceText = text;

            if (string.Equals(_lastSourceName, sourceText, StringComparison.Ordinal))
            {
                return true;
            }

            Client.Game.UO.FileManager.Fonts.SetUseHTML(true);
            Client.Game.UO.FileManager.Fonts.RecalculateWidthByInfo = true;

            int width = Client.Game.UO.FileManager.Fonts.GetWidthUnicode(_renderedText.Font, text);

            if (width > Constants.OBJECT_HANDLES_GUMP_WIDTH)
            {
                text = Client.Game.UO.FileManager.Fonts.GetTextByWidthUnicode(
                    _renderedText.Font,
                    text.AsSpan(),
                    Constants.OBJECT_HANDLES_GUMP_WIDTH,
                    true,
                    TEXT_ALIGN_TYPE.TS_CENTER,
                    (ushort)FontStyle.BlackBorder
                );

                width = Constants.OBJECT_HANDLES_GUMP_WIDTH;
            }

            _renderedText.MaxWidth = width;
            _renderedText.Text = text;
            _lastSourceName = sourceText;

            Client.Game.UO.FileManager.Fonts.RecalculateWidthByInfo = false;
            Client.Game.UO.FileManager.Fonts.SetUseHTML(false);

            Width = _background.Width = Math.Max(60, _renderedText.Width) + 4;
            Height = _background.Height = Constants.OBJECT_HANDLES_GUMP_HEIGHT + 4;

            WantUpdateSize = false;

            return true;
        }

        private void BuildGump()
        {
            Entity entity = World.Get(LocalSerial);

            if (entity == null)
            {
                Dispose();

                return;
            }

            // ## BEGIN - END ## // NAMEOVERHEAD
            float bgAlpha = ProfileManager.CurrentProfile != null
                ? ProfileManager.CurrentProfile.NamePlateOpacity / 100f
                : 0.7f;
            Add(
                _background = new AlphaBlendControl(bgAlpha)
                {
                    WantUpdateSize = false,
                    Hue = entity is Mobile m ? Notoriety.GetHue(m.NotorietyFlag) : (ushort)0x0481,
                    IsVisible = ProfileManager.CurrentProfile == null
                        || !ProfileManager.CurrentProfile.NameOverheadBackgroundToggled
                }
            );
            // ## BEGIN - END ## // NAMEOVERHEAD
        }

        protected override void CloseWithRightClick()
        {
            Entity entity = World.Get(LocalSerial);

            if (entity != null)
            {
                entity.ObjectHandlesStatus = ObjectHandlesStatus.CLOSED;
            }

            base.CloseWithRightClick();
        }

        private void DoDrag()
        {
            var delta = Mouse.Position - _lastLeftMousePositionDown;

            if (
                Math.Abs(delta.X) <= Constants.MIN_GUMP_DRAG_DISTANCE
                && Math.Abs(delta.Y) <= Constants.MIN_GUMP_DRAG_DISTANCE
            )
            {
                return;
            }

            _leftMouseIsDown = false;
            _positionLocked = false;

            Entity entity = World.Get(LocalSerial);

            if (entity is Mobile || entity is Item it && it.IsDamageable)
            {
                if (UIManager.IsDragging)
                {
                    return;
                }

                BaseHealthBarGump gump = UIManager.GetGump<BaseHealthBarGump>(LocalSerial);
                gump?.Dispose();

                if (entity == World.Player && ProfileManager.CurrentProfile.StatusGumpBarMutuallyExclusive)
                {
                    StatusGumpBase.GetStatusGump()?.Dispose();
                }

                if (ProfileManager.CurrentProfile.CustomBarsToggled)
                {
                    Rectangle rect = new Rectangle(
                        0,
                        0,
                        HealthBarGumpCustom.HPB_WIDTH,
                        HealthBarGumpCustom.HPB_HEIGHT_SINGLELINE
                    );

                    UIManager.Add(
                        gump = new HealthBarGumpCustom(World, entity)
                        {
                            X = Mouse.Position.X - (rect.Width >> 1),
                            Y = Mouse.Position.Y - (rect.Height >> 1)
                        }
                    );
                }
                else
                {
                    ref readonly var gumpInfo = ref Client.Game.UO.Gumps.GetGump(0x0804);

                    UIManager.Add(
                        gump = new HealthBarGump(World, entity)
                        {
                            X = Mouse.LClickPosition.X - (gumpInfo.UV.Width >> 1),
                            Y = Mouse.LClickPosition.Y - (gumpInfo.UV.Height >> 1)
                        }
                    );
                }

                UIManager.AttemptDragControl(gump, true);
            }
            else if (entity != null)
            {
                GameActions.PickUp(World, LocalSerial, 0, 0);

                //if (entity.Texture != null)
                //    GameActions.PickUp(LocalSerial, entity.Texture.Width >> 1, entity.Texture.Height >> 1);
                //else
                //    GameActions.PickUp(LocalSerial, 0, 0);
            }
        }

        protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                if (SerialHelper.IsMobile(LocalSerial))
                {
                    if (World.Player.InWarMode)
                    {
                        GameActions.Attack(World, LocalSerial);
                    }
                    else
                    {
                        GameActions.DoubleClick(World, LocalSerial);
                    }
                }
                else
                {
                    if (!GameActions.OpenCorpse(World, LocalSerial))
                    {
                        GameActions.DoubleClick(World, LocalSerial);
                    }
                }

                return true;
            }

            return false;
        }

        protected override void OnMouseDown(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                _lastLeftMousePositionDown = Mouse.Position;
                _leftMouseIsDown = true;
            }

            base.OnMouseDown(x, y, button);
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                _leftMouseIsDown = false;

                if (!Client.Game.UO.GameCursor.ItemHold.Enabled)
                {
                    if (
                        UIManager.IsDragging
                        || Math.Max(Math.Abs(Mouse.LDragOffset.X), Math.Abs(Mouse.LDragOffset.Y))
                            >= 1
                    )
                    {
                        _positionLocked = false;

                        return;
                    }
                }

                if (World.TargetManager.IsTargeting)
                {
                    switch (World.TargetManager.TargetingState)
                    {
                        case CursorTarget.Position:
                        case CursorTarget.Object:
                        case CursorTarget.Grab:
                        case CursorTarget.SetGrabBag:
                        case CursorTarget.CallbackTarget:
                            World.TargetManager.Target(LocalSerial);
                            Mouse.LastLeftButtonClickTime = 0;

                            break;

                        case CursorTarget.SetTargetClientSide:
                            World.TargetManager.Target(LocalSerial);
                            Mouse.LastLeftButtonClickTime = 0;
                            UIManager.Add(new InspectorGump(World, World.Get(LocalSerial)));

                            break;

                        case CursorTarget.HueCommandTarget:
                            World.CommandManager.OnHueTarget(World.Get(LocalSerial));

                            break;
                    }
                }
                else
                {
                    if (
                        Client.Game.UO.GameCursor.ItemHold.Enabled
                        && !Client.Game.UO.GameCursor.ItemHold.IsFixedPosition
                    )
                    {
                        uint drop_container = 0xFFFF_FFFF;
                        bool can_drop = false;
                        ushort dropX = 0;
                        ushort dropY = 0;
                        sbyte dropZ = 0;

                        Entity obj = World.Get(LocalSerial);

                        if (obj != null)
                        {
                            can_drop = obj.Distance <= Constants.DRAG_ITEMS_DISTANCE;

                            if (can_drop)
                            {
                                if (obj is Item it && it.ItemData.IsContainer || obj is Mobile)
                                {
                                    dropX = 0xFFFF;
                                    dropY = 0xFFFF;
                                    dropZ = 0;
                                    drop_container = obj.Serial;
                                }
                                else if (
                                    obj is Item it2
                                    && (
                                        it2.ItemData.IsSurface
                                        || it2.ItemData.IsStackable
                                            && it2.DisplayedGraphic
                                                == Client.Game.UO.GameCursor.ItemHold.DisplayedGraphic
                                    )
                                )
                                {
                                    dropX = obj.X;
                                    dropY = obj.Y;
                                    dropZ = obj.Z;

                                    if (it2.ItemData.IsSurface)
                                    {
                                        dropZ += (sbyte)(
                                            it2.ItemData.Height == 0xFF ? 0 : it2.ItemData.Height
                                        );
                                    }
                                    else
                                    {
                                        drop_container = obj.Serial;
                                    }
                                }
                            }
                            else
                            {
                                Client.Game.Audio.PlaySound(0x0051);
                            }

                            if (can_drop)
                            {
                                if (drop_container == 0xFFFF_FFFF && dropX == 0 && dropY == 0)
                                {
                                    can_drop = false;
                                }

                                if (can_drop)
                                {
                                    GameActions.DropItem(
                                        Client.Game.UO.GameCursor.ItemHold.Serial,
                                        dropX,
                                        dropY,
                                        dropZ,
                                        drop_container
                                    );
                                }
                            }
                        }
                    }
                    else if (!World.DelayedObjectClickManager.IsEnabled)
                    {
                        World.DelayedObjectClickManager.Set(
                            LocalSerial,
                            Mouse.Position.X,
                            Mouse.Position.Y,
                            Time.Ticks + Mouse.MOUSE_DELAY_DOUBLE_CLICK
                        );
                    }
                }
            }

            base.OnMouseUp(x, y, button);
        }

        protected override void OnMouseOver(int x, int y)
        {
            if (_leftMouseIsDown)
            {
                DoDrag();
            }

            if (!_positionLocked && SerialHelper.IsMobile(LocalSerial))
            {
                Mobile m = World.Mobiles.Get(LocalSerial);

                if (m == null)
                {
                    Dispose();

                    return;
                }

                _positionLocked = true;

                Client.Game.UO.Animations.GetAnimationDimensions(
                    m.AnimIndex,
                    m.GetGraphicForAnimation(),
                    /*(byte) m.GetDirectionForAnimation()*/
                    0,
                    /*Mobile.GetGroupForAnimation(m, isParent:true)*/
                    0,
                    m.IsMounted,
                    /*(byte) m.AnimIndex*/
                    0,
                    out int centerX,
                    out int centerY,
                    out int width,
                    out int height
                );

                _lockedPosition.X = (int)(m.RealScreenPosition.X + m.Offset.X + 22 + 5);

                _lockedPosition.Y = (int)(
                    m.RealScreenPosition.Y
                    + (m.Offset.Y - m.Offset.Z)
                    - (height + centerY + 15)
                    + (
                        m.IsGargoyle && m.IsFlying
                            ? -22
                            : !m.IsMounted
                                ? 22
                                : 0
                    )
                );
            }

            base.OnMouseOver(x, y);
        }

        protected override void OnMouseExit(int x, int y)
        {
            _positionLocked = false;

            base.OnMouseExit(x, y);
        }

        public override void Update()
        {
            base.Update();

            Entity entity = World.Get(LocalSerial);

            if (
                entity == null
                || entity.IsDestroyed
                || entity.ObjectHandlesStatus == ObjectHandlesStatus.NONE
                || entity.ObjectHandlesStatus == ObjectHandlesStatus.CLOSED
            )
            {
                Dispose();
            }
            else
            {
                if (ProfileManager.CurrentProfile != null)
                {
                    _background.Alpha = ProfileManager.CurrentProfile.NamePlateOpacity / 100f;

                    if (ProfileManager.CurrentProfile.NameOverheadBackgroundToggled)
                    {
                        Control mo = UIManager.MouseOverControl;
                        _background.IsVisible = mo != null && mo.RootParent == this;
                    }
                    else
                    {
                        _background.IsVisible = true;
                    }
                }

                bool isLastTarget = entity == World.TargetManager.LastTargetInfo.Serial;
                if (isLastTarget != _lastIsLastTarget)
                {
                    _borderColor = isLastTarget ? BORDER_COLOR_RED : BORDER_COLOR_BLACK;
                    _lastIsLastTarget = isLastTarget;
                }

                ushort hue = entity is Mobile m
                    ? Notoriety.GetHue(m.NotorietyFlag)
                    : (ushort)0x0481;

                if (!_hasDisplayHue || _lastDisplayHue != hue)
                {
                    _background.Hue = _renderedText.Hue = hue;
                    _lastDisplayHue = hue;
                    _hasDisplayHue = true;
                }
                // ## BEGIN - END ## // NAMEOVERHEAD
                if (_hpLineBorder != null)
                {
                    if (ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.ShowHPLineInNOH)
                    {
                        if (entity is Mobile mobile)
                        {
                            _hpLineBorder.X = _background.X - 1;
                            _hpLineRed.X = _hpLine.X = _background.X;
                            _hpLineBorder.Y = _background.Y - 8;
                            _hpLineRed.Y = _hpLine.Y = _background.Y - 7;

                            int bgW = Math.Max(_background.Width, Width);
                            if (bgW <= 0)
                            {
                                bgW = 60;
                            }

                            _hpLineBorder.LineWidth = bgW + 2;
                            _hpLineRed.LineWidth = bgW;

                            int hits = mobile.HitsMax > 0
                                ? CalculatePercents(mobile.HitsMax, mobile.Hits, bgW)
                                : bgW;
                            if (hits != _hpLine.LineWidth)
                            {
                                _hpLine.LineWidth = hits;
                            }

                            if (mobile.IsPoisoned)
                                _hpLine.LineColor = HPB_COLOR_POISON;
                            else if (mobile.IsParalyzed)
                                _hpLine.LineColor = HPB_COLOR_PARA;
                            else if (mobile.IsYellowHits)
                                _hpLine.LineColor = HPB_COLOR_YELLOW;
                            else
                                _hpLine.LineColor = HPB_COLOR_BLUE;
                        }
                    }
                    else
                    {
                        _hpLineBorder.X = _hpLineRed.X = _hpLine.X = _background.X;
                        _hpLineBorder.Y = _hpLineRed.Y = _hpLine.Y = _background.Y;
                        _hpLineBorder.LineWidth = _hpLineRed.LineWidth = _hpLine.LineWidth = 0;
                    }
                }
                // ## BEGIN - END ## // NAMEOVERHEAD
            }
        }

        public override bool AddToRenderLists(RenderLists renderLists, int x, int y, ref float layerDepthRef)
        {
            if (IsDisposed || !SetName())
            {
                return false;
            }

            // ## BEGIN - END ## // TAZUO
            bool _isMobile = false;
            double _hpPercent = 1;
            // ## BEGIN - END ## // TAZUO

            if (SerialHelper.IsMobile(LocalSerial))
            {
                Mobile m = World.Mobiles.Get(LocalSerial);

                if (m == null)
                {
                    Dispose();

                    return false;
                }

                // ## BEGIN - END ## // TAZUO
                _isMobile = true;
                _hpPercent = m.HitsMax > 0 ? (double)m.Hits / (double)m.HitsMax : 1d;

                // ## BEGIN - END ## // NAMEOVERHEAD
                if (ProfileManager.CurrentProfile?.NamePlateHideAtFullHealth == true
                    && _hpPercent >= 1
                    && World.Player.InWarMode
                    && m != World.Player)
                {
                    return false;
                }
                // ## BEGIN - END ## // NAMEOVERHEAD
                // ## BEGIN - END ## // TAZUO

                if (_positionLocked)
                {
                    x = _lockedPosition.X;
                    y = _lockedPosition.Y;
                }
                else
                {
                    Client.Game.UO.Animations.GetAnimationDimensions(
                        m.AnimIndex,
                        m.GetGraphicForAnimation(),
                        /*(byte) m.GetDirectionForAnimation()*/
                        0,
                        /*Mobile.GetGroupForAnimation(m, isParent:true)*/
                        0,
                        m.IsMounted,
                        /*(byte) m.AnimIndex*/
                        0,
                        out int centerX,
                        out int centerY,
                        out int width,
                        out int height
                    );

                    x = (int)(m.RealScreenPosition.X + m.Offset.X + 22 + 5);
                    y = (int)(
                        m.RealScreenPosition.Y
                        + (m.Offset.Y - m.Offset.Z)
                        - (height + centerY + 15)
                        + (
                            m.IsGargoyle && m.IsFlying
                                ? -22
                                : !m.IsMounted
                                    ? 22
                                    : 0
                        )
                    );
                }
            }
            else if (SerialHelper.IsItem(LocalSerial))
            {
                Item item = World.Items.Get(LocalSerial);

                if (item == null)
                {
                    Dispose();

                    return false;
                }

                var bounds = Client.Game.UO.Arts.GetRealArtBounds(item.Graphic);

                x = item.RealScreenPosition.X + (int)item.Offset.X + 22 + 5;
                y =
                    item.RealScreenPosition.Y
                    + (int)(item.Offset.Y - item.Offset.Z)
                    + (bounds.Height >> 1);
            }

            Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);

            Point p = Client.Game.Scene.Camera.WorldToScreen(new Point(x, y));
            x = p.X - (Width >> 1);
            y = p.Y - (Height >> 1);

            var camera = Client.Game.Scene.Camera;
            x += camera.Bounds.X;
            y += camera.Bounds.Y;

            if (x < camera.Bounds.X || x + Width > camera.Bounds.Right)
            {
                return false;
            }

            if (y < camera.Bounds.Y || y + Height > camera.Bounds.Bottom)
            {
                return false;
            }
            X = x;
            Y = y;

            if (_background.IsVisible)
            {
                float borderDepth = layerDepthRef;
                renderLists.AddGumpNoAtlas(
                    batcher =>
                    {
                        batcher.DrawRectangle(_borderColor, x - 1, y - 1, Width + 1, Height + 1, hueVector, borderDepth);
                        return true;
                    }
                );
                layerDepthRef += CHILD_LAYER_INCREMENT;
            }

            base.AddToRenderLists(renderLists, x, y, ref layerDepthRef);

            // ## BEGIN - END ## // TAZUO
            if (ProfileManager.CurrentProfile?.NamePlateHealthBar == true && _isMobile)
            {
                float hpOpacity = ProfileManager.CurrentProfile.NamePlateHealthBarOpacity / 100f;
                ushort bgHue = _background.Hue;
                int hpWidth = (int)(Width * _hpPercent);

                if (hpOpacity > 0f && hpWidth > 0 && Height > 0)
                {
                    layerDepthRef += CHILD_LAYER_INCREMENT;
                    float hpDepth = layerDepthRef;
                    renderLists.AddGumpNoAtlas(batcher =>
                    {
                        batcher.Draw(
                            HPB_COLOR_WHITE,
                            new Rectangle(x, y, hpWidth, Height),
                            ShaderHueTranslator.GetHueVector(bgHue, false, hpOpacity),
                            hpDepth
                        );
                        return true;
                    });
                }
            }
            // ## BEGIN - END ## // TAZUO

            layerDepthRef += CHILD_LAYER_INCREMENT;
            float textDepth = layerDepthRef;
            int renderedTextOffset = Math.Max(0, Width - _renderedText.Width - 4) >> 1;
            renderLists.AddGumpNoAtlas(batcher =>
                {
                    return _renderedText.Draw(
                        batcher,
                        Width,
                        Height,
                        x + 2 + renderedTextOffset,
                        y + 2,
                        Width,
                        Height,
                        0,
                        0,
                        textDepth
                    );
                }
            );

            return true;
        }

        public override void Dispose()
        {
            _renderedText?.Destroy();
            base.Dispose();
        }
    }
}
