using FishNet.Object;
using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T      instance;
    private static object lockObject            = new object();
    private static bool   applicationIsQuitting;

    public static T Instance
    {
        get
        {
            if (applicationIsQuitting)
            {
                Debug.LogWarning($"[Singleton] Instance '{typeof(T)}' already destroyed on application quit. Won't create again - returning null.");
                return null;
            }

            lock (lockObject)
            {
                if (instance == null)
                {
                    instance = FindAnyObjectByType<T>();

                    if (instance == null)
                    {
                      //  Debug.LogError ("Instance is invalid");
                    }
                }
            }
            
            return instance;
        }
    }

    protected virtual void OnDestroy()
    {
        if (applicationIsQuitting) return;

        applicationIsQuitting = true;

        if (instance != null)
        {
            Destroy(instance.gameObject);
        }
    }
}


public abstract class NetSingleton<T> : NetworkBehaviour where T : NetworkBehaviour {
    private static T      instance;
    private static object lockObject = new object();
    private static bool   applicationIsQuitting;

    public static T Instance {
        get {
            if (applicationIsQuitting) {
                Debug.LogWarning($"[Singleton] Instance '{typeof(T)}' already destroyed on application quit. Won't create again - returning null.");
                return null;
            }

            lock (lockObject) {
                if (instance == null) {
                    instance = FindAnyObjectByType<T>();

                    if (instance == null) {
                        //  Debug.LogError ("Instance is invalid");
                    }
                }
            }

            return instance;
        }
    }

    protected virtual void OnDestroy() {
        if (applicationIsQuitting) return;

        applicationIsQuitting = true;

        if (instance != null) {
            Destroy(instance.gameObject);
        }
    }
}
