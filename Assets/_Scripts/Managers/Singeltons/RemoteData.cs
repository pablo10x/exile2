using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.RemoteConfig;

namespace Managers {

    public class RemoteData : Singleton <RemoteData> {


        

        #region Events

        public event Action OnRemoteDataFetched;
        public event Action OnRemoteDataFetchError;

        #endregion


        #region configs

         public static string MasterServer { get; private set; }
         public static string MasterServerSocket { get; private set; }

         [ NotNull ] public static string Localserver = "http://localhost:2022";

         public static string AssetsRemotePath { get; private set; }
         public static string APIKey       { get; private set; }

        #endregion

        private async void Awake ()
        {
            await InitializeRemoteConfigAsync ();

             LoadRemoteData ();
        }



      

        async Task InitializeRemoteConfigAsync ()
        {

            await Task.Delay (500); // a fix for some devices
            await UnityServices.InitializeAsync ();
             if (!AuthenticationService.Instance.IsSignedIn) {
                 await AuthenticationService.Instance.SignInAnonymouslyAsync  ();
             }
            
        }


        private async void LoadRemoteData ()
        {
            if (Utilities.CheckForInternetConnection ()) {
                await InitializeRemoteConfigAsync ();
            }

            RemoteConfigService.Instance.FetchConfigs (new AppAttributes (), new UserAttributes ());
            RemoteConfigService.Instance.FetchCompleted += ApplyRemoteConfig;

        }

        private void ApplyRemoteConfig ( ConfigResponse configResponse )
        {
            switch (configResponse.status) {
                case ConfigRequestStatus.Success:
                    MasterServer     = RemoteConfigService.Instance.appConfig.GetString ("masterserver");
                    MasterServerSocket     = RemoteConfigService.Instance.appConfig.GetString ("masterserversocket");
                    APIKey           = RemoteConfigService.Instance.appConfig.GetString ("apiKey");
                    AssetsRemotePath = RemoteConfigService.Instance.appConfig.GetString ("remoteAssetsPath");
                    OnRemoteDataFetched?.Invoke ();
                    break;
                default:
                    OnRemoteDataFetchError?.Invoke ();
                    break;
            }
        }

    }

}

public struct UserAttributes {

}

public struct AppAttributes {

}
