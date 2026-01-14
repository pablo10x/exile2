using UnityEngine;
using System.Collections.Generic;

namespace Salvage.ClothingCuller.Components {
    public class ModularClothingSpawner : MonoBehaviour {
        private Occludee[] spawnedOccludees;

        #region SerializeFields

        [SerializeField] private ClothingCuller clothingCuller;
        [SerializeField] private Occludee       body;
        [SerializeField] private Occludee[]     clothingPrefabs;

        #endregion

        #region SyncVars

        // Track which clothing indices are equipped
        private readonly List<int> equippedIndices = new List<int>();

        #endregion

        #region MonoBehaviour Methods

        private void Awake() {
            spawnedOccludees = new Occludee[clothingPrefabs.Length];
        }
        
        public void OnStartNetwork() {
            //base.OnStartNetwork();
            //equippedIndices.OnChange += OnEquippedIndicesChanged;
        }

        public void OnStopNetwork() {
            //base.OnStopNetwork();
            //equippedIndices.OnChange -= OnEquippedIndicesChanged;
        }

        public void OnStartClient() {
            //base.OnStartClient();
            
            // Register body for all clients
            if (clothingCuller != null && body != null) {
               // clothingCuller.Register(body, false);
            }

            // Sync existing equipped items for late-joining clients
            //if (!IsServerStarted) {
                foreach (int index in equippedIndices) {
                    EquipLocal(index);
                }
            //}
        }
        
        private void OnGUI() {
            //if (!IsOwner) return; // Only show UI for the owner
            
            GUILayout.BeginHorizontal();
            for (int i = 0; i < clothingPrefabs.Length; i++) {
                drawEquipOrUnEquipButton(i);
            }
            GUILayout.EndHorizontal();
        }

        #endregion

        #region Private Methods

        private void drawEquipOrUnEquipButton(int index) {
            float scale = Camera.main.pixelHeight / 1080f;
            var   style = new GUIStyle(GUI.skin.button) { 
                fontSize = (int)(30 * scale), 
                fixedHeight = 100f * scale, 
                fixedWidth = 300f * scale 
            };

            Occludee occludee = spawnedOccludees[index];
            Occludee prefab   = clothingPrefabs[index];

            if (occludee != null) {
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button($"Unequip {occludee.name}", style)) {
                    ServerUnequip(index);
                }
                return;
            }

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button($"Equip {prefab.name}", style)) {
                ServerEquip(index);
            }
        }

        private void EquipLocal(int index) {
            if (index < 0 || index >= clothingPrefabs.Length) return;
            if (spawnedOccludees[index] != null) return;

            Occludee prefab = clothingPrefabs[index];
            Occludee occludee = Instantiate(prefab, clothingCuller.transform);
            occludee.name = prefab.name;
            clothingCuller.Register(occludee);
            spawnedOccludees[index] = occludee;
        }

        private void UnequipLocal(int index) {
            if (index < 0 || index >= spawnedOccludees.Length) return;
            
            Occludee occludee = spawnedOccludees[index];
            if (occludee == null) return;
            
            clothingCuller.Deregister(occludee);
            Destroy(occludee.gameObject);
            spawnedOccludees[index] = null;
        }

        private void OnEquippedIndicesChanged(int index, int oldItem, int newItem, bool asServer) {
            // Don't process on server, server already handled it
            //if (IsServerStarted) return;

            //switch (op) {
            //    case SyncListOperation.Add:
            //        EquipLocal(newItem);
            //        break;
            //    case SyncListOperation.RemoveAt:
            //        UnequipLocal(oldItem);
            //        break;
            //    case SyncListOperation.Clear:
            //        for (int i = 0; i < spawnedOccludees.Length; i++) {
            //            UnequipLocal(i);
            //        }
            //        break;
            //}
        }

        #endregion

        #region Network Methods

        
        private void ServerEquip(int index) {
            if (index < 0 || index >= clothingPrefabs.Length) return;
            if (equippedIndices.Contains(index)) return;

            equippedIndices.Add(index);
            EquipLocal(index); // Equip on server too
        }


        private void ServerUnequip(int index) {
            if (!equippedIndices.Contains(index)) return;

            equippedIndices.Remove(index);
            UnequipLocal(index); // Unequip on server too
        }

        #endregion
    }
}