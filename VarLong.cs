using System;
using System.IO;

namespace NyxNet.Protocol
{
    /// <summary>
    /// Variable-length long integer encoding/decoding utilities
    /// </summary>
    public static class VarLong
    {
        /// <summary>
        /// Write a VarLong to a BinaryWriter
        /// </summary>
        public static void Write(BinaryWriter writer, long value)
        {
            ulong uValue = (ulong)value;
            while ((uValue & ~0x7FUL) != 0)
            {
                writer.Write((byte)((uValue & 0x7F) | 0x80));
                uValue >>= 7;
            }
            writer.Write((byte)uValue);
        }
        
        /// <summary>
        /// Read a VarLong from a BinaryReader
        /// </summary>
        public static long Read(BinaryReader reader)
        {
            long value = 0;
            int shift = 0;
            byte b;
            
            do
            {
                if (shift >= 64)
                    throw new InvalidDataException("VarLong is too large");
                    
                b = reader.ReadByte();
                value |= (long)(b & 0x7F) << shift;
                shift += 7;
            } while ((b & 0x80) != 0);
            
            return value;
        }
        
        /// <summary>
        /// Get the size in bytes of a VarLong value
        /// </summary>
        public static int GetSize(long value)
        {
            ulong uValue = (ulong)value;
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
