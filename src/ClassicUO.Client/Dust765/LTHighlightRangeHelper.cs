// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.GameObjects;
namespace ClassicUO.Dust765
{
    internal static class LTHighlightRangeHelper
    {
        public static bool TryGetLandHighlightHue(World world, ushort tileX, ushort tileY, out ushort hue)
        {
            hue = 0;
            Profile p = ProfileManager.CurrentProfile;
            if (p == null || world.Player == null)
            {
                return false;
            }

            int d = ChebyshevFromPlayer(world, tileX, tileY);

            if (p.LTHighlightRangeOnCast && GameActions.iscasting && d == p.LTHighlightRangeOnCastRange)
            {
                hue = p.LTHighlightRangeOnCastHue;
                return true;
            }

            if (p.LTHighlightRangeOnActivated && d == p.LTHighlightRangeOnActivatedRange)
            {
                hue = p.LTHighlightRangeOnActivatedHue;
                return true;
            }

            return false;
        }

        private static int ChebyshevFromPlayer(World world, ushort x, ushort y)
        {
            int px = world.RangeSize.X;
            int py = world.RangeSize.Y;
            PlayerMobile player = world.Player;
            if (player != null && player.Steps.Count != 0)
            {
                ref Mobile.Step st = ref player.Steps.Back();
                px = st.X;
                py = st.Y;
            }

            return Math.Max(Math.Abs(x - px), Math.Abs(y - py));
        }
    }
}
