using System;

namespace NyxNet.Models
{
    /// <summary>
    /// Represents information about a game server
    /// </summary>
    [Serializable]
    public class ServerInfo
    {
        public string ServerId;
        public string ServerName;
        public string Host;
        public int Port;
        public int CurrentPlayers;
        public int MaxPlayers;
        public float Load;
        public string Region;
        
        public ServerInfo()
        {
        }
        
        public ServerInfo(string serverId, string host, int port)
        {
            ServerId = serverId;
            Host = host;
            Port = port;
        }
        
        public bool IsAvailable => CurrentPlayers < MaxPlayers;
        
        public override string ToString()
        {
            return $"Server[ID={ServerId}, Name={ServerName}, {Host}:{Port}, Players={CurrentPlayers}/{MaxPlayers}]";
        }
    }
}
