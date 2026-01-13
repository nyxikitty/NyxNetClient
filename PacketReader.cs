using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace NyxNet.Protocol
{
    /// <summary>
    /// Helper class for reading packet payloads
    /// </summary>
    public class PacketReader : IDisposable
    {
        private MemoryStream stream;
        private BinaryReader reader;
        
        public PacketReader(byte[] data)
        {
            stream = new MemoryStream(data);
            reader = new BinaryReader(stream);
        }
        
        /// <summary>Read a string (UTF-8) with length prefix</summary>
        public string ReadString()
        {
            int length = VarInt.Read(reader);
            if (length == 0)
                return string.Empty;
                
            byte[] bytes = reader.ReadBytes(length);
            return Encoding.UTF8.GetString(bytes);
        }
        
        /// <summary>Read a 32-bit integer</summary>
        public int ReadInt()
        {
            return reader.ReadInt32();
        }
        
        /// <summary>Read a variable-length integer</summary>
        public int ReadVarInt()
        {
            return VarInt.Read(reader);
        }
        
        /// <summary>Read a 64-bit long</summary>
        public long ReadLong()
        {
            return reader.ReadInt64();
        }
        
        /// <summary>Read a variable-length long</summary>
        public long ReadVarLong()
        {
            return VarLong.Read(reader);
        }
        
        /// <summary>Read a 32-bit float</summary>
        public float ReadFloat()
        {
            return reader.ReadSingle();
        }
        
        /// <summary>Read a 64-bit double</summary>
        public double ReadDouble()
        {
            return reader.ReadDouble();
        }
        
        /// <summary>Read a boolean</summary>
        public bool ReadBool()
        {
            return reader.ReadBoolean();
        }
        
        /// <summary>Read a single byte</summary>
        public byte ReadByte()
        {
            return reader.ReadByte();
        }
        
        /// <summary>Read a specific number of bytes</summary>
        public byte[] ReadBytes(int count)
        {
            return reader.ReadBytes(count);
        }
        
        /// <summary>Read remaining bytes</summary>
        public byte[] ReadRemainingBytes()
        {
            long remaining = stream.Length - stream.Position;
            return reader.ReadBytes((int)remaining);
        }
        
        /// <summary>Read a Vector3</summary>
        public Vector3 ReadVector3()
        {
            float x = reader.ReadSingle();
            float y = reader.ReadSingle();
            float z = reader.ReadSingle();
            return new Vector3(x, y, z);
        }
        
        /// <summary>Read a Quaternion</summary>
        public Quaternion ReadQuaternion()
        {
            float x = reader.ReadSingle();
            float y = reader.ReadSingle();
            float z = reader.ReadSingle();
            float w = reader.ReadSingle();
            return new Quaternion(x, y, z, w);
        }
        
        /// <summary>Read a Color</summary>
        public Color ReadColor()
        {
            float r = reader.ReadSingle();
            float g = reader.ReadSingle();
            float b = reader.ReadSingle();
            float a = reader.ReadSingle();
            return new Color(r, g, b, a);
        }
        
        /// <summary>Read a JSON-serialized object</summary>
        public T ReadJson<T>()
        {
            string json = ReadString();
            return JsonUtility.FromJson<T>(json);
        }
        
        /// <summary>Check if there are bytes remaining to read</summary>
        public bool HasRemaining => stream.Position < stream.Length;
        
        /// <summary>Get the current position in the stream</summary>
        public long Position => stream.Position;
        
        /// <summary>Get the total length of the data</summary>
        public long Length => stream.Length;
        
        public void Dispose()
        {
            reader?.Dispose();
            stream?.Dispose();
        }
    }
}
