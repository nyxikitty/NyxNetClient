using System;

namespace NyxNet.Protocol
{
    /// <summary>
    /// Packet flags for special handling
    /// </summary>
    [Flags]
    public enum PacketFlags : byte
    {
        /// <summary>No special flags</summary>
        None = 0,
        
        /// <summary>Packet payload is compressed</summary>
        Compressed = 1 << 0,
        
        /// <summary>Packet payload is encrypted</summary>
        Encrypted = 1 << 1,
        
        /// <summary>Packet is part of a fragmented message</summary>
        Fragmented = 1 << 2,
        
        /// <summary>Packet requires acknowledgment</summary>
        RequiresAck = 1 << 3
    }
}
