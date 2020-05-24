﻿// TeaTime v0.8.6 beta

// TeaTime is a fast & simple queue for timed callbacks, focused on solving
// common coroutines patterns in Unity games.

// Andrés Villalobos ~ twitter.com/matnesis ~ andresalvivar@gmail.com
// Created 2014/12/26 12:21 am ~ Rewritten 2015/09/15 12:28 pm

// Copyright (c) 2014/12/26 andresalvivar@gmail.com

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// Timed Task node.
internal class ttTask
{
    public bool isLoop = false;

    public float time = 0;
    public Func<float> timeByFunc = null;

    public Action callback = null;
    public Action<ttHandler> callbackWithHandler = null;
}

/// TeaTime handler for callbacks.
public class ttHandler
{
    /// Current TeaTime queue.
    public TeaTime self;

    public float t = 0;
    public float deltaTime = 0;
    public float timeSinceStart = 0;

    internal bool isLooping = false;
    internal bool isReversed = false;
    internal List<YieldInstruction> yieldsToWait = null;

    /// Ends the current loop.
    public void EndLoop()
    {
        isLooping = false;
    }

    /// Appends a YieldInstruction to wait after the current callback execution.
    public void Wait(YieldInstruction yi)
    {
        if (yieldsToWait == null)
            yieldsToWait = new List<YieldInstruction>();

        yieldsToWait.Add(yi);
    }

    /// Appends a time delay to wait after the current callback execution.
    public void Wait(float time)
    {
        if (time <= 0) return;

        Wait(new WaitForSeconds(time));
    }

    /// Appends a TeaTime to wait after the current callback execution that
    /// is also affected by the queue .Stop() and .Reset().
    public void Wait(TeaTime tt)
    {
        // A reference to the waiting list
        if (!self._waiting.Contains(tt))
        {
            self._waiting.Add(tt);
            Wait(tt.WaitForCompletion());
        }
    }

    /// Appends a boolean condition to wait until true after the current
    /// callback execution.
    public void Wait(Func<bool> condition, float checkDelay)
    {
        // #todo This need to be cached somehow
        Wait(self._instance.tt().Wait(condition, checkDelay));
    }
}

/// TeaTime core extensions (static magic).
public static class TeaTimeExtensions
{
    private static Dictionary<MonoBehaviour, Dictionary<string, TeaTime>> ttRegister; // Queues bounded by 'tt(string)'

    /// Returns a new TeaTime queue ready to be used. This is basically a
    /// shorcut to 'new TeaTime(this);' in MonoBehaviours.
    public static TeaTime tt(this MonoBehaviour instance)
    {
        return new TeaTime(instance);
    }

    /// Returns a TeaTime queue bounded to his name, unique per
    /// MonoBehaviour instance, new on the first call. This allows you to
    /// access queues without a formal definition.
    public static TeaTime tt(this MonoBehaviour instance, string queueName)
    {
        // #todo ttRegister will (probably) need an auto clean up from
        // time to time if this technique is used in volatile GameObjects.

        // First time
        if (ttRegister == null)
            ttRegister = new Dictionary<MonoBehaviour, Dictionary<string, TeaTime>>();

        if (!ttRegister.ContainsKey(instance))
            ttRegister[instance] = new Dictionary<string, TeaTime>();

        if (!ttRegister[instance].ContainsKey(queueName))
            ttRegister[instance][queueName] = new TeaTime(instance);

        return ttRegister[instance][queueName];
    }
}

/// (Currently unused) TeaTime custom yield that waits for completion.
internal class ttWaitForCompletion : CustomYieldInstruction
{
    private TeaTime tt;

    public override bool keepWaiting { get { return tt.IsCompleted; } }

    public ttWaitForCompletion(TeaTime tt) { this.tt = tt; }
}

/// YieldInstruction static cache!
/// Found here http://forum.unity3d.com/threads/c-coroutine-waitforseconds-garbage-collection-tip.224878/
public static class ttYield
{
    class FloatComparer : IEqualityComparer<float>
    {
        bool IEqualityComparer<float>.Equals(float x, float y) { return x == y; }
        int IEqualityComparer<float>.GetHashCode(float obj) { return obj.GetHashCode(); }
    }

    static Dictionary<float, WaitForSeconds> _secondsCache = new Dictionary<float, WaitForSeconds>(100, new FloatComparer());

    static WaitForEndOfFrame _endOfFrame = new WaitForEndOfFrame();
    public static WaitForEndOfFrame EndOfFrame
    {
        get { return _endOfFrame; }
    }

    static WaitForFixedUpdate _fixedUpdate = new WaitForFixedUpdate();
    public static WaitForFixedUpdate FixedUpdate
    {
        get { return _fixedUpdate; }
    }

    public static WaitForSeconds Seconds(float seconds)
    {
        WaitForSeconds wfs = null;

        if (!_secondsCache.TryGetValue(seconds, out wfs))
            _secondsCache.Add(seconds, wfs = new WaitForSeconds(seconds));

        return wfs;
    }
}

/// TeaTime is a fast & simple queue for timed callbacks, focused on solving
/// common coroutines patterns in Unity games.
public class TeaTime
{
    // Queue
    private List<ttTask> _tasks = new List<ttTask>(); // Tasks list used as a queue
    internal List<TeaTime> _waiting = new List<TeaTime>(); // TeaTimes to wait via ttHandler.Wait(
    private int _currentTask = 0; // Current task mark (to be executed)
    private int _executedCount = 0; // Executed task count
    private int _lastPlayExecutedCount = 0; // Executed task count during the last play

    // Dependencies
    internal MonoBehaviour _instance = null; // Required to access Unity coroutine fuctions
    private Coroutine _currentCoroutine = null; // Coroutine that holds the queue execution

    // States
    private bool _isPlaying = false; // True while queue execution
    private bool _isPaused = false; // On .Pause()
    private bool _isImmutable = false; // On .Immutable() mode
    private bool _isRepeating = false; // On .Repeat() mode
    private bool _isConsuming = false; // On .Consume() mode
    private bool _isReversed = false; // On .Reverse() Backward() Forward() mode
    private bool _isYoyo = false; // On .Yoyo() mode

    /// True while the queue is being executed.
    public bool IsPlaying
    {
        get { return _isPlaying; }
    }

    /// True if the queue execution is done.
    public bool IsCompleted
    {
        get { return _currentTask >= _tasks.Count && !_isPlaying; }
    }

    /// Queue count.
    public int Count
    {
        get { return _tasks.Count; }
    }

    /// Current queue position to be executed.
    public int Current
    {
        get { return _currentTask; }
    }

    /// Executed callback count.
    public int ExecutedCount
    {
        get { return _executedCount; }
    }

    /// A TeaTime queue requires a MonoBehaviour instance to access his
    /// coroutine fuctions.
    public TeaTime(MonoBehaviour instance)
    {
        _instance = instance;
    }

    // ADD

    /// Appends a new ttTask.
    private TeaTime Add(float timeDelay, Func<float> timeDelayByFunc, Action callback, Action<ttHandler> callbackWithHandler)
    {
        // Ignores appends on Immutable mode
        if (!_isImmutable)
        {
            ttTask newTask = new ttTask();
            newTask.time = timeDelay;
            newTask.timeByFunc = timeDelayByFunc;
            newTask.callback = callback;
            newTask.callbackWithHandler = callbackWithHandler;

            _tasks.Add(newTask);
        }

        // Autoplay if not paused or playing
        return _isPaused || _isPlaying ? this : this.Play();
    }

    /// Appends a timed callback.
    public TeaTime Add(float timeDelay, Action callback)
    {
        return Add(timeDelay, null, callback, null);
    }

    /// Appends a timed callback.
    public TeaTime Add(Func<float> timeByFunc, Action callback)
    {
        return Add(0, timeByFunc, callback, null);
    }

    /// Appends a timed callback.
    public TeaTime Add(float timeDelay, Action<ttHandler> callback)
    {
        return Add(timeDelay, null, null, callback);
    }

    /// Appends a timed callback.
    public TeaTime Add(Func<float> timeByFunc, Action<ttHandler> callback)
    {
        return Add(0, timeByFunc, null, callback);
    }

    /// Appends a time delay.
    public TeaTime Add(float timeDelay)
    {
        return Add(timeDelay, null, null, null);
    }

    /// Appends a time delay.
    public TeaTime Add(Func<float> timeByFunc)
    {
        return Add(0, timeByFunc, null, null);
    }

    /// Appends a callback.
    public TeaTime Add(Action callback)
    {
        return Add(0, null, callback, null);
    }

    /// Appends a callback.
    public TeaTime Add(Action<ttHandler> callback)
    {
        return Add(0, null, null, callback);
    }

    // LOOP

    /// Appends a callback loop (if duration is less than 0, the loop runs
    /// infinitely).
    private TeaTime Loop(float duration, Func<float> durationByFunc, Action<ttHandler> callback)
    {
        // Ignores appends on Immutable mode
        if (!_isImmutable)
        {
            ttTask newTask = new ttTask();
            newTask.isLoop = true;
            newTask.time = duration;
            newTask.timeByFunc = durationByFunc;
            newTask.callbackWithHandler = callback;

            _tasks.Add(newTask);
        }

        // Autoplay if not paused or playing
        return _isPaused || _isPlaying ? this : this.Play();
    }

    /// Appends a callback loop (if duration is less than 0,
    /// the loop runs infinitely).
    public TeaTime Loop(float duration, Action<ttHandler> callback)
    {
        return Loop(duration, null, callback);
    }

    /// Appends a callback loop (if duration is less than 0,
    /// the loop runs infinitely).
    public TeaTime Loop(Func<float> durationByFunc, Action<ttHandler> callback)
    {
        return Loop(0, durationByFunc, callback);
    }

    /// Appends an infinite callback loop.
    public TeaTime Loop(Action<ttHandler> callback)
    {
        return Loop(-1, null, callback);
    }

    // QUEUE MODES

    /// Enables Immutable mode, the queue will ignore new appends (.Add
    /// .Loop .If)
    public TeaTime Immutable()
    {
        _isImmutable = true;

        return this;
    }

    /// Enables Repeat mode, the queue will always be restarted on
    /// completion.
    public TeaTime Repeat()
    {
        _isRepeating = true;

        return this;
    }

    /// Enables Consume mode, the queue will remove each callback after
    /// execution.
    public TeaTime Consume()
    {
        _isConsuming = true;

        return this;
    }

    /// Reverses the callback execution order (From .Forward() to
    /// .Backward() mode and viceversa).
    public TeaTime Reverse()
    {
        _isReversed = !_isReversed;
        if (IsPlaying) _currentTask = _tasks.Count - _currentTask;

        return this;
    }

    /// Enables Backward mode, executing callbacks on reverse order
    /// (including Loops).
    public TeaTime Backward()
    {
        if (!_isReversed) return this.Reverse();
        return this;
    }

    /// Enables Forward mode (the default), executing callbacks one after the
    /// other.
    public TeaTime Forward()
    {
        if (_isReversed) return this.Reverse();
        return this;
    }

    /// Enables Yoyo mode, that will .Reverse() the callback execution order
    /// when the queue is completed. Only once per play without Repeat mode.
    public TeaTime Yoyo()
    {
        _isYoyo = true;

        return this;
    }

    /// Disables all modes (Immutable, Repeat, Consume, Backward, Yoyo).
    /// Just like new.
    public TeaTime Release()
    {
        _isImmutable = _isRepeating = _isConsuming = _isYoyo = false;
        return this.Forward();
    }

    // CONTROL

    /// Pauses the queue execution (use .Play() to resume).
    public TeaTime Pause()
    {
        _isPaused = true;

        return this;
    }

    /// Stops the queue execution (use .Play() to start over).
    public TeaTime Stop()
    {
        if (_currentCoroutine != null)
            _instance.StopCoroutine(_currentCoroutine);

        _currentTask = 0;
        _isPlaying = false;

        // Stop all TeaTimes on .Wait(
        for (int i = 0, len = _waiting.Count; i < len; i++)
            _waiting[i].Stop();
        _waiting.Clear();

        return this;
    }

    /// Starts or resumes the queue execution.
    public TeaTime Play()
    {
        // Unpause always
        _isPaused = false;

        // Ignore if currently playing
        if (_isPlaying) return this;

        // or Empty?
        if (_tasks.Count <= 0) return this;

        // Restart if already finished
        if (_currentTask >= _tasks.Count)
            _currentTask = 0;

        // Execute!
        _currentCoroutine = _instance.StartCoroutine(ExecuteQueue());

        return this;
    }

    /// Restarts the queue execution (.Stop().Play()).
    public TeaTime Restart()
    {
        // Alias
        return this.Stop().Play();
    }

    // DESTRUCTION

    /// Stops and cleans the queue, turning off all modes (Immutable,
    /// Repeat, Consume, Backward, Yoyo). Just like new.
    public TeaTime Reset()
    {
        // Reset current
        if (_currentCoroutine != null)
            _instance.StopCoroutine(_currentCoroutine);

        _tasks.Clear();
        _currentTask = 0;
        _executedCount = 0;

        _isPlaying = false;
        _isPaused = false;

        // Modes off
        _isImmutable = false;
        _isRepeating = false;
        _isConsuming = false;
        _isYoyo = false;
        this.Forward();

        // Reset all TeaTimes on .Wait(
        for (int i = 0, len = _waiting.Count; i < len; i++)
            _waiting[i].Reset();
        _waiting.Clear();

        return this;
    }

    // SPECIAL

    /// The queue will stop if the condition isn't fullfiled, or restarted
    /// on Repeat mode.
    public TeaTime If(Func<bool> condition)
    {
        return this.Add(() =>
        {
            if (!condition())
            {
                if (_isRepeating)
                {
                    this.Restart();
                }
                else
                {
                    this.Stop();
                }
            }
        });
    }

    // A note about .Wait(:

    // 1 It would be redundant to add a .Wait(time) because there is an
    // Add(time) currently. Hm, I need to address this to avoid uglyness.

    // 2 A Wait(YieldInstruction) would be useless after the first time a
    // TeaTime runs, because the Yield reference can't be used again when a
    // TeaTime is replayed.

    /// The queue will wait until the TeaTime is fullfiled.
    public TeaTime Wait(TeaTime tt)
    {
        return this.Add((ttHandler t) =>
        {
            t.Wait(tt);
        });
    }

    /// The queue will wait until the boolean condition is fullfiled.
    public TeaTime Wait(Func<bool> untilCondition, float checkDelay = 0)
    {
        return this.Loop((ttHandler t) =>
        {
            if (untilCondition()) t.EndLoop();
            t.Wait(checkDelay);
        });
    }

    // CUSTOM YIELDS

    /// IEnumerator that waits the completion of a TeaTime.
    private IEnumerator WaitForCompletion(TeaTime tt)
    {
        while (!tt.IsCompleted) yield return null;
    }

    /// Returns a YieldInstruction that waits until the queue is completed.
    public YieldInstruction WaitForCompletion()
    {
        // #todo Could this be cached somehow?
        return _instance.StartCoroutine(WaitForCompletion(this));
    }

    // THE COROUTINE

    /// This is the main algorithm. Executes all tasks, one after the
    /// other, calling their callbacks according to type, time and queue
    /// config.
    private IEnumerator ExecuteQueue()
    {
        _isPlaying = true;

        int reverseLastTask = -1; // Important: This value needs to be reset to default on most queue changes
        _lastPlayExecutedCount = 0;

        // :D!
        // Let's wait
        // 1 For secuencial Adds or Loops before their first execution
        // 2 Maybe a callback is trying to modify his own queue
        yield return ttYield.EndOfFrame;

        while (_currentTask < _tasks.Count)
        {
            // Current task to be executed
            int taskId = _currentTask;
            if (_isReversed) taskId = _tasks.Count - 1 - _currentTask;
            ttTask currentTask = _tasks[taskId];

            // Next task (or previous if the queue is backward)
            _currentTask++;

            // Avoid executing a task twice when reversed and the queue
            // hasn't reached the end
            if (taskId == reverseLastTask) continue;
            reverseLastTask = taskId;

            // :D?
            // yield return ttYield.EndOfFrame;

            // It's a loop
            if (currentTask.isLoop)
            {
                // Holds the duration
                float loopDuration = currentTask.time;

                // Func<float> added
                if (currentTask.timeByFunc != null)
                    loopDuration += currentTask.timeByFunc();

                // Nothing to do, skip
                if (loopDuration == 0)
                    continue;

                // Loops will always need a handler
                ttHandler loopHandler = new ttHandler();
                loopHandler.self = this;
                loopHandler.isLooping = true;
                loopHandler.isReversed = _isReversed;

                // Negative time means the loop is infinite
                bool isInfinite = loopDuration < 0;

                // T quotient
                float tRate = isInfinite ? 0 : 1 / loopDuration;

                // Progresion depends on current direction
                if (loopHandler.isReversed)
                {
                    loopHandler.t = 1f;
                    tRate = -tRate;
                }

                // While looping and, until time or infinite
                while (loopHandler.isLooping && (loopHandler.isReversed ? loopHandler.t >= 0 : loopHandler.t <= 1))
                {
                    // Check for queue reversal
                    if (_isReversed != loopHandler.isReversed)
                    {
                        tRate = -tRate;
                        loopHandler.isReversed = _isReversed;
                    }

                    float unityDeltaTime = Time.deltaTime;

                    // Completion % from 0 to 1
                    if (!isInfinite)
                        loopHandler.t += tRate * unityDeltaTime;

                    // On finite loops this .deltaTime is sincronized with
                    // the exact loop duration
                    loopHandler.deltaTime =
                        isInfinite ?
                        unityDeltaTime :
                        1 / (loopDuration - loopHandler.timeSinceStart) * unityDeltaTime;

                    // .deltaTime is also reversed
                    if (loopHandler.isReversed)
                        loopHandler.deltaTime = -loopHandler.deltaTime;

                    // A classic
                    loopHandler.timeSinceStart += unityDeltaTime;

                    // Pause?
                    while (_isPaused)
                        yield return null;

                    // Loops will always have a callback with a handler
                    currentTask.callbackWithHandler(loopHandler);

                    // Handler .WaitFor(
                    if (loopHandler.yieldsToWait != null)
                    {
                        for (int i = 0, len = loopHandler.yieldsToWait.Count; i < len; i++)
                            yield return loopHandler.yieldsToWait[i];

                        loopHandler.yieldsToWait.Clear();
                    }

                    // Minimum sane delay
                    if (loopHandler.yieldsToWait == null)
                        yield return null;
                }

                // Executed +1
                _executedCount += 1;
                _lastPlayExecutedCount += 1;
            }
            // It's a timed callback
            else
            {
                // Holds the delay
                float delayDuration = currentTask.time;

                // Func<float> added
                if (currentTask.timeByFunc != null)
                    delayDuration += currentTask.timeByFunc();

                // // Time delay
                // if (delayDuration > 0)
                //     yield return ttYield.Seconds(delayDuration);

                // Is this more precise that the previous commented code?
                float time = 0;
                while (time < delayDuration)
                {
                    time += Time.deltaTime;
                    yield return null;
                }

                // Pause?
                while (_isPaused)
                    yield return null;

                // Normal callback
                if (currentTask.callback != null)
                    currentTask.callback();

                // Callback with handler
                if (currentTask.callbackWithHandler != null)
                {
                    ttHandler handler = new ttHandler();
                    handler.self = this;

                    handler.t = 1;
                    handler.timeSinceStart = delayDuration;
                    handler.deltaTime = Time.deltaTime;

                    currentTask.callbackWithHandler(handler);

                    // Handler WaitFor
                    if (handler.yieldsToWait != null)
                    {
                        for (int i = 0, len = handler.yieldsToWait.Count; i < len; i++)
                            yield return handler.yieldsToWait[i];

                        handler.yieldsToWait.Clear();
                    }

                    // Minimum sane delay
                    if (delayDuration <= 0 && handler.yieldsToWait == null)
                        yield return null;
                }
                else if (delayDuration <= 0)
                    yield return null;

                // Executed +1
                _executedCount += 1;
                _lastPlayExecutedCount += 1;
            }

            // Just at the end of a complete queue execution
            if (_tasks.Count > 0 && _currentTask >= _tasks.Count)
            {
                // Forget current nested queues
                _waiting.Clear();
            }

            // Consume mode removes the task after execution
            // #todo Need to be tested with .Reverse() stuff
            if (_isConsuming)
            {
                _currentTask -= 1;
                _tasks.Remove(currentTask);

                reverseLastTask = -1; // To default
            }

            // On Yoyo mode the queue is reversed at the end, only once per
            // play without Repeat mode
            if (_isYoyo && _currentTask >= _tasks.Count && (_lastPlayExecutedCount <= _tasks.Count || _isRepeating))
            {
                this.Reverse();

                reverseLastTask = -1; // To default
            }

            // Repeats on Repeat mode
            if (_isRepeating && _tasks.Count > 0 && _currentTask >= _tasks.Count)
            {
                _currentTask = 0;

                reverseLastTask = -1; // To default
            }
        }

        // Done!
        _isPlaying = false;

        yield return null;
    }
}

// <3
// Lerp t Formulas

// Ease out
// t = Mathf.Sin(t * Mathf.PI * 0.5f);

// Ease in
// t = 1f - Mathf.Cos(t * Mathf.PI * 0.5f)

// Exponential
// t = t*t

// Smoothstep
// t = t*t * (3f - 2f*t)

// Smootherstep
// t = t*t*t * (t * (6f*t - 15f) + 10f)