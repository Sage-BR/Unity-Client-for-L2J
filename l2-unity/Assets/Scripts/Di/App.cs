using UnityEngine;
using VContainer;
using VContainer.Unity;

public static class App
{
    public static IObjectResolver Container { get; set; }
    public static IObjectResolver GameContainer { get; set; }

    public static bool HasContainer
    {
        get { return Container != null; }
    }

    public static T Resolve<T>()
    {
        return Container.Resolve<T>();
    }

    public static void InjectGameObject(GameObject go)
    {
        if (go == null)
            return;
        if (GameContainer == null)
        {
            GameLifetimeScope scope = UnityEngine.Object.FindAnyObjectByType<GameLifetimeScope>(FindObjectsInactive.Include);
            if (scope != null)
                GameContainer = scope.Container;
        }
        if (GameContainer != null)
            GameContainer.InjectGameObject(go);
    }
}
