// SPDX-License-Identifier: BSD-2-Clause

using System;
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
        private const float CENTER = SIZE / 2f;
        private const float RADIUS = 4.5f;

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
            Vector2 center = new Vector2(x + CENTER, y + CENTER);
            Vector3 hue = ShaderHueTranslator.GetHueVector(0, false, Alpha);

            renderLists.AddGumpNoAtlas(batcher =>
            {
                FillCircle(batcher, _fillTexture, center, RADIUS, hue, layerDepth);
                return true;
            });

            return true;
        }

        private static void FillCircle(UltimaBatcher2D batcher, Texture2D texture, Vector2 center, float radius, Vector3 hue, float depth)
        {
            const int segments = 16;
            float stroke = radius * 0.32f;

            for (int i = 0; i < segments; i++)
            {
                float angle = MathHelper.TwoPi * i / segments;
                Vector2 edge = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
                batcher.DrawLine(texture, center, edge, hue, stroke, depth);
            }
        }
    }
}
