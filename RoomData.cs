using System;
using System.Collections.Generic;

namespace NyxNet.Models
{
    /// <summary>
    /// Represents a game room
    /// </summary>
    [Serializable]
    public class RoomData
    {
        public string RoomId;
        public string Name;
        public int MaxPlayers;
        public int CurrentPlayers;
        public List<string> PlayerIds;
        public Dictionary<string, string> CustomData;
        
        public RoomData()
        {
            PlayerIds = new List<string>();
            CustomData = new Dictionary<string, string>();
        }
        
        public RoomData(string roomId, string name, int maxPlayers)
        {
            RoomId = roomId;
            Name = name;
            MaxPlayers = maxPlayers;
            CurrentPlayers = 0;
            PlayerIds = new List<string>();
            CustomData = new Dictionary<string, string>();
        }
        
        public bool IsFull => CurrentPlayers >= MaxPlayers;
        
        public override string ToString()
        {
            return $"Room[ID={RoomId}, Name={Name}, Players={CurrentPlayers}/{MaxPlayers}]";
        }
    }
}
