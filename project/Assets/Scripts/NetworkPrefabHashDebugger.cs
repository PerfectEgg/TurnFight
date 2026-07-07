using Unity.Netcode;
using UnityEngine;

#if UNITY_EDITOR
[UnityEditor.InitializeOnLoad]
public static class NetworkPrefabHashDebugger
{
    static NetworkPrefabHashDebugger()
    {
        if (NetworkManager.Singleton != null)
        {
            foreach (var prefab in NetworkManager.Singleton.NetworkConfig.Prefabs.Prefabs)
            {
                if (prefab.Prefab != null)
                    Debug.Log($"{prefab.Prefab.name} = {prefab.Prefab.GetComponent<NetworkObject>().PrefabIdHash}");
            }
        }
    }
}
#endif