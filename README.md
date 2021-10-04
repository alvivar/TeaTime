# TeaTime

TeaTime is a fast & simple queue for timed callbacks, focused on solving common coroutines patterns in Unity games.

    // Inside MonoBehaviours
    TeaTime queue = this.tt().Add(1, () =>
    {
        // The queue autoplays by default
        Debug.Log($"One second later! {Time.time}");
    })
    .Loop(3, (TeaHandler loop) =>
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
    queue.Reverse();
    queue.Yoyo();
    queue.Immutable();

Check out the _[examples](https://github.com/alvivar/TeaTime/tree/master/Examples)_ and the _[API](https://github.com/alvivar/TeaTime/tree/master/API.md)_.
