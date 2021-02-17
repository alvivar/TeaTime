# TeaTime v0.8.8 beta

TeaTime is a fast & simple queue for timed callbacks, focused on solving common
coroutines patterns in Unity games.

    // Inside MonoBehaviours
    TeaTime queue = this.tt().Add(1, () =>
    {
        // The queue autoplays by default
        Debug.Log($"One second later! {Time.time}");
    })
    .Loop(3, (ttHandler loop) =>
    {
        // This callback is repeated per frame during the loop duration
        transform.position =
            Vector3.Lerp(
                transform.position,
                transform.position + Random.insideUnitSphere,
                loop.deltaTime // deltaTime sincronized with the loop duration
            );
    })
    .Add(() =>
    {
        Debug.Log($"The loop is done! {Time.time}");
    })
    .Repeat(); // Repeats forever!

    // And more!
    queue.Pause();
    queue.Immutable();
    queue.Immutable();
    queue.Play();
    queue.Stop();

_[Examples](https://github.com/alvivar/TeaTime/tree/master/Examples)_!
_[API](https://github.com/alvivar/TeaTime/tree/master/API.md)_

Feel free to ask me about it!

By **[Andrés Villalobos](https://twitter.com/matnesis)**.

> Created 2014/12/26 12:21 am ~ Rewritten 2015/09/15 12:28 pm ~ Last revision 2021.02.16 11.53 pm
