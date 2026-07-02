// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Game.Data
{
    internal static class TreeReplace
    {
        public static bool TryApply(int replaceType, ref ushort graphic, ref ushort hue, ref bool partial, ref float heightScale)
        {
            switch (replaceType)
            {
                case 1:
                    graphic = Constants.TREE_REPLACE_GRAPHIC;
                    return true;

                case 2:
                    graphic = Constants.TREE_REPLACE_GRAPHIC_TILE;
                    hue = Constants.TREE_REPLACE_BLOCK_HUE;
                    partial = false;
                    heightScale = Constants.TREE_REPLACE_BLOCK_HEIGHT_SCALE;
                    return true;

                case 3:
                    graphic = Constants.TREE_REPLACE_STUMP_BROWN;
                    return true;

                case 4:
                    graphic = Constants.TREE_REPLACE_STUMP_WALL_WHITE;
                    return true;

                default:
                    return false;
            }
        }
    }
}
