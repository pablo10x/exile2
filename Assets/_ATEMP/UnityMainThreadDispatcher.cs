// Helper class to dispatch actions to Unity main thread

using System;
using UnityEngine;
using System.Collections.Generic;
using System.Threading;
using Unity.VisualScripting;


public class UnityMainThreadDispatcher : MonoBehaviour
{
      private static UnityMainThreadDispatcher _instance;
      private readonly Queue<Action> _executionQueue = new();

      public static UnityMainThreadDispatcher Instance()
      {
            if (_instance == null)
            {
                  var obj = new GameObject("UnityMainThreadDispatcher");
                  _instance = obj.AddComponent<UnityMainThreadDispatcher>();
                  DontDestroyOnLoad(obj);
            }
            return _instance;
      }

      public void Enqueue(Action action)
      {
            lock (_executionQueue)
            {
                  _executionQueue.Enqueue(action);
            }
      }

      private void Update()
      {
            lock (_executionQueue)
            {
                  while (_executionQueue.Count > 0)
                  {
                        _executionQueue.Dequeue().Invoke();
                  }
            }
      }
}