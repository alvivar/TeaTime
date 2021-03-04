// Useful TeaTime patterns!

// 2016/03/18 06:15 PM

using UnityEngine;

public class ClassicPatterns : MonoBehaviour
{
    void Start()
    {
        // A simple 2 seconds delay.
        TeaTime simpleDelay = this.tt().Add(2, () =>
        {
            Debug.Log($"simpleDelay: Two seconds later, just once! {Time.time}");
        });

        // Something that repeats itself every 3 seconds.
        TeaTime repeatDelay = this.tt().Add(() =>
            {
                Debug.Log($"repeatDelay: Every 3 seconds, repeats forever! {Time.time}");
            })
            .Add(3).Repeat();

        // A controlled frame by frame loop (update-like) with 1.5 seconds duration!
        TeaTime updateLike = this.tt().Loop(1.5f, (ttHandler loop) =>
        {
            Debug.Log($"updateLike: Frame by frame during 1.5 seconds, just once! {Time.time}");
        });

        // A simple delay without autoplay.
        TeaTime somethingForLater = this.tt().Pause().Add(3, () =>
        {
            Debug.Log($"somethingForLater: Someone called 'somethingForLater.Play()' 3 second ago! {Time.time}");
        });

        somethingForLater.Play();

        // A tween-like with before and after setup.
        TeaTime tweenLike = this.tt().Add(() =>
            {
                Debug.Log($"tweenLike: Just before the 4 seconds loop! {Time.time}");
                transform.position = new Vector3(999, 999, 999);
            })
            .Loop(4, (ttHandler loop) =>
            {
                transform.position = Vector3.Lerp(
                    transform.position,
                    Vector3.zero,
                    loop.deltaTime);
            })
            .Add(() =>
            {
                Debug.Log($"tweenLike: Just after the 4 seconds loop! {Time.time}");
            });
    }
}

// @todo Fix this pattern as example of a waiting loop Loading the first level.

// this.tt("@LoadUnload").Pause().Add((ttHandler t) =>
// {
//     if (levelToLoad >= 0)
//         loading = SceneManager.LoadSceneAsync(levelToLoad, LoadSceneMode.Additive);
// })
// .Loop((ttHandler t) =>
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