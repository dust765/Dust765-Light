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
        public static uint GetTrackedMobileSerial(World world)
        {
            if (world?.Player == null || world.TargetManager == null)
            {
                return 0;
            }

            uint serial = world.TargetManager.LastTargetInfo.Serial;

            if (SerialHelper.IsMobile(serial) && serial != world.Player.Serial)
            {
                return serial;
            }

            serial = world.TargetManager.LastAttack;

            if (SerialHelper.IsMobile(serial) && serial != world.Player.Serial)
            {
                return serial;
            }

            serial = world.TargetManager.SelectedTarget;

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
                Mobile target = world.Mobiles.Get(GetTrackedMobileSerial(world));

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

            int weaponRange = WeaponRangeHelper.GetEquippedWeaponRange(world);
            return ClassifyDistance(target.Distance, weaponRange);
        }

        public static EnemyRangeBucket GetMobileBucket(World world, Mobile mobile)
        {
            if (!ShouldIncludeForBucketCount(world, mobile, false))
            {
                return EnemyRangeBucket.None;
            }

            int weaponRange = WeaponRangeHelper.GetEquippedWeaponRange(world);
            return ClassifyDistance(mobile.Distance, weaponRange);
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
