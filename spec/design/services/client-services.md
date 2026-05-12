# Client Services

Design documentation for the remote faction server client infrastructure (Req 11: Client Connectivity, Req 16: Data Portability).

## Overview

The `Client/` folder contains infrastructure classes for communicating with the Remote Faction Service. These classes handle HTTP connectivity, offline queuing, and synchronization.

## Classes

### RemoteFactionClient

HTTP client that communicates with the Remote Faction Service API. Handles:
- Bearer-token authentication on all requests
- Self-signed certificate pinning via thumbprint validation
- Connection status tracking with events
- Health checks, CRUD operations, sync snapshots, and data export

### SyncManager

Coordinates data flow between local storage and the remote server:
- Write-through: when local data changes, also writes to server
- Offline queuing: when disconnected, queues changes for later
- Queue flushing: on reconnection, replays queued changes
- Operating mode awareness (LocalOnly, ServerOnly, ServerAndLocal)

### OfflineQueue

Persists queued data changes to `%LOCALAPPDATA%\OE2EmpireTracker\offline-queue.json`:
- Enqueue/dequeue changes
- Persist to and load from disk
- Survives application restarts

### QueuedChange

Data class representing a single queued change:
- CharacterUUID, DataType, Json payload, QueuedUtc timestamp

### OperatingMode

Enum defining data access modes:
- `LocalOnly` — no server connection
- `ServerOnly` — all reads/writes go to server
- `ServerAndLocal` — dual-write (server primary, local backup)

### ServerConnectionSettings

Settings POCO for the remote server connection:
- ServerUrl, BearerToken, TrustedThumbprint
- Mode (OperatingMode), DualWriteEnabled flag

### ConnectionStatusChangedEventArgs

Event args for connection status change notifications:
- IsConnected flag, human-readable Message
