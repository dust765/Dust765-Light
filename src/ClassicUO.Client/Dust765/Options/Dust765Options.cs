// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.UI.Gumps;

namespace ClassicUO.Dust765.Options
{
    internal static class Dust765Options
    {
        internal static void RegisterOptionsPage(OptionsGump gump) => gump.BuildDust765();

        internal static void ApplyProfile(OptionsGump gump) => gump.ApplyDust765Profile();
    }
}
