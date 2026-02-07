# Changelog

All notable changes to TeaTime are documented in this file.
Versions are listed from newest to oldest.

## [v0.9] - 2021-10-03

- **Revision:** Bug hunt, code cleanup, visual polish, and better examples. TeaTime is no longer **beta**.
- **Changed:** `.Add(...)` can now wait for other TeaTimes. `TeaHandler.Wait(...)` no longer waits TeaTimes.
- **Fixed:** `.Reverse()` now works correctly when a TeaTime starts reversed from the beginning.

## [v0.8.8 beta] - 2021-02-17

- **Revision:** Experimental optimizations.
- **Changed:** `TeaHandler.EndLoop(...)` was renamed to `.Break(...)` (more natural/classic naming).

## [v0.8.7 beta] - 2020-05-23

- **Changed:** The namespace was removed; just add the file to your project and use it directly.
- **Docs:** Removed `Summary` entries from comments.

## [v0.8.5 beta] - 2016-06-18

- **Fixed:** Nested TeaTimes inside another TeaTime in `Repeat` mode could fail. The `_waiting` list was not being cleared properly.

## [v0.8.4 beta] - 2016-06-18

- **Added:** `TeaHandler.Wait(Func<bool>, checkDelay)` now waits until the boolean condition is fulfilled after the current callback execution, checking every `checkDelay`.

## [v0.8.3 beta] - 2016-05-27

- **Added:** `.Wait(Func<bool>, checkDelay)` now waits until the boolean condition is fulfilled, checking every `checkDelay`.
- **Changed:** `TeaHandler.WaitFor(...)` was renamed to `TeaHandler.Wait(...)` (API simplification).
- **Added:** All TeaTimes waiting in `TeaHandler.Wait(...)` are now affected by their parent `.Stop()` and `.Reset()`.
- **Fixed:** `.Consume()` was not working because of `.Reverse()` mode validation.

## [v0.8.1 beta] - 2016-04-26

- **Added:** `.Yoyo()` mode reverses execution order when the queue completes (once per play when not in `Repeat` mode).

## [v0.8 beta] - 2016-04-26

- **Added:** `.Backward()`, `.Forward()`, and `.Reverse()` to control queue execution direction.

_Thanks [Xerios](http://github.com/alvivar/TeaTime/pull/8)!_

## [v0.7.9 beta] - 2016-04-26

- **Performance:** `YieldInstruction`s like `WaitForEndOfFrame` and `WaitForSeconds` are now cached.
- **Performance:** Calls to `StartCoroutine` were reduced significantly.
- **Performance:** Removed `foreach` usage and added minor optimizations.

## [v0.7.7 beta] - 2016-04-24

- **Added:** `.WaitForCompletion()` now returns a `YieldInstruction`, allowing TeaTime to `TeaHandler.WaitFor(...)` other TeaTimes.
- **Changed:** `TeaHandler.Break()` was renamed to `.EndLoop(...)`.
- **Fixed:** Using a `Func<float>` as time was not working as expected.

## [v0.7.4 beta] - 2016-03-17

- **Changed:** `.Wait()` was renamed to `.Immutable()`.
- **Changed:** `.Add(...)` and `.Loop(...)` can now use a `Func<float>` as time.
- **Changed:** `.Reset()` now also turns off all queue modes.
- **Changed:** `.Unlock()` (which turns off all queue modes) was renamed to `.Release()`.
- **Changed:** `.Add(...)` no longer uses `YieldInstruction` as time.
- **Changed:** `.Stop()` no longer pauses the queue.

## [v0.7.3.1 beta] - 2016-02-08

- **Changed:** `.Stop()` also pauses the queue.

## [v0.7.3 beta] - 2015-12-14

- **Added:** `TeaHandler` now holds a reference to itself (`.self`).

## [v0.7.2 beta] - 2015-10-19

- **Added:** `.If(Func<bool>)` appends a boolean condition that stops the queue when the condition is not fulfilled. In `Repeat` mode, the queue restarts. This interruption also affects `Consume` mode (no execution, no removal).
- **Changed:** `.Repeat()` mode no longer ignores newly appended callbacks. That behavior is now exclusive to `.Wait()`.

## [v0.7 beta] - 2015-10-10

- Total code rewrite: same pattern, but faster, cleaner, and fully C#-compliant.
- **Changed:** `TeaTime` is now a normal instantiable object: `TeaTime queue = new TeaTime(MonoBehaviour);`
- **Changed:** `this.tt()` (MonoBehaviour extension) returns a new TeaTime queue ready to use.
- **Changed:** `this.tt("queueName")` returns a TeaTime queue bound to the given name (unique per instance, created on first call), enabling queue access without formal field definitions.
- **Changed:** Each TeaTime queue now handles itself (one coroutine per queue). There are no global controls yet; only per-queue controls.
- **Performance:** Callbacks are saved permanently by default.
- **Rule:** Calling `.Add(...)` or `.Loop(...)` activates stopped queues, even during `.Wait()` or `.Repeat()` modes, unless they are `.Pause()`d.
- **Added:** `Consume` mode (callbacks are removed from the queue after execution; non-accumulative).
- **Added:** `.IsPlaying` property (`true` during execution).
- **Added:** `.IsCompleted` property (`true` when queue execution is done).
- **Added:** Queue `.Count` property.
- **Added:** `.Current` property (current queue position to execute).

## [v0.6.5.4]

- `.ttPlay(...)` can now restart a queue even if it has already finished.

## [v0.6.5.3]

- **Changed:** `TeaHandler.WaitFor()` now queues its arguments on each call and can execute/wait `IEnumerator`s.
- **Changed:** `.ttAdd()` now supports single `YieldInstruction`s.
- Minor optimizations.

## [v0.6.5.1]

- **Fixed:** Using a TeaTime queue inside another TeaTime queue caused queue-name reference issues.

## [v0.6.5]

- **Changed:** `.ttRepeat(...)` default parameter changed to `-1` (`n = -1`) for infinite repetition (used to be `1`).
- **Added:** `ttPause(...)`, `ttStop(...)`, and `ttPlay(...)`.
- **Added:** Play/Pause/Stop example.
- Minor optimizations and code cleanup.

## [v0.6.2]

- **Added:** `ttReset(...)` is back; it stops and resets the current queue.
- **Changed:** The optional parameter in `tt(...)` can no longer reset a queue (use `ttReset(...)` after `tt(...)` instead).
- Minor optimizations.
- Deprecated code cleanup.
- Updated examples.

## [v0.6]

- **Added:** `.ttRepeat(n)` repeats the current queue `n` times or infinitely (`n <= -1`).
- **Changed:** `ttAdd(...)` and `ttLoop(...)` can no longer create/change the current queue; use `tt(...)` instead.
- **Added:** Better examples.

## [v0.5.9]

- **Changed:** `ttNew(...)` was upgraded to `tt(...)` and can now change the current queue. It can also optionally reset the content of an existing queue. When used without a name, the queue is anonymous (immune to `ttWait(...)`).

## [v0.5.8.4]

- **Added:** `ttNew(...)` creates or changes the current queue using a unique anonymous identifier.
- **Changed:** `ttNow(...)` was removed. Fast/safe timers can be created with `ttNew(...)` at the start of a queue (with both `ttAdd(...)` and `ttLoop(...)`).

## [v0.5.8.3]

- **Changed:** `ttReset(...)` was upgraded to `TeaTime.Reset(...)` (can stop and reset queues).
- **Added:** `TeaTime.ResetAll(...)` stops and clears all queues in all instances.

## [v0.5.8]

- **Added:** `TeaHandler.t` returned. It contains loop completion percentage from `0` to `1` for timed loops (for example, half duration => `t = 0.5`).

## [v0.5.7]

- **Added:** `ttReset(...)` stops and resets a queue.
- **Changed:** `ttWaitForCompletion(...)` was renamed to `ttWait(...)`.

## [v0.5.4]

- **Changed:** `TeaHandler.t` was renamed to `TeaHandler.deltaTime` (algorithm greatly improved).

## [v0.5.2]

- **Changed:** `ttAppend(...)` was renamed to `ttAdd(...)`.
- **Changed:** `ttAppendLoop(...)` was renamed to `ttLoop(...)`.
- **Changed:** `ttInvoke(...)` was renamed to `ttNow(...)`.
- **Changed:** `ttLock(...)` was renamed to `ttWaitForCompletion(...)`.

## [v0.5.1]

- `TeaTimer` was renamed to `TeaTime`.
- **Fixed:** In concurrent environments, `AppendLoop(...)` with a manual `Break()` could append the wrong next chained callback.
- Minor optimizations.

## [v0.5]

- **Added:** `TeaHandler` now supports `Append(...)`.
- **Added:** `TeaHandler.WaitFor(...)` waits for a yield or time after the current callback execution, before the next queued callback.

## [v0.4]

- **Added:** `AppendLoop(...)` appends a callback that runs frame by frame for its duration (or infinitely) into a queue.
- **Added:** `TeaHandler` supports/manages `AppendLoop(...)` with `t` (completion rate from `0` to `1`), `timeSinceStart`, and `Break()`.
- **Changed:** `ttNow(...)` was renamed to `ttInvoke(...)`.

_Thanks [@tzamora](http://github.com/tzamora) for the loop code._

## [v0.3]

- **Added:** `ttLock(...)` locks the current queue until all previous callbacks are done (safe during arbitrary cycles such as `Update()`).
- **Changed:** `ttInsert(...)` was renamed to `ttNow(...)`.

## [v0.2]

- **Added:** `ttAppend(...)` can name queues (different queues can coexist at the same time).

## [v0.1]

- **Added:** `ttAppend(...)` appends a timed callback into a queue.
- **Added:** `ttInsert(...)` executes a timed callback.
