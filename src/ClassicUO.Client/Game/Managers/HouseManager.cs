// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Map;
using ClassicUO.Game.Scenes;
using ClassicUO.Network;

namespace ClassicUO.Game.Managers
{
    internal sealed class HouseManager
    {
        private readonly Dictionary<uint, House> _houses = new Dictionary<uint, House>();
        private readonly HashSet<uint> _housesNeedingRelink = new HashSet<uint>();
        private readonly Dictionary<uint, int> _relinkAttempts = new Dictionary<uint, int>();
        private readonly World _world;
        private uint _nextHouseWallsCheckAt;
        private uint _nextRelinkAt;
        private const uint HouseWallsCheckIntervalMs = 500;
        private const uint RelinkIntervalMs = 250;
        private const int MaxRelinkAttempts = 12;

        public HouseManager(World world)
        {
            _world = world;
        }

        public IReadOnlyCollection<House> Houses => _houses.Values;

        public void Add(uint serial, House revision)
        {
            _houses[serial] = revision;
        }

        public bool TryGetHouse(uint serial, out House house)
        {
            return _houses.TryGetValue(serial, out house);
        }

        public bool TryToRemove(uint serial, int distance)
        {
            if (!IsHouseInRange(serial, distance))
            {
                if (_houses.TryGetValue(serial, out House house))
                {
                    house.ClearComponents();
                    _houses.Remove(serial);
                }


                return true;
            }

            return false;
        }

        public bool IsHouseInRange(uint serial, int distance)
        {
            if (TryGetHouse(serial, out _))
            {
                int currX = _world.Player != null ? _world.Player.X : _world.RangeSize.X;
                int currY = _world.Player != null ? _world.Player.Y : _world.RangeSize.Y;

                Item found = _world.Items.Get(serial);

                if (found == null)
                {
                    return true;
                }

                distance += found.MultiDistanceBonus;

                return Math.Abs(found.X - currX) <= distance && Math.Abs(found.Y - currY) <= distance;
            }

            return false;
        }

        public void UpdateHouseMaintenance()
        {
            if (_housesNeedingRelink.Count > 0)
            {
                if (Time.Ticks >= _nextRelinkAt)
                {
                    _nextRelinkAt = Time.Ticks + RelinkIntervalMs;
                    ProcessPendingRelinks();
                }
            }
            else if (Time.Ticks >= _nextHouseWallsCheckAt)
            {
                _nextHouseWallsCheckAt = Time.Ticks + HouseWallsCheckIntervalMs;
                EnsurePlayerHouseWalls();
            }
        }

        public void EnsurePlayerHouseWalls()
        {
            if (_world.Player == null || _world.Map == null)
            {
                return;
            }

            foreach (Item foundation in _world.Items.Values)
            {
                if (foundation == null || foundation.IsDestroyed || !foundation.IsMulti || !foundation.OnGround)
                {
                    continue;
                }

                if (!EntityIntoHouse(foundation.Serial, _world.Player))
                {
                    continue;
                }

                TryGetHouse(foundation.Serial, out House house);

                int customCount = 0;

                if (house != null)
                {
                    for (int i = 0; i < house.Components.Count; i++)
                    {
                        Multi component = house.Components[i];

                        if (component.IsCustom && !component.IsDestroyed)
                        {
                            customCount++;
                        }
                    }
                }

                if (customCount == 0)
                {
                    PacketHandlers.RequestCustomHouseData(_world, foundation.Serial);

                    continue;
                }

                _world.Map.EnsureChunksLoadedForHouse(foundation, maxPerCall: 12);

                if (house != null && HouseNeedsRelink(house))
                {
                    house.RelinkComponentsToTiles();
                    ScheduleRelink(foundation.Serial);
                }
            }
        }

        public void ScheduleRelink(uint serial)
        {
            if (serial != 0 && _housesNeedingRelink.Add(serial))
            {
                _relinkAttempts[serial] = 0;
            }
        }

        public void RelinkCustomHousesNearPlayer()
        {
            if (_world.Player == null || _world.Map == null)
            {
                return;
            }

            bool relinked = false;

            foreach (House house in _houses.Values)
            {
                if (!house.IsCustom)
                {
                    continue;
                }

                Item foundation = _world.Items.Get(house.Serial);

                if (foundation == null)
                {
                    continue;
                }

                if (
                    !EntityIntoHouse(house.Serial, _world.Player)
                    && !IsHouseInRange(house.Serial, _world.ClientViewRange)
                )
                {
                    continue;
                }

                if (!HouseNeedsRelink(house))
                {
                    continue;
                }

                _world.Map.EnsureChunksLoadedForHouse(foundation, maxPerCall: 12);
                house.RelinkComponentsToTiles();
                relinked = true;
            }

            if (relinked)
            {
                InvalidateSceneDrawState();
            }
        }

        private static void InvalidateSceneDrawState()
        {
            GameScene scene = Client.Game.GetScene<GameScene>();

            if (scene != null)
            {
                scene.UpdateMaxDrawZ(true);
                scene.UpdateDrawPosition = true;
            }
        }

        public void ProcessPendingRelinks()
        {
            if (_housesNeedingRelink.Count == 0)
            {
                RelinkCustomHousesNearPlayer();

                return;
            }

            if (_world.Player == null || _world.Map == null)
            {
                return;
            }

            uint[] pending = new uint[_housesNeedingRelink.Count];
            _housesNeedingRelink.CopyTo(pending);
            bool relinked = false;

            for (int i = 0; i < pending.Length; i++)
            {
                uint serial = pending[i];

                if (!TryGetHouse(serial, out House house) || !house.IsCustom)
                {
                    ClearPendingRelink(serial);

                    continue;
                }

                Item foundation = _world.Items.Get(serial);

                if (foundation == null)
                {
                    ClearPendingRelink(serial);

                    continue;
                }

                _world.Map.EnsureChunksLoadedForHouse(foundation, maxPerCall: 12);
                house.RelinkComponentsToTiles();
                relinked = true;

                _relinkAttempts.TryGetValue(serial, out int attempts);
                attempts++;

                // A house whose components never land on a loaded chunk would otherwise stay
                // pending forever, re-linking every tick.
                if (HouseNeedsRelink(house) && attempts < MaxRelinkAttempts)
                {
                    _relinkAttempts[serial] = attempts;

                    continue;
                }

                ClearPendingRelink(serial);
            }

            if (relinked)
            {
                InvalidateSceneDrawState();
            }

            RelinkCustomHousesNearPlayer();
        }

        private void ClearPendingRelink(uint serial)
        {
            _housesNeedingRelink.Remove(serial);
            _relinkAttempts.Remove(serial);
        }

        private bool HouseNeedsRelink(House house)
        {
            for (int i = 0; i < house.Components.Count; i++)
            {
                Multi component = house.Components[i];

                if (component.IsDestroyed || !component.IsCustom)
                {
                    continue;
                }

                if (ComponentNeedsRelink(component))
                {
                    return true;
                }
            }

            return false;
        }

        private bool ComponentNeedsRelink(Multi component)
        {
            Chunk chunk = _world.Map?.GetChunk(component.X, component.Y, false);

            if (chunk == null)
            {
                return true;
            }

            int cellX = component.X % 8;
            int cellY = component.Y % 8;

            for (GameObject obj = chunk.GetHeadObject(cellX, cellY); obj != null; obj = obj.TNext)
            {
                if (ReferenceEquals(obj, component))
                {
                    return false;
                }
            }

            return true;
        }

        public bool EntityIntoHouse(uint house, GameObject obj)
        {
            if (obj != null && TryGetHouse(house, out _))
            {
                Item found = _world.Items.Get(house);

                if (found == null || !found.MultiInfo.HasValue)
                {
                    return true;
                }

                int minX = found.X + found.MultiInfo.Value.X;
                int maxX = found.X + found.MultiInfo.Value.Width;
                int minY = found.Y + found.MultiInfo.Value.Y;
                int maxY = found.Y + found.MultiInfo.Value.Height;

                return obj.X >= minX && obj.X <= maxX && obj.Y >= minY && obj.Y <= maxY;
            }

            return false;
        }

        public void Remove(uint serial)
        {
            if (TryGetHouse(serial, out House house))
            {
                house.ClearComponents();
                _houses.Remove(serial);
            }
        }

        public void RemoveMultiTargetHouse()
        {
            if (_houses.TryGetValue(0, out House house))
            {
                house.ClearComponents();
                _houses.Remove(0);
            }
        }

        public bool Exists(uint serial)
        {
            return _houses.ContainsKey(serial);
        }

        public void Clear()
        {
            foreach (KeyValuePair<uint, House> house in _houses)
            {
                house.Value.ClearComponents();
            }

            _houses.Clear();
        }
    }
}
