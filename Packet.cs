using System;
using System.IO;

namespace NyxNet.Protocol
{
    /// <summary>
    /// NyxNet protocol packet with binary serialization
    /// Packet structure: [Magic: 0x42 0x4E] [Version: 0x01] [Flags] [Opcode] [Length: VarInt] [Payload] [Checksum]
    /// </summary>
    public class Packet
    {
        // Protocol constants
        private const byte MAGIC_BYTE_1 = 0x42; // 'B'
        private const byte MAGIC_BYTE_2 = 0x4E; // 'N'
        private const byte PROTOCOL_VERSION = 0x01;
        
        /// <summary>Packet operation code</summary>
        public PacketOpcode Opcode { get; set; }
        
        /// <summary>Packet flags</summary>
        public PacketFlags Flags { get; set; }
        
        /// <summary>Packet payload data</summary>
        public byte[] Payload { get; set; }
        
        /// <summary>
        /// Create a new packet
        /// </summary>
        public Packet(PacketOpcode opcode, byte[] payload = null, PacketFlags flags = PacketFlags.None)
        {
            Opcode = opcode;
            Payload = payload ?? Array.Empty<byte>();
            Flags = flags;
        }
        
        /// <summary>
        /// Serialize packet to byte array
        /// </summary>
        public byte[] Serialize()
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                // Magic bytes
                writer.Write(MAGIC_BYTE_1);
                writer.Write(MAGIC_BYTE_2);
                
                // Protocol version
                writer.Write(PROTOCOL_VERSION);
                
                // Flags
                writer.Write((byte)Flags);
                
                // Opcode
                writer.Write((byte)Opcode);
                
                // Payload length (VarInt)
                VarInt.Write(writer, Payload.Length);
                
                // Payload data
                writer.Write(Payload);
                
                // Calculate and write checksum
                byte[] dataWithoutChecksum = ms.ToArray();
                uint checksum = CalculateChecksum(dataWithoutChecksum);
                writer.Write(checksum);
                
                return ms.ToArray();
            }
        }
        
        /// <summary>
        /// Deserialize packet from byte array
        /// </summary>
        public static Packet Deserialize(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                // Verify magic bytes
                byte magic1 = reader.ReadByte();
                byte magic2 = reader.ReadByte();
                
                if (magic1 != MAGIC_BYTE_1 || magic2 != MAGIC_BYTE_2)
                    throw new InvalidDataException($"Invalid magic bytes: 0x{magic1:X2} 0x{magic2:X2}");
                
                // Read and verify protocol version
                byte version = reader.ReadByte();
                if (version != PROTOCOL_VERSION)
                    throw new InvalidDataException($"Unsupported protocol version: {version}");
                
                // Read flags
                PacketFlags flags = (PacketFlags)reader.ReadByte();
                
                // Read opcode
                PacketOpcode opcode = (PacketOpcode)reader.ReadByte();
                
                // Read payload length
                int payloadLength = VarInt.Read(reader);
                
                // Read payload
                byte[] payload = reader.ReadBytes(payloadLength);
                
                // Read and verify checksum
                uint receivedChecksum = reader.ReadUInt32();
                
                // Calculate expected checksum
                long checksumPosition = ms.Position - 4;
                byte[] dataForChecksum = new byte[checksumPosition];
                Array.Copy(data, 0, dataForChecksum, 0, checksumPosition);
                uint calculatedChecksum = CalculateChecksum(dataForChecksum);
                
                if (receivedChecksum != calculatedChecksum)
                    throw new InvalidDataException("Checksum verification failed");
                
                return new Packet(opcode, payload, flags);
            }
        }
        
        /// <summary>
        /// Calculate CRC32 checksum for data
        /// </summary>
        private static uint CalculateChecksum(byte[] data)
        {
            uint crc = 0xFFFFFFFF;
            
            for (int i = 0; i < data.Length; i++)
            {
                crc ^= data[i];
                for (int j = 0; j < 8; j++)
                {
                    if ((crc & 1) != 0)
                        crc = (crc >> 1) ^ 0xEDB88320;
                    else
                        crc >>= 1;
                }
            }
            
            return ~crc;
        }
        
        /// <summary>
        /// Get string representation of packet
        /// </summary>
        public override string ToString()
        {
            return $"Packet[Opcode={Opcode}, Flags={Flags}, PayloadSize={Payload.Length}]";
        }
    }
}
