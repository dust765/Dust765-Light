// SPDX-License-Identifier: BSD-2-Clause

using System;

namespace ClassicUO.Dust765
{
    internal static class BlockedCharacterLogin
    {
        private static readonly string[] Names = { "Barba Ruiva", "Monstra", "barba ruiva", "monstra" };

        internal static bool IsBlocked(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            string t = name.Trim();

            for (int i = 0; i < Names.Length; i++)
            {
                if (t.Equals(Names[i], StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
