using UnityEngine;
using System;
using System.Collections.Generic;

public class MainThreadDispatcher : MonoBehaviour
{
    private static readonly Queue<Action> ExecutionQueue = new Queue<Action>();
    private static MainThreadDispatcher instance = null;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    public static MainThreadDispatcher Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<MainThreadDispatcher>();
                if (instance == null)
                {
                    GameObject go = new GameObject("MainThreadDispatcher");
                    instance = go.AddComponent<MainThreadDispatcher>();
                }
            }
            return instance;
        }
    }

    void Update()
    {
        lock(ExecutionQueue)
        {
            while (ExecutionQueue.Count > 0)
            {
                ExecutionQueue.Dequeue().Invoke();
            }
        }
    }

    public void EnqueueAction(Action action)
    {
        lock(ExecutionQueue)
        {
            ExecutionQueue.Enqueue(action);
        }
    }
}