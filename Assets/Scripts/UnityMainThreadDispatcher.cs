using UnityEngine;
using System.Collections.Generic;

// Permite ejecutar código en el hilo principal de Unity desde otros hilos.
public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static readonly Queue<System.Action> queue = new Queue<System.Action>();
    private static UnityMainThreadDispatcher instance;

    // Se crea solo, automáticamente, antes de cargar la primera escena.
    // (Crear GameObjects desde otro hilo da error, por eso se hace aquí.)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        if (instance != null) return;
        var go = new GameObject("[MainThreadDispatcher]");
        instance = go.AddComponent<UnityMainThreadDispatcher>();
        DontDestroyOnLoad(go);
    }

    public static void Enqueue(System.Action action)
    {
        lock (queue) queue.Enqueue(action);
    }

    void Update()
    {
        while (true)
        {
            System.Action action;
            lock (queue)
            {
                if (queue.Count == 0) break;
                action = queue.Dequeue();
            }
            action();
        }
    }
}
