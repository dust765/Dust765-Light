// SPDX-License-Identifier: BSD-2-Clause

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

            if (lastTargetOnly)
            {
                return mobile.Serial == world.TargetManager.LastTargetInfo.Serial;
            }

            return IsHostileNotoriety(mobile.NotorietyFlag);
        }

        public static EnemyRangeBucket ClassifyDistance(int distance, int weaponRange)
        {
            if (distance <= weaponRange)
            {
                return EnemyRangeBucket.Green;
            }

            if (distance <= 7)
            {
                return EnemyRangeBucket.Yellow;
            }

            if (distance >= 8)
            {
                return EnemyRangeBucket.Red;
            }

            return EnemyRangeBucket.None;
        }

        public static void CountBuckets(World world, bool lastTargetOnly, out int green, out int yellow, out int red)
        {
            green = 0;
            yellow = 0;
            red = 0;

            if (world?.Player == null)
            {
                return;
            }

            int weaponRange = WeaponRangeHelper.GetEquippedWeaponRange(world);

            if (lastTargetOnly)
            {
                Mobile target = world.Mobiles.Get(world.TargetManager.LastTargetInfo.Serial);

                if (!ShouldIncludeForBucketCount(world, target, true))
                {
                    return;
                }

                AddBucketCount(ClassifyDistance(target.Distance, weaponRange), ref green, ref yellow, ref red);
                return;
            }

            foreach (Mobile mobile in world.Mobiles.Values)
            {
                if (!ShouldIncludeForBucketCount(world, mobile, false))
                {
                    continue;
                }

                AddBucketCount(ClassifyDistance(mobile.Distance, weaponRange), ref green, ref yellow, ref red);
            }
        }

        public static EnemyRangeBucket GetLastTargetBucket(World world)
        {
            if (world?.Player == null)
            {
                return EnemyRangeBucket.None;
            }

            Mobile target = world.Mobiles.Get(world.TargetManager.LastTargetInfo.Serial);

            if (target == null || target == world.Player || target.IsDead)
            {
                return EnemyRangeBucket.None;
            }

            int weaponRange = WeaponRangeHelper.GetEquippedWeaponRange(world);
            return ClassifyDistance(target.Distance, weaponRange);
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
