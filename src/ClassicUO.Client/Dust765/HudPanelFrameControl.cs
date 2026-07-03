// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Dust765
{
    internal sealed class HudPanelFrameControl : Control
    {
        private static readonly Texture2D _texture = SolidColorTextureCache.GetTexture(Color.White);

        public ushort OuterHue { get; set; } = 34;

        public override bool AddToRenderLists(RenderLists renderLists, int x, int y, ref float layerDepthRef)
        {
            float layerDepth = layerDepthRef;
            Vector3 outer = ShaderHueTranslator.GetHueVector(OuterHue, false, Alpha * 0.55f);
            Vector3 inner = ShaderHueTranslator.GetHueVector(0, false, Alpha * 0.35f);

            renderLists.AddGumpNoAtlas(batcher =>
            {
                batcher.DrawRectangle(_texture, x, y, Width, Height, outer, layerDepth);
                batcher.DrawRectangle(_texture, x + 1, y + 1, Width - 2, Height - 2, inner, layerDepth);
                return true;
            });

            return true;
        }
    }
}
