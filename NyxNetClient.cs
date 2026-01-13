using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using NyxNet.Protocol;
using NyxNet.Models;

namespace NyxNet
{
    /// <summary>
    /// Main client for connecting to NyxNet game servers
    /// </summary>
    public class NyxNetClient : IDisposable
    {
        // Connection state
        private TcpClient tcpClient;
        private NetworkStream stream;
        private CancellationTokenSource cancellationTokenSource;
        private bool isConnected;
        private bool isDisposed;
        
        // Client data
        private string playerId;
        private string username;
        private string currentRoomId;
        
        // Packet queue for main thread processing
        private readonly Queue<Packet> packetQueue = new Queue<Packet>();
        private readonly object queueLock = new object();
        
        // Configuration
        public int ReceiveBufferSize { get; set; } = 8192;
        public int SendTimeout { get; set; } = 5000;
        public int ReceiveTimeout { get; set; } = 5000;
        
        // Properties
        public string PlayerId => playerId;
        public string Username => username;
        public string CurrentRoomId => currentRoomId;
        public bool IsConnected => isConnected && tcpClient?.Connected == true;
        
        // ==================== Events ====================
        
        /// <summary>Fired when connection is established</summary>
        public event Action OnConnected;
        
        /// <summary>Fired when disconnected from server</summary>
        public event Action OnDisconnected;
        
        /// <summary>Fired when authentication succeeds</summary>
        public event Action<string> OnAuthenticated;
        
        /// <summary>Fired when authentication fails</summary>
        public event Action<string> OnAuthenticationFailed;
        
        /// <summary>Fired when player successfully logs in</summary>
        public event Action<string, string> OnPlayerLoggedIn; // playerId, username
        
        /// <summary>Fired when joined a room</summary>
        public event Action<RoomData> OnRoomJoined;
        
        /// <summary>Fired when left a room</summary>
        public event Action<string> OnRoomLeft; // roomId
        
        /// <summary>Fired when room is created</summary>
        public event Action<RoomData> OnRoomCreated;
        
        /// <summary>Fired when room list is received</summary>
        public event Action<List<RoomData>> OnRoomListReceived;
        
        /// <summary>Fired when a player joins the current room</summary>
        public event Action<PlayerData> OnPlayerJoinedRoom;
        
        /// <summary>Fired when a player leaves the current room</summary>
        public event Action<string> OnPlayerLeftRoom; // playerId
        
        /// <summary>Fired when a player update is received</summary>
        public event Action<PlayerData> OnPlayerUpdated;
        
        /// <summary>Fired when a chat message is received</summary>
        public event Action<ChatMessage> OnChatMessageReceived;
        
        /// <summary>Fired when a direct message is received</summary>
        public event Action<ChatMessage> OnDirectMessageReceived;
        
        /// <summary>Fired when matchmaking finds a match</summary>
        public event Action<RoomData> OnMatchFound;
        
        /// <summary>Fired when server list is received</summary>
        public event Action<List<ServerInfo>> OnServerListReceived;
        
        /// <summary>Fired when a ping response is received</summary>
        public event Action<long> OnPongReceived; // roundTripTime
        
        /// <summary>Fired when a generic success response is received</summary>
        public event Action<string> OnSuccess;
        
        /// <summary>Fired when an error occurs</summary>
        public event Action<string> OnError;
        
        /// <summary>Fired for any unhandled packet (for custom extensions)</summary>
        public event Action<PacketOpcode, byte[]> OnCustomPacketReceived;
        
        // ==================== Connection Methods ====================
        
        /// <summary>
        /// Connect to the NyxNet server
        /// </summary>
        public async Task<bool> ConnectAsync(string host, int port)
        {
            if (isConnected)
            {
                Debug.LogWarning("Already connected to server");
                return true;
            }
            
            try
            {
                Debug.Log($"Connecting to NyxNet server at {host}:{port}...");
                
                tcpClient = new TcpClient();
                tcpClient.SendTimeout = SendTimeout;
                tcpClient.ReceiveTimeout = ReceiveTimeout;
                
                await tcpClient.ConnectAsync(host, port);
                stream = tcpClient.GetStream();
                isConnected = true;
                
                cancellationTokenSource = new CancellationTokenSource();
                
                // Start receiving packets on background thread
                _ = Task.Run(() => ReceivePacketsAsync(cancellationTokenSource.Token));
                
                Debug.Log($"Successfully connected to {host}:{port}");
                OnConnected?.Invoke();
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to connect: {ex.Message}");
                OnError?.Invoke($"Connection failed: {ex.Message}");
                Cleanup();
                return false;
            }
        }
        
        /// <summary>
        /// Disconnect from the server
        /// </summary>
        public void Disconnect()
        {
            if (!isConnected)
                return;
                
            Debug.Log("Disconnecting from NyxNet server...");
            
            try
            {
                // Send logout if we're logged in
                if (!string.IsNullOrEmpty(playerId))
                {
                    SendPacket(new Packet(PacketOpcode.PLAYER_LOGOUT));
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Error during logout: {ex.Message}");
            }
            
            Cleanup();
            OnDisconnected?.Invoke();
        }
        
        private void Cleanup()
        {
            isConnected = false;
            
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;
            
            stream?.Close();
            stream?.Dispose();
            stream = null;
            
            tcpClient?.Close();
            tcpClient?.Dispose();
            tcpClient = null;
            
            lock (queueLock)
            {
                packetQueue.Clear();
            }
        }
        
        /// <summary>
        /// Process queued packets on the main thread (call from Update)
        /// </summary>
        public void ProcessPackets()
        {
            lock (queueLock)
            {
                while (packetQueue.Count > 0)
                {
                    var packet = packetQueue.Dequeue();
                    HandlePacket(packet);
                }
            }
        }
        
        // ==================== Authentication Methods ====================
        
        /// <summary>
        /// Authenticate with the server using a specific auth plugin
        /// </summary>
        public void Authenticate(string authPlugin, string username, string password)
        {
            using (var builder = new PacketBuilder())
            {
                builder.WriteString(authPlugin);
                builder.WriteString(username);
                builder.WriteString(password);
                
                var packet = new Packet(PacketOpcode.AUTH, builder.Build());
                SendPacket(packet);
            }
        }
        
        /// <summary>
        /// Authenticate using simple auth (username/password)
        /// </summary>
        public void AuthenticateSimple(string username, string password)
        {
            Authenticate("simple", username, password);
        }
        
        /// <summary>
        /// Authenticate using API key
        /// </summary>
        public void AuthenticateApiKey(string apiKey)
        {
            Authenticate("apikey", apiKey, "");
        }
        
        /// <summary>
        /// Login as a player
        /// </summary>
        public void Login(string username)
        {
            this.username = username;
            
            using (var builder = new PacketBuilder())
            {
                builder.WriteString(username);
                
                var packet = new Packet(PacketOpcode.PLAYER_LOGIN, builder.Build());
                SendPacket(packet);
            }
        }
        
        /// <summary>
        /// Logout from the server
        /// </summary>
        public void Logout()
        {
            var packet = new Packet(PacketOpcode.PLAYER_LOGOUT);
            SendPacket(packet);
            
            playerId = null;
            username = null;
            currentRoomId = null;
        }
        
        // ==================== Room Methods ====================
        
        /// <summary>
        /// Create a new room
        /// </summary>
        public void CreateRoom(string roomName, int maxPlayers, Dictionary<string, string> customData = null)
        {
            using (var builder = new PacketBuilder())
            {
                builder.WriteString(roomName);
                builder.WriteInt(maxPlayers);
                
                // Serialize custom data
                if (customData != null && customData.Count > 0)
                {
                    builder.WriteInt(customData.Count);
                    foreach (var kvp in customData)
                    {
                        builder.WriteString(kvp.Key);
                        builder.WriteString(kvp.Value);
                    }
                }
                else
                {
                    builder.WriteInt(0);
                }
                
                var packet = new Packet(PacketOpcode.CREATE_ROOM, builder.Build());
                SendPacket(packet);
            }
        }
        
        /// <summary>
        /// Join an existing room
        /// </summary>
        public void JoinRoom(string roomId)
        {
            using (var builder = new PacketBuilder())
            {
                builder.WriteString(roomId);
                
                var packet = new Packet(PacketOpcode.JOIN_ROOM, builder.Build());
                SendPacket(packet);
            }
        }
        
        /// <summary>
        /// Leave the current room
        /// </summary>
        public void LeaveRoom()
        {
            if (string.IsNullOrEmpty(currentRoomId))
            {
                Debug.LogWarning("Not in a room");
                return;
            }
            
            using (var builder = new PacketBuilder())
            {
                builder.WriteString(currentRoomId);
                
                var packet = new Packet(PacketOpcode.LEAVE_ROOM, builder.Build());
                SendPacket(packet);
            }
        }
        
        /// <summary>
        /// Request list of available rooms
        /// </summary>
        public void RequestRoomList()
        {
            var packet = new Packet(PacketOpcode.ROOM_LIST);
            SendPacket(packet);
        }
        
        // ==================== Player Update Methods ====================
        
        /// <summary>
        /// Update player position and rotation
        /// </summary>
        public void UpdatePlayer(Vector3 position, Quaternion rotation, Dictionary<string, string> customData = null)
        {
            using (var builder = new PacketBuilder())
            {
                builder.WriteVector3(position);
                builder.WriteQuaternion(rotation);
                
                // Serialize custom data
                if (customData != null && customData.Count > 0)
                {
                    builder.WriteInt(customData.Count);
                    foreach (var kvp in customData)
                    {
                        builder.WriteString(kvp.Key);
                        builder.WriteString(kvp.Value);
                    }
                }
                else
                {
                    builder.WriteInt(0);
                }
                
                var packet = new Packet(PacketOpcode.PLAYER_UPDATE, builder.Build());
                SendPacket(packet);
            }
        }
        
        /// <summary>
        /// Update only player position
        /// </summary>
        public void UpdatePlayerPosition(Vector3 position)
        {
            UpdatePlayer(position, Quaternion.identity);
        }
        
        // ==================== Chat Methods ====================
        
        /// <summary>
        /// Send a chat message to the current room
        /// </summary>
        public void SendChatMessage(string message)
        {
            if (string.IsNullOrEmpty(currentRoomId))
            {
                Debug.LogWarning("Not in a room, cannot send chat message");
                return;
            }
            
            SendChatMessageToRoom(currentRoomId, message);
        }
        
        /// <summary>
        /// Send a chat message to a specific room
        /// </summary>
        public void SendChatMessageToRoom(string roomId, string message)
        {
            using (var builder = new PacketBuilder())
            {
                builder.WriteString(roomId);
                builder.WriteString(message);
                
                var packet = new Packet(PacketOpcode.CHAT_MESSAGE, builder.Build());
                SendPacket(packet);
            }
        }
        
        /// <summary>
        /// Send a direct message to another player
        /// </summary>
        public void SendDirectMessage(string targetPlayerId, string message)
        {
            using (var builder = new PacketBuilder())
            {
                builder.WriteString(targetPlayerId);
                builder.WriteString(message);
                
                var packet = new Packet(PacketOpcode.CHAT_DIRECT_MESSAGE, builder.Build());
                SendPacket(packet);
            }
        }
        
        /// <summary>
        /// Join a chat room
        /// </summary>
        public void JoinChatRoom(string roomId)
        {
            using (var builder = new PacketBuilder())
            {
                builder.WriteString(roomId);
                
                var packet = new Packet(PacketOpcode.CHAT_ROOM_JOIN, builder.Build());
                SendPacket(packet);
            }
        }
        
        /// <summary>
        /// Leave a chat room
        /// </summary>
        public void LeaveChatRoom(string roomId)
        {
            using (var builder = new PacketBuilder())
            {
                builder.WriteString(roomId);
                
                var packet = new Packet(PacketOpcode.CHAT_ROOM_LEAVE, builder.Build());
                SendPacket(packet);
            }
        }
        
        // ==================== Matchmaking Methods ====================
        
        /// <summary>
        /// Join matchmaking queue
        /// </summary>
        public void JoinMatchmaking(string gameMode, int skillLevel = 0)
        {
            using (var builder = new PacketBuilder())
            {
                builder.WriteString(gameMode);
                builder.WriteInt(skillLevel);
                
                var packet = new Packet(PacketOpcode.MATCHMAKING_QUEUE, builder.Build());
                SendPacket(packet);
            }
        }
        
        /// <summary>
        /// Cancel matchmaking
        /// </summary>
        public void CancelMatchmaking()
        {
            var packet = new Packet(PacketOpcode.MATCHMAKING_CANCEL);
            SendPacket(packet);
        }
        
        // ==================== Voice Methods ====================
        
        /// <summary>
        /// Join a voice channel
        /// </summary>
        public void JoinVoiceChannel(string channelId)
        {
            using (var builder = new PacketBuilder())
            {
                builder.WriteString(channelId);
                
                var packet = new Packet(PacketOpcode.VOICE_JOIN_CHANNEL, builder.Build());
                SendPacket(packet);
            }
        }
        
        /// <summary>
        /// Leave a voice channel
        /// </summary>
        public void LeaveVoiceChannel(string channelId)
        {
            using (var builder = new PacketBuilder())
            {
                builder.WriteString(channelId);
                
                var packet = new Packet(PacketOpcode.VOICE_LEAVE_CHANNEL, builder.Build());
                SendPacket(packet);
            }
        }
        
        /// <summary>
        /// Send voice data
        /// </summary>
        public void SendVoiceData(byte[] audioData)
        {
            var packet = new Packet(PacketOpcode.VOICE_DATA, audioData);
            SendPacket(packet);
        }
        
        /// <summary>
        /// Toggle voice mute
        /// </summary>
        public void SetVoiceMute(bool muted)
        {
            using (var builder = new PacketBuilder())
            {
                builder.WriteBool(muted);
                
                var packet = new Packet(PacketOpcode.VOICE_MUTE, builder.Build());
                SendPacket(packet);
            }
        }
        
        // ==================== Server Management Methods ====================
        
        /// <summary>
        /// Request list of available game servers
        /// </summary>
        public void RequestServerList()
        {
            var packet = new Packet(PacketOpcode.SERVER_LIST);
            SendPacket(packet);
        }
        
        /// <summary>
        /// Send a ping to measure latency
        /// </summary>
        public void Ping()
        {
            using (var builder = new PacketBuilder())
            {
                builder.WriteLong(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                
                var packet = new Packet(PacketOpcode.PING, builder.Build());
                SendPacket(packet);
            }
        }
        
        // ==================== Custom Packet Methods ====================
        
        /// <summary>
        /// Send a custom packet with raw payload
        /// </summary>
        public void SendCustomPacket(PacketOpcode opcode, byte[] payload, PacketFlags flags = PacketFlags.None)
        {
            var packet = new Packet(opcode, payload, flags);
            SendPacket(packet);
        }
        
        // ==================== Internal Packet Handling ====================
        
        private void SendPacket(Packet packet)
        {
            if (!IsConnected)
            {
                Debug.LogError("Cannot send packet: not connected to server");
                return;
            }
            
            try
            {
                byte[] data = packet.Serialize();
                stream.Write(data, 0, data.Length);
                stream.Flush();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to send packet: {ex.Message}");
                OnError?.Invoke($"Send failed: {ex.Message}");
                Disconnect();
            }
        }
        
        private async Task ReceivePacketsAsync(CancellationToken cancellationToken)
        {
            byte[] lengthBuffer = new byte[5]; // Max VarInt size
            byte[] headerBuffer = new byte[5]; // Magic(2) + Version(1) + Flags(1) + Opcode(1)
            
            try
            {
                while (!cancellationToken.IsCancellationRequested && IsConnected)
                {
                    // Read packet header
                    int headerBytesRead = await stream.ReadAsync(headerBuffer, 0, 5, cancellationToken);
                    if (headerBytesRead == 0)
                    {
                        Debug.Log("Server closed connection");
                        break;
                    }
                    
                    // Read payload length (VarInt)
                    using (var ms = new MemoryStream())
                    using (var writer = new BinaryWriter(ms))
                    {
                        writer.Write(headerBuffer);
                        
                        // Read VarInt for length
                        int lengthBytes = 0;
                        byte b;
                        do
                        {
                            b = (byte)stream.ReadByte();
                            writer.Write(b);
                            lengthBytes++;
                        } while ((b & 0x80) != 0 && lengthBytes < 5);
                        
                        // Read payload length value
                        ms.Position = 5;
                        using (var reader = new BinaryReader(ms))
                        {
                            int payloadLength = VarInt.Read(reader);
                            
                            // Read payload
                            byte[] payload = new byte[payloadLength];
                            int payloadBytesRead = 0;
                            while (payloadBytesRead < payloadLength)
                            {
                                int read = await stream.ReadAsync(payload, payloadBytesRead, 
                                    payloadLength - payloadBytesRead, cancellationToken);
                                if (read == 0)
                                    throw new IOException("Connection closed while reading payload");
                                payloadBytesRead += read;
                            }
                            
                            writer.Write(payload);
                            
                            // Read checksum
                            byte[] checksumBytes = new byte[4];
                            await stream.ReadAsync(checksumBytes, 0, 4, cancellationToken);
                            writer.Write(checksumBytes);
                        }
                        
                        // Deserialize complete packet
                        byte[] completePacket = ms.ToArray();
                        var packet = Packet.Deserialize(completePacket);
                        
                        // Queue packet for main thread processing
                        lock (queueLock)
                        {
                            packetQueue.Enqueue(packet);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Debug.Log("Packet receiving cancelled");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error receiving packets: {ex.Message}");
                OnError?.Invoke($"Receive error: {ex.Message}");
            }
            finally
            {
                if (IsConnected)
                {
                    Disconnect();
                }
            }
        }
        
        private void HandlePacket(Packet packet)
        {
            try
            {
                switch (packet.Opcode)
                {
                    case PacketOpcode.AUTH:
                        HandleAuthResponse(packet);
                        break;
                        
                    case PacketOpcode.PLAYER_LOGIN:
                        HandlePlayerLogin(packet);
                        break;
                        
                    case PacketOpcode.CREATE_ROOM:
                        HandleRoomCreated(packet);
                        break;
                        
                    case PacketOpcode.JOIN_ROOM:
                        HandleRoomJoined(packet);
                        break;
                        
                    case PacketOpcode.LEAVE_ROOM:
                        HandleRoomLeft(packet);
                        break;
                        
                    case PacketOpcode.ROOM_LIST:
                        HandleRoomList(packet);
                        break;
                        
                    case PacketOpcode.PLAYER_UPDATE:
                        HandlePlayerUpdate(packet);
                        break;
                        
                    case PacketOpcode.CHAT_MESSAGE:
                        HandleChatMessage(packet);
                        break;
                        
                    case PacketOpcode.CHAT_DIRECT_MESSAGE:
                        HandleDirectMessage(packet);
                        break;
                        
                    case PacketOpcode.MATCHMAKING_FOUND:
                        HandleMatchFound(packet);
                        break;
                        
                    case PacketOpcode.SERVER_LIST:
                        HandleServerList(packet);
                        break;
                        
                    case PacketOpcode.PONG:
                        HandlePong(packet);
                        break;
                        
                    case PacketOpcode.SUCCESS:
                        HandleSuccess(packet);
                        break;
                        
                    case PacketOpcode.ERROR:
                        HandleError(packet);
                        break;
                        
                    default:
                        // Unknown packet - fire custom event
                        OnCustomPacketReceived?.Invoke(packet.Opcode, packet.Payload);
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error handling packet {packet.Opcode}: {ex.Message}");
                OnError?.Invoke($"Packet handling error: {ex.Message}");
            }
        }
        
        private void HandleAuthResponse(Packet packet)
        {
            using (var reader = new PacketReader(packet.Payload))
            {
                bool success = reader.ReadBool();
                string message = reader.ReadString();
                
                if (success)
                {
                    OnAuthenticated?.Invoke(message);
                }
                else
                {
                    OnAuthenticationFailed?.Invoke(message);
                }
            }
        }
        
        private void HandlePlayerLogin(Packet packet)
        {
            using (var reader = new PacketReader(packet.Payload))
            {
                playerId = reader.ReadString();
                string username = reader.ReadString();
                
                OnPlayerLoggedIn?.Invoke(playerId, username);
            }
        }
        
        private void HandleRoomCreated(Packet packet)
        {
            var room = ParseRoomData(packet.Payload);
            currentRoomId = room.RoomId;
            OnRoomCreated?.Invoke(room);
        }
        
        private void HandleRoomJoined(Packet packet)
        {
            var room = ParseRoomData(packet.Payload);
            currentRoomId = room.RoomId;
            OnRoomJoined?.Invoke(room);
        }
        
        private void HandleRoomLeft(Packet packet)
        {
            using (var reader = new PacketReader(packet.Payload))
            {
                string roomId = reader.ReadString();
                if (roomId == currentRoomId)
                {
                    currentRoomId = null;
                }
                OnRoomLeft?.Invoke(roomId);
            }
        }
        
        private void HandleRoomList(Packet packet)
        {
            using (var reader = new PacketReader(packet.Payload))
            {
                int count = reader.ReadInt();
                var rooms = new List<RoomData>();
                
                for (int i = 0; i < count; i++)
                {
                    var room = new RoomData
                    {
                        RoomId = reader.ReadString(),
                        Name = reader.ReadString(),
                        CurrentPlayers = reader.ReadInt(),
                        MaxPlayers = reader.ReadInt()
                    };
                    rooms.Add(room);
                }
                
                OnRoomListReceived?.Invoke(rooms);
            }
        }
        
        private void HandlePlayerUpdate(Packet packet)
        {
            var player = ParsePlayerData(packet.Payload);
            OnPlayerUpdated?.Invoke(player);
        }
        
        private void HandleChatMessage(Packet packet)
        {
            using (var reader = new PacketReader(packet.Payload))
            {
                var message = new ChatMessage
                {
                    RoomId = reader.ReadString(),
                    SenderId = reader.ReadString(),
                    SenderName = reader.ReadString(),
                    Content = reader.ReadString(),
                    Timestamp = reader.ReadLong()
                };
                
                OnChatMessageReceived?.Invoke(message);
            }
        }
        
        private void HandleDirectMessage(Packet packet)
        {
            using (var reader = new PacketReader(packet.Payload))
            {
                var message = new ChatMessage
                {
                    SenderId = reader.ReadString(),
                    SenderName = reader.ReadString(),
                    Content = reader.ReadString(),
                    Timestamp = reader.ReadLong(),
                    IsDirectMessage = true
                };
                
                OnDirectMessageReceived?.Invoke(message);
            }
        }
        
        private void HandleMatchFound(Packet packet)
        {
            var room = ParseRoomData(packet.Payload);
            OnMatchFound?.Invoke(room);
        }
        
        private void HandleServerList(Packet packet)
        {
            using (var reader = new PacketReader(packet.Payload))
            {
                int count = reader.ReadInt();
                var servers = new List<ServerInfo>();
                
                for (int i = 0; i < count; i++)
                {
                    var server = new ServerInfo
                    {
                        ServerId = reader.ReadString(),
                        ServerName = reader.ReadString(),
                        Host = reader.ReadString(),
                        Port = reader.ReadInt(),
                        CurrentPlayers = reader.ReadInt(),
                        MaxPlayers = reader.ReadInt()
                    };
                    servers.Add(server);
                }
                
                OnServerListReceived?.Invoke(servers);
            }
        }
        
        private void HandlePong(Packet packet)
        {
            using (var reader = new PacketReader(packet.Payload))
            {
                long sentTime = reader.ReadLong();
                long currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                long roundTripTime = currentTime - sentTime;
                
                OnPongReceived?.Invoke(roundTripTime);
            }
        }
        
        private void HandleSuccess(Packet packet)
        {
            using (var reader = new PacketReader(packet.Payload))
            {
                string message = reader.ReadString();
                OnSuccess?.Invoke(message);
            }
        }
        
        private void HandleError(Packet packet)
        {
            using (var reader = new PacketReader(packet.Payload))
            {
                string errorMessage = reader.ReadString();
                OnError?.Invoke(errorMessage);
            }
        }
        
        private RoomData ParseRoomData(byte[] payload)
        {
            using (var reader = new PacketReader(payload))
            {
                var room = new RoomData
                {
                    RoomId = reader.ReadString(),
                    Name = reader.ReadString(),
                    MaxPlayers = reader.ReadInt(),
                    CurrentPlayers = reader.ReadInt()
                };
                
                // Read custom data if present
                if (reader.HasRemaining)
                {
                    int customDataCount = reader.ReadInt();
                    for (int i = 0; i < customDataCount; i++)
                    {
                        string key = reader.ReadString();
                        string value = reader.ReadString();
                        room.CustomData[key] = value;
                    }
                }
                
                return room;
            }
        }
        
        private PlayerData ParsePlayerData(byte[] payload)
        {
            using (var reader = new PacketReader(payload))
            {
                var player = new PlayerData
                {
                    PlayerId = reader.ReadString(),
                    Username = reader.ReadString(),
                    Position = reader.ReadVector3(),
                    Rotation = reader.ReadQuaternion()
                };
                
                // Read custom data if present
                if (reader.HasRemaining)
                {
                    int customDataCount = reader.ReadInt();
                    for (int i = 0; i < customDataCount; i++)
                    {
                        string key = reader.ReadString();
                        string value = reader.ReadString();
                        player.CustomData[key] = value;
                    }
                }
                
                return player;
            }
        }
        
        // ==================== IDisposable ====================
        
        public void Dispose()
        {
            if (isDisposed)
                return;
                
            Disconnect();
            isDisposed = true;
        }
    }
}
