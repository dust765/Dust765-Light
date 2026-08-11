// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Scenes;
using ClassicUO.Network;

namespace ClassicUO.Dust765
{
    internal static class HouseContentVisibilityHelper
    {
        private struct HouseBounds
        {
            public uint Serial;
            public int MinX;
            public int MaxX;
            public int MinY;
            public int MaxY;
        }

        private static readonly List<HouseBounds> _nearbyHouses = new List<HouseBounds>(16);
        private static bool _showHouseContentEnabled;
        private static uint _playerHouseSerial;
        private static bool? _lastPacketState;

        public static bool IsPlayerInsideHouse => _playerHouseSerial != 0;

        public static uint PlayerHouseSerial => _playerHouseSerial;

        public static void PrepareFrame(World world, Profile profile)
        {
            _showHouseContentEnabled = profile != null && profile.ShowHouseContent;
            _playerHouseSerial = 0;

            if (world?.Player == null)
            {
                return;
            }

            foreach (House house in world.HouseManager.Houses)
            {
                if (world.HouseManager.EntityIntoHouse(house.Serial, world.Player))
                {
                    _playerHouseSerial = house.Serial;
                    break;
                }
            }

            SyncShowHouseContentPacket(world, profile);
        }

        public static void BeginFrame(
            World world,
            Profile profile,
            int viewMinX,
            int viewMinY,
            int viewMaxX,
            int viewMaxY
        )
        {
            _nearbyHouses.Clear();
            _showHouseContentEnabled = profile != null && profile.ShowHouseContent;

            // Nothing can be hidden in these states, so the bounds cache is never consulted.
            if (world?.Player == null || _showHouseContentEnabled || _playerHouseSerial != 0)
            {
                return;
            }

            foreach (House house in world.HouseManager.Houses)
            {
                Item foundation = world.Items.Get(house.Serial);

                if (
                    foundation == null
                    || foundation.IsDestroyed
                    || !foundation.MultiInfo.HasValue
                )
                {
                    continue;
                }

                int minX = foundation.X + foundation.MultiInfo.Value.X;
                int maxX = foundation.X + foundation.MultiInfo.Value.Width;
                int minY = foundation.Y + foundation.MultiInfo.Value.Y;
                int maxY = foundation.Y + foundation.MultiInfo.Value.Height;

                if (maxX < viewMinX || minX > viewMaxX || maxY < viewMinY || minY > viewMaxY)
                {
                    continue;
                }

                _nearbyHouses.Add(
                    new HouseBounds
                    {
                        Serial = house.Serial,
                        MinX = minX,
                        MaxX = maxX,
                        MinY = minY,
                        MaxY = maxY
                    }
                );
            }
        }

        public static bool ShouldDrawItem(Item item)
        {
            if (_showHouseContentEnabled || _playerHouseSerial != 0)
            {
                return true;
            }

            if (item == null || item.IsDestroyed || !item.OnGround || item.IsMulti)
            {
                return true;
            }

            return GetContainingHouseSerial(item.X, item.Y) == 0;
        }

        public static void SendShowHouseContentPreference(World world, Profile profile)
        {
            if (profile == null || Client.Game.UO.Version < Utility.ClientVersion.CV_70796)
            {
                return;
            }

            PrepareFrame(world, profile);
            bool shouldSend = GetShouldSendPublicHouseContent(profile);
            _lastPacketState = shouldSend;
            NetClient.Socket.Send_ShowPublicHouseContent(shouldSend);
            RefreshSceneAfterHouseContentChange(shouldSend);
        }

        public static void ResetPacketState()
        {
            _lastPacketState = null;
        }

        private static void SyncShowHouseContentPacket(World world, Profile profile)
        {
            if (profile == null || Client.Game.UO.Version < Utility.ClientVersion.CV_70796)
            {
                return;
            }

            bool shouldSend = GetShouldSendPublicHouseContent(profile);

            if (_lastPacketState.HasValue && _lastPacketState.Value == shouldSend)
            {
                return;
            }

            _lastPacketState = shouldSend;
            NetClient.Socket.Send_ShowPublicHouseContent(shouldSend);
            RefreshSceneAfterHouseContentChange(shouldSend);
        }

        private static bool GetShouldSendPublicHouseContent(Profile profile)
        {
            if (IsPlayerInsideHouse)
            {
                return true;
            }

            return profile != null && profile.ShowHouseContent;
        }

        private static void RefreshSceneAfterHouseContentChange(bool showingContent)
        {
            GameScene scene = Client.Game.GetScene<GameScene>();

            if (scene == null)
            {
                return;
            }

            scene.UpdateDrawPosition = true;

            if (showingContent)
            {
                scene.UpdateMaxDrawZ(true);
            }
        }

        private static uint GetContainingHouseSerial(int x, int y)
        {
            for (int i = 0; i < _nearbyHouses.Count; i++)
            {
                HouseBounds bounds = _nearbyHouses[i];

                if (x >= bounds.MinX && x <= bounds.MaxX && y >= bounds.MinY && y <= bounds.MaxY)
                {
                    return bounds.Serial;
                }
            }

            return 0;
        }
    }
}
