# Networking Implementation Guide

This document provides an overview of the new networking architecture based on LiteNetLib, designed for an open-world survival shooter looter game.

## Architecture Overview

The networking system is divided into three main layers:

1.  **Transport Layer**: Handles raw network communication.
    *   `ServerManager.cs`: Manages server-side connections, listens for clients, and sends/receives packets. Also handles server discovery (pings).
    *   `ClientManager.cs`: Manages client-side connections and sends/receives packets. Handles scene loading.
    *   `ServerBrowser.cs`: A client-side tool to discover servers on the local network.

2.  **Routing & Management Layer**: Manages networked entities and routes packets to them.
    *   `NetworkEntityManager.cs`: Keeps track of all `NetworkEntity` objects in the scene.
    *   `InventoryNetManager.cs`: Manages all networked inventories, handles synchronization, and processes item operations.
    *   `InterestManager.cs`: A server-side system that determines which entities are visible to which clients, sending Spawn and Destroy packets as needed.

3.  **Game Object Layer**: The actual networked objects in your game.
    *   `NetworkEntity.cs`: The base class for any object that needs to be synchronized over the network. It contains `OwnerId`, `EntityId`, and `TypeId`.
    *   `PredictedEntity.cs`: An abstract class that extends `NetworkEntity` for objects that require client-side prediction and server reconciliation (e.g., player characters).
    *   `NetworkedInventory.cs`: A component that gives any `NetworkEntity` a synchronized inventory.

## Connection Flow

1.  **Discovery**: The client uses `ServerBrowser` to send a broadcast ping.
2.  **Response**: `ServerManager` receives the ping and sends back an unconnected message with server info (name, player count, etc.).
3.  **Connect**: The client connects to the selected server.
4.  **Join Accept**: The server accepts the connection and sends a `JoinAcceptPacket` containing the `MapName`.
5.  **Scene Load**: `ClientManager` receives the packet and loads the specified map scene.
6.  **Client Ready**: Once the scene is loaded, `ClientManager` sends a `ClientReadyPacket` to the server.
7.  **Player Spawn**: `NetworkGameManager` on the server receives this and spawns the player character.
8.  **World Sync**: `InterestManager` on the server detects the new player and starts sending `SpawnPacket`s for all nearby entities.

## Scalability: Interest Management

The `InterestManager.cs` is the core of the "entity-based" system you requested.

*   **How it works**: It runs on the server and periodically checks the distance between each player and every other entity in the world.
*   **Spawning**: If an entity enters a player's `InterestRadius`, the manager sends a `SpawnPacket` for that entity to that specific player.
*   **Destroying**: If an entity leaves the `InterestRadius`, the manager sends an `EntityDestroyPacket` to that player, who then removes the object from their scene.
*   **Benefit**: This ensures that players only receive data for what's around them, dramatically reducing bandwidth and client-side processing for a large open world.

## Detailed Implementation Examples

### 1. Player Character (Movement & Prediction)

The `Character.cs` script is your reference. It inherits from `PredictedEntity`.

*   **Client**: In `FixedUpdate`, gather input (WASD, Mouse), apply it locally (Prediction), and send `PlayerInputPacket` to the server.
*   **Server**: Receives `PlayerInputPacket`, validates it (Anti-Cheat check: is the move distance reasonable?), applies it, and broadcasts `PlayerStatePacket`.
*   **Reconciliation**: The client receives `PlayerStatePacket`. If the server's position differs significantly from the client's predicted position, the client snaps to the server's position and re-simulates inputs.

### 2. AI / Enemies

Create a class `EnemyAI` inheriting from `NetworkEntity`.

*   **Server Authority**: AI logic runs **only** on the server.
*   **State Sync**:
    *   Define a struct `EnemyStatePacket : INetSerializable` (Position, Rotation, AnimationState).
    *   In `FixedUpdate` on the Server, if the enemy moves, broadcast `EnemyStatePacket` **to players within interest range**.
*   **Client**:
    *   The client component should just be a "dumb" visual representation.
    *   It receives `EnemyStatePacket` and interpolates (smooths) the movement between the previous and current position.

### 3. Loot Containers (Inventory Sync)

To create a lootable container (e.g., a chest or dead body):

1.  Add the `NetworkedInventory` component to your prefab.
2.  Configure the `Width`, `Height`, and `InventoryName` in the Inspector.
3.  Ensure `InventoryNetManager` has a reference to your `ItemDatabase`.

**How it works:**
*   **Opening**: When a client opens the UI for this container, send a `RequestInventorySyncPacket` with the container's `EntityId`.
*   **Sync**: The server receives the request, adds the client to the "viewers" list for that inventory, and sends back an `InventorySyncPacket` with all items.
*   **Interaction**: When the player moves an item in the UI, call `InventoryNetManager.Instance.RequestMoveItem(...)`.
*   **Update**: The server validates the move. If valid, it updates the inventory and broadcasts a new `InventorySyncPacket` to all clients currently viewing that inventory.
