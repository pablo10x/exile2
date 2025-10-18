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

        [FoldoutGroup("Debug")] public Transform tempSpawnPos;
        [FoldoutGroup("Debug")] public bool      ShouldSpawnPlayer = true;

        #region events

        public event Action OnPlayerSpawned;

        #endregion

        private void Start() {
            Application.runInBackground = true;


            if (ShouldSpawnPlayer && tempSpawnPos != null)
                SpawnPlayer(tempSpawnPos.position, tempSpawnPos.rotation);
            //fps
            Application.targetFrameRate = 90;

            // load quality settings
            GraphicManager.Instance.LoadSettings();

            //SpawnPlayer(tempSpawnPos.position, tempSpawnPos.rotation);
        }

        public async void SpawnPlayer(Vector3 position, Quaternion rotation) {
            if (playerPrefab is null) return;

            // Load the Addressable Asset asynchronously
            PlayerPrefabHandler = playerPrefab.InstantiateAsync(position, rotation);
            if (defaultCamera != null)
                defaultCamera.gameObject.SetActive(false);

            await PlayerPrefabHandler.Task;
            character = PlayerPrefabHandler.Result.GetComponent<Character>();
            character.GetComponent<CharacterEquipmentManager>()
                     .SetSkin(GlobalConfig.defaultcharacterSkin, false);
            // character.GetComponent<CharacterEquipmentManager>()
            //          .BuildCharacter(GlobalConfig.defaultcharacterSkin);
            OnPlayerSpawned?.Invoke();
        }

        public void DisableMainCamera() {
            if (defaultCamera != null)
                defaultCamera.gameObject.SetActive(false);
        }

        public void EnableMainCamera() {
            if (defaultCamera != null)
                defaultCamera.gameObject.SetActive(true);
        }

        [Button("Respawn Player")]
        public void ResPawnPlayer() {
            if (character is null) return;
            defaultCamera.gameObject.SetActive(true);
            character.orbitCamera.gameObject.SetActive(false);
            Destroy(character.orbitCamera.gameObject, 10f);
            Destroy(character.gameObject, 10f);
            SpawnPlayer(tempSpawnPos.position, tempSpawnPos.rotation);
        }

        [Button("Assign Skin")]
        public void AssignSkin() {
            if (character is null) return;
            character.GetComponent<CharacterEquipmentManager>()
                     .SetSkin(GlobalConfig.defaultcharacterSkin, false);
        }

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