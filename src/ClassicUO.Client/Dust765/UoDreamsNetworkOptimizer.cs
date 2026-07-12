using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.GameObjects;
using ClassicUO.Network;
using System;

namespace ClassicUO.Dust765
{
    internal static class UoDreamsNetworkOptimizer
    {
        public const ushort StoneRoofGraphic = 0x0577;

        internal static bool IsShard(World world)
        {
            if (world == null)
            {
                return false;
            }

            if (world.ServerName.IndexOf("uodreams", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            string ip = Settings.GlobalSettings.IP;

            return !string.IsNullOrEmpty(ip)
                && ip.IndexOf("uodreams", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        internal static bool IsStoneRoofProblemItem(World world, Item item)
        {
            return item != null && IsShard(world) && item.Graphic == StoneRoofGraphic;
        }

        internal static void EnsureStubOpl(World world, uint serial, Item item)
        {
            PacketHandlers.MarkOversizedOplSerial(serial);

            if (world.OPL.TryGetRevision(serial, out _))
            {
                return;
            }

            string name = item?.Name;

            if (string.IsNullOrEmpty(name) && item != null)
            {
                name = item.ItemData.Name;
            }

            if (string.IsNullOrEmpty(name))
            {
                name = "stone roof";
            }

            world.OPL.Add(serial, 1, name, string.Empty, 0, null);
        }
    }
}
