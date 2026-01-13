# NyxNet Unity Client

A complete C# client library for Unity that connects to the [NyxNet Game Server](https://github.com/nyxikitty/NyxNetGameServer).

## Features

- ✅ Full NyxNet protocol implementation with binary serialization
- ✅ VarInt/VarLong encoding for efficient network traffic
- ✅ Async/await TCP connection handling
- ✅ Automatic packet queue for main thread processing
- ✅ Event-driven architecture
- ✅ Authentication support (Simple, OAuth, API Key)
- ✅ Room management (create, join, leave)
- ✅ Player state synchronization
- ✅ Real-time chat messaging
- ✅ Matchmaking system
- ✅ Voice channel support
- ✅ Server discovery
- ✅ Ping/latency measurement
- ✅ Unity-friendly Vector3/Quaternion serialization

## Installation

1. Copy all `.cs` files to your Unity project's `Assets/Scripts/NyxNet/` folder
2. Ensure your project targets .NET 4.x or .NET Standard 2.1+

## Quick Start

### Basic Connection Example

```csharp
using UnityEngine;
using NyxNet;

public class GameManager : MonoBehaviour
{
    private NyxNetClient client;
    
    async void Start()
    {
        // Create client
        client = new NyxNetClient();
        
        // Subscribe to events
        client.OnConnected += () => Debug.Log("Connected!");
        client.OnPlayerLoggedIn += (id, name) => Debug.Log($"Logged in as {name}");
        
        // Connect to server
        await client.ConnectAsync("localhost", 9000);
        
        // Authenticate
        client.AuthenticateSimple("username", "password");
        
        // Login
        client.Login("MyPlayer");
    }
    
    void Update()
    {
        // Process packets on main thread
        client?.ProcessPackets();
    }
    
    void OnDestroy()
    {
        client?.Dispose();
    }
}
```

## Usage Guide

### Connection & Authentication

```csharp
// Connect to server
await client.ConnectAsync("game.server.com", 9000);

// Authenticate with simple auth
client.AuthenticateSimple("username", "password");

// Or authenticate with API key
client.AuthenticateApiKey("your-api-key");

// Login as player
client.Login("PlayerName");

// Logout
client.Logout();

// Disconnect
client.Disconnect();
```

### Room Management

```csharp
// Create a room
client.CreateRoom("My Room", maxPlayers: 10);

// Join a room
client.JoinRoom("room-id-123");

// Leave current room
client.LeaveRoom();

// Get room list
client.RequestRoomList();
```

### Player Updates

```csharp
// Update player position and rotation
client.UpdatePlayer(transform.position, transform.rotation);

// Update with custom data
var customData = new Dictionary<string, string>
{
    { "health", "100" },
    { "score", "1500" }
};
client.UpdatePlayer(transform.position, transform.rotation, customData);
```

### Chat

```csharp
// Send chat message to current room
client.SendChatMessage("Hello everyone!");

// Send message to specific room
client.SendChatMessageToRoom("room-id", "Hello!");

// Send direct message to player
client.SendDirectMessage("player-id", "Hey there!");

// Join chat room
client.JoinChatRoom("lobby");
```

### Matchmaking

```csharp
// Join matchmaking queue
client.JoinMatchmaking("casual", skillLevel: 1000);

// Cancel matchmaking
client.CancelMatchmaking();

// Handle match found
client.OnMatchFound += (room) => {
    Debug.Log($"Match found! Room: {room.Name}");
    client.JoinRoom(room.RoomId);
};
```

### Voice

```csharp
// Join voice channel
client.JoinVoiceChannel("team-voice");

// Send voice data
client.SendVoiceData(audioBytes);

// Mute/unmute
client.SetVoiceMute(true);

// Leave voice channel
client.LeaveVoiceChannel("team-voice");
```

### Server Management

```csharp
// Get server list
client.RequestServerList();

// Measure ping
client.Ping();
client.OnPongReceived += (rtt) => {
    Debug.Log($"Ping: {rtt}ms");
};
```

## Events Reference

### Connection Events
- `OnConnected` - Fired when connection is established
- `OnDisconnected` - Fired when disconnected from server

### Authentication Events
- `OnAuthenticated(message)` - Authentication succeeded
- `OnAuthenticationFailed(message)` - Authentication failed
- `OnPlayerLoggedIn(playerId, username)` - Player logged in successfully

### Room Events
- `OnRoomCreated(room)` - Room was created
- `OnRoomJoined(room)` - Joined a room
- `OnRoomLeft(roomId)` - Left a room
- `OnRoomListReceived(rooms)` - Received list of rooms

### Player Events
- `OnPlayerJoinedRoom(player)` - Another player joined current room
- `OnPlayerLeftRoom(playerId)` - Player left current room
- `OnPlayerUpdated(player)` - Player state updated

### Chat Events
- `OnChatMessageReceived(message)` - Chat message received
- `OnDirectMessageReceived(message)` - Direct message received

### Matchmaking Events
- `OnMatchFound(room)` - Matchmaking found a match

### Server Events
- `OnServerListReceived(servers)` - Received server list
- `OnPongReceived(roundTripTime)` - Ping response received

### Generic Events
- `OnSuccess(message)` - Generic success response
- `OnError(message)` - Error occurred
- `OnCustomPacketReceived(opcode, payload)` - Custom/unknown packet received

## Protocol Details

### Packet Structure

```
[Magic: 0x42 0x4E] [Version: 0x01] [Flags] [Opcode] [Length: VarInt] [Payload] [Checksum: CRC32]
```

### Opcodes

| Category | Opcode | Value | Description |
|----------|--------|-------|-------------|
| **Auth** | AUTH | 0x01 | Authentication request |
| | PLAYER_LOGIN | 0x02 | Player login |
| | PLAYER_LOGOUT | 0x03 | Player logout |
| **Server** | REGISTER_SERVER | 0x10 | Register game server |
| | SERVER_LIST | 0x11 | Request server list |
| | PING | 0x12 | Ping request |
| | PONG | 0x13 | Pong response |
| **Game** | CREATE_ROOM | 0x20 | Create room |
| | JOIN_ROOM | 0x21 | Join room |
| | LEAVE_ROOM | 0x22 | Leave room |
| | PLAYER_UPDATE | 0x23 | Update player state |
| | ROOM_UPDATE | 0x24 | Room state update |
| **Chat** | CHAT_MESSAGE | 0x30 | Send chat message |
| | CHAT_ROOM_JOIN | 0x31 | Join chat room |
| | CHAT_DIRECT_MESSAGE | 0x32 | Send DM |
| **Voice** | VOICE_JOIN_CHANNEL | 0x40 | Join voice channel |
| | VOICE_DATA | 0x41 | Voice data packet |
| | VOICE_MUTE | 0x42 | Mute/unmute |
| **Matchmaking** | MATCHMAKING_QUEUE | 0x50 | Join queue |
| | MATCHMAKING_CANCEL | 0x51 | Cancel matchmaking |
| | MATCHMAKING_FOUND | 0x52 | Match found |

## Advanced Usage

### Custom Packets

```csharp
// Send custom packet
var builder = new PacketBuilder();
builder.WriteString("custom_data");
builder.WriteInt(42);
byte[] payload = builder.Build();

client.SendCustomPacket(PacketOpcode.CUSTOM, payload);

// Handle custom packets
client.OnCustomPacketReceived += (opcode, payload) => {
    using (var reader = new PacketReader(payload))
    {
        string data = reader.ReadString();
        int value = reader.ReadInt();
        // Process custom data...
    }
};
```

### Building Custom Payloads

```csharp
using (var builder = new PacketBuilder())
{
    builder.WriteString("Hello");
    builder.WriteInt(123);
    builder.WriteFloat(45.67f);
    builder.WriteBool(true);
    builder.WriteVector3(new Vector3(1, 2, 3));
    builder.WriteQuaternion(Quaternion.identity);
    
    byte[] payload = builder.Build();
}
```

### Reading Custom Payloads

```csharp
using (var reader = new PacketReader(payload))
{
    string text = reader.ReadString();
    int number = reader.ReadInt();
    float decimal = reader.ReadFloat();
    bool flag = reader.ReadBool();
    Vector3 position = reader.ReadVector3();
    Quaternion rotation = reader.ReadQuaternion();
}
```

## File Structure

```
NyxNetUnityClient/
├── Protocol/
│   ├── VarInt.cs              # Variable-length integer encoding
│   ├── VarLong.cs             # Variable-length long encoding
│   ├── PacketOpcode.cs        # Packet operation codes
│   ├── PacketFlags.cs         # Packet flags enum
│   ├── Packet.cs              # Core packet class
│   ├── PacketBuilder.cs       # Helper for building packets
│   └── PacketReader.cs        # Helper for reading packets
├── Models/
│   ├── PlayerData.cs          # Player data model
│   ├── RoomData.cs            # Room data model
│   ├── ChatMessage.cs         # Chat message model
│   └── ServerInfo.cs          # Server info model
├── NyxNetClient.cs            # Main client class
└── Examples/
    └── NyxNetExample.cs       # Example MonoBehaviour
```

## Requirements

- Unity 2020.3 or higher
- .NET 4.x or .NET Standard 2.1+
- NyxNet Game Server (Node.js/TypeScript)

## Thread Safety

The client handles network I/O on a background thread and queues packets for processing on Unity's main thread. Always call `ProcessPackets()` in your `Update()` method:

```csharp
void Update()
{
    client?.ProcessPackets();
}
```

## Error Handling

```csharp
client.OnError += (error) => {
    Debug.LogError($"NyxNet Error: {error}");
    // Handle error (reconnect, show UI message, etc.)
};

client.OnDisconnected += () => {
    Debug.Log("Lost connection to server");
    // Attempt reconnection or return to menu
};
```

## Performance Tips

1. **Throttle Updates**: Don't send player updates every frame
   ```csharp
   private float updateInterval = 0.05f; // 20 updates/sec
   private float nextUpdateTime = 0f;
   
   void Update()
   {
       if (Time.time >= nextUpdateTime)
       {
           client.UpdatePlayer(transform.position, transform.rotation);
           nextUpdateTime = Time.time + updateInterval;
       }
   }
   ```

2. **Buffer Size**: Adjust receive buffer for your needs
   ```csharp
   client.ReceiveBufferSize = 16384; // 16KB for high-traffic games
   ```

3. **Dispose Properly**: Always dispose the client
   ```csharp
   void OnDestroy()
   {
       client?.Dispose();
   }
   ```

## Troubleshooting

### Connection Issues
- Ensure the server is running and accessible
- Check firewall settings
- Verify the correct host and port

### Packet Not Received
- Ensure `ProcessPackets()` is called in `Update()`
- Check if event handlers are subscribed before connecting

### Authentication Fails
- Verify credentials are correct
- Check auth plugin is enabled on server
- Review server logs for details

## License

MIT License - See LICENSE file for details

## Contributing

Contributions are welcome! Please submit issues and pull requests to the GitHub repository.

## Links

- [NyxNet Game Server](https://github.com/nyxikitty/NyxNetGameServer)
- [Server Documentation](https://github.com/nyxikitty/NyxNetGameServer/blob/main/README.md)

## Support

For questions and support:
- Open an issue on GitHub
- Check the example code in `Examples/NyxNetExample.cs`
- Review the server's README for protocol details
