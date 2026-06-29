// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Game.GameObjects;

namespace ClassicUO.Game.Data
{
    internal static class PaperdollOrder
    {
        private static readonly Layer[] T1 =
        {
            Layer.Invalid, Layer.Cloak, Layer.Shirt, Layer.Pants, Layer.Shoes, Layer.Legs,
            Layer.Torso, Layer.Tunic, Layer.Ring, Layer.Bracelet, Layer.Face, Layer.Arms,
            Layer.Gloves, Layer.Skirt, Layer.Robe, Layer.Waist, Layer.Necklace, Layer.Hair,
            Layer.Beard, Layer.Earrings, Layer.Helmet, Layer.OneHanded, Layer.TwoHanded,
            Layer.Backpack, Layer.Talisman
        };

        private static readonly Layer[] T2 =
        {
            Layer.Invalid, Layer.Cloak, Layer.Shirt, Layer.Pants, Layer.Shoes, Layer.Legs,
            Layer.Arms, Layer.Torso, Layer.Tunic, Layer.Ring, Layer.Bracelet, Layer.Face,
            Layer.Gloves, Layer.Skirt, Layer.Robe, Layer.Waist, Layer.Necklace, Layer.Hair,
            Layer.Beard, Layer.Earrings, Layer.Helmet, Layer.OneHanded, Layer.TwoHanded,
            Layer.Backpack, Layer.Talisman
        };

        private static readonly Layer[] T3 =
        {
            Layer.Invalid, Layer.Cloak, Layer.Torso, Layer.Shirt, Layer.Pants, Layer.Shoes,
            Layer.Legs, Layer.Tunic, Layer.Ring, Layer.Bracelet, Layer.Face, Layer.Arms,
            Layer.Gloves, Layer.Skirt, Layer.Robe, Layer.Waist, Layer.Necklace, Layer.Hair,
            Layer.Beard, Layer.Earrings, Layer.Helmet, Layer.OneHanded, Layer.TwoHanded,
            Layer.Backpack, Layer.Talisman
        };

        public const int N = 0x19;

        public static void Build(ReadOnlySpan<ushort> graphic, bool altTorsoTable, Span<Layer> order)
        {
            Layer[] table = T2;

            uint arms = graphic[(int)Layer.Arms];
            bool armsAlternate;
            if (arms < 0x3d0)
            {
                armsAlternate = arms == 0x3cf || arms == 0x210 || arms == 0x3b3;
            }
            else
            {
                armsAlternate = arms == 0x3dd;
            }

            if (armsAlternate)
            {
                table = T1;
            }
            else
            {
                uint torso = graphic[(int)Layer.Torso];
                if (torso == 0x21a) table = T1;
                else if (torso - 0x399 < 5 && altTorsoTable) table = T3;
            }

            table.AsSpan(0, N).CopyTo(order);
            Span<Layer> o = order.Slice(0, N);

            if (graphic[(int)Layer.Shirt] != 0 && graphic[(int)Layer.Pants] == 0x398)
            {
                MoveTo(o, Layer.Pants, Layer.Shirt);
            }

            uint pants = graphic[(int)Layer.Pants];
            bool skipFinalPantsCheck = false;
            if (pants < 0x201)
            {
                if (pants == 0x200 || pants == 0x1eb || pants == 0x1fa)
                {
                    int iShoes = IndexOf(o, Layer.Shoes), iPants = IndexOf(o, Layer.Pants);
                    if (iShoes >= 0 && iPants >= 0 && iPants < iShoes)
                    {
                        o[iShoes] = Layer.Pants;
                        o[iPants] = Layer.Shoes;
                    }
                }
            }
            else if (pants - 0x513u < 2)
            {
                if (graphic[(int)Layer.Shoes] != 0) MoveTo(o, Layer.Pants, Layer.Shoes);
                skipFinalPantsCheck = true;
            }

            if (!skipFinalPantsCheck && graphic[(int)Layer.Shoes] != 0 && graphic[(int)Layer.Pants] == 0x3e4)
            {
                MoveTo(o, Layer.Pants, Layer.Shoes);
            }

            if (graphic[(int)Layer.Tunic] == 0x238)
            {
                MoveAfter(o, Layer.Tunic, Layer.Waist);
                if (graphic[(int)Layer.Robe] != 0)
                {
                    uint r = graphic[(int)Layer.Robe];
                    if (r == 0x4e8 || r == 0x4e9 || r == 0x4ea || r == 0x4eb ||
                        r == 0x5e2 || r == 0x5e3 || r == 0x5e4 || r == 0x5e5)
                    {
                        MoveTo(o, Layer.Robe, Layer.Necklace);
                    }
                }
            }

            uint cloak = graphic[(int)Layer.Cloak];
            if (cloak == 0x380 || cloak == 0x5f3) MoveAfter(o, Layer.Cloak, Layer.Robe);

            uint helm = graphic[(int)Layer.Helmet];
            if (helm < 0x202)
            {
                uint neck = graphic[(int)Layer.Necklace];
                if ((helm == 0x201 || helm == 0x1a9) &&
                    (neck == 0x1c8 || (neck > 0x1d6 && neck < 0x1d9)))
                {
                    MoveAfter(o, Layer.Necklace, Layer.Helmet);
                    return;
                }
            }
            else if (helm - 0x5e9u < 2 && graphic[(int)Layer.Robe] != 0 && (graphic[(int)Layer.Robe] - 0x5e2u) < 4)
            {
                MoveTo(o, Layer.Robe, Layer.Helmet);
            }
        }

        public static void GraphicsFromEntity(Entity entity, Span<ushort> gfx)
        {
            gfx.Slice(0, N).Clear();

            for (int layer = (int)Layer.OneHanded; layer <= (int)Layer.Legs; layer++)
            {
                Item item = entity.FindItemByLayer((Layer)layer);
                if (item != null)
                {
                    gfx[layer] = item.ItemData.AnimID;
                }
            }
        }

        public static int Filter(ReadOnlySpan<Layer> order, bool includeBackpack, Span<Layer> dest)
        {
            int c = 0;
            foreach (Layer layer in order)
            {
                if (layer == Layer.Invalid || layer == Layer.Mount) continue;
                if (layer == Layer.Backpack && !includeBackpack) continue;
                dest[c++] = layer;
            }

            return c;
        }

        public static int BuildInWorld(Entity entity, bool altTorsoTable, byte direction, Span<Layer> dest)
        {
            Span<ushort> gfx = stackalloc ushort[N];
            GraphicsFromEntity(entity, gfx);

            Span<Layer> order = stackalloc Layer[N];
            Build(gfx, altTorsoTable, order);

            int count = Filter(order, includeBackpack: false, dest);
            return ApplyDirectionCloak(dest, count, direction);
        }

        public static int ApplyDirectionCloak(Span<Layer> layers, int count, byte dir)
        {
            int idx = -1;
            for (int i = 0; i < count; i++)
            {
                if (layers[i] == Layer.Cloak) { idx = i; break; }
            }

            if (idx < 0) return count;

            for (int i = idx; i < count - 1; i++) layers[i] = layers[i + 1];
            count--;

            int insert;
            if (dir == 0)
            {
                insert = count;
            }
            else if (dir == 3)
            {
                insert = 0;
            }
            else
            {
                insert = count;
                for (int i = 0; i < count; i++)
                {
                    if (layers[i] == Layer.Helmet) { insert = i; break; }
                }
            }

            for (int i = count; i > insert; i--) layers[i] = layers[i - 1];
            layers[insert] = Layer.Cloak;
            return count + 1;
        }

        private static int IndexOf(ReadOnlySpan<Layer> a, Layer v)
        {
            for (int i = 0; i < N; i++)
            {
                if (a[i] == v) return i;
            }

            return -1;
        }

        private static void MoveTo(Span<Layer> a, Layer A, Layer B)
        {
            int i1 = IndexOf(a, A); if (i1 < 0) return;
            int i2 = IndexOf(a, B); if (i2 < 0 || i2 == i1) return;
            if (i2 < i1) { for (int k = i1; k > i2; k--) a[k] = a[k - 1]; a[i2] = A; }
            else { for (int k = i1; k < i2; k++) a[k] = a[k + 1]; a[i2] = A; }
        }

        private static void MoveAfter(Span<Layer> a, Layer A, Layer B)
        {
            int i1 = IndexOf(a, A); if (i1 < 0) return;
            int i2 = IndexOf(a, B); if (i2 < 0 || i2 == i1) return;
            if (i2 < i1) { for (int k = i1; k > i2 + 1; k--) a[k] = a[k - 1]; a[i2 + 1] = A; }
            else { for (int k = i1; k < i2; k++) a[k] = a[k + 1]; a[i2] = A; }
        }
    }
}
