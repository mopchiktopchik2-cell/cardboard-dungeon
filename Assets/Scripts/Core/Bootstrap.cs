using UnityEngine;

public static class Bootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Boot()
    {
        if (Object.FindObjectOfType<Game>() != null) return;
        var go = new GameObject("CardboardDungeon");
        go.AddComponent<Game>();
    }
}
