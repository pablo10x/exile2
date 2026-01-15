using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour {
    private static T      instance;
    private static object lockObject = new object();
    

    public static T Instance {
        get {
            

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
     

        if (instance != null) {
            Destroy(instance.gameObject);
        }
    }
}

