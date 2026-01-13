# Installation & Setup Guide

## Installation

### Method 1: Copy Files Directly

1. Download and extract the NyxNetUnityClient.zip
2. Copy all `.cs` files to your Unity project:
   ```
   Assets/
   └── Scripts/
       └── NyxNet/
           ├── Protocol/
           │   ├── VarInt.cs
           │   ├── VarLong.cs
           │   ├── PacketOpcode.cs
           │   ├── PacketFlags.cs
           │   ├── Packet.cs
           │   ├── PacketBuilder.cs
           │   └── PacketReader.cs
           ├── Models/
           │   ├── PlayerData.cs
           │   ├── RoomData.cs
           │   ├── ChatMessage.cs
           │   └── ServerInfo.cs
           └── NyxNetClient.cs
   ```

3. Copy the example to your project (optional):
   ```
   Assets/
   └── Scripts/
       └── NyxNetExample.cs
   ```

### Method 2: Unity Package Manager (Git URL)

1. Open Unity Package Manager (Window > Package Manager)
2. Click the '+' button
3. Select "Add package from git URL"
4. Enter the repository URL (when published)

## Project Configuration

### 1. Configure Player Settings

Go to `Edit > Project Settings > Player`:

- **Scripting Backend**: Mono or IL2CPP (both supported)
- **API Compatibility Level**: .NET 4.x or .NET Standard 2.1
- **Allow 'unsafe' Code**: Not required (but can enable if needed)

### 2. Configure Build Settings

For different platforms:

**Windows/Mac/Linux:**
- No special configuration needed
- Networking works out of the box

**Android:**
- Add `INTERNET` permission in AndroidManifest.xml
- Go to `Player Settings > Android > Other Settings`
- Check "Require" for Internet Access

**iOS:**
- Networking permissions are automatic
- Ensure you're not blocking sockets in Info.plist

**WebGL:**
- Current implementation uses System.Net.Sockets (TCP)
- For WebGL, you'll need WebSocket support (future update)
- Consider using a proxy server for now

## Basic Scene Setup

### 1. Create a Game Manager

Create a new script `GameManager.cs`:

```csharp
using UnityEngine;
using NyxNet;

public class GameManager : MonoBehaviour
{
    [Header("Server Settings")]
    public string serverHost = "localhost";
    public int serverPort = 9000;
    
    [Header("Player Settings")]
    public string playerUsername = "Player";
    
    private NyxNetClient client;
    
    void Start()
    {
        InitializeClient();
    }
    
    void Update()
    {
        client?.ProcessPackets();
    }
    
    void OnDestroy()
    {
        client?.Dispose();
    }
    
    private async void InitializeClient()
    {
        client = new NyxNetClient();
        
        // Subscribe to events
        client.OnConnected += OnConnected;
        client.OnDisconnected += OnDisconnected;
        client.OnError += OnError;
        
        // Connect
        await client.ConnectAsync(serverHost, serverPort);
    }
    
    private void OnConnected()
    {
        Debug.Log("Connected to server!");
        // Authenticate and login here
    }
    
    private void OnDisconnected()
    {
        Debug.Log("Disconnected from server!");
    }
    
    private void OnError(string error)
    {
        Debug.LogError($"Error: {error}");
    }
}
```

### 2. Add to Scene

1. Create empty GameObject: `GameObject > Create Empty`
2. Rename it to "GameManager"
3. Add the GameManager component
4. Configure server host and port in the Inspector

### 3. Test Connection

1. Ensure NyxNet server is running
2. Press Play in Unity
3. Check Console for connection messages

## Example: Complete Multiplayer Setup

Here's a complete example with player spawning and movement:

### PlayerController.cs

```csharp
using UnityEngine;
using NyxNet;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float rotateSpeed = 180f;
    public float updateRate = 20f; // Updates per second
    
    private NyxNetClient client;
    private float nextUpdateTime;
    private Dictionary<string, GameObject> otherPlayers = new Dictionary<string, GameObject>();
    
    public void Initialize(NyxNetClient client)
    {
        this.client = client;
        
        // Subscribe to player events
        client.OnPlayerJoinedRoom += OnPlayerJoined;
        client.OnPlayerLeftRoom += OnPlayerLeft;
        client.OnPlayerUpdated += OnPlayerUpdated;
    }
    
    void Update()
    {
        HandleInput();
        SendPositionUpdate();
    }
    
    private void HandleInput()
    {
        // Movement
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        Vector3 movement = new Vector3(horizontal, 0, vertical) * moveSpeed * Time.deltaTime;
        transform.Translate(movement, Space.World);
        
        // Rotation
        if (movement.magnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(movement);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, targetRotation, rotateSpeed * Time.deltaTime);
        }
    }
    
    private void SendPositionUpdate()
    {
        if (Time.time >= nextUpdateTime)
        {
            client?.UpdatePlayer(transform.position, transform.rotation);
            nextUpdateTime = Time.time + (1f / updateRate);
        }
    }
    
    private void OnPlayerJoined(NyxNet.Models.PlayerData player)
    {
        if (player.PlayerId == client.PlayerId)
            return; // Don't spawn ourselves
        
        // Spawn other player
        GameObject otherPlayer = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        otherPlayer.name = player.Username;
        otherPlayer.transform.position = player.Position;
        otherPlayer.transform.rotation = player.Rotation;
        
        otherPlayers[player.PlayerId] = otherPlayer;
    }
    
    private void OnPlayerLeft(string playerId)
    {
        if (otherPlayers.TryGetValue(playerId, out GameObject player))
        {
            Destroy(player);
            otherPlayers.Remove(playerId);
        }
    }
    
    private void OnPlayerUpdated(NyxNet.Models.PlayerData player)
    {
        if (otherPlayers.TryGetValue(player.PlayerId, out GameObject playerObj))
        {
            // Smooth movement
            playerObj.transform.position = Vector3.Lerp(
                playerObj.transform.position, player.Position, Time.deltaTime * 10f);
            playerObj.transform.rotation = Quaternion.Slerp(
                playerObj.transform.rotation, player.Rotation, Time.deltaTime * 10f);
        }
    }
}
```

### Enhanced GameManager.cs

```csharp
using UnityEngine;
using NyxNet;

public class EnhancedGameManager : MonoBehaviour
{
    [Header("Server")]
    public string serverHost = "localhost";
    public int serverPort = 9000;
    
    [Header("Player")]
    public string username = "Player";
    public GameObject playerPrefab;
    
    private NyxNetClient client;
    private GameObject localPlayer;
    
    async void Start()
    {
        client = new NyxNetClient();
        
        // Subscribe to events
        client.OnConnected += OnConnected;
        client.OnPlayerLoggedIn += OnPlayerLoggedIn;
        client.OnRoomJoined += OnRoomJoined;
        
        // Connect
        await client.ConnectAsync(serverHost, serverPort);
    }
    
    void Update()
    {
        client?.ProcessPackets();
    }
    
    void OnDestroy()
    {
        client?.Dispose();
    }
    
    private void OnConnected()
    {
        // Authenticate and login
        client.AuthenticateSimple(username, "password");
        client.Login(username);
    }
    
    private void OnPlayerLoggedIn(string playerId, string username)
    {
        // Create or join a room
        client.CreateRoom("Test Room", 10);
    }
    
    private void OnRoomJoined(NyxNet.Models.RoomData room)
    {
        // Spawn local player
        SpawnLocalPlayer();
    }
    
    private void SpawnLocalPlayer()
    {
        if (playerPrefab != null)
        {
            localPlayer = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
        }
        else
        {
            localPlayer = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        }
        
        localPlayer.name = "Local Player";
        
        // Add controller
        var controller = localPlayer.AddComponent<PlayerController>();
        controller.Initialize(client);
    }
}
```

## Scene Hierarchy Example

```
Scene
├── GameManager (EnhancedGameManager.cs)
├── Main Camera
├── Directional Light
└── Environment
    ├── Ground (Plane)
    └── Walls
```

## Server Setup

Before testing, ensure the NyxNet server is running:

```bash
# Clone the server
git clone https://github.com/nyxikitty/NyxNetGameServer.git
cd NyxNetGameServer

# Install dependencies
npm install

# Run the server
npm run dev
```

The server will start on:
- Name Server: `localhost:8888`
- Master Server: `localhost:9000`
- Game Servers: `localhost:9001-9003`

## Testing Checklist

- [ ] Server is running
- [ ] Unity project configured (.NET 4.x)
- [ ] NyxNet client files copied
- [ ] GameManager added to scene
- [ ] Server host/port configured
- [ ] Play mode shows "Connected" in console
- [ ] No red errors in console

## Common Issues

### "Connection refused"
- Server not running
- Wrong host/port
- Firewall blocking connection

### "Namespace not found"
- Files not in correct location
- Missing `using NyxNet;`
- Wrong API compatibility level

### "Packets not processing"
- Forgot to call `ProcessPackets()` in Update
- Client not initialized

### "No player movement sync"
- Not in a room
- UpdatePlayer not being called
- Event handlers not subscribed

## Next Steps

1. Review the full example: `Examples/NyxNetExample.cs`
2. Read the complete API documentation in `README.md`
3. Implement your game logic
4. Test with multiple clients
5. Deploy server to production environment

## Support

Need help? Check:
- README.md for full documentation
- NyxNetExample.cs for reference implementation
- GitHub issues for common problems
- Server documentation for protocol details
