using UnityEngine;
using ExileSurvival.Networking.Entities;

namespace ExileSurvival.Networking.Core
{
    public class PrefabManager : Singleton<PrefabManager>
    {
        private NetworkPrefabDatabase _database;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            DontDestroyOnLoad(gameObject);

            _database = Resources.Load<NetworkPrefabDatabase>("NetworkPrefabDatabase");
            if (_database == null)
            {
                Debug.LogError("NetworkPrefabDatabase not found in Resources folder. Please create one via the Tools menu.");
            }
            else
            {
                _database.Initialize();
            }
        }

        public NetworkEntity GetPrefab(string guid)
        {
            if (_database == null) return null;
            return _database.GetPrefab(guid);
        }
    }
}