using System;

namespace NyxNet.Models
{
    /// <summary>
    /// Represents a chat message
    /// </summary>
    [Serializable]
    public class ChatMessage
    {
        public string MessageId;
        public string SenderId;
        public string SenderName;
        public string Content;
        public string RoomId;
        public long Timestamp;
        public bool IsDirectMessage;
        
        public ChatMessage()
        {
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
        
        public ChatMessage(string senderId, string content, string roomId = null)
        {
            MessageId = Guid.NewGuid().ToString();
            SenderId = senderId;
            Content = content;
            RoomId = roomId;
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            IsDirectMessage = roomId == null;
        }
        
        public override string ToString()
        {
            return $"ChatMessage[From={SenderName ?? SenderId}, Content={Content}]";
        }
    }
}
