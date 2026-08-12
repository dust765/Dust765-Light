// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.GameObjects;

namespace ClassicUO.Game.Data
{
    internal static class LayerOrder
    {
        public const ushort ParrotEpauletsGraphic = 0xA2CB;

        public static bool IsParrotEpaulets(Item item) =>
            item != null
            && ((ushort)(item.Graphic & 0xFFFF) == ParrotEpauletsGraphic
                || (ushort)(item.DisplayedGraphic & 0xFFFF) == ParrotEpauletsGraphic);

        public static readonly Layer[] ParrotLayers =
        {
            Layer.Shirt,
            Layer.Pants,
            Layer.Shoes,
            Layer.Legs,
            Layer.Arms,
            Layer.Torso,
            Layer.Tunic,
            Layer.Ring,
            Layer.Bracelet,
            Layer.Face,
            Layer.Gloves,
            Layer.Skirt,
            Layer.Robe,
            Layer.Cloak,
            Layer.Necklace,
            Layer.Hair,
            Layer.Beard,
            Layer.Earrings,
            Layer.Helmet,
            Layer.OneHanded,
            Layer.TwoHanded,
            Layer.Talisman,
            Layer.Waist
        };

        public static readonly Layer[] QuiverLayers =
        {
            Layer.Shirt,
            Layer.Pants,
            Layer.Shoes,
            Layer.Legs,
            Layer.Arms,
            Layer.Torso,
            Layer.Tunic,
            Layer.Ring,
            Layer.Bracelet,
            Layer.Face,
            Layer.Gloves,
            Layer.Skirt,
            Layer.Robe,
            Layer.Cloak,
            Layer.Necklace,
            Layer.Hair,
            Layer.Beard,
            Layer.Earrings,
            Layer.Helmet,
            Layer.OneHanded,
            Layer.TwoHanded,
            Layer.Talisman,
            Layer.Waist
        };
    }
}
