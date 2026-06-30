// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Dust765
{
    internal static class EnemyRangeBucketDrawHelper
    {
        private const int MARKER_SIZE = 10;
        private static Texture2D _greenTex;
        private static Texture2D _yellowTex;
        private static Texture2D _redTex;

        public static void DrawMarker(UltimaBatcher2D batcher, int x, int y, EnemyRangeBucket bucket, float depth)
        {
            Texture2D tex = GetTexture(bucket);

            if (tex == null)
            {
                return;
            }

            int drawX = x - MARKER_SIZE / 2;
            int drawY = y - MARKER_SIZE / 2;
            Vector3 hue = ShaderHueTranslator.GetHueVector(0, false, 1f);

            batcher.Draw(tex, new Rectangle(drawX, drawY, MARKER_SIZE, MARKER_SIZE), hue, depth);
            batcher.DrawRectangle(tex, drawX, drawY, MARKER_SIZE, MARKER_SIZE, hue, depth);
        }

        private static Texture2D GetTexture(EnemyRangeBucket bucket)
        {
            switch (bucket)
            {
                case EnemyRangeBucket.Green:
                    return _greenTex ??= SolidColorTextureCache.GetTexture(Color.FromNonPremultiplied(80, 185, 75, 255));

                case EnemyRangeBucket.Yellow:
                    return _yellowTex ??= SolidColorTextureCache.GetTexture(Color.FromNonPremultiplied(210, 175, 55, 255));

                case EnemyRangeBucket.Red:
                    return _redTex ??= SolidColorTextureCache.GetTexture(Color.FromNonPremultiplied(200, 70, 55, 255));

                default:
                    return null;
            }
        }
    }
}
