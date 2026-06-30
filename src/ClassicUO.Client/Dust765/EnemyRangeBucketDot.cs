// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Dust765
{
    internal sealed class EnemyRangeBucketDot : Control
    {
        private const int SIZE = 12;

        private Texture2D _fillTexture;

        public EnemyRangeBucketDot()
        {
            Width = SIZE;
            Height = SIZE;
            AcceptMouseInput = false;
            IsVisible = false;
        }

        public void SetBucket(EnemyRangeBucket bucket)
        {
            if (bucket == EnemyRangeBucket.None)
            {
                IsVisible = false;
                return;
            }

            Color color = bucket switch
            {
                EnemyRangeBucket.Green => Color.FromNonPremultiplied(80, 185, 75, 255),
                EnemyRangeBucket.Yellow => Color.FromNonPremultiplied(210, 175, 55, 255),
                EnemyRangeBucket.Red => Color.FromNonPremultiplied(200, 70, 55, 255),
                _ => Color.Transparent
            };

            _fillTexture = SolidColorTextureCache.GetTexture(color);
            IsVisible = true;
        }

        public override bool AddToRenderLists(RenderLists renderLists, int x, int y, ref float layerDepthRef)
        {
            if (!IsVisible || _fillTexture == null)
            {
                return false;
            }

            float layerDepth = layerDepthRef;
            Vector3 hue = ShaderHueTranslator.GetHueVector(0, false, Alpha);

            renderLists.AddGumpNoAtlas(batcher =>
            {
                batcher.Draw(_fillTexture, new Rectangle(x, y, SIZE, SIZE), hue, layerDepth);
                batcher.DrawRectangle(_fillTexture, x, y, SIZE, SIZE, hue, layerDepth);
                return true;
            });

            return true;
        }
    }
}
