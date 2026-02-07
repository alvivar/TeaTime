# TeaTime API

TeaTime is a **chainable queue for timed callbacks** in Unity.
It helps you replace many coroutine patterns with a compact fluent API: delays, loops, conditional stops, replay modes, and runtime control.

---

## Create a queue (MonoBehaviour extensions)

- `this.tt()`
  Returns a **new** `TeaTime` queue.

- `this.tt(string queueName)`
  Returns a queue by name, unique per `MonoBehaviour` instance (created on first call, reused after).

---

## TeaTime (core queue)

### State

- `IsPlaying` — true while executing.
- `IsCompleted` — true when execution reached the end and is no longer playing.
- `Count` — total queued tasks.
- `Current` — current queue index.
- `ExecutedCount` — total callbacks executed.

### Build tasks

`Add(...)` appends timed steps (autoplays unless already playing/paused):

- `Add(float timeDelay, Action callback)`
- `Add(Func<float> timeByFunc, Action callback)`
- `Add(float timeDelay, Action<TeaHandler> callback)`
- `Add(Func<float> timeByFunc, Action<TeaHandler> callback)`
- `Add(float timeDelay)`
- `Add(Func<float> timeByFunc)`
- `Add(Action callback)`
- `Add(Action<TeaHandler> callback)`
- `Add(TeaTime tt)` (wait for another queue)

`Loop(...)` appends per-frame loop callbacks:

- `Loop(float duration, Action<TeaHandler> callback)`
- `Loop(Func<float> durationByFunc, Action<TeaHandler> callback)`
- `Loop(Action<TeaHandler> callback)` (infinite)

> `duration < 0` means infinite loop.

### Queue modes

- `Immutable()` — ignore future `Add/Loop/If` appends.
- `Repeat()` — restart automatically when completed.
- `Consume()` — remove each task after execution.
- `Reverse()` — toggle direction.
- `Backward()` — force reverse direction.
- `Forward()` — force normal direction.
- `Yoyo()` — reverse when queue completes (once per play unless repeating).
- `Release()` — disable all modes (similar to fresh queue config, keeps tasks).

### Control

- `Play()` — start/resume.
- `Pause()` — pause execution.
- `Stop()` — stop and reset position to start.
- `Restart()` — `Stop().Play()`.
- `Reset()` — stop, clear tasks/state, disable modes (full cleanup).

### Conditions / waiting

- `If(Func<bool> condition)`
  If false, queue stops (or restarts when `Repeat` mode is active).

- `Wait(Func<bool> until, float tick = 0)`
  Poll until condition is true.

- `WaitForCompletion()`
  Returns a `YieldInstruction` to yield until this queue completes.

---

## TeaHandler (callback context)

Available inside `Action<TeaHandler>` callbacks (`Add` and `Loop`).

### Readable state

- `self` — current `TeaTime` queue.
- `t` — normalized progress (0..1 for finite loops).
- `deltaTime` — loop delta (scaled to loop duration; reversed in backward mode).
- `timeSinceStart` — elapsed loop/task time.
- `isLooping` — loop running flag.
- `isReversed` — current loop direction.

### Flow control

- `Break()` — end current loop.
- `Wait(YieldInstruction yi)` — yield after callback.
- `Wait(float time)` — delay after callback.
- `Wait(TeaTime tt)` — wait for another queue, integrated with `Stop/Reset` behavior.
- `Wait(Func<bool> condition, float checkDelay)` — wait until condition becomes true.

---

## TeaYield (cached Unity yields)

Utility cache to reduce allocations:

- `TeaYield.EndOfFrame`
- `TeaYield.FixedUpdate`
- `TeaYield.WaitForSeconds(float seconds)`

---

## Minimal example

```csharp
TeaTime queue = this.tt()
    .Add(1f, () => Debug.Log("One second later"))
    .Loop(2f, t =>
    {
        // Called every frame during loop
        if (t.t >= 1f) t.Break();
    })
    .Add(() => Debug.Log("Done"))
    .Repeat();
```
