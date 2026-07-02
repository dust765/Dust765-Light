// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.GameObjects;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Dust765
{
    internal static class TargetRangeIndicatorHelper
    {
        public static bool ShouldDrawOnMobile(World world, Mobile mobile)
        {
            Profile profile = ProfileManager.CurrentProfile;

            if (profile == null || mobile == null || mobile.IsDestroyed || world?.Player == null)
            {
                return false;
            }

            if (profile.ShowTargetRangeIndicator)
            {
                if (ReferenceEquals(SelectedObject.Object, mobile))
                {
                    return true;
                }

                if (EnemyRangeBucketHelper.IsTrackedMobile(world, mobile.Serial))
                {
                    return true;
                }
            }

            if (
                profile.EnemyRangeIndicator
                && profile.EnemyRangeIndicator_ShowOnLastTarget
                && EnemyRangeBucketHelper.IsTrackedMobile(world, mobile.Serial)
            )
            {
                return true;
            }

            return false;
        }

        public static void DrawMobileDistance(
            UltimaBatcher2D batcher,
            Mobile mobile,
            int drawX,
            int drawY,
            float depth
        )
        {
            if (mobile == null)
            {
                return;
            }

            string dist = mobile.Distance.ToString();
            int textX = drawX - 8;
            int textY = drawY - 38;

            Vector3 shadow = new Vector3(0, 1, 1f);
            batcher.DrawString(Fonts.Bold, dist, textX - 1, textY - 1, shadow, depth);
            batcher.DrawString(Fonts.Bold, dist, textX, textY, Vector3.One, depth);
        }
    }
}
