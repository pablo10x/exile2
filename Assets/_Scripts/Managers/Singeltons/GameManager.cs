using System;
using _Scripts.Managers.Singeltons;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace core.Managers
{
    public class GameManager : Singleton<GameManager>
    {
       

        [BoxGroup("Camera")] public Camera                              defaultCamera;
        [BoxGroup("Scenes")] public AssetReference                      lobbyscene;
        private                     AsyncOperationHandle<SceneInstance> _sceneLoadHandle;


        [Required] public AssetReferenceT<GameObject>      playerPrefab;
        public            AsyncOperationHandle<GameObject> PlayerPrefabHandler;



        [BoxGroup("Global Settings")] public GlobalDataSO GlobalConfig;
        
        //player

        [FormerlySerializedAs("player"),FormerlySerializedAs("playerController")] public Character character;


        #region events

        public event Action OnPlayerSpawned;

        #endregion

        private void Start() {

            Application.runInBackground = true;
          

            //fps
            Application.targetFrameRate = 90;

            // load quality settings
            GraphicManager.Instance.LoadSettings();
        }


        public async void SpawnPlayer(Vector3 position, Quaternion rotation)
        {
            if (playerPrefab is null) return;

            // Load the Addressable Asset asynchronously
            PlayerPrefabHandler = playerPrefab.InstantiateAsync(position, rotation);
            defaultCamera.gameObject.SetActive(false);

            await PlayerPrefabHandler.Task;
            character = PlayerPrefabHandler.Result.GetComponent<Character>();
            OnPlayerSpawned?.Invoke();
        }


        public bool IsLobbySceneLoaded()
        {
            return _sceneLoadHandle.IsValid();
        }
        public void LoadLobbyScene()
        {
            _sceneLoadHandle = lobbyscene.LoadSceneAsync(LoadSceneMode.Additive);
            // Attach a callback for when the loading is complete
            _sceneLoadHandle.Completed += handle =>
            {
                // SpawnPlayer (new Vector3 (0f, 0.5f, 0f), quaternion.identity);
                SceneManager.SetActiveScene(handle.Result.Scene);
               // UiManager.Instance.loginpage.Disable();
                Destroy(defaultCamera.gameObject);
            };
        }
    }
}