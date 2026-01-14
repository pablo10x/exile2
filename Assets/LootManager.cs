using System.Collections.Generic;
using UnityEngine;
using ExileSurvival.Networking.Core;
using ExileSurvival.Networking.Entities;

public class LootManager : NetworkEntity {

    public List<GameObject> lootItems = new List<GameObject>();
    public List<Transform> spawnPoints = new List<Transform>();
    
    private void Start() {
        if (ServerManager.Instance != null && ServerManager.Instance.IsServer) {
            SpawnLoot();
        }
    }

    private void SpawnLoot() {
        if (lootItems.Count == 0 || spawnPoints.Count == 0) {
            Debug.LogWarning("Loot items or spawn points are not set in the LootManager.");
            return;
        }

        foreach (Transform spawnPoint in spawnPoints) {
            int randomIndex = Random.Range(0, lootItems.Count);
            GameObject lootPrefab = lootItems[randomIndex];
            GameObject spawnedLoot = Instantiate(lootPrefab, spawnPoint.position, spawnPoint.rotation);
            
            // TODO: Implement proper network spawning
            // Manager.ServerSpawn(spawnedLoot); 
        }
    }
}