using System;
using System.Collections.Generic;
using UnityEngine;

namespace UPP.Utils
{

    public class MainThreadDispatcher : MonoBehaviour
    {
        private static readonly Queue<Action> ActionQueue = new Queue<Action>();
        public static MainThreadDispatcher Instance;

        public void Enqueue(Action action)
        {
            lock (ActionQueue)
                ActionQueue.Enqueue(action);
        }

        private void Awake()
        {
            if (Instance is not null)
                Debug.LogError("There can only be one MainThreadDispatcher");

            Instance = this;
        }

        private void OnDestroy() => Instance = null;

        private void Update()
        {
            lock (ActionQueue)
                while (ActionQueue.Count > 0)
                    ActionQueue.Dequeue()?.Invoke();
        }
    }
}
