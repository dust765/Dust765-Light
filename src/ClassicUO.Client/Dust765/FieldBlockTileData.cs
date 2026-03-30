// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Assets;
using ClassicUO;

namespace ClassicUO.Dust765
{
    internal static class FieldBlockTileData
    {
        public static void SetImpassable(ushort graphic, bool impassable)
        {
            ref StaticTiles[] tiles = ref Client.Game.UO.FileManager.TileData.StaticData;
            if (graphic >= tiles.Length)
            {
                return;
            }

            ref StaticTiles st = ref tiles[graphic];
            if (impassable)
            {
                st.Flags |= TileFlag.Impassable;
            }
            else
            {
                st.Flags &= ~TileFlag.Impassable;
            }
        }
    }
}
