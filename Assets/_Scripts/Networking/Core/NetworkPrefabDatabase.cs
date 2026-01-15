using System.Collections.Generic;
using UnityEngine;
using ExileSurvival.Networking.Entities;

namespace ExileSurvival.Networking.Core
{
    [CreateAssetMenu(fileName = "NetworkPrefabDatabase", menuName = "Exile/Networking/Prefab Database")]
    public class NetworkPrefabDatabase : ScriptableObject
    {
        [System.Serializable]
        public class PrefabEntry
        {
            public string Guid;
            public NetworkEntity Prefab;
        }

        [SerializeField]
        private List<PrefabEntry> _prefabs = new List<PrefabEntry>();

        private Dictionary<string, NetworkEntity> _lookup;

        public void Initialize()
        {
            _lookup = new Dictionary<string, NetworkEntity>();
            foreach (var entry in _prefabs)
            {
                if (!_lookup.ContainsKey(entry.Guid))
                {
                    _lookup.Add(entry.Guid, entry.Prefab);
                }
            }
        }

        public NetworkEntity GetPrefab(string guid)
        {
            if (_lookup == null) Initialize();
            
            _lookup.TryGetValue(guid, out var prefab);
            return prefab;
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("Exile/Networking/Update Prefab Database")]
        public static void UpdateDatabase()
        {
            var guids = UnityEditor.AssetDatabase.FindAssets("t:GameObject");
            var database = FindOrCreateDatabase();
            database._prefabs.Clear();

            foreach (var guid in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null && prefab.GetComponent<NetworkEntity>() != null)
                {
                    database._prefabs.Add(new PrefabEntry { Guid = guid, Prefab = prefab.GetComponent<NetworkEntity>() });
                }
            }
            UnityEditor.EditorUtility.SetDirty(database);
            UnityEditor.AssetDatabase.SaveAssets();
            Debug.Log($"Network Prefab Database Updated. Found {database._prefabs.Count} network prefabs.");
        }

        private static NetworkPrefabDatabase FindOrCreateDatabase()
        {
            var guids = UnityEditor.AssetDatabase.FindAssets("t:NetworkPrefabDatabase");
            if (guids.Length > 0)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                return UnityEditor.AssetDatabase.LoadAssetAtPath<NetworkPrefabDatabase>(path);
            }
            
            var newDb = CreateInstance<NetworkPrefabDatabase>();
            UnityEditor.AssetDatabase.CreateAsset(newDb, "Assets/Resources/NetworkPrefabDatabase.asset");
            return newDb;
        }
#endif
    }
}