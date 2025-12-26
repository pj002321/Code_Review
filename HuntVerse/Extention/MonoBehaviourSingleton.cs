using UnityEngine;

public class MonoBehaviourSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T shared;
    public static T Shared => shared;

    protected virtual bool DontDestroy => true;

    protected virtual void Awake()
    {
        if (shared != null && shared != this)
        {
            Destroy(gameObject);
            return;
        }

        shared = this as T;

        if (DontDestroy)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    public static T Create(string name = null, Transform parent = null)
    {
        if (shared != null) return shared;

        var g = new GameObject(name ?? typeof(T).ToString());
        if (parent != null)
            g.transform.SetParent(parent, false);

        shared = g.AddComponent<T>();
        return shared;
    }

    protected virtual void OnDestroy()
    {
        if (shared == this)
            shared = null;
    }
}
