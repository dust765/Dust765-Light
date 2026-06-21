// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Map;
using Microsoft.Xna.Framework;

namespace ClassicUO.Dust765
{
    internal static class HouseVisibilityHelper
    {
        private static World _world;
        private static bool _invisibleEnabled;
        private static bool _transparentEnabled;
        private static int _playerZ = int.MinValue;
        private static int _invisibleZ;
        private static int _transparentZ;
        private static float _transparentAlpha;
        private static int _dontRemoveBelowZ;
        private static int _cacheX = int.MinValue;
        private static int _cacheY;
        private static int _cacheGroundZ;

        public static bool IsFilterActive => _invisibleEnabled || _transparentEnabled;

        public static void BeginFrame(World world, Profile profile)
        {
            _world = world;
            _cacheX = int.MinValue;

            if (profile == null || world?.Player == null)
            {
                _invisibleEnabled = false;
                _transparentEnabled = false;
                _playerZ = int.MinValue;
                return;
            }

            _invisibleEnabled = profile.InvisibleHousesEnabled;
            _transparentEnabled = profile.TransparentHousesEnabled;
            _playerZ = world.Player.Z;
            _invisibleZ = profile.InvisibleHousesZ;
            _transparentZ = profile.TransparentHousesZ;
            _transparentAlpha = profile.TransparentHousesTransparency / 10f;
            _dontRemoveBelowZ = profile.DontRemoveHouseBelowZ;
        }

        public static uint PackFilterState(Profile profile)
        {
            if (profile == null)
            {
                return 0;
            }

            return (uint)(profile.InvisibleHousesEnabled ? 1 : 0)
                | (uint)(profile.TransparentHousesEnabled ? 2 : 0)
                | (uint)((profile.InvisibleHousesZ & 0xFF) << 2)
                | (uint)((profile.TransparentHousesZ & 0xFF) << 10)
                | (uint)((profile.TransparentHousesTransparency & 0xF) << 18)
                | (uint)((profile.DontRemoveHouseBelowZ & 0xFF) << 22);
        }

        public static bool IsInvisibleHouseTile(GameObject obj, Chunk chunk = null)
        {
            EnsureFrame();

            if (!_invisibleEnabled || _playerZ == int.MinValue || obj is Mobile)
            {
                return false;
            }

            if ((obj.Z - _playerZ) <= _invisibleZ)
            {
                return false;
            }

            int groundZ = GetGroundZ(obj.X, obj.Y, chunk);
            return (obj.Z - groundZ) > _dontRemoveBelowZ;
        }

        public static bool TryApplyTransparentHouseAlpha(ref Vector3 hueVec, GameObject obj, Chunk chunk = null)
        {
            EnsureFrame();

            if (!_transparentEnabled || _playerZ == int.MinValue)
            {
                return false;
            }

            if ((obj.Z - _playerZ) <= _transparentZ)
            {
                return false;
            }

            int groundZ = GetGroundZ(obj.X, obj.Y, chunk);
            if ((obj.Z - groundZ) <= _dontRemoveBelowZ)
            {
                return false;
            }

            hueVec.Z = _transparentAlpha;
            return true;
        }

        private static void EnsureFrame()
        {
            if (_playerZ != int.MinValue)
            {
                return;
            }

            Profile profile = ProfileManager.CurrentProfile;
            World world = Client.Game?.UO?.World;

            if (profile != null && world != null)
            {
                BeginFrame(world, profile);
            }
        }

        private static int GetGroundZ(int x, int y, Chunk chunk)
        {
            if (chunk != null)
            {
                int lx = x & 7;
                int ly = y & 7;

                for (GameObject tile = chunk.GetHeadObject(lx, ly); tile != null; tile = tile.TNext)
                {
                    if (tile is Land land)
                    {
                        return land.Z;
                    }
                }

                return 0;
            }

            if (_cacheX == x && _cacheY == y)
            {
                return _cacheGroundZ;
            }

            _cacheX = x;
            _cacheY = y;
            GameObject mapTile = _world?.Map?.GetTile(x, y);
            _cacheGroundZ = mapTile?.Z ?? 0;
            return _cacheGroundZ;
        }
    }
}
