using UnityEngine;

/// <summary>
/// Zero-setup bootstrap: ensures InteractionUI + DiaryUIManager + CollectiblesManager
/// exist in the main game scene (scene). Runs automatically on load,
/// so you only need to attach DiaryInteractable to the two diaries.
/// </summary>
public static class DiaryBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void EnsureSystems()
    {
        Ensure("InteractionSystems", typeof(InteractionUI), typeof(DiaryUIManager), typeof(CollectiblesManager));
    }

    private static void Ensure(string goName, params System.Type[] components)
    {
        GameObject go = GameObject.Find(goName);
        if (go == null)
        {
            go = new GameObject(goName);
            Object.DontDestroyOnLoad(go);
        }
        foreach (var t in components)
        {
            if (go.GetComponent(t) == null && Object.FindObjectOfType(t) == null)
                go.AddComponent(t);
        }
    }
}
