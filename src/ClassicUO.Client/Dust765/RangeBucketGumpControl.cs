// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Assets;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Dust765
{
    internal sealed class RangeBucketGumpControl : Control
    {
        private const float IDLE_ALPHA = 0.50f;
        private const float ACTIVE_ALPHA = 1.0f;

        private readonly Texture2D _fillTexture;
        private readonly Texture2D _ringTexture;
        private readonly int _bucketSize;
        private readonly ushort _countHue;
        private int _count;
        private string _countText = string.Empty;
        private ushort _borderHue;
        private bool _useBorderHue;

        public RangeBucketGumpControl(Color fillColor, ushort countHue, int bucketSize)
        {
            _bucketSize = bucketSize;
            _countHue = countHue;
            Width = bucketSize;
            Height = bucketSize;
            AcceptMouseInput = false;

            _fillTexture = SolidColorTextureCache.GetTexture(fillColor);
            _ringTexture = SolidColorTextureCache.GetTexture(fillColor);
        }

        public void SetCount(int count)
        {
            _count = count;
            _countText = count > 0 ? (count > 99 ? "99" : count.ToString()) : string.Empty;
        }

        public void SetBorderHue(ushort hue)
        {
            _borderHue = hue;
            _useBorderHue = hue > 0;
        }

        public override bool AddToRenderLists(RenderLists renderLists, int x, int y, ref float layerDepthRef)
        {
            float layerDepth = layerDepthRef;
            bool active = _count > 0;
            float alpha = active ? ACTIVE_ALPHA : IDLE_ALPHA;
            Vector3 fillHue = ShaderHueTranslator.GetHueVector(0, false, Alpha * alpha);
            Vector3 ringHue = _useBorderHue && active
                ? ShaderHueTranslator.GetHueVector(_borderHue, false, Alpha * alpha)
                : ShaderHueTranslator.GetHueVector(0, false, Alpha * (active ? alpha : alpha * 0.85f));

            renderLists.AddGumpNoAtlas(batcher =>
            {
                int inner = active ? 14 : 10;
                int ix = x + (_bucketSize - inner) / 2;
                int iy = y + (_bucketSize - inner) / 2;
                batcher.Draw(_fillTexture, new Rectangle(ix, iy, inner, inner), fillHue, layerDepth);

                if (_useBorderHue && active)
                {
                    batcher.DrawRectangle(_ringTexture, x + 1, y + 1, _bucketSize - 2, _bucketSize - 2, ringHue, layerDepth);
                    batcher.DrawRectangle(_ringTexture, x, y, _bucketSize, _bucketSize, ringHue, layerDepth);
                }
                else
                {
                    batcher.DrawRectangle(_ringTexture, x + 2, y + 2, _bucketSize - 4, _bucketSize - 4, ringHue, layerDepth);

                    if (active)
                    {
                        batcher.DrawRectangle(_ringTexture, x, y, _bucketSize, _bucketSize, ringHue, layerDepth);
                    }
                }

                if (active && _countText.Length > 0)
                {
                    Vector3 textHue = ShaderHueTranslator.GetHueVector(_countHue, false, Alpha);
                    Vector3 textShadow = new Vector3(0, 1, Alpha);
                    int textX = x + (_bucketSize - 8) / 2;
                    int textY = y + 6;
                    batcher.DrawString(Fonts.Bold, _countText, textX - 1, textY, textShadow, layerDepth);
                    batcher.DrawString(Fonts.Bold, _countText, textX, textY, textHue, layerDepth);
                }

                return true;
            });

            return true;
        }
    }
}
