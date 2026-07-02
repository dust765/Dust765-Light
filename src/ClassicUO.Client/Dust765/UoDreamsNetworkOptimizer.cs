using ClassicUO.Configuration;

using ClassicUO.Game;

using ClassicUO.Game.GameObjects;

using ClassicUO.Network;

using System;

using System.Collections.Generic;



namespace ClassicUO.Dust765

{

    internal static class UoDreamsNetworkOptimizer

    {

        public const ushort StoneRoofGraphic = 0x0577;



        private static readonly Dictionary<uint, uint> _pendingHouseLoads = new Dictionary<uint, uint>();

        private const uint HouseLoadPendingTimeoutMs = 8000;



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



        internal static bool IsHouseLoadPending(uint serial)

        {

            return serial != 0 && _pendingHouseLoads.ContainsKey(serial);

        }



        internal static bool HasPendingHouseLoads()

        {

            if (_pendingHouseLoads.Count == 0)

            {

                return false;

            }



            uint now = Time.Ticks;

            uint[] serials = new uint[_pendingHouseLoads.Count];

            _pendingHouseLoads.Keys.CopyTo(serials, 0);



            for (int i = 0; i < serials.Length; i++)

            {

                if (now - _pendingHouseLoads[serials[i]] > HouseLoadPendingTimeoutMs)

                {

                    _pendingHouseLoads.Remove(serials[i]);

                }

            }



            return _pendingHouseLoads.Count > 0;

        }



        internal static void MarkHouseLoadPending(uint serial)

        {

            if (serial == 0)

            {

                return;

            }



            _pendingHouseLoads[serial] = Time.Ticks;

        }



        internal static void ClearHouseLoadPending(uint serial)

        {

            _pendingHouseLoads.Remove(serial);

        }



        internal static void RequestHouseDataImmediate(World world, uint serial)

        {

            if (!IsShard(world) || serial == 0)

            {

                return;

            }



            if (IsHouseLoadPending(serial))

            {

                return;

            }



            MarkHouseLoadPending(serial);

            NetClient.Socket.Send_CustomHouseDataRequest(serial);

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


