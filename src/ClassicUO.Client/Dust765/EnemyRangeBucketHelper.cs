// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;

namespace ClassicUO.Dust765
{
    internal enum EnemyRangeBucket
    {
        None = 0,
        Green,
        Yellow,
        Red
    }

    internal static class EnemyRangeBucketHelper
    {
        public const int DefaultYellowMaxTiles = 7;
        public const int DefaultRedMaxTiles = 24;
        public const int MinRangeTiles = 1;
        public const int MaxRangeTiles = 24;

        public static void NormalizeRangeTiles(Profile profile)
        {
            if (profile == null)
            {
                return;
            }

            profile.EnemyRangeIndicator_GreenMaxTiles = Math.Clamp(
                profile.EnemyRangeIndicator_GreenMaxTiles,
                0,
                MaxRangeTiles
            );
            profile.EnemyRangeIndicator_YellowMaxTiles = Math.Clamp(
                profile.EnemyRangeIndicator_YellowMaxTiles,
                MinRangeTiles,
                MaxRangeTiles
            );
            profile.EnemyRangeIndicator_RedMaxTiles = Math.Clamp(
                profile.EnemyRangeIndicator_RedMaxTiles,
                MinRangeTiles,
                MaxRangeTiles
            );

            if (
                profile.EnemyRangeIndicator_GreenMaxTiles > 0
                && profile.EnemyRangeIndicator_YellowMaxTiles < profile.EnemyRangeIndicator_GreenMaxTiles
            )
            {
                profile.EnemyRangeIndicator_YellowMaxTiles = profile.EnemyRangeIndicator_GreenMaxTiles;
            }

            if (profile.EnemyRangeIndicator_RedMaxTiles < profile.EnemyRangeIndicator_YellowMaxTiles)
            {
                profile.EnemyRangeIndicator_RedMaxTiles = profile.EnemyRangeIndicator_YellowMaxTiles;
            }
        }

        public static void GetRangeThresholds(
            Profile profile,
            int weaponRange,
            out int greenMax,
            out int yellowMax,
            out int redMax
        )
        {
            NormalizeRangeTiles(profile);

            int configuredGreen = profile?.EnemyRangeIndicator_GreenMaxTiles ?? 0;
            greenMax = configuredGreen > 0
                ? configuredGreen
                : Math.Max(weaponRange, MinRangeTiles);
            yellowMax = profile?.EnemyRangeIndicator_YellowMaxTiles ?? DefaultYellowMaxTiles;
            redMax = profile?.EnemyRangeIndicator_RedMaxTiles ?? DefaultRedMaxTiles;

            if (yellowMax < greenMax)
            {
                yellowMax = greenMax;
            }

            if (redMax < yellowMax)
            {
                redMax = yellowMax;
            }
        }

        public static uint GetTrackedMobileSerial(World world)
        {
            if (world?.Player == null || world.TargetManager == null)
            {
                return 0;
            }

            uint serial = world.TargetManager.NewTargetSystemSerial;

            if (SerialHelper.IsMobile(serial) && serial != world.Player.Serial)
            {
                return serial;
            }

            serial = world.TargetManager.LastTargetInfo.Serial;

            if (SerialHelper.IsMobile(serial) && serial != world.Player.Serial)
            {
                return serial;
            }

            serial = world.TargetManager.SelectedTarget;

            if (SerialHelper.IsMobile(serial) && serial != world.Player.Serial)
            {
                return serial;
            }

            serial = world.TargetManager.LastAttack;

            if (SerialHelper.IsMobile(serial) && serial != world.Player.Serial)
            {
                return serial;
            }

            return 0;
        }

        public static bool IsTrackedMobile(World world, uint mobileSerial)
        {
            uint tracked = GetTrackedMobileSerial(world);
            return tracked != 0 && mobileSerial == tracked;
        }

        public static bool IsHostileNotoriety(NotorietyFlag flag)
        {
            switch (flag)
            {
                case NotorietyFlag.Innocent:
                case NotorietyFlag.Ally:
                case NotorietyFlag.Invulnerable:
                    return false;
            }

            return true;
        }

        public static bool ShouldIncludeForBucketCount(World world, Mobile mobile, bool lastTargetOnly)
        {
            if (world?.Player == null || mobile == null || mobile == world.Player || mobile.IsDead)
            {
                return false;
            }

            if (world.Party.Contains(mobile.Serial))
            {
                return false;
            }

            bool isLastTarget = IsTrackedMobile(world, mobile.Serial);

            if (lastTargetOnly)
            {
                return isLastTarget;
            }

            if (isLastTarget)
            {
                return true;
            }

            return IsHostileNotoriety(mobile.NotorietyFlag);
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

        public static void CountBuckets(World world, out int green, out int yellow, out int red)
        {
            green = 0;
            yellow = 0;
            red = 0;

            if (world?.Player == null)
            {
                return;
            }

            Profile profile = ProfileManager.CurrentProfile;
            int weaponRange = WeaponRangeHelper.GetEquippedWeaponRange(world);
            GetRangeThresholds(profile, weaponRange, out int greenMax, out int yellowMax, out int redMax);
            bool lastTargetOnly = profile?.EnemyRangeIndicator_LastTargetOnly ?? false;

            foreach (Mobile mobile in world.Mobiles.Values)
            {
                if (!ShouldIncludeForBucketCount(world, mobile, lastTargetOnly))
                {
                    continue;
                }

                AddBucketCount(
                    ClassifyDistance(mobile.Distance, greenMax, yellowMax, redMax),
                    ref green,
                    ref yellow,
                    ref red
                );
            }
        }

        public static EnemyRangeBucket GetLastTargetBucket(World world)
        {
            if (world?.Player == null)
            {
                return EnemyRangeBucket.None;
            }

            uint serial = GetTrackedMobileSerial(world);

            if (serial == 0)
            {
                return EnemyRangeBucket.None;
            }

            Mobile target = world.Mobiles.Get(serial);

            if (target == null || target.IsDead)
            {
                return EnemyRangeBucket.None;
            }

            Profile profile = ProfileManager.CurrentProfile;
            int weaponRange = WeaponRangeHelper.GetEquippedWeaponRange(world);
            GetRangeThresholds(profile, weaponRange, out int greenMax, out int yellowMax, out int redMax);
            return ClassifyDistance(target.Distance, greenMax, yellowMax, redMax);
        }

        public static EnemyRangeBucket GetMobileBucket(World world, Mobile mobile)
        {
            if (!ShouldIncludeForBucketCount(world, mobile, false))
            {
                return EnemyRangeBucket.None;
            }

            Profile profile = ProfileManager.CurrentProfile;
            int weaponRange = WeaponRangeHelper.GetEquippedWeaponRange(world);
            GetRangeThresholds(profile, weaponRange, out int greenMax, out int yellowMax, out int redMax);
            return ClassifyDistance(mobile.Distance, greenMax, yellowMax, redMax);
        }

        private static void AddBucketCount(EnemyRangeBucket bucket, ref int green, ref int yellow, ref int red)
        {
            switch (bucket)
            {
                case EnemyRangeBucket.Green:
                    green++;
                    break;

                case EnemyRangeBucket.Yellow:
                    yellow++;
                    break;

                case EnemyRangeBucket.Red:
                    red++;
                    break;
            }
        }
    }
}
