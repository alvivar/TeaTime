# TeaTime Examples (Self-Checking Test Harnesses)

This folder now contains **readable, runnable example scripts** that also act as **timing smoke tests**.

Each updated script keeps the original TeaTime usage pattern, but adds checks and a summary like:

- `PASS ...`
- `FAIL ...`
- `COMPLETE: X/Y checks passed`

This makes examples useful both for learning TeaTime and for regression testing.

---

## Quick start

1. Open a scene and add one example component to a GameObject.
2. Press Play.
3. Filter Console by the test prefix (for example: `ClassicPatternsTest`).
4. Read the final summary line.

> Tip: Run one harness at a time for clean logs.

---

## How to read results

- **PASS**: behavior is within expected tolerance.
- **FAIL**: timing/order did not match expectation.
- **COMPLETE**: final count of passed checks.

Timing checks use tolerances because Unity frame timing is not exact.
If your machine is under heavy load, isolated timing failures can happen.

---

## Updated harness examples

### `ClassicPatterns.cs` (`[ClassicPatternsTest]`)

Validates core patterns from the original sample:

- simple delay (`~2s`)
- repeat interval (`~3s`)
- finite loop duration (`~1s`)
- paused queue played later (`~3s from Play()`)
- tween loop duration (`~4s`)

Good first smoke test when changing queue timing behavior.

---

### `ImmutableUpdate.cs` (`[ImmutableUpdateTest]`)

Validates named queues rebuilt from `Update()` with `Immutable()` + `Reset()`:

- time lock callback cadence (`~1s`)
- strict order `A -> B -> C -> D`
- `C -> D` delayed by `~1s`
- cycle interval consistency

Useful for testing **idempotent queue construction** patterns.

---

### `OneSecondDelay.cs` (`[OneSecondDelayTest]`)

Validates a longer mixed chain (`Add`, `Loop`, nested waits) with strict step markers:

- full step sequence from `Start 0` to `End 0`
- order correctness
- expected `~1s` spacing for each major transition
- total sequence duration around `~9s`

Useful for regressions in complex queue flow control.

---

### `ReverseInfinite.cs` (`[ReverseInfiniteTest]`)

Validates reverse + repeat behavior in an ongoing sequence:

- initial reversed prefix (`4 -> 3 -> REV`)
- values remain in expected range (`0..4`)
- event spacing near `~1s`
- reverse callback occurs multiple times
- direction flips around reverse points

Useful for reverse-direction safety checks.

---

### `ReverseYoyo.cs` (`[ReverseYoyoTest]`)

Validates `Yoyo()` behavior on finite loops:

- queue completes in non-repeat mode
- loop runs both forward and reverse
- `t` covers expected range in both directions
- `deltaTime` sign flips with direction
- average loop run duration near `~1s`
- total yoyo sequence duration near `~4s`

Useful for validating forward/backward interpolation semantics.

---

### `TweenDeltaOrT.cs` (`[TweenDeltaOrTTest]`)

Compares two tweening approaches:

- loop 1: `deltaTime`-driven lerp to target over `2.5s`
- loop 2: `t` + easing over `2.5s`

Checks include:

- frame activity
- loop durations
- monotonic `t`
- `t` range coverage
- final positions (`y ~ 1`, then `y ~ -1`)
- total sequence duration (`~5s`)

Useful when touching `TeaHandler.t` or `TeaHandler.deltaTime` logic.

---

### `TweenQueue1.cs` (`[TweenQueue1Test]`)

Validates queues that keep appending tweens while running:

- deterministic sequence of 4 queued tweens (color/scale/color/scale)
- per-tween start/completion, frame count, duration, deltaTime activity
- final value closeness to target
- queue completion and total sequence duration (`~4s`)

Useful for append-while-running scenarios.

---

### `WaitForFunc.cs` (`[WaitForFuncTest]`)

Validates equivalence of:

- `Wait(() => condition, tick)`
- manual loop + `Break()` + `Wait(tick)`

Checks include:

- both queues complete
- prelude/action callbacks happen
- prelude occurs after condition flip
- latency between both approaches is closely matched

Useful when changing `Wait` semantics.

---

### `WaitNested.cs` (`[WaitNestedTest]`)

Validates nested queue waiting with deterministic cycles:

- cycle 1 waits `@1` for `1s`
- cycle 2 waits `@2` for `2s`
- cycle 3 waits `@3` for `3s`

Checks include:

- expected cycle and callback counts
- no duplicate/invalid nested callbacks
- queue order `@1 -> @2 -> @3`
- per-cycle wait durations
- cycle start intervals and total nested duration (`~6s`)

Useful for nested `t.Wait(otherTeaTime)` behavior.

---

### `StressSmoke.cs` (`[StressSmokeTest]`)

Finite stress smoke harness with deterministic pass/fail output:

- spins up multiple worker queues in parallel
- appends tasks while workers are already running
- injects pause/resume churn on a subset of workers
- verifies all workers complete and all appended callbacks are consumed
- checks loop-frame floor and overall completion window

Use this as a **fast sanity check** after changing queue internals.

---

### `StressBenchmark.cs` (`[StressBenchmark]`)

Metrics-oriented stress benchmark (reporting, not strict pass/fail):

- sustained parallel worker loops
- optional append-mutation load during benchmark window
- drain phase to observe pending backlog recovery
- callback throughput (`/s`), frame-time stats, queue snapshots
- GC collection deltas and managed-memory drift

Use this to compare TeaTime performance across branches/machines.

---

## Additional manual / profiler examples

### `StressTest.cs`

Legacy high-volume profiler scenario (chaos mode).
Useful for manual profiling sessions alongside `StressSmoke` and `StressBenchmark`.

### `WaitLoadSceneAsync.cs` (`[WaitLoadSceneAsyncTest]`)

Validates async scene load/unload orchestration through TeaTime waits:

- starts additive async load (path/name fallback)
- waits for load completion with poll loop + `t.Wait(tick)`
- resolves the newly loaded scene from scene-handle delta
- starts async unload for that exact scene
- waits for unload completion and verifies `isLoaded` transitions

Checks include request creation, completion, polling activity, and timeline order.
Useful when changing wait-loop behavior around Unity `AsyncOperation` scene workflows.

---

## Practical notes for TeaTime users

- Keep `Time.timeScale = 1` when evaluating these tests.
- Avoid heavy editor load if you want stable timing measurements.
- Treat these scripts as **smoke tests** (fast confidence), not strict CI-grade proofs.
- If needed, migrate critical checks into Unity PlayMode tests with hard assertions.
