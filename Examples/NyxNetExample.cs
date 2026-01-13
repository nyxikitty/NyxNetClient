using UnityEngine;
using NyxNet;
using NyxNet.Models;

/// <summary>
/// Example Unity component showing how to use the NyxNet client
/// </summary>
public class NyxNetExample : MonoBehaviour
{
    [Header("Connection Settings")]
    [SerializeField] private string serverHost = "localhost";
    [SerializeField] private int serverPort = 9000;
    
    [Header("Player Settings")]
    [SerializeField] private string username = "Player";
    [SerializeField] private string password = "password";
    
    [Header("Room Settings")]
    [SerializeField] private string roomName = "Test Room";
    [SerializeField] private int maxPlayers = 10;
    
    private NyxNetClient client;
    private bool isConnected = false;
    
    void Start()
    {
        // Create client instance
        client = new NyxNetClient();
        
        // Subscribe to events
        client.OnConnected += OnConnected;
        client.OnDisconnected += OnDisconnected;
        client.OnAuthenticated += OnAuthenticated;
        client.OnAuthenticationFailed += OnAuthenticationFailed;
        client.OnPlayerLoggedIn += OnPlayerLoggedIn;
        client.OnRoomCreated += OnRoomCreated;
        client.OnRoomJoined += OnRoomJoined;
        client.OnRoomLeft += OnRoomLeft;
        client.OnPlayerJoinedRoom += OnPlayerJoinedRoom;
        client.OnPlayerLeftRoom += OnPlayerLeftRoom;
        client.OnPlayerUpdated += OnPlayerUpdated;
        client.OnChatMessageReceived += OnChatMessageReceived;
        client.OnDirectMessageReceived += OnDirectMessageReceived;
        client.OnMatchFound += OnMatchFound;
        client.OnError += OnError;
        
        Debug.Log("NyxNet client initialized. Press 'C' to connect, 'D' to disconnect.");
    }
    
    void Update()
    {
        // Process packets on main thread
        if (client != null && isConnected)
        {
            client.ProcessPackets();
        }
        
        // Example controls
        if (Input.GetKeyDown(KeyCode.C) && !isConnected)
        {
            ConnectToServer();
        }
        
        if (Input.GetKeyDown(KeyCode.D) && isConnected)
        {
            DisconnectFromServer();
        }
        
        // Update player position every frame if connected and in a room
        if (isConnected && !string.IsNullOrEmpty(client.CurrentRoomId))
        {
            UpdatePlayerPosition();
        }
    }
    
    void OnDestroy()
    {
        // Clean up
        if (client != null)
        {
            client.Dispose();
        }
    }
    
    // ==================== Connection Methods ====================
    
    private async void ConnectToServer()
    {
        Debug.Log($"Connecting to {serverHost}:{serverPort}...");
        bool success = await client.ConnectAsync(serverHost, serverPort);
        
        if (success)
        {
            Debug.Log("Connected! Authenticating...");
            client.AuthenticateSimple(username, password);
        }
    }
    
    private void DisconnectFromServer()
    {
        Debug.Log("Disconnecting...");
        client.Disconnect();
    }
    
    // ==================== Event Handlers ====================
    
    private void OnConnected()
    {
        Debug.Log("✓ Connected to NyxNet server");
        isConnected = true;
    }
    
    private void OnDisconnected()
    {
        Debug.Log("✗ Disconnected from NyxNet server");
        isConnected = false;
    }
    
    private void OnAuthenticated(string message)
    {
        Debug.Log($"✓ Authenticated: {message}");
        
        // Login as player
        client.Login(username);
    }
    
    private void OnAuthenticationFailed(string message)
    {
        Debug.LogError($"✗ Authentication failed: {message}");
    }
    
    private void OnPlayerLoggedIn(string playerId, string username)
    {
        Debug.Log($"✓ Logged in as {username} (ID: {playerId})");
        
        // Create or join a room
        client.CreateRoom(roomName, maxPlayers);
        // Or: client.JoinRoom("room-id-here");
    }
    
    private void OnRoomCreated(RoomData room)
    {
        Debug.Log($"✓ Room created: {room}");
    }
    
    private void OnRoomJoined(RoomData room)
    {
        Debug.Log($"✓ Joined room: {room}");
        
        // Send a chat message
        client.SendChatMessage("Hello everyone!");
    }
    
    private void OnRoomLeft(string roomId)
    {
        Debug.Log($"✓ Left room: {roomId}");
    }
    
    private void OnPlayerJoinedRoom(PlayerData player)
    {
        Debug.Log($"→ Player joined: {player}");
    }
    
    private void OnPlayerLeftRoom(string playerId)
    {
        Debug.Log($"← Player left: {playerId}");
    }
    
    private void OnPlayerUpdated(PlayerData player)
    {
        Debug.Log($"↻ Player updated: {player}");
        
        // Update player GameObject position
        // GameObject playerObj = GetPlayerGameObject(player.PlayerId);
        // if (playerObj != null)
        // {
        //     playerObj.transform.position = player.Position;
        //     playerObj.transform.rotation = player.Rotation;
        // }
    }
    
    private void OnChatMessageReceived(ChatMessage message)
    {
        Debug.Log($"💬 [{message.SenderName}]: {message.Content}");
    }
    
    private void OnDirectMessageReceived(ChatMessage message)
    {
        Debug.Log($"📨 DM from [{message.SenderName}]: {message.Content}");
    }
    
    private void OnMatchFound(RoomData room)
    {
        Debug.Log($"🎮 Match found! Joining room: {room}");
        client.JoinRoom(room.RoomId);
    }
    
    private void OnError(string error)
    {
        Debug.LogError($"❌ Error: {error}");
    }
    
    // ==================== Game Logic ====================
    
    private void UpdatePlayerPosition()
    {
        // Example: send player position and rotation
        Vector3 position = transform.position;
        Quaternion rotation = transform.rotation;
        
        client.UpdatePlayer(position, rotation);
    }
    
    // ==================== UI Methods (call from buttons) ====================
    
    public void OnSendChatButtonClicked(string message)
    {
        if (isConnected && !string.IsNullOrEmpty(client.CurrentRoomId))
        {
            client.SendChatMessage(message);
        }
    }
    
    public void OnLeaveRoomButtonClicked()
    {
        if (isConnected && !string.IsNullOrEmpty(client.CurrentRoomId))
        {
            client.LeaveRoom();
        }
    }
    
    public void OnJoinMatchmakingButtonClicked()
    {
        if (isConnected)
        {
            client.JoinMatchmaking("casual", 1000);
        }
    }
}
