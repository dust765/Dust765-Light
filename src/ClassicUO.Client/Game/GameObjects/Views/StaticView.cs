// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Dust765;
using ClassicUO.Game.Data;
using ClassicUO.Game.Scenes;
using ClassicUO.IO;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.GameObjects
{
    internal sealed partial class Static
    {
        private int _canBeTransparent;

        public override bool TransparentTest(int z)
        {
            bool r = true;

            if (Z <= z - ItemData.Height)
            {
                r = false;
            }
            else if (z < Z && (_canBeTransparent & 0xFF) == 0)
            {
                r = false;
            }

            return r;
        }

        public override bool Draw(UltimaBatcher2D batcher, int posX, int posY, float depth)
        {
            if (!AllowedToDraw || IsDestroyed)
            {
                return false;
            }

            ushort graphic = Graphic;
            ushort hue = Hue;
            bool partial = ItemData.IsPartialHue;

            if (ProfileManager.CurrentProfile.HighlightGameObjects && SelectedObject.Object == this)
            {
                hue = Constants.HIGHLIGHT_CURRENT_OBJECT_HUE;
                partial = false;
            }
            else if (
                ProfileManager.CurrentProfile.NoColorObjectsOutOfRange
                && Distance > World.ClientViewRange
            )
            {
                hue = Constants.OUT_RANGE_COLOR;
                partial = false;
            }
            else if (World.Player.IsDead && ProfileManager.CurrentProfile.EnableBlackWhiteEffect)
            {
                hue = Constants.DEAD_RANGE_COLOR;
                partial = false;
            }

            if (ProfileManager.CurrentProfile.PreviewFields && CombatCollection.ObjectFieldPreview(World, this))
            {
                hue = 0x0040;
                partial = false;
            }

            bool isTree = StaticFilters.IsTree(graphic, out _);
            float heightScale = 1f;

            bool cot = !isTree && !ItemData.IsFoliage && TransparentTest(World.Player.Z + 5);

            if (isTree)
            {
                TreeReplace.TryApply(
                    ProfileManager.CurrentProfile.TreeReplaceType,
                    ref graphic,
                    ref hue,
                    ref partial,
                    ref heightScale
                );
            }

            Vector3 hueVec = ShaderHueTranslator.GetHueVector(hue, partial, AlphaHue / 255f, circletrans: cot);

            DrawStaticAnimated(
                batcher,
                graphic,
                posX,
                posY,
                hueVec,
                ProfileManager.CurrentProfile.ShadowsEnabled
                    && ProfileManager.CurrentProfile.ShadowsStatics
                    && (isTree || ItemData.IsFoliage || StaticFilters.IsRock(graphic)),
                depth,
                ProfileManager.CurrentProfile.AnimatedWaterEffect && ItemData.IsWet,
                heightScale
            );

            if (ItemData.IsLight && !InChunkMesh)
            {
                Client.Game.GetScene<GameScene>().AddLight(this, this, posX + 22, posY + 22);
            }

            return true;
        }

        public override bool CheckMouseSelection()
        {
            if (
                !(
                    SelectedObject.Object == this
                    || FoliageIndex != -1
                        && Client.Game.GetScene<GameScene>().FoliageIndex == FoliageIndex
                )
            )
            {
                if (HouseVisibilityHelper.IsInvisibleHouseTile(this))
                {
                    return false;
                }

                ushort graphic = Graphic;

                bool isTree = StaticFilters.IsTree(graphic, out _);
                float heightScale = 1f;
                ushort hue = Hue;
                bool partial = ItemData.IsPartialHue;

                if (isTree)
                {
                    TreeReplace.TryApply(
                        ProfileManager.CurrentProfile.TreeReplaceType,
                        ref graphic,
                        ref hue,
                        ref partial,
                        ref heightScale
                    );
                }

                ref var index = ref Client.Game.UO.FileManager.Arts.File.GetValidRefEntry(graphic + 0x4000);
                graphic = (ushort)(graphic + index.AnimOffset);

                ref readonly var artInfo = ref Client.Game.UO.Arts.GetArt(graphic);

                Point position = RealScreenPosition;
                position.X -= (artInfo.UV.Width >> 1) - 22;
                position.Y -= artInfo.UV.Height - 44;

                int mouseX = SelectedObject.TranslatedMousePositionByViewport.X - position.X;
                int mouseY = SelectedObject.TranslatedMousePositionByViewport.Y - position.Y;

                if (heightScale != 1f)
                {
                    int artHeight = artInfo.UV.Height;
                    int visibleHeight = (int)(artHeight * heightScale);
                    mouseY -= artHeight - visibleHeight;

                    if (mouseX < 0 || mouseY < 0 || mouseY >= visibleHeight)
                    {
                        return false;
                    }

                    mouseY = (int)(mouseY / heightScale);
                }

                return Client.Game.UO.Arts.PixelCheck(graphic, mouseX, mouseY);
            }

            return false;
        }
    }
}
