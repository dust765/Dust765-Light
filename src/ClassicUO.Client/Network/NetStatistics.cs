// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Diagnostics;

namespace ClassicUO.Network
{
    sealed class NetStatistics
    {
        private const uint CLIENT_STALL_THRESHOLD_MS = 20;
        private const uint CLIENT_STALL_CAP_ABOVE_MEDIAN_MS = 5;

        private readonly NetClient _socket;
        private uint _lastTotalBytesReceived, _lastTotalBytesSent, _lastTotalPacketsReceived, _lastTotalPacketsSent;
        private byte _pingIdx;

        private readonly uint[] _pings = new uint[5];
        private readonly long[] _pingSendTick = new long[5];
        private uint _statisticsTimer;


        public NetStatistics(NetClient socket)
        {
            _socket = socket;
        }


        public DateTime ConnectedFrom { get; set; }

        public uint TotalBytesReceived { get; set; }

        public uint TotalBytesSent { get; set; }

        public uint TotalPacketsReceived { get; set; }

        public uint TotalPacketsSent { get; set; }

        public uint DeltaBytesReceived { get; private set; }

        public uint DeltaBytesSent { get; private set; }

        public uint DeltaPacketsReceived { get; private set; }

        public uint DeltaPacketsSent { get; private set; }

        public uint RawPing => GetMinPing();

        public uint Ping => RawPing;

        public uint DisplayPing { get; private set; }

        public bool HasPendingPing()
        {
            for (int i = 0; i < _pingSendTick.Length; i++)
            {
                if (_pingSendTick[i] != 0)
                {
                    return true;
                }
            }

            return false;
        }

        public void MarkPingSentAtWire(byte idx)
        {
            _pingSendTick[idx % _pings.Length] = Stopwatch.GetTimestamp();
        }

        public void PingReceived(byte idx)
        {
            int i = idx % _pings.Length;
            long t0 = _pingSendTick[i];
            if (t0 == 0)
            {
                return;
            }

            double ms = Stopwatch.GetElapsedTime(t0).TotalMilliseconds;
            _pingSendTick[i] = 0;

            if (ms < 0 || ms > 120_000)
            {
                return;
            }

            uint sample = (uint)ms;
            uint median = GetMedianPing(excludeSlot: i);

            if (median > 0 && sample > median + CLIENT_STALL_THRESHOLD_MS)
            {
                sample = median + CLIENT_STALL_CAP_ABOVE_MEDIAN_MS;
            }

            _pings[i] = sample;
            UpdateDisplayPing();
        }

        public void SendPing()
        {
            if (!_socket.IsConnected)
            {
                return;
            }

            _socket.Send_Ping(_pingIdx);
            _pingIdx = (byte)((_pingIdx + 1) % _pings.Length);
        }

        public void Reset()
        {
            Array.Clear(_pingSendTick);
            Array.Clear(_pings);
            DisplayPing = 0;
            ConnectedFrom = DateTime.MinValue;
            _lastTotalBytesReceived = _lastTotalBytesSent = _lastTotalPacketsReceived = _lastTotalPacketsSent = 0;
            TotalBytesReceived = TotalBytesSent = TotalPacketsReceived = TotalPacketsSent = 0;
            DeltaBytesReceived = DeltaBytesSent = DeltaPacketsReceived = DeltaPacketsSent = 0;
        }

        public void Update()
        {
            if (_statisticsTimer > Time.Ticks) return;

            _statisticsTimer = Time.Ticks + 500;

            DeltaBytesReceived = TotalBytesReceived - _lastTotalBytesReceived;
            DeltaBytesSent = TotalBytesSent - _lastTotalBytesSent;
            DeltaPacketsReceived = TotalPacketsReceived - _lastTotalPacketsReceived;
            DeltaPacketsSent = TotalPacketsSent - _lastTotalPacketsSent;
            _lastTotalBytesReceived = TotalBytesReceived;
            _lastTotalBytesSent = TotalBytesSent;
            _lastTotalPacketsReceived = TotalPacketsReceived;
            _lastTotalPacketsSent = TotalPacketsSent;
        }

        private uint GetMinPing()
        {
            uint min = 0;

            for (byte i = 0; i < 5; i++)
            {
                if (_pings[i] == 0)
                {
                    continue;
                }

                if (min == 0 || _pings[i] < min)
                {
                    min = _pings[i];
                }
            }

            return min;
        }

        private uint GetMedianPing(int excludeSlot = -1)
        {
            Span<uint> values = stackalloc uint[5];
            int count = 0;

            for (int j = 0; j < 5; j++)
            {
                if (j == excludeSlot || _pings[j] == 0)
                {
                    continue;
                }

                values[count++] = _pings[j];
            }

            if (count == 0)
            {
                return 0;
            }

            for (int a = 1; a < count; a++)
            {
                uint key = values[a];
                int b = a - 1;

                while (b >= 0 && values[b] > key)
                {
                    values[b + 1] = values[b];
                    b--;
                }

                values[b + 1] = key;
            }

            return values[count / 2];
        }

        private void UpdateDisplayPing()
        {
            uint raw = RawPing;

            if (raw == 0)
            {
                DisplayPing = 0;
                return;
            }

            if (DisplayPing == 0)
            {
                DisplayPing = raw;
                return;
            }

            if (raw <= DisplayPing)
            {
                DisplayPing = (DisplayPing + raw) >> 1;
            }
            else
            {
                DisplayPing = (DisplayPing * 4 + raw) / 5;
            }
        }

        public override string ToString()
        {
            return $"Packets:\n >> {DeltaPacketsReceived}\n << {DeltaPacketsSent}\nBytes:\n >> {GetSizeAdaptive(DeltaBytesReceived)}\n << {GetSizeAdaptive(DeltaBytesSent)}";
        }

        public static string GetSizeAdaptive(long bytes)
        {
            decimal num = bytes;
            string arg = "B";

            if (!(num < 1024m))
            {
                arg = "KB";
                num /= 1024m;

                if (!(num < 1024m))
                {
                    arg = "MB";
                    num /= 1024m;

                    if (!(num < 1024m))
                    {
                        arg = "GB";
                        num /= 1024m;
                    }
                }
            }

            return $"{Math.Round(num, 2):0.##} {arg}";
        }
    }
}
