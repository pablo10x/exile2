using Sirenix.OdinInspector;
using UnityEngine;

namespace ExileSurvival.Networking.Core
{
    public enum NetworkStartMode
    {
        Server,
        Client
    }

    public class NetworkBootstrapper : MonoBehaviour
    {
        [Header("Startup Mode")]
        [Tooltip("Determines which network components to launch. Set to Server for a dedicated server build.")]
        public NetworkStartMode StartMode = NetworkStartMode.Client;

        [Header("Connection Info (Client Only)")]
        [Tooltip("The IP address of the server to connect to.")]
        [HideIf("StartMode", NetworkStartMode.Server)]
        public string ServerAddress = "127.0.0.1";
        
        [Tooltip("The port of the server to connect to.")]
        [HideIf("StartMode", NetworkStartMode.Server)]
        public int ServerPort = 9050;

        [Header("Manager Prefabs")]
        public GameObject ServerManagerPrefab;
        public GameObject ClientManagerPrefab;
        public GameObject NetworkEntityManagerPrefab;
        public GameObject InventoryNetManagerPrefab;
        public GameObject NetworkGameManagerPrefab;
        public GameObject InterestManagerPrefab;
        public GameObject PrefabManagerPrefab;

        private void Start()
        {
            // In a real build, you would use command-line arguments to set the start mode.
            // For editor testing, we use the public StartMode enum.

#if UNITY_SERVER
            StartMode = NetworkStartMode.Server;
#endif

            switch (StartMode)
            {
                case NetworkStartMode.Server:
                    StartServer();
                    break;
                case NetworkStartMode.Client:
                    StartClient();
                    break;
            }
        }

        private void InstantiateSharedManagers()
        {
            if (NetworkEntityManager.Instance == null) Instantiate(NetworkEntityManagerPrefab);
            if (InventoryNetManager.Instance == null) Instantiate(InventoryNetManagerPrefab);
            if (NetworkGameManager.Instance == null) Instantiate(NetworkGameManagerPrefab);
            if (PrefabManager.Instance == null) Instantiate(PrefabManagerPrefab);
        }

        private void StartServer()
        {
            Debug.Log("--- Starting in SERVER mode... ---");
            InstantiateSharedManagers();
            Instantiate(ServerManagerPrefab);
            Instantiate(InterestManagerPrefab);
            ServerManager.Instance.StartServer();
        }

        private void StartClient()
        {
            Debug.Log("--- Starting in CLIENT mode... ---");
            InstantiateSharedManagers();
            Instantiate(ClientManagerPrefab);
            ClientManager.Instance.Connect(ServerAddress, ServerPort, "ExileSurvival_Dev"); // Example key
        }
    }
}