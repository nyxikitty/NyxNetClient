using System;
using System.Collections.Generic;
using UnityEngine;

namespace NyxNet.Models
{
    /// <summary>
    /// Represents a player in the game
    /// </summary>
    [Serializable]
    public class PlayerData
    {
        public string PlayerId;
        public string Username;
        public Vector3 Position;
        public Quaternion Rotation;
        public Dictionary<string, string> CustomData;
        
        public PlayerData()
        {
            Position = Vector3.zero;
            Rotation = Quaternion.identity;
            CustomData = new Dictionary<string, string>();
        }
        
        public PlayerData(string playerId, string username)
        {
            PlayerId = playerId;
            Username = username;
            Position = Vector3.zero;
            Rotation = Quaternion.identity;
            CustomData = new Dictionary<string, string>();
        }
        
        public override string ToString()
        {
            return $"Player[ID={PlayerId}, Username={Username}, Pos={Position}]";
        }
    }
}
