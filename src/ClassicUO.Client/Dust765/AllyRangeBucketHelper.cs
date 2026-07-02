// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;

namespace ClassicUO.Dust765
{
    internal static class AllyRangeBucketHelper
    {
        public const int DefaultGreenMaxTiles = 5;
        public const int DefaultYellowMaxTiles = 10;
        public const int DefaultRedMaxTiles = 18;
        public const int MinRangeTiles = 1;
        public const int MaxRangeTiles = 24;

        public static void NormalizeRangeTiles(Profile profile)
        {
            if (profile == null)
            {
                return;
            }

            profile.AllyRangeIndicator_GreenMaxTiles = Math.Clamp(
                profile.AllyRangeIndicator_GreenMaxTiles,
                MinRangeTiles,
                MaxRangeTiles
            );
            profile.AllyRangeIndicator_YellowMaxTiles = Math.Clamp(
                profile.AllyRangeIndicator_YellowMaxTiles,
                MinRangeTiles,
                MaxRangeTiles
            );
            profile.AllyRangeIndicator_RedMaxTiles = Math.Clamp(
                profile.AllyRangeIndicator_RedMaxTiles,
                MinRangeTiles,
                MaxRangeTiles
            );

            if (profile.AllyRangeIndicator_YellowMaxTiles < profile.AllyRangeIndicator_GreenMaxTiles)
            {
                profile.AllyRangeIndicator_YellowMaxTiles = profile.AllyRangeIndicator_GreenMaxTiles;
            }

            if (profile.AllyRangeIndicator_RedMaxTiles < profile.AllyRangeIndicator_YellowMaxTiles)
            {
                profile.AllyRangeIndicator_RedMaxTiles = profile.AllyRangeIndicator_YellowMaxTiles;
            }
        }

        public static void GetRangeThresholds(
            Profile profile,
            out int greenMax,
            out int yellowMax,
            out int redMax
        )
        {
            NormalizeRangeTiles(profile);

            greenMax = profile?.AllyRangeIndicator_GreenMaxTiles ?? DefaultGreenMaxTiles;
            yellowMax = profile?.AllyRangeIndicator_YellowMaxTiles ?? DefaultYellowMaxTiles;
            redMax = profile?.AllyRangeIndicator_RedMaxTiles ?? DefaultRedMaxTiles;

            if (yellowMax < greenMax)
            {
                yellowMax = greenMax;
            }

            if (redMax < yellowMax)
            {
                redMax = yellowMax;
            }
        }

        public static bool IsAllyMobile(World world, Mobile mobile)
        {
            if (world?.Player == null || mobile == null || mobile == world.Player || mobile.IsDead)
            {
                return false;
            }

            if (world.Party.Leader != 0 && world.Party.Contains(mobile.Serial))
            {
                return true;
            }

            return mobile.NotorietyFlag == NotorietyFlag.Ally;
        }

        public static EnemyRangeBucket ClassifyDistance(
            int distance,
            int greenMax,
            int yellowMax,
            int redMax
        )
        {
            if (distance <= greenMax)
            {
                return EnemyRangeBucket.Green;
            }

            if (distance <= yellowMax)
            {
                return EnemyRangeBucket.Yellow;
            }

            if (distance <= redMax)
            {
                return EnemyRangeBucket.Red;
            }

            return EnemyRangeBucket.None;
        }

        public static void CountBuckets(
            World world,
            out int green,
            out int yellow,
            out int red,
            out ushort greenHue,
            out ushort yellowHue,
            out ushort redHue
        )
        {
            green = 0;
            yellow = 0;
            red = 0;
            greenHue = 0;
            yellowHue = 0;
            redHue = 0;

            if (world?.Player == null)
            {
                return;
            }

            Profile profile = ProfileManager.CurrentProfile;
            GetRangeThresholds(profile, out int greenMax, out int yellowMax, out int redMax);

            foreach (Mobile mobile in world.Mobiles.Values)
            {
                if (!IsAllyMobile(world, mobile))
                {
                    continue;
                }

                ushort borderHue = GetAllyBorderHue(world, mobile);

                switch (ClassifyDistance(mobile.Distance, greenMax, yellowMax, redMax))
                {
                    case EnemyRangeBucket.Green:
                        green++;
                        if (greenHue == 0)
                        {
                            greenHue = borderHue;
                        }

                        break;

                    case EnemyRangeBucket.Yellow:
                        yellow++;
                        if (yellowHue == 0)
                        {
                            yellowHue = borderHue;
                        }

                        break;

                    case EnemyRangeBucket.Red:
                        red++;
                        if (redHue == 0)
                        {
                            redHue = borderHue;
                        }

                        break;
                }
            }
        }

        public static void CountBuckets(World world, out int green, out int yellow, out int red)
        {
            CountBuckets(world, out green, out yellow, out red, out _, out _, out _);
        }

        private static ushort GetAllyBorderHue(World world, Mobile mobile)
        {
            if (EnemyRangeBucketHelper.IsTrackedMobile(world, mobile.Serial))
            {
                return 0x0026;
            }

            NotorietyFlag flag = mobile.NotorietyFlag;

            if (flag == NotorietyFlag.Criminal || flag == NotorietyFlag.Gray)
            {
                return 0x0034;
            }

            ushort hue = Notoriety.GetHue(flag);
            return hue > 0 ? hue : (ushort)0x0034;
        }
    }
}
