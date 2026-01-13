using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace NyxNet.Protocol
{
    /// <summary>
    /// Helper class for building packet payloads
    /// </summary>
    public class PacketBuilder : IDisposable
    {
        private MemoryStream stream;
        private BinaryWriter writer;
        
        public PacketBuilder()
        {
            stream = new MemoryStream();
            writer = new BinaryWriter(stream);
        }
        
        /// <summary>Write a string (UTF-8) with length prefix</summary>
        public PacketBuilder WriteString(string value)
        {
            if (value == null)
            {
                VarInt.Write(writer, 0);
                return this;
            }
            
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            VarInt.Write(writer, bytes.Length);
            writer.Write(bytes);
            return this;
        }
        
        /// <summary>Write a 32-bit integer</summary>
        public PacketBuilder WriteInt(int value)
        {
            writer.Write(value);
            return this;
        }
        
        /// <summary>Write a variable-length integer</summary>
        public PacketBuilder WriteVarInt(int value)
        {
            VarInt.Write(writer, value);
            return this;
        }
        
        /// <summary>Write a 64-bit long</summary>
        public PacketBuilder WriteLong(long value)
        {
            writer.Write(value);
            return this;
        }
        
        /// <summary>Write a variable-length long</summary>
        public PacketBuilder WriteVarLong(long value)
        {
            VarLong.Write(writer, value);
            return this;
        }
        
        /// <summary>Write a 32-bit float</summary>
        public PacketBuilder WriteFloat(float value)
        {
            writer.Write(value);
            return this;
        }
        
        /// <summary>Write a 64-bit double</summary>
        public PacketBuilder WriteDouble(double value)
        {
            writer.Write(value);
            return this;
        }
        
        /// <summary>Write a boolean</summary>
        public PacketBuilder WriteBool(bool value)
        {
            writer.Write(value);
            return this;
        }
        
        /// <summary>Write a single byte</summary>
        public PacketBuilder WriteByte(byte value)
        {
            writer.Write(value);
            return this;
        }
        
        /// <summary>Write a byte array</summary>
        public PacketBuilder WriteBytes(byte[] value)
        {
            if (value != null)
            {
                writer.Write(value);
            }
            return this;
        }
        
        /// <summary>Write a Vector3</summary>
        public PacketBuilder WriteVector3(Vector3 value)
        {
            writer.Write(value.x);
            writer.Write(value.y);
            writer.Write(value.z);
            return this;
        }
        
        /// <summary>Write a Quaternion</summary>
        public PacketBuilder WriteQuaternion(Quaternion value)
        {
            writer.Write(value.x);
            writer.Write(value.y);
            writer.Write(value.z);
            writer.Write(value.w);
            return this;
        }
        
        /// <summary>Write a Color</summary>
        public PacketBuilder WriteColor(Color value)
        {
            writer.Write(value.r);
            writer.Write(value.g);
            writer.Write(value.b);
            writer.Write(value.a);
            return this;
        }
        
        /// <summary>Write a JSON-serializable object</summary>
        public PacketBuilder WriteJson<T>(T obj)
        {
            string json = JsonUtility.ToJson(obj);
            WriteString(json);
            return this;
        }
        
        /// <summary>Build the final byte array</summary>
        public byte[] Build()
        {
            return stream.ToArray();
        }
        
        /// <summary>Get the current size of the payload</summary>
        public int Size => (int)stream.Length;
        
        public void Dispose()
        {
            writer?.Dispose();
            stream?.Dispose();
        }
    }
}
