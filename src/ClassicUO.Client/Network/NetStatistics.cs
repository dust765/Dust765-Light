// SPDX-License-Identifier: BSD-2-Clause

using System;

namespace ClassicUO.Network
{
    sealed class NetStatistics
    {
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

        public uint Ping
        {
            get
            {
                byte count = 0;
                uint sum = 0;

                for (byte i = 0; i < 5; i++)
                {
                    if (_pings[i] != 0)
                    {
                        count++;
                        sum += _pings[i];
                    }
                }

                if (count == 0)
                {
                    return 0;
                }

                return sum / count;
            }
        }

        public void PingReceived(byte idx)
        {
            int i = idx % _pings.Length;
            long t0 = _pingSendTick[i];
            if (t0 == 0)
            {
                return;
            }

            long delta = Environment.TickCount64 - t0;
            if (delta < 0 || delta > 120_000)
            {
                return;
            }

            _pings[i] = (uint)delta;
        }

        public void SendPing()
        {
            if (!_socket.IsConnected)
            {
                return;
            }

            int i = _pingIdx % _pings.Length;
            _pingSendTick[i] = Environment.TickCount64;
            _socket.Send_Ping(_pingIdx);
            _pingIdx = (byte)((_pingIdx + 1) % _pings.Length);
        }

        public void Reset()
        {
            Array.Clear(_pingSendTick);
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