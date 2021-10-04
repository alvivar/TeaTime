using UnityEngine;

public class WaitLoadSceneAsync : MonoBehaviour
{
    void Start()
    {
        // @todo Fix this pattern as example of a waiting loop to async load and
        // unload scenes.

        // this.tt("@LoadUnload").Pause().Add((TeaHandler t) =>
        // {
        //     if (levelToLoad >= 0)
        //         loading = SceneManager.LoadSceneAsync(levelToLoad, LoadSceneMode.Additive);
        // })
        // .Loop((TeaHandler t) =>
        // {
        //     if (loading == null || loading.isDone)
        //         t.EndLoop();
        // })
        // .Add(1, () =>
        // {
        //     if (levelToUnload >= 0)
        //         SceneManager.UnloadScene(levelToUnload);
        // })
        // .Immutable();
    }
}

// 2021/10/03 04:10 pm