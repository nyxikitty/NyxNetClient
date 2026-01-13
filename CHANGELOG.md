# Changelog

All notable changes to the NyxNet Unity Client will be documented in this file.

## [1.0.0] - 2026-01-12

### Added
- Initial release of NyxNet Unity Client
- Full protocol implementation matching NyxNet Game Server v1
- VarInt/VarLong encoding for efficient serialization
- Async TCP connection handling with automatic reconnection support
- Event-driven architecture for all server responses
- Authentication support:
  - Simple username/password auth
  - OAuth 2.0 integration
  - API key authentication
- Room management:
  - Create rooms with custom properties
  - Join/leave rooms
  - Room list retrieval
- Player synchronization:
  - Position and rotation updates
  - Custom player data support
- Chat system:
  - Room-based chat
  - Direct messaging
  - Chat room management
- Matchmaking:
  - Queue system with skill-based matching
  - Match found notifications
- Voice support:
  - Join/leave voice channels
  - Voice data transmission
  - Mute controls
- Server discovery and management
- Ping/latency measurement
- Comprehensive example MonoBehaviour
- Full documentation with usage examples
- Thread-safe packet queue for Unity main thread processing

### Protocol Features
- Binary packet serialization with CRC32 checksums
- Packet compression support
- Packet encryption support (flags ready)
- Fragmented packet support (flags ready)
- Acknowledgment system (flags ready)

### Developer Features
- PacketBuilder for easy payload construction
- PacketReader for convenient payload parsing
- Unity-friendly types (Vector3, Quaternion, Color)
- JSON serialization helpers
- Custom packet extension support
- Comprehensive error handling
- Proper IDisposable implementation

### Documentation
- Complete README with quick start guide
- API reference for all public methods
- Event documentation
- Protocol specification
- Performance optimization tips
- Troubleshooting guide
- Example implementations

## [Future]

### Planned Features
- Reconnection with session recovery
- Automatic compression for large payloads
- Built-in encryption with key exchange
- Packet acknowledgment system implementation
- Bandwidth monitoring and optimization
- Connection quality metrics
- NAT punchthrough for P2P
- WebSocket support for WebGL builds
- Unity package manager integration
- Additional authentication methods (Steam, Discord, etc.)
- Voice codec integration
- Lobby system
- Leaderboard support
- Achievement system
- In-game store integration
