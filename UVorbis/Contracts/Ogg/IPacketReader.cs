using System;

namespace NVorbis.Contracts.Ogg
{
    interface IPacketReader
    {
        ArraySegment<byte> GetPacketData(int pagePacketIndex);

        void InvalidatePacketCache(IPacket packet);
    }
}
