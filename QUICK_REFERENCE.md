# NyxNet Unity Client - Quick Reference

## Installation
1. Extract `NyxNetUnityClient.zip`
2. Copy all `.cs` files to `Assets/Scripts/NyxNet/`
3. Set API Compatibility to .NET 4.x or .NET Standard 2.1

## Minimal Working Example

```csharp
using UnityEngine;
using NyxNet;

public class Simple : MonoBehaviour
{
    private NyxNetClient client;
    
    async void Start()
    {
        client = new NyxNetClient();
        client.OnConnected += () => Debug.Log("✓ Connected!");
        
        await client.ConnectAsync("localhost", 9000);
        client.AuthenticateSimple("username", "password");
        client.Login("Player");
    }
    
    void Update() => client?.ProcessPackets();
    void OnDestroy() => client?.Dispose();
}
```

## Essential Methods

### Connection
```csharp
await client.ConnectAsync(host, port);
client.Disconnect();
```

### Authentication
```csharp
client.AuthenticateSimple(username, password);
client.Login(username);
```

### Rooms
```csharp
client.CreateRoom(name, maxPlayers);
client.JoinRoom(roomId);
client.LeaveRoom();
```

### Player Updates
```csharp
client.UpdatePlayer(position, rotation);
```

### Chat
```csharp
client.SendChatMessage("Hello!");
client.SendDirectMessage(playerId, "Hi!");
```

### Matchmaking
```csharp
client.JoinMatchmaking(gameMode, skillLevel);
```

## Important Events

```csharp
client.OnConnected += () => { };
client.OnPlayerLoggedIn += (id, name) => { };
client.OnRoomJoined += (room) => { };
client.OnPlayerUpdated += (player) => { };
client.OnChatMessageReceived += (msg) => { };
client.OnError += (error) => { };
```

## File Structure

```
NyxNetUnityClient/
├── VarInt.cs, VarLong.cs         # Encoding utilities
├── PacketOpcode.cs, PacketFlags.cs # Protocol definitions
├── Packet.cs                      # Core packet class
├── PacketBuilder.cs, PacketReader.cs # Payload helpers
├── PlayerData.cs, RoomData.cs    # Data models
├── ChatMessage.cs, ServerInfo.cs # More models
├── NyxNetClient.cs               # Main client
└── Examples/
    └── NyxNetExample.cs          # Full example
```

## Key Points

✓ Always call `ProcessPackets()` in `Update()`
✓ Always `Dispose()` the client in `OnDestroy()`
✓ Subscribe to events BEFORE connecting
✓ Use async/await for `ConnectAsync()`
✓ Check `IsConnected` before sending packets

## Server Requirements

NyxNet server must be running:
- Master Server: default port 9000
- Name Server: default port 8888

## Troubleshooting

**Connection fails:** Check server is running, correct host/port
**No packets received:** Ensure `ProcessPackets()` is called in Update
**Build errors:** Set API Compatibility Level to .NET 4.x

## Full Documentation

See `README.md` for complete API documentation
See `SETUP_GUIDE.md` for detailed setup instructions
See `Examples/NyxNetExample.cs` for full implementation

## Support

GitHub: https://github.com/nyxikitty/NyxNetGameServer
