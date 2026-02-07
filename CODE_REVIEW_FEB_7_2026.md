# TeaTime.cs Code Review

**Date:** 2026-02-07
**Reviewer:** Coding Assistant
**File Reviewed:** `TeaTime.cs`

---

## 1) Executive Summary

`TeaTime.cs` is a solid, practical coroutine queue system for Unity with a fluent API and useful modes (`Repeat`, `Consume`, `Reverse`, `Yoyo`, nested waits, etc.).

The code is generally clean and usable, but there are a few correctness risks that should be addressed, especially one high-impact issue that can cause tight-loop behavior.

---

## 2) What’s Good

- Clear API design and chaining ergonomics.
- Good abstraction split (`TeaTask`, `TeaHandler`, `TeaTime`, extension helpers).
- Useful coroutine caching utility in `TeaYield`.
- Supports advanced sequencing patterns beyond standard Unity coroutines.

---

## 3) Findings by Priority

| Severity   | Finding                                                             | Location                                                                    |
| ---------- | ------------------------------------------------------------------- | --------------------------------------------------------------------------- |
| **High**   | Potential tight-loop/freeze due to `yields` list lifecycle          | `ExecuteQueue()` loop + callback branches (~725-734, ~787-797)              |
| **High**   | `WaitForCompletion()` can wait forever if target queue is stopped   | `IsCompleted` (~216), `Stop()` (~463-474), `WaitForCompletion()` (~594-597) |
| **Medium** | Pause does not pause time already inside `WaitForSeconds`           | Timed callback path (~753 then ~764)                                        |
| **Medium** | Loop `deltaTime` formula becomes unstable near loop end             | Loop branch (~706-708)                                                      |
| **Low**    | Named queue static register may retain stale MonoBehaviour refs     | `TeaTimeExtensions.register` (~109-136)                                     |
| **Low**    | WaitForSeconds cache can grow unbounded with many unique float keys | `TeaYield.secondsCache` (~157-179)                                          |
| **Low**    | `Stop()` leaves `currentCoroutine` non-null                         | `Stop()` (~463-476)                                                         |
| **Low**    | `Wait(until, tick)` waits one extra tick after condition turns true | `Wait(Func<bool>, float)` (~578-587)                                        |

---

## 4) Detailed Findings

### 4.1 High: Potential tight-loop/freeze after first handler wait usage

**Where**

- Loop path in `ExecuteQueue()`: ~725-734
- Timed callback with handler path: ~787-797

**Why it happens**

- Code checks `if (task.handler.yields != null)`.
- When waits are present, yields are consumed and then `task.handler.yields.Clear()` is called.
- The list remains non-null (just empty).
- Later frames enter the `!= null` branch again but may have **0 yields**, and no fallback `yield return null` occurs.

This can cause extremely tight iteration within a single frame (especially dangerous for infinite loops), potentially freezing/stalling.

**Recommended fix**
Use count-aware checks:

- `if (task.handler.yields != null && task.handler.yields.Count > 0)` then yield each.
- Else do fallback `yield return null`.

And in callback path, replace `task.handler.yields == null` logic with `task.handler.yields == null || task.handler.yields.Count == 0`.

**Status (current code fix)**
- ✅ Implemented in `TeaTime.cs`.
- Loop handler wait now uses count-aware check:
  - `task.handler.yields != null && task.handler.yields.Count > 0`
- Timed callback handler wait now uses the same count-aware check.
- Minimum-delay fallback now treats null and empty lists as equivalent:
  - `task.handler.yields == null || task.handler.yields.Count == 0`
- Added inline comments near both sites documenting why this is required (to avoid tight-loop/freeze after `Clear()`).

---

### 4.2 High: `WaitForCompletion()` can hang forever when queue is stopped

**Where**

- `IsCompleted`: returns `taskIndex >= tasks.Count && !isPlaying`.
- `Stop()`: sets `taskIndex = 0`, `isPlaying = false`.
- `WaitForCompletion(TeaTime tt)`: loops while `!tt.IsCompleted`.

**Impact**
For a non-empty queue, `Stop()` makes `IsCompleted == false` and keeps it false indefinitely unless queue is played to completion. Any coroutine waiting on `WaitForCompletion()` may never finish.

**Recommended options**

1. Treat stopped as completed (behavioral change; likely easiest for waiting semantics), or
2. Add explicit state (`isStopped`) and update `WaitForCompletion` logic accordingly, or
3. Provide separate API semantics (`WaitForFinish` vs `WaitForCompletion`) and document differences.

---

### 4.3 Medium: Pause does not pause positive delay already in progress

**Where**

- Delay is awaited first: `yield return TeaYield.WaitForSeconds(delayDuration)`
- Pause check comes after delay.

**Impact**
If queue is paused during that delay, elapsed time continues. This may be surprising if users expect pause to freeze all progress.

**Suggested adjustment**
If full pause semantics are desired, replace direct `WaitForSeconds` with a loop accumulating only while not paused.

---

### 4.4 Medium: Finite loop `deltaTime` formula is numerically unstable near completion

**Where**

- `task.handler.deltaTime = 1 / (loopDuration - task.handler.timeSinceStart) * unityDeltaTime`

**Impact**
As `timeSinceStart` approaches `loopDuration`, denominator approaches zero and `deltaTime` spikes. This can lead to abrupt interpolation or unstable motion.

**Suggested adjustment**
If intent is normalized progress delta, use:

- `unityDeltaTime / loopDuration` (with guards for tiny durations), then negate when reversed.

---

### 4.5 Low: Static queue register can retain stale references

**Where**

- `TeaTimeExtensions.register` dictionary keyed by `MonoBehaviour`.

**Impact**
Destroyed or volatile objects may remain referenced unless explicitly cleaned, causing memory growth over long sessions.

**Suggestion**
Implement periodic cleanup of null/destroyed keys, or avoid long-lived static registration for volatile objects.

---

### 4.6 Low: `WaitForSeconds` cache may grow without bounds

**Where**

- `TeaYield.secondsCache` keyed by raw `float`.

**Impact**
Many unique float delays can create an ever-growing cache.

**Suggestion**
Quantize seconds (e.g., milliseconds), cap cache size, or expose a clear/reset strategy.

---

### 4.7 Low: `Stop()` leaves coroutine handle non-null

**Where**

- `Stop()` stops coroutine but does not set `currentCoroutine = null`.

**Impact**
Mostly minor/stale handle risk; not currently breaking behavior.

**Suggestion**
Set `currentCoroutine = null` in `Stop()` for consistency with `Reset()`.

---

### 4.8 Low: `Wait(until, tick)` incurs one extra tick delay after condition becomes true

**Where**

- In `Wait(Func<bool>, float)`, `t.Wait(tick)` is always called even after `t.Break()`.

**Impact**
Adds one unnecessary wait tick when condition is met.

**Suggestion**
Only call `t.Wait(tick)` in `else` branch:

- `if (until()) t.Break(); else t.Wait(tick);`

---

## 5) Suggested Fix Order

1. Fix yields-list tight-loop issue in `ExecuteQueue()` (**urgent**).
2. Decide and implement explicit semantics for stopped queues vs completion.
3. Stabilize loop `deltaTime` formula.
4. Align pause semantics with expected behavior (if desired).
5. Address low-priority lifecycle/cache hygiene items.

---

## 6) Final Assessment

The library architecture is strong and practical. Most issues are edge-case or semantics related, but two high-priority items (yields tight-loop and completion wait hang on stop) are worth fixing before wider production use.
