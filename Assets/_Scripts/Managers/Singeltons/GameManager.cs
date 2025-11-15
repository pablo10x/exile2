using System;
using _Scripts.Managers.Singeltons;
using core.player;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using Sirenix.OdinInspector;

namespace core.Managers {
    public class GameManager : Singleton<GameManager> {
        [BoxGroup("Camera")] public Camera                              defaultCamera;
        [BoxGroup("Scenes")] public AssetReference                      lobbyscene;
        private                     AsyncOperationHandle<SceneInstance> _sceneLoadHandle;

        [Required] public AssetReferenceT<GameObject> playerPrefab;

        public AsyncOperationHandle<GameObject> PlayerPrefabHandler;

        [BoxGroup("Global Settings")] public GlobalDataSO GlobalConfig;

        //player

        [FormerlySerializedAs("player"), FormerlySerializedAs("playerController")] public Character character;

    

        #region events

        public event Action OnPlayerSpawned;

        #endregion

        private void Start() {
            Application.runInBackground = true;


           
            //fps
            Application.targetFrameRate = 60;

            // load quality settings
            GraphicManager.Instance.LoadSettings();

            //SpawnPlayer(tempSpawnPos.position, tempSpawnPos.rotation);
        }

       

        public void DisableMainCamera() {
            if (defaultCamera != null)
                defaultCamera.gameObject.SetActive(false);
        }

        public void EnableMainCamera() {
            if (defaultCamera != null)
                defaultCamera.gameObject.SetActive(true);
        }

  
        public void PlayerSpawnedEventDispatcher() => OnPlayerSpawned?.Invoke();

        

        public bool IsLobbySceneLoaded() {
            return _sceneLoadHandle.IsValid();
        }

        public void LoadLobbyScene() {
            _sceneLoadHandle = lobbyscene.LoadSceneAsync(LoadSceneMode.Additive);
            // Attach a callback for when the loading is complete
            _sceneLoadHandle.Completed += handle => {
                // SpawnPlayer (new Vector3 (0f, 0.5f, 0f), quaternion.identity);
                SceneManager.SetActiveScene(handle.Result.Scene);
                // UiManager.Instance.loginpage.Disable();
                Destroy(defaultCamera.gameObject);
            };
        }
    }
}