using System.Collections;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.Exceptions;
using UnityEngine.SceneManagement;

namespace core.Managers {

    public class Initializer : MonoBehaviour {

        [ BoxGroup ("TESTING") ] public bool clearCacheAtStart;


        [Required] [ SerializeField ] private AssetLabelReference bundlesToPreloadLabel;


        [Required] [ SerializeField ] private TMP_Text version;
        [Required]
        [ BoxGroup ("Scenes") ] public AssetReference mainScene;


        public                     TMP_Text         infotext;
        public                     TMP_Text         progressNumber;
        public                     TMP_Text         currentSpeed;
       


        private       int _retryCount;
        private const int MaxRetry = 5;

        private void Awake ()
        {


            version.text = Application.version;
            
            if (clearCacheAtStart) Clear ();

            PrepareAssetBundles();

        }

        /// <summary>
        /// check if endpoint is reachable
        /// </summary>
        /// <returns>Continue to download the resources if it succeed</returns>
        void VeryifyRemoteServer ()
        {
            //Addressables.WebRequestOverride

        }

        private async void PrepareAssetBundles ()
        {

            long operation;

            //Addressables.WebRequestOverride = EditWebRequestURL;

            // Add the WebRequestOverride to the Addressables settings

            Addressables.InitializeAsync ();

            operation = await Addressables.GetDownloadSizeAsync (bundlesToPreloadLabel).Task;

            if (operation > 200) {
                infotext.text = $"Preparing game data...\n{helper.SizeSuffix (operation)}";
                
            }

           

            StartCoroutine (DownloadBundles ());

        }

        


        public IEnumerator DownloadBundles ()
        {
            yield return new WaitForSeconds (2);
            AsyncOperationHandle downloadDependenciesAsync;

            try {
                downloadDependenciesAsync = Addressables.DownloadDependenciesAsync (bundlesToPreloadLabel);

            } catch (OperationException) {
                infotext.text = "Error during download: \nPlease restart the game";

                yield break;
            }

            long  previousDownloadedBytes = 0;
            float previousTime            = Time.time;

            while ( !downloadDependenciesAsync.IsDone ) {
                long  currentDownloadedBytes = downloadDependenciesAsync.GetDownloadStatus ().DownloadedBytes;
                float currentTime            = Time.time;

                // Calculate download speed
                long  bytesDelta    = currentDownloadedBytes - previousDownloadedBytes;
                float timeDelta     = currentTime - previousTime;
                long  downloadSpeed = (long)( bytesDelta / timeDelta );

                infotext.text = "Downloading\n" +
                                $" <color=#f67c41>{helper.SizeSuffix (currentDownloadedBytes)}</color>|<color=#f67c41>{helper.SizeSuffix (downloadDependenciesAsync.GetDownloadStatus ().TotalBytes)}</color> ";

                currentSpeed.text = $"{helper.ConvertBytesToKbPerSecond (downloadSpeed)}";
                int progressPercentage = (int)( downloadDependenciesAsync.GetDownloadStatus ().Percent * 100f );
                progressNumber.text = $"{progressPercentage}%";

                // Update previous values
                previousDownloadedBytes = currentDownloadedBytes;
                previousTime            = currentTime;

                // Introduce a delay to update every 0.2 seconds
                yield return new WaitForSeconds (0.3f);
            }

            switch (downloadDependenciesAsync.Status) {
                case AsyncOperationStatus.Succeeded:
                    infotext.text = "Loading ..";

                    infotext.enabled       = false;
                    currentSpeed.enabled   = false;
                    progressNumber.enabled = false;
                   

                    mainScene.LoadSceneAsync (LoadSceneMode.Additive).Completed += _ => {
                        infotext.enabled       = false;
                        currentSpeed.enabled   = false;
                        progressNumber.enabled = false;
                     //   RemoteData.Instance.gameObject.transform.parent = GameManager.Instance.gameObject.transform.root;
                     //   RemoteData.Instance.InvokeEvent (); // tell other managers that data are loaded already
                        SceneManager.UnloadSceneAsync (0, UnloadSceneOptions.None);
                    };

                    break;
                case AsyncOperationStatus.Failed:
                    if (_retryCount > 0) {
                    infotext.text = "Problem downloading Resources ..\n" +
                                    $"Retry ({_retryCount}/{MaxRetry})";
                    
                    } else {
                        infotext.text = "Problem downloading Resources ..\nReconnecting ..";
                    }

                    yield return new WaitForSeconds (5);

                    if (_retryCount >= MaxRetry) {
                        infotext.text       = "Error during download: \nPlease restart the game";
                        progressNumber.text = "0.0 %";

                        yield break;
                    }

                    _retryCount ++;

                    PrepareAssetBundles ();

                    break;
            }

        }


        [ Button ("clear cache") ] private void Clear ()
        {

            Addressables.ClearDependencyCacheAsync (bundlesToPreloadLabel);

            Caching.ClearCache ();
          
        }

    }

}
