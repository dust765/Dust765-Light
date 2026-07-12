// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.IO;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ClassicUO.Assets
{
    public sealed class GumpsLoader : UOFileLoader
    {
        public const int MAX_GUMP_DATA_INDEX_COUNT = 0x10000;


        private UOFile _file;

        public GumpsLoader(UOFileManager fileManager) : base(fileManager) { }


        public bool UseUOPGumps = false;
        public UOFile File => _file;

        public override void Load()
        {
            string path = FileManager.GetUOFilePath("gumpartLegacyMUL.uop");

            if (FileManager.IsUOPInstallation && System.IO.File.Exists(path))
            {
                _file = new UOFileUop(path, "build/gumpartlegacymul/{0:D8}.tga", true);
                UseUOPGumps = true;
            }
            else
            {
                path = FileManager.GetUOFilePath("gumpart.mul");
                string pathidx = FileManager.GetUOFilePath("gumpidx.mul");

                if (!System.IO.File.Exists(path))
                {
                    path = FileManager.GetUOFilePath("Gumpart.mul");
                }

                if (!System.IO.File.Exists(pathidx))
                {
                    pathidx = FileManager.GetUOFilePath("Gumpidx.mul");
                }

                if (!System.IO.File.Exists(path))
                    Log.Warn($"[GumpsLoader] gumpart.mul não encontrado: {path}");

                if (!System.IO.File.Exists(pathidx))
                    Log.Warn($"[GumpsLoader] gumpidx.mul não encontrado: {pathidx}");

                _file = new UOFileMul(path, pathidx);

                UseUOPGumps = false;
            }

            _file.FillEntries();

            string pathdef = FileManager.GetUOFilePath("gump.def");

            if (!System.IO.File.Exists(pathdef))
            {
                return;
            }

            using (DefReader defReader = new DefReader(pathdef, 3))
            {
                while (defReader.Next())
                {
                    int ingump = defReader.ReadInt();

                    if (
                        ingump < 0
                        || ingump >= MAX_GUMP_DATA_INDEX_COUNT
                        || ingump >= _file.Entries.Length
                        || _file.Entries[ingump].Length > 0
                    )
                    {
                        continue;
                    }

                    int[] group = defReader.ReadGroup();

                    if (group == null)
                    {
                        continue;
                    }

                    for (int i = 0; i < group.Length; i++)
                    {
                        int checkIndex = group[i];

                        if (
                            checkIndex < 0
                            || checkIndex >= MAX_GUMP_DATA_INDEX_COUNT
                            || checkIndex >= _file.Entries.Length
                            || _file.Entries[checkIndex].Length <= 0
                        )
                        {
                            continue;
                        }

                        _file.Entries[ingump] = _file.Entries[checkIndex];
                        _file.Entries[ingump].Hue = (ushort)defReader.ReadInt();

                        break;
                    }
                }
            }
        }

        public GumpInfo GetGump(uint index)
        {
            ref var entry = ref _file.GetValidRefEntry((int)index);

            if (entry.Length <= 0)
            {
                return default;
            }

            ushort color = entry.Hue;

            var file = _file;
            if (entry.File != null)
                file = entry.File;

            file.Seek(entry.Offset, SeekOrigin.Begin);

            var cbuf = new byte[entry.Length];
            file.Read(cbuf);
            ReadOnlySpan<byte> raw = cbuf;

            bool hintCompressed =
                FileManager.Version >= ClientVersion.CV_7010400
                || GumpDataLooksZlibCompressed(raw);

            GumpInfo gi = default;
            if (hintCompressed)
            {
                gi = TryDecodeGumpZlibBwt(cbuf, color, ref entry);
            }

            if (gi.Pixels.IsEmpty)
            {
                gi = TryDecodeGumpLegacyMul(raw, entry.Width, entry.Height, color);
            }

            if (gi.Pixels.IsEmpty && !hintCompressed)
            {
                gi = TryDecodeGumpZlibBwt(cbuf, color, ref entry);
            }

            return gi;
        }

        private static bool GumpDataLooksZlibCompressed(ReadOnlySpan<byte> s) =>
            s.Length >= 2 && s[0] == 0x78 && (s[1] == 0x01 || s[1] == 0x5E || s[1] == 0x9C || s[1] == 0xDA);

        private static bool TryZlibInflateToArray(ReadOnlySpan<byte> source, out byte[] inflated)
        {
            inflated = null;
            try
            {
                byte[] arr = source.ToArray();
                using var ms = new MemoryStream(arr, writable: false);
                using var ds = new ZLibStream(ms, CompressionMode.Decompress);
                using var output = new MemoryStream();
                ds.CopyTo(output);
                inflated = output.ToArray();
                return inflated.Length > 0;
            }
            catch
            {
                return false;
            }
        }

        private GumpInfo TryDecodeGumpZlibBwt(byte[] cbuf, ushort color, ref UOFileIndex entry)
        {
            byte[] zlibOut = null;

            if (entry.CompressionFlag >= CompressionType.Zlib && entry.DecompressedLength > 0)
            {
                var dbuf = new byte[entry.DecompressedLength];
                var result = ZLib.Decompress(cbuf.AsSpan(), dbuf);
                if (result == ZLib.ZLibError.Ok)
                {
                    zlibOut = dbuf;
                }
            }

            if (zlibOut == null && !TryZlibInflateToArray(cbuf, out zlibOut))
            {
                return default;
            }

            if (zlibOut.Length < 8)
            {
                return default;
            }

            byte[] payload;
            try
            {
                payload = BwtDecompress.Decompress(zlibOut);
            }
            catch
            {
                payload = zlibOut;
            }

            if (payload == null || payload.Length < 8)
            {
                return default;
            }

            ReadOnlySpan<byte> p = payload;
            uint w = BinaryPrimitives.ReadUInt32LittleEndian(p.Slice(0, 4));
            uint h = BinaryPrimitives.ReadUInt32LittleEndian(p.Slice(4, 4));

            if (entry.Width <= 0)
                entry.Width = (int)w;
            if (entry.Height <= 0)
                entry.Height = (int)h;

            return DecodeGumpRunLengthPixels(p.Slice(8), w, h, color);
        }

        private GumpInfo TryDecodeGumpLegacyMul(ReadOnlySpan<byte> raw, int idxW, int idxH, ushort color)
        {
            if (idxW <= 0 || idxH <= 0)
            {
                return default;
            }

            return DecodeGumpRunLengthPixels(raw, (uint)idxW, (uint)idxH, color);
        }

        private GumpInfo DecodeGumpRunLengthPixels(ReadOnlySpan<byte> runData, uint w, uint h, ushort color)
        {
            if (w == 0 || h == 0 || w > 0x4000 || h > 0x4000)
            {
                return default;
            }

            ulong pixelCount = (ulong)w * h;

            if (pixelCount > int.MaxValue)
            {
                return default;
            }

            var pixels = new uint[(int)pixelCount];
            var reader = new StackDataReader(runData);
            var len = reader.Remaining;
            var halfLen = len >> 2;

            if (len < (int)(h * sizeof(int)))
            {
                return default;
            }

            var start = reader.Position;
            var rowLookup = new int[h];
            reader.Read(MemoryMarshal.AsBytes<int>(rowLookup.AsSpan()));

            for (var y = 0; y < h; ++y)
            {
                reader.Seek(start + (rowLookup[y] << 2));
                var pixelIndex = (int)(y * w);
                var rowEnd = pixelIndex + (int)w;
                var gsize = y < h - 1 ? rowLookup[y + 1] - rowLookup[y] : halfLen - rowLookup[y];

                if (rowLookup[y] < 0 || rowLookup[y] > halfLen || gsize < 0 || rowLookup[y] + gsize > halfLen)
                {
                    return default;
                }

                for (var i = 0; i < gsize; ++i)
                {
                    var value = reader.ReadUInt16LE();
                    var run = reader.ReadUInt16LE();
                    var rbga = 0u;

                    if (color != 0 && value != 0)
                    {
                        value = FileManager.Hues.GetColor16(value, color);
                    }

                    if (value != 0)
                    {
                        rbga = HuesHelper.Color16To32(value) | 0xFF_00_00_00;
                    }

                    if (run == 0)
                    {
                        continue;
                    }

                    if (pixelIndex + run > rowEnd || pixelIndex + run > pixels.Length)
                    {
                        return default;
                    }

                    pixels.AsSpan().Slice(pixelIndex, run).Fill(rbga);
                    pixelIndex += run;
                }

                if (pixelIndex != rowEnd)
                {
                    return default;
                }
            }

            return new GumpInfo()
            {
                Pixels = pixels,
                Width = (int)w,
                Height = (int)h
            };
        }
    }

    public ref struct GumpInfo
    {
        public Span<uint> Pixels;
        public int Width;
        public int Height;
    }
}
