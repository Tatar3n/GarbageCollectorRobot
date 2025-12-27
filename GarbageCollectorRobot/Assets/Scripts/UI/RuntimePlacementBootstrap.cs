using UnityEngine;

public static class RuntimePlacementBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        if (Object.FindObjectOfType<RuntimePlacementTool>() != null)
        {
            return;
        }

        GameObject go = new GameObject("RuntimePlacementTool");
        go.AddComponent<RuntimePlacementTool>();
    }
}

