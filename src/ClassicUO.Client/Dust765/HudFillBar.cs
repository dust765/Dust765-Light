// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Dust765
{
    internal sealed class HudFillBar : Control
    {
        private Texture2D _texture;

        public HudFillBar(int x, int y, int maxWidth, int height, uint colorPacked)
        {
            X = x;
            Y = y;
            Width = maxWidth;
            Height = height;
            SetColor(colorPacked);
        }

        public int FillWidth { get; set; }

        public void SetColor(uint colorPacked)
        {
            _texture = SolidColorTextureCache.GetTexture(new Color { PackedValue = colorPacked });
        }

        public override bool AddToRenderLists(RenderLists renderLists, int x, int y, ref float layerDepthRef)
        {
            float layerDepth = layerDepthRef;
            int w = Math.Max(0, Math.Min(FillWidth, Width));
            Vector3 hueVector = ShaderHueTranslator.GetHueVector(0, false, Alpha);

            renderLists.AddGumpNoAtlas(batcher =>
            {
                batcher.Draw(_texture, new Rectangle(x, y, w, Height), hueVector, layerDepth);
                return true;
            });

            return true;
        }
    }
}
