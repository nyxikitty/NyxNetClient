using System;
using System.IO;

namespace NyxNet.Protocol
{
    /// <summary>
    /// Variable-length integer encoding/decoding utilities
    /// </summary>
    public static class VarInt
    {
        /// <summary>
        /// Write a VarInt to a BinaryWriter
        /// </summary>
        public static void Write(BinaryWriter writer, int value)
        {
            uint uValue = (uint)value;
            while ((uValue & ~0x7F) != 0)
            {
                writer.Write((byte)((uValue & 0x7F) | 0x80));
                uValue >>= 7;
            }
            writer.Write((byte)uValue);
        }
        
        /// <summary>
        /// Read a VarInt from a BinaryReader
        /// </summary>
        public static int Read(BinaryReader reader)
        {
            int value = 0;
            int shift = 0;
            byte b;
            
            do
            {
                if (shift >= 32)
                    throw new InvalidDataException("VarInt is too large");
                    
                b = reader.ReadByte();
                value |= (b & 0x7F) << shift;
                shift += 7;
            } while ((b & 0x80) != 0);
            
            return value;
        }
        
        /// <summary>
        /// Get the size in bytes of a VarInt value
        /// </summary>
        public static int GetSize(int value)
        {
            uint uValue = (uint)value;
            int size = 0;
            
            do
            {
                size++;
                uValue >>= 7;
            } while (uValue != 0);
            
            return size;
        }
    }
}
