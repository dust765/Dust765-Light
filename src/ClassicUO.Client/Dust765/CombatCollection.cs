// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;

namespace ClassicUO.Dust765
{
    internal static class CombatCollection
    {
        // ------- Hue constants -------
        private const ushort BRIGHT_WHITE_COLOR    = 0x080A;
        private const ushort BRIGHT_PINK_COLOR     = 0x0503;
        private const ushort BRIGHT_ICE_COLOR      = 0x0480;
        private const ushort BRIGHT_FIRE_COLOR     = 0x0496;
        private const ushort BRIGHT_POISON_COLOR   = 0x0A0B;
        private const ushort BRIGHT_PARALYZE_COLOR = 0x0A13;

        // ------- Stealth art detection -------

        /// <summary>Returns true when <paramref name="graphic"/> is one of the
        /// four stealth-walk animation frames (0x1E03–0x1E06).</summary>
        public static bool IsStealthArt(ushort graphic)
        {
            return graphic >= 0x1E03 && graphic <= 0x1E06;
        }

        /// <summary>Returns the hue that should be applied to a stealth-walk
        /// art graphic according to the current profile settings.</summary>
        public static ushort StealthHue(ushort hue)
        {
            Profile profile = ProfileManager.CurrentProfile;
            if (profile == null) return hue;

            if (profile.ColorStealth)
                hue = profile.StealthHue;

            switch (profile.StealthNeonType)
            {
                case 1: return BRIGHT_WHITE_COLOR;
                case 2: return BRIGHT_PINK_COLOR;
                case 3: return BRIGHT_ICE_COLOR;
                case 4: return BRIGHT_FIRE_COLOR;
            }

            return hue;
        }

        // ------- Last-target hue -------

        /// <summary>Returns the hue override to apply to the last-target mobile.
        /// Status conditions (poisoned / paralyzed) override the base hue.</summary>
        public static ushort LastTargetHue(Mobile mobile, ushort hue)
        {
            Profile profile = ProfileManager.CurrentProfile;
            if (profile == null) return hue;

            if (profile.HighlightLastTargetType == 0)
            {
                return Notoriety.GetHue(mobile.NotorietyFlag);
            }
            


            hue = ApplyNeonOrCustomHue(
                hue,
                profile.HighlightLastTargetType,
                profile.HighlightLastTargetTypeHue);

            if (mobile.IsPoisoned)
            {
                hue = profile.HighlightLastTargetTypePoison == 6
                    ? BRIGHT_POISON_COLOR
                    : ApplyNeonOrCustomHue(hue, profile.HighlightLastTargetTypePoison,
                          profile.HighlightLastTargetTypePoisonHue);
            }

            if (mobile.IsParalyzed && mobile.NotorietyFlag != NotorietyFlag.Invulnerable)
            {
                hue = profile.HighlightLastTargetTypePara == 6
                    ? BRIGHT_PARALYZE_COLOR
                    : ApplyNeonOrCustomHue(hue, profile.HighlightLastTargetTypePara,
                          profile.HighlightLastTargetTypeParaHue);
            }

            return hue;
        }

        // ------- Preview Fields -------

        /// <summary>Returns true when <paramref name="mobile"/> would be covered
        /// by a field spell currently on the cursor.
        /// Spell indexes: 24=Wall of Stone, 28=Fire Field, 39=Energy Field,
        /// 47=Poison Field, 50=Paralyze Field.</summary>
        public static bool MobileFieldPreview(World world, Mobile mobile)
        {
            if (!TryGetFieldPreviewState(world, out int targetX, out int targetY, out bool fieldEastToWest))
            {
                return false;
            }

            if (fieldEastToWest)
            {
                return mobile.Y == targetY &&
                       mobile.X >= targetX - 2 && mobile.X <= targetX + 2;
            }

            return mobile.X == targetX &&
                   mobile.Y >= targetY - 2 && mobile.Y <= targetY + 2;
        }

        public static bool ObjectFieldPreview(World world, GameObject gameObject)
        {
            if (!TryGetFieldPreviewState(world, out int targetX, out int targetY, out bool fieldEastToWest))
            {
                return false;
            }

            if (fieldEastToWest)
            {
                return gameObject.Y == targetY &&
                       gameObject.X >= targetX - 2 && gameObject.X <= targetX + 2;
            }

            return gameObject.X == targetX &&
                   gameObject.Y >= targetY - 2 && gameObject.Y <= targetY + 2;
        }

        private static bool TryGetFieldPreviewState(World world, out int targetX, out int targetY, out bool fieldEastToWest)
        {
            targetX = 0;
            targetY = 0;
            fieldEastToWest = false;

            if (world == null || !world.TargetManager.IsTargeting)
            {
                return false;
            }

            int spellIndex = GameActions.LastSpellIndex;
            if (spellIndex != 24 && spellIndex != 28 &&
                spellIndex != 39 && spellIndex != 47 && spellIndex != 50)
            {
                return false;
            }

            if (SelectedObject.Object is not GameObject target)
            {
                return false;
            }

            targetX = target.X;
            targetY = target.Y;
            int dx = targetX - world.Player.X;
            int dy = targetY - world.Player.Y;
            fieldEastToWest = Math.Abs(dx) <= Math.Abs(dy);

            return true;
        }

        // ------- Private helpers -------

        private static ushort ApplyNeonOrCustomHue(ushort hue, int neonType, ushort customHue)
        {
            return neonType switch
            {
                1 => BRIGHT_WHITE_COLOR,
                2 => BRIGHT_PINK_COLOR,
                3 => BRIGHT_ICE_COLOR,
                4 => BRIGHT_FIRE_COLOR,
                5 => customHue,
                _ => hue  // 0 = off, keep existing hue
            };
        }
    }
}
