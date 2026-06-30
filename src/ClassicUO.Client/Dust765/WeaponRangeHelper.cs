// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using System.IO;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Utility;

namespace ClassicUO.Dust765
{
    internal static class WeaponRangeHelper
    {
        private const int DefaultMeleeRange = 1;
        private const uint RefreshIntervalMs = 100;

        private static readonly List<ushort> WeaponsList = new List<ushort>();
        private static uint _nextRefreshTick;
        private static int _cachedRange = DefaultMeleeRange;

        public static int GetEquippedWeaponRange(World world)
        {
            if (world?.Player == null)
            {
                return DefaultMeleeRange;
            }

            uint now = Time.Ticks;

            if (now >= _nextRefreshTick)
            {
                _nextRefreshTick = now + RefreshIntervalMs;
                _cachedRange = ResolveEquippedWeaponRange(world);
            }

            return _cachedRange;
        }

        public static void EnsureLoaded()
        {
            if (WeaponsList.Count == 0)
            {
                LoadFile();
            }
        }

        private static int ResolveEquippedWeaponRange(World world)
        {
            EnsureLoaded();

            Item rightHand = world.Player.FindItemByLayer(Layer.TwoHanded);
            Item leftHand = world.Player.FindItemByLayer(Layer.OneHanded);

            int range = 0;

            if (rightHand != null)
            {
                range = LookupRange(rightHand.Graphic);
            }

            if (leftHand != null)
            {
                int leftRange = LookupRange(leftHand.Graphic);

                if (leftRange > range)
                {
                    range = leftRange;
                }
            }

            return range > 0 ? range : DefaultMeleeRange;
        }

        private static int LookupRange(ushort graphic)
        {
            int index = WeaponsList.IndexOf(graphic);

            if (index >= 0 && index + 1 < WeaponsList.Count)
            {
                return WeaponsList[index + 1];
            }

            return 0;
        }

        private static void LoadFile()
        {
            string path = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Client");

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            string autorangePath = Path.Combine(path, "autorange.txt");

            if (!File.Exists(autorangePath))
            {
                CreateDefaultAutorangeFile(autorangePath);
            }

            TextFileParser parser = new TextFileParser(
                File.ReadAllText(autorangePath),
                new[] { ' ', '\t', ',', '=' },
                new[] { '#', ';' },
                new[] { '"', '"' }
            );

            while (!parser.IsEOF())
            {
                List<string> tokens = parser.ReadTokens();

                if (tokens == null || tokens.Count == 0)
                {
                    continue;
                }

                if (tokens.Count > 0 && ushort.TryParse(tokens[0], out ushort graphic))
                {
                    WeaponsList.Add(graphic);
                }

                if (tokens.Count > 1 && ushort.TryParse(tokens[1], out ushort range))
                {
                    WeaponsList.Add(range);
                }
            }
        }

        private static void CreateDefaultAutorangeFile(string autorangePath)
        {
            (ushort graphic, ushort range)[] defaults =
            {
                (0x13B1, 10), (0x13B2, 10),
                (0x26C2, 10), (0x26CC, 10),
                (0x26C3, 7), (0x26CD, 7),
                (0x27A5, 10), (0x27F0, 10),
                (0x0F4F, 8), (0x0F50, 8),
                (0x13FC, 8), (0x13FD, 8),
                (0x2D1E, 10), (0x2D2A, 10),
                (0x2D1F, 10), (0x2D2B, 10)
            };

            using StreamWriter writer = new StreamWriter(autorangePath);

            foreach ((ushort graphic, ushort range) entry in defaults)
            {
                writer.WriteLine($"{entry.graphic}={entry.range}");
            }
        }
    }
}
