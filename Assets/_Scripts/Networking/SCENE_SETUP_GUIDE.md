# Networking Scene Setup Guide

This guide explains how to structure your Unity scene and GameObjects to work with the Exile Survival networking system.

## 1. Essential Scene Managers

To get the networking running, you need a set of manager objects in your scene. You can create a single empty GameObject named `NetworkManagers` and add the following components to it (or separate GameObjects if you prefer):

1.  **`ServerManager`**:
    *   **Role**: Handles the server logic, listening port, and tick rate.
    *   **Setup**: Add the `ServerManager` component.
    *   **Inspector**: Set `ServerName`, `MapName`, `Port`, and `TickRate`.

2.  **`ClientManager`**:
    *   **Role**: Handles the client connection to the server.
    *   **Setup**: Add the `ClientManager` component.
    *   **Inspector**: Set `ConnectionKey` to match the Server's key.

3.  **`NetworkEntityManager`**:
    *   **Role**: Tracks all networked entities (Players, AI, Boxes).
    *   **Setup**: Add the `NetworkEntityManager` component.

4.  **`InventoryNetManager`**:
    *   **Role**: Manages all inventory synchronization.
    *   **Setup**: Add the `InventoryNetManager` component.
    *   **IMPORTANT**: You **MUST** assign your `ItemDatabase` ScriptableObject to the `Item Database` field in the Inspector.

5.  **`NetworkGameManager`**:
    *   **Role**: Spawns players when they are ready.
    *   **Setup**: Add the `NetworkGameManager` component.
    *   **Inspector**: Assign your `Player Prefab`.

6.  **`InterestManager`**:
    *   **Role**: A server-side system that manages entity visibility for clients.
    *   **Setup**: Add the `InterestManager` component.
    *   **Inspector**: Set `InterestRadius` to control how far players can "see" other entities.

---

## 2. Player Setup

To create a networked player character:

1.  **Create a Prefab**: Start with your character model.
2.  **Add `Character` Script**:
    *   This script (located in `_Scripts/player/Character.cs`) inherits from `PredictedEntity`.
    *   It handles movement, input prediction, and state synchronization.
3.  **Add `NetworkedInventory`**:
    *   This gives the player an inventory (Backpack, Pockets, etc.).
    *   **Inspector**:
        *   `Width`: 10 (example)
        *   `Height`: 5 (example)
        *   `Inventory Name`: "Backpack"
4.  **Register in Manager**: Drag this prefab into the `Player Prefab` slot of the `NetworkGameManager` in your scene.

**Note**: The `Character` script automatically checks `IsLocalPlayer`. If it is the local player, it enables the camera and input. If it's a remote player, it disables them and just smooths movement.

---

## 3. Loot Container (Box) Setup

To create a box that players can open and loot:

1.  **Create a Prefab**: Use a crate or box model.
2.  **Add `NetworkedInventory`**:
    *   **Inspector**:
        *   `Width`: 8
        *   `Height`: 4
        *   `Inventory Name`: "Military Crate"
3.  **Add `NetworkEntity`** (Optional if `NetworkedInventory` is already there, as it inherits from `NetworkEntity`):
    *   Ensure `TypeId` is set correctly if you are spawning it dynamically (e.g., TypeId 2 for Containers).
4.  **Add a Collider**: BoxCollider or SphereCollider for raycasting/interaction.
5.  **Interaction Logic**:
    *   You need a script to handle the "Open" action.
    *   When the player presses 'E' on the box, your interaction script should call:
        ```csharp
        // Get the NetworkedInventory component from the box
        var boxInventory = hitObject.GetComponent<NetworkedInventory>();
        
        // Request the server to send the items
        ClientManager.Instance.SendPacket(new RequestInventorySyncPacket { 
            InventoryId = boxInventory.Inventory.NetworkInventoryId 
        }, DeliveryMethod.ReliableOrdered);
        ```

---

## 4. AI / Enemy Setup

To create a networked enemy:

1.  **Create a Prefab**: Your enemy model.
2.  **Add `NetworkEntity`** (or a script inheriting from it, like `EnemyAI`):
    *   This ensures the enemy has an ID and can receive updates.
3.  **Add `NetworkedInventory`** (Optional):
    *   If you want the enemy to be lootable after death, add this component.
4.  **Server-Side Logic**:
    *   Attach your AI script (e.g., `ZombieController`).
    *   **Crucial**: Wrap your AI logic (NavMeshAgent, attacking) in a check:
        ```csharp
        if (IsOwnedByServer) {
            // Run AI logic
        }
        ```
    *   Clients should only receive position/rotation updates (via `NetworkEntity` or a custom state packet). The `InterestManager` will handle sending these updates only to nearby players.
