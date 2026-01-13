namespace NyxNet.Protocol
{
    /// <summary>
    /// Packet operation codes matching the NyxNet server protocol
    /// </summary>
    public enum PacketOpcode : byte
    {
        // ========== Authentication ==========
        /// <summary>Authentication request</summary>
        AUTH = 0x01,
        
        /// <summary>Player login</summary>
        PLAYER_LOGIN = 0x02,
        
        /// <summary>Player logout</summary>
        PLAYER_LOGOUT = 0x03,
        
        // ========== Server Management ==========
        /// <summary>Register a game server</summary>
        REGISTER_SERVER = 0x10,
        
        /// <summary>Request server list</summary>
        SERVER_LIST = 0x11,
        
        /// <summary>Ping request</summary>
        PING = 0x12,
        
        /// <summary>Pong response</summary>
        PONG = 0x13,
        
        // ========== Game ==========
        /// <summary>Create a new room</summary>
        CREATE_ROOM = 0x20,
        
        /// <summary>Join an existing room</summary>
        JOIN_ROOM = 0x21,
        
        /// <summary>Leave a room</summary>
        LEAVE_ROOM = 0x22,
        
        /// <summary>Update player state</summary>
        PLAYER_UPDATE = 0x23,
        
        /// <summary>Room state update</summary>
        ROOM_UPDATE = 0x24,
        
        /// <summary>List available rooms</summary>
        ROOM_LIST = 0x25,
        
        /// <summary>Destroy a room</summary>
        ROOM_DESTROY = 0x26,
        
        // ========== Chat ==========
        /// <summary>Send chat message</summary>
        CHAT_MESSAGE = 0x30,
        
        /// <summary>Join chat room</summary>
        CHAT_ROOM_JOIN = 0x31,
        
        /// <summary>Send direct message</summary>
        CHAT_DIRECT_MESSAGE = 0x32,
        
        /// <summary>Leave chat room</summary>
        CHAT_ROOM_LEAVE = 0x33,
        
        // ========== Voice ==========
        /// <summary>Join voice channel</summary>
        VOICE_JOIN_CHANNEL = 0x40,
        
        /// <summary>Voice data packet</summary>
        VOICE_DATA = 0x41,
        
        /// <summary>Mute/unmute voice</summary>
        VOICE_MUTE = 0x42,
        
        /// <summary>Leave voice channel</summary>
        VOICE_LEAVE_CHANNEL = 0x43,
        
        // ========== Matchmaking ==========
        /// <summary>Join matchmaking queue</summary>
        MATCHMAKING_QUEUE = 0x50,
        
        /// <summary>Cancel matchmaking</summary>
        MATCHMAKING_CANCEL = 0x51,
        
        /// <summary>Match found notification</summary>
        MATCHMAKING_FOUND = 0x52,
        
        // ========== Generic ==========
        /// <summary>Success response</summary>
        SUCCESS = 0xF0,
        
        /// <summary>Error response</summary>
        ERROR = 0xF1,
        
        /// <summary>Notification</summary>
        NOTIFICATION = 0xF2
    }
}
