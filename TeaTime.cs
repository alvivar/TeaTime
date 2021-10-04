// TeaTime v0.9

// TeaTime is a fast & simple queue for timed callbacks, focused on solving
// common coroutines patterns in Unity games.

// Author: Andrés Villalobos | andresalvivar@gmail.com | github.com/alvivar

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
public class TeaTask
{
    public bool isLoop = false;

    public float time = 0;
    public Func<float> timeByFunc = null;

    public Action callback = null;
    public Action<TeaHandler> callbackWithHandler = null;

    public TeaHandler handler = null;
}

/// TeaTime handler for callbacks.
public class TeaHandler
{
    /// Current TeaTime queue.
    public TeaTime self;

    public float t = 0;
    public float deltaTime = 0;
    public float timeSinceStart = 0;

    public bool isLooping = false;
    public bool isReversed = false;
    public List<YieldInstruction> yields = null;

    /// Ends the current loop.
    public void Break()
    {
        isLooping = false;
    }

    /// Appends a YieldInstruction to wait after the current callback execution.
    public void Wait(YieldInstruction yi)
    {
        if (yields == null)
            yields = new List<YieldInstruction>();

        yields.Add(yi);
    }

    /// Appends a time delay to wait after the current callback execution.
    public void Wait(float time)
    {
        if (time <= 0)
            return;

        Wait(TeaYield.WaitForSeconds(time));
    }

    /// Appends a TeaTime to wait after the current callback execution that is
    /// also affected by the queue .Stop() and .Reset().
    public void Wait(TeaTime tt)
    {
        // A reference to the waiting list
        if (!self.waiting.Contains(tt))
        {
            self.waiting.Add(tt);
            Wait(tt.WaitForCompletion());
        }
    }

    /// Appends a boolean condition to wait until true after the current
    /// callback execution.
    public void Wait(Func<bool> condition, float checkDelay)
    {
        // @todo This need to be cached somehow
        Wait(self.monoBehaviour.tt().Wait(condition, checkDelay));
    }
}

/// TeaTime core extensions (static magic).
public static class TeaTimeExtensions
{
    private static Dictionary<MonoBehaviour, Dictionary<string, TeaTime>> register; // Queues bounded by 'tt(string)'

    /// Returns a new TeaTime queue ready to be used. This is basically a
    /// shorcut to 'new TeaTime(this);' in MonoBehaviours.
    public static TeaTime tt(this MonoBehaviour instance)
    {
        return new TeaTime(instance);
    }

    /// Returns a TeaTime queue bounded to his name, unique per MonoBehaviour
    /// instance, new on the first call. This allows you to access queues
    /// without a formal definition.
    public static TeaTime tt(this MonoBehaviour instance, string queueName)
    {
        // @todo 'register' will (probably) need an auto clean up from time to
        // time if this technique is used in volatile GameObjects.

        // First time
        if (register == null)
            register = new Dictionary<MonoBehaviour, Dictionary<string, TeaTime>>();

        if (!register.ContainsKey(instance))
            register[instance] = new Dictionary<string, TeaTime>();

        if (!register[instance].ContainsKey(queueName))
            register[instance][queueName] = new TeaTime(instance);

        return register[instance][queueName];
    }
}

/// YieldInstruction static cache! Found here:
/// http://forum.unity3d.com/threads/c-coroutine-waitforseconds-garbage-collection-tip.224878/
public static class TeaYield
{
    private class FloatComparer : IEqualityComparer<float>
    {
        bool IEqualityComparer<float>.Equals(float x, float y) { return x == y; }
        int IEqualityComparer<float>.GetHashCode(float obj) { return obj.GetHashCode(); }
    }

    private static Dictionary<float, WaitForSeconds> secondsCache = new Dictionary<float, WaitForSeconds>(new FloatComparer());

    private static WaitForEndOfFrame endOfFrame = new WaitForEndOfFrame();
    public static WaitForEndOfFrame EndOfFrame { get { return endOfFrame; } }

    private static WaitForFixedUpdate fixedUpdate = new WaitForFixedUpdate();
    public static WaitForFixedUpdate FixedUpdate { get { return fixedUpdate; } }

    public static WaitForSeconds WaitForSeconds(float seconds)
    {
        WaitForSeconds wfs;

        if (!secondsCache.TryGetValue(seconds, out wfs))
            secondsCache.Add(seconds, wfs = new WaitForSeconds(seconds));

        return wfs;
    }
}

/// TeaTime is a fast & simple queue for timed callbacks, focused on solving
/// common coroutines patterns in Unity games.
public class TeaTime
{
    // Queue
    private List<TeaTask> tasks = new List<TeaTask>(); // Tasks list used as a queue
    public List<TeaTime> waiting = new List<TeaTime>(); // TeaTimes to wait via TeaHandler.Wait(
    private int taskIndex = 0; // Current task mark (to be executed)
    private int executedCount = 0; // Executed task count
    private int lastPlayExecutedCount = 0; // Executed task count during the last play

    // Dependencies
    public MonoBehaviour monoBehaviour = null; // Required to access Unity coroutine fuctions
    private Coroutine currentCoroutine = null; // Coroutine that holds the queue execution

    // States
    private bool isPlaying = false; // True while queue execution
    private bool isPaused = false; // On .Pause()
    private bool isImmutable = false; // On .Immutable() mode
    private bool isRepeating = false; // On .Repeat() mode
    private bool isConsuming = false; // On .Consume() mode
    private bool isReversed = false; // On .Reverse() Backward() Forward() mode
    private bool isYoyo = false; // On .Yoyo() mode

    /// True while the queue is being executed.
    public bool IsPlaying
    {
        get { return isPlaying; }
    }

    /// True if the queue execution is done.
    public bool IsCompleted
    {
        get { return taskIndex >= tasks.Count && !isPlaying; }
    }

    /// Queue count.
    public int Count
    {
        get { return tasks.Count; }
    }

    /// Current queue position to be executed.
    public int Current
    {
        get { return taskIndex; }
    }

    /// Executed callback count.
    public int ExecutedCount
    {
        get { return executedCount; }
    }

    /// A TeaTime queue requires a MonoBehaviour instance to access his coroutine fuctions.
    public TeaTime(MonoBehaviour instance)
    {
        monoBehaviour = instance;
    }

    // ADD

    /// Appends a new TeaTask.
    private TeaTime Add(float timeDelay, Func<float> timeDelayByFunc, Action callback, Action<TeaHandler> callbackWithHandler)
    {
        // Ignores appends on Immutable mode
        if (!isImmutable)
        {
            TeaTask newTask = new TeaTask();
            newTask.time = timeDelay;
            newTask.timeByFunc = timeDelayByFunc;
            newTask.callback = callback;
            newTask.callbackWithHandler = callbackWithHandler;

            tasks.Add(newTask);
        }

        // Autoplay if not paused or playing
        return isPaused || isPlaying ? this : Play();
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
    public TeaTime Add(float timeDelay, Action<TeaHandler> callback)
    {
        return Add(timeDelay, null, null, callback);
    }

    /// Appends a timed callback.
    public TeaTime Add(Func<float> timeByFunc, Action<TeaHandler> callback)
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
    public TeaTime Add(Action<TeaHandler> callback)
    {
        return Add(0, null, null, callback);
    }

    /// Appends a TeaTime.
    public TeaTime Add(TeaTime tt)
    {
        return Add((TeaHandler t) => t.Wait(tt));
    }

    // LOOP

    /// Appends a callback loop (if duration is less than 0, the loop runs infinitely).
    private TeaTime Loop(float duration, Func<float> durationByFunc, Action<TeaHandler> callback)
    {
        // Ignores appends on Immutable mode
        if (!isImmutable)
        {
            TeaTask newTask = new TeaTask();
            newTask.isLoop = true;
            newTask.time = duration;
            newTask.timeByFunc = durationByFunc;
            newTask.callbackWithHandler = callback;

            tasks.Add(newTask);
        }

        // Autoplay if not paused or playing
        return isPaused || isPlaying ? this : Play();
    }

    /// Appends a callback loop (if duration is less than 0, the loop runs
    /// infinitely).
    public TeaTime Loop(float duration, Action<TeaHandler> callback)
    {
        return Loop(duration, null, callback);
    }

    /// Appends a callback loop (if duration is less than 0, the loop runs
    /// infinitely).
    public TeaTime Loop(Func<float> durationByFunc, Action<TeaHandler> callback)
    {
        return Loop(0, durationByFunc, callback);
    }

    /// Appends an infinite callback loop.
    public TeaTime Loop(Action<TeaHandler> callback)
    {
        return Loop(-1, null, callback);
    }

    // QUEUE MODES

    /// Enables Immutable mode, the queue will ignore new appends (.Add .Loop
    /// .If)
    public TeaTime Immutable()
    {
        isImmutable = true;

        return this;
    }

    /// Enables Repeat mode, the queue will always be restarted on completion.
    public TeaTime Repeat()
    {
        isRepeating = true;

        return this;
    }

    /// Enables Consume mode, the queue will remove each callback after
    /// execution.
    public TeaTime Consume()
    {
        isConsuming = true;

        return this;
    }

    /// Reverses the callback execution order (From .Forward() to .Backward()
    /// mode and viceversa).
    public TeaTime Reverse()
    {
        isReversed = !isReversed;

        // 1 I don't remember why IsPlaying is needed to allow reversing the
        // index, probably en edge case? @todo Or maybe I need to remove it and
        // test everything again.

        // 2 ...But taskIndex != 0 is important when Reverse() is called before
        // executing the first task, to make sure everything runs reversed from
        // the beginning.

        if (IsPlaying && taskIndex != 0)
            taskIndex = tasks.Count - taskIndex;

        return this;
    }

    /// Enables Backward mode, executing callbacks on reverse order (including
    /// Loops).
    public TeaTime Backward()
    {
        if (!isReversed)
            return Reverse();

        return this;
    }

    /// Enables Forward mode (the default), executing callbacks one after the
    /// other.
    public TeaTime Forward()
    {
        if (isReversed)
            return Reverse();

        return this;
    }

    /// Enables Yoyo mode, that will .Reverse() the callback execution order
    /// when the queue is completed. Only once per play without Repeat mode.
    public TeaTime Yoyo()
    {
        isYoyo = true;

        return this;
    }

    /// Disables all modes (Immutable, Repeat, Consume, Backward, Yoyo). Just
    /// like new.
    public TeaTime Release()
    {
        isImmutable = isRepeating = isConsuming = isYoyo = false;

        return Forward();
    }

    // CONTROL

    /// Pauses the queue execution (use .Play() to resume).
    public TeaTime Pause()
    {
        isPaused = true;

        return this;
    }

    /// Stops the queue execution (use .Play() to start over).
    public TeaTime Stop()
    {
        if (currentCoroutine != null)
            monoBehaviour.StopCoroutine(currentCoroutine);

        taskIndex = 0;
        isPlaying = false;

        // Stop all TeaTimes on .Wait(
        for (int i = 0, len = waiting.Count; i < len; i++)
            waiting[i].Stop();
        waiting.Clear();

        return this;
    }

    /// Starts or resumes the queue execution.
    public TeaTime Play()
    {
        // Unpause always
        isPaused = false;

        // Ignore if currently playing
        if (isPlaying)
            return this;

        // or empty?
        if (tasks.Count <= 0)
            return this;

        // Restart if already finished
        if (taskIndex >= tasks.Count)
            taskIndex = 0;

        // Execute!
        currentCoroutine = monoBehaviour.StartCoroutine(ExecuteQueue());

        return this;
    }

    /// Restarts the queue execution (.Stop().Play()).
    public TeaTime Restart()
    {
        // Alias
        return Stop().Play();
    }

    // DESTRUCTION

    /// Stops and cleans the queue, turning off all modes (Immutable, Repeat,
    /// Consume, Backward, Yoyo). Just like new.
    public TeaTime Reset()
    {
        // Reset current
        if (currentCoroutine != null)
        {
            monoBehaviour.StopCoroutine(currentCoroutine);
            currentCoroutine = null;
        }

        tasks.Clear();
        taskIndex = 0;
        executedCount = 0;

        isPlaying = false;
        isPaused = false;

        // Modes off
        isImmutable = false;
        isRepeating = false;
        isConsuming = false;
        isYoyo = false;
        Forward();

        // Reset all TeaTimes on .Wait(
        for (int i = 0, len = waiting.Count; i < len; i++)
            waiting[i].Reset();
        waiting.Clear();

        return this;
    }

    // SPECIAL

    // A note about .Wait(

    // 1 It would be redundant to add a .Wait(time) because there is an
    // Add(time) currently. The same with .Wait(TeaTime).

    // 2 A Wait(YieldInstruction) would be useless after the first time a
    // TeaTime runs, because the Yield reference can't be used again when a
    // TeaTime is replayed.

    /// The queue will stop if the condition isn't fullfiled, or restarted on
    /// Repeat mode.
    public TeaTime If(Func<bool> condition)
    {
        return Add(() =>
        {
            if (!condition())
            {
                if (isRepeating)
                {
                    Restart();
                }
                else
                {
                    Stop();
                }
            }
        });
    }

    /// The queue will wait until the boolean condition is fullfiled, checking
    /// every tick.
    public TeaTime Wait(Func<bool> until, float tick = 0)
    {
        return Loop((TeaHandler t) =>
        {
            if (until())
                t.Break();

            t.Wait(tick);
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
        // @todo Could this be cached somehow?
        return monoBehaviour.StartCoroutine(WaitForCompletion(this));
    }

    // THE COROUTINE

    /// This is the main algorithm. Executes all tasks, one after the other,
    /// calling their callbacks according to type, time and config.
    private IEnumerator ExecuteQueue()
    {
        isPlaying = true;

        int reverseLastTask = -1; // Important: This value needs to be reset to default on most queue changes

        lastPlayExecutedCount = 0;

        // Let's wait a frame
        // 1 For secuencial Adds or Loops before their first execution
        // 2 Maybe a callback is trying to modify his own queue
        yield return null;

        while (taskIndex < tasks.Count)
        {
            // Current task to be executed
            int taskId = taskIndex;

            if (isReversed)
                taskId = tasks.Count - 1 - taskIndex;

            TeaTask task = tasks[taskId];

            // Next task (or previous if the queue is backward)
            taskIndex++;

            // Avoid executing a task twice when reversed and the queue hasn't
            // reached the end
            if (taskId == reverseLastTask)
                continue;

            reverseLastTask = taskId;

            // It's a loop
            if (task.isLoop)
            {
                // Holds the duration
                float loopDuration = task.time;

                // Func<float> added
                if (task.timeByFunc != null)
                    loopDuration += task.timeByFunc();

                // Nothing to do, skip
                if (loopDuration == 0)
                    continue;

                // Loops always need a handler
                if (task.handler == null)
                {
                    task.handler = new TeaHandler();
                    task.handler.self = this;
                }

                // Reset handler to defaults
                task.handler.t = 0;
                task.handler.timeSinceStart = 0;

                task.handler.isReversed = isReversed;
                task.handler.isLooping = true;

                // Negative time means the loop is infinite
                bool isInfinite = loopDuration < 0;

                // T quotient
                float tRate = isInfinite ? 0 : 1 / loopDuration;

                // Progresion depends on current direction
                if (task.handler.isReversed)
                {
                    task.handler.t = 1f;
                    tRate = -tRate;
                }

                // While looping and, until time or infinite
                while (task.handler.isLooping && (task.handler.isReversed ? task.handler.t >= 0 : task.handler.t <= 1))
                {
                    // Check for queue reversal
                    if (isReversed != task.handler.isReversed)
                    {
                        tRate = -tRate;
                        task.handler.isReversed = isReversed;
                    }

                    float unityDeltaTime = Time.deltaTime;

                    // Completion % from 0 to 1
                    if (!isInfinite)
                        task.handler.t += tRate * unityDeltaTime;

                    // On finite loops this .deltaTime is sincronized with the
                    // exact loop duration
                    task.handler.deltaTime =
                        isInfinite ?
                        unityDeltaTime :
                        1 / (loopDuration - task.handler.timeSinceStart) * unityDeltaTime;

                    // .deltaTime is also reversed
                    if (task.handler.isReversed)
                        task.handler.deltaTime = -task.handler.deltaTime;

                    // A classic
                    task.handler.timeSinceStart += unityDeltaTime;

                    // Pause?
                    while (isPaused)
                        yield return null;

                    // Loops will always have a callback with a handler
                    task.callbackWithHandler(task.handler);

                    // Handler .WaitFor(
                    if (task.handler.yields != null)
                    {
                        for (int i = 0, len = task.handler.yields.Count; i < len; i++)
                            yield return task.handler.yields[i];

                        task.handler.yields.Clear();
                    }
                    // Minimum sane delay
                    else yield return null;
                }

                // Executed +1
                executedCount += 1;
                lastPlayExecutedCount += 1;
            }
            // It's a timed callback
            else
            {
                // Holds the delay
                float delayDuration = task.time;

                // Func<float> added
                if (task.timeByFunc != null)
                    delayDuration += task.timeByFunc();

                // Time delay
                if (delayDuration > 0)
                    yield return TeaYield.WaitForSeconds(delayDuration);

                // Is this more precise that the previous code?
                // float time = 0;
                // while (time < delayDuration)
                // {
                //     time += Time.deltaTime;
                //     yield return null;
                // }

                // Pause?
                while (isPaused)
                    yield return null;

                // Normal callback
                if (task.callback != null)
                    task.callback();

                // Callback with handler
                if (task.callbackWithHandler != null)
                {
                    if (task.handler == null)
                    {
                        task.handler = new TeaHandler();
                        task.handler.self = this;
                    }

                    task.handler.t = 1;
                    task.handler.deltaTime = Time.deltaTime;
                    task.handler.timeSinceStart = delayDuration;

                    task.callbackWithHandler(task.handler);

                    // Handler WaitFor
                    if (task.handler.yields != null)
                    {
                        for (int i = 0, len = task.handler.yields.Count; i < len; i++)
                            yield return task.handler.yields[i];

                        task.handler.yields.Clear();
                    }

                    // Minimum sane delay
                    if (delayDuration <= 0 && task.handler.yields == null)
                        yield return null;
                }
                else if (delayDuration <= 0)
                    yield return null;

                // Executed +1
                executedCount += 1;
                lastPlayExecutedCount += 1;
            }

            // Just at the end of a complete queue execution
            if (tasks.Count > 0 && taskIndex >= tasks.Count)
            {
                // Forget current nested queues
                waiting.Clear();
            }

            // Consume mode removes the task after execution
            // @todo Need to be tested with .Reverse() stuff
            if (isConsuming)
            {
                taskIndex -= 1;
                tasks.Remove(task);

                reverseLastTask = -1; // To default
            }

            // On Yoyo mode the queue is reversed at the end, only once per play
            // without Repeat mode
            if (isYoyo && taskIndex >= tasks.Count && (lastPlayExecutedCount <= tasks.Count || isRepeating))
            {
                Reverse();

                reverseLastTask = -1; // To default
            }

            // Repeats on Repeat mode
            if (isRepeating && tasks.Count > 0 && taskIndex >= tasks.Count)
            {
                taskIndex = 0;

                reverseLastTask = -1; // To default
            }
        }

        // Done!
        isPlaying = false;
    }
}

// <3
// Lerp Formulas

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

// Created       2014/12/26 12:21 am
// Rewritten     2015/09/15 12:28 pm
// Last revision 2021/02/16 11.53 pm, 2021/09/26 01:37 am