using System;
using System.Threading.Tasks;
using Firebase.Extensions;
using Firebase.RemoteConfig;

public class frRemote : Singleton<frRemote> {
    public event Action OnRemoteDataFetched;

    private async void Start() {
        try {
            await FetchDataAsync();
            
            // Register the real-time config update listener AFTER initial fetch
            FirebaseRemoteConfig.DefaultInstance.OnConfigUpdateListener += (ConfigUpdateListenerEventHandler);
            UnityEngine.Debug.Log("Real-time config update listener registered.");
        }
        catch (Exception e) {
            UnityEngine.Debug.Log($"Error fetching data:: {e}");
        }
    }

    // Start a fetch request.
    // FetchAsync only fetches new data if the current data is older than the provided
    // timespan.  Otherwise it assumes the data is "recent enough", and does nothing.
    // By default the timespan is 12 hours, and for production apps, this is a good
    // number. For this example though, it's set to a timespan of zero, so that
    // changes in the console will always show up immediately.
    public Task FetchDataAsync() {
        UnityEngine.Debug.Log("Fetching data...");
        var fetchTask = FirebaseRemoteConfig.DefaultInstance.FetchAsync(TimeSpan.Zero);
        return fetchTask.ContinueWithOnMainThread(FetchComplete);
    }

    private void FetchComplete(Task fetchTask) {
        if (!fetchTask.IsCompleted) {
            UnityEngine.Debug.Log("Retrieval hasn't finished.");
            return;
        }

        var remoteConfig = FirebaseRemoteConfig.DefaultInstance;
        var info         = remoteConfig.Info;
        if (info.LastFetchStatus != LastFetchStatus.Success) {
            UnityEngine.Debug.Log($"{nameof(FetchComplete)} was unsuccessful\n{nameof(info.LastFetchStatus)}: {info.LastFetchStatus}");
            return;
        }

        // Fetch successful. Parameter values must be activated to use.
        remoteConfig.ActivateAsync()
                    .ContinueWithOnMainThread(task => {
                         UnityEngine.Debug.Log($"masterserver:: {remoteConfig.GetValue("masterserver").StringValue}");
                         UnityEngine.Debug.Log($"Remote data loaded and ready for use. Last fetch time {info.FetchTime}.");
                         
                         // Trigger the event after data is activated
                         OnRemoteDataFetched?.Invoke();
                     });
    }

    // Handle real-time Remote Config events.
    void ConfigUpdateListenerEventHandler(object sender, ConfigUpdateEventArgs args) {
        if (args.Error != RemoteConfigError.None) {
            UnityEngine.Debug.Log(String.Format("Error occurred while listening: {0}", args.Error));
            return;
        }

        UnityEngine.Debug.Log("Config update detected!");
        UnityEngine.Debug.Log("Updated keys: " + string.Join(", ", args.UpdatedKeys));
        
        var remoteConfig = FirebaseRemoteConfig.DefaultInstance;
        
        // Activate all fetched values and then trigger event
        remoteConfig.ActivateAsync()
                    .ContinueWithOnMainThread(task => { 
                        UnityEngine.Debug.Log("Real-time data updated and activated");
                        OnRemoteDataFetched?.Invoke();
                    });
    }
    
    private void OnDestroy() {
        // Clean up listener when object is destroyed
        // try {
        //     FirebaseRemoteConfig.DefaultInstance.RemoveOnConfigUpdateListener(ConfigUpdateListenerEventHandler);
        // }
        // catch (Exception e) {
        //     UnityEngine.Debug.Log($"Error removing listener: {e}");
        // }
    }
}