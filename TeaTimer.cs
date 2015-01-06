// TeaTimer v0.5 alpha

// By Andrés Villalobos > andresalvivar@gmail.com > twitter.com/matnesis
// In collaboration with Antonio Zamora > tzamora@gmail.com > twitter.com/tzamora
// Created 2014/12/26 12:21 am

// TeaTimer is a fast & simple queue for timed callbacks, fashioned as a
// MonoBehaviour extension set, focused on solving common coroutines patterns in
// Unity games.

// Just put 'TeaTimer.cs' somewhere in your project and call it inside any
// MonoBehaviour using 'this.tt'.

// this.ttAppend("Queue name", 2, () =>
// {
//     Debug.Log("2 second since start " + Time.time);
// })
// .ttAppendLoop(3, delegate(LoopHandler loop)
// {
//     // An append loop will run frame by frame for all his duration.
//     // loop.t holds the fraction of time by frame.
//     Camera.main.backgroundColor = Color.Lerp(Color.black, Color.white, loop.t);
// })
// .ttAppend(2, () =>
// {
//     Debug.Log("The append loop started 5 seconds ago  " + Time.time);
// })
// .ttInvoke(1, () =>
// {
//     Debug.Log("ttInvoke is arbitrary and ignores the queue " + Time.time);
// })
// .ttLock(); // Locks the queue, ignoring new appends until all callbacks are done.

// And that's it!

// Some important details:
// - Execution starts immediately
// - Locking a queue ensures a safe run during continuous calls
// - Naming a queue is highly recommended (but optional)
// - You can use a YieldInstruction instead of time in ttAppend (Dotween!)
// - ttHandler adds special control features to your callbacks
// - Queues are unique to his MonoBehaviour


using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Timed callback data.
/// </summary>
public class TeaTask
{
    public float time = 0f;
    public YieldInstruction yieldInstruction = null;
    public Action callback = null;
    public Action<ttHandler> callbackWithHandler = null;
    public bool isLoop = false;


    public TeaTask(float time, YieldInstruction yield, Action callback, Action<ttHandler> callbackWithHandler, bool isLoop)
    {
        this.time = time;
        this.yieldInstruction = yield;
        this.callback = callback;
        this.callbackWithHandler = callbackWithHandler;
        this.isLoop = isLoop;
    }
}


/// <summary>
/// TeaTimer Handler.
/// </summary>
public class ttHandler
{
    public float t = 0f;
    public float timeSinceStart = 0f;
    public bool isBroken = false;
    public YieldInstruction yieldToWait = null;


    /// <summary>
    /// Breaks the current AppendLoop.
    /// </summary>
    public void Break()
    {
        this.isBroken = true;
    }


    /// <summary>
    /// Waits for a time interval after the current callback.
    /// </summary>
    public void WaitFor(float interval)
    {
        this.yieldToWait = new WaitForSeconds(interval);
    }


    /// <summary>
    /// Waits for a YieldInstruction after the current callback.
    /// </summary>
    public void WaitFor(YieldInstruction yieldToWait)
    {
        this.yieldToWait = yieldToWait;
    }
}


/// <summary>
/// TeaTimer is a fast & simple queue for timed callbacks, designed as a
/// MonoBehaviour extension set, focused on solving common coroutines patterns.
/// </summary>
public static class TeaTimer
{
    /// <summary>
    /// Main queue for all the timed callbacks.
    /// </summary>
    private static Dictionary<MonoBehaviour, Dictionary<string, List<TeaTask>>> queue;

    /// <summary>
    /// Holds the currently running queues.
    /// </summary>
    private static Dictionary<MonoBehaviour, List<string>> currentlyRunning;

    /// <summary>
    /// Holds the last queue name used.
    /// </summary>
    private static Dictionary<MonoBehaviour, string> lastQueueName;

    /// <summary>
    /// Holds the locked queues.
    /// </summary>
    private static Dictionary<MonoBehaviour, List<string>> lockedQueue;


    /// <summary>
    /// Prepares the main queue for the instance.
    /// </summary>
    private static void PrepareInstanceQueue(MonoBehaviour instance)
    {
        if (queue == null)
            queue = new Dictionary<MonoBehaviour, Dictionary<string, List<TeaTask>>>();

        if (queue.ContainsKey(instance) == false)
            queue.Add(instance, new Dictionary<string, List<TeaTask>>());
    }


    /// <summary>
    /// Prepares the last queue name for the instance.
    /// </summary>
    private static void PrepareInstanceLastQueueName(MonoBehaviour instance)
    {
        if (lastQueueName == null)
            lastQueueName = new Dictionary<MonoBehaviour, string>();

        // Default name
        if (lastQueueName.ContainsKey(instance) == false)
            lastQueueName[instance] = "TEATIMER_DEFAULT_QUEUE_NAME";
    }


    /// <summary>
    /// Prepares the locked queue for the instance.
    /// </summary>
    private static void PrepareInstanceLockedQueue(MonoBehaviour instance)
    {
        if (lockedQueue == null)
            lockedQueue = new Dictionary<MonoBehaviour, List<string>>();

        if (lockedQueue.ContainsKey(instance) == false)
            lockedQueue.Add(instance, new List<string>());
    }


    /// <summary>
    /// Returns true if the queue is currently locked.
    /// </summary>
    private static bool IsLocked(MonoBehaviour instance, string queueName)
    {
        PrepareInstanceLockedQueue(instance);

        // It is?
        if (lockedQueue[instance].Contains(queueName))
            return true;

        return false;
    }


    /// <summary>
    ///// Appends a callback (timed or looped) into a queue.
    /// </summary>
    private static MonoBehaviour ttAppend(this MonoBehaviour instance, string queueName, float timeDelay, YieldInstruction yieldDelay,
        Action callback, Action<ttHandler> callbackWithHandler, bool isLoop)
    {
        // Ignore locked queues
        if (IsLocked(instance, queueName))
        {
            return instance;
        }
        //else
        //{
        //    if (isLoop)
        //    {
        //        Debug.Log("Queue < ttAppendLoop " + queueName);
        //    }
        //    else
        //    {
        //        Debug.Log("Queue < ttAppend " + queueName);
        //    }
        //}

        PrepareInstanceQueue(instance);
        PrepareInstanceLastQueueName(instance);

        // Adds callback list & last queue name 
        lastQueueName[instance] = queueName;
        if (queue[instance].ContainsKey(queueName) == false)
            queue[instance].Add(queueName, new List<TeaTask>());

        // Appends a new task
        List<TeaTask> taskList = queue[instance][queueName];
        taskList.Add(new TeaTask(timeDelay, yieldDelay, callback, callbackWithHandler, isLoop));

        // Execute queue
        instance.StartCoroutine(ExecuteQueue(instance, queueName));

        return instance;
    }


    /// <summary>
    /// Appends a timed callback into a queue.
    /// </summary>
    public static MonoBehaviour ttAppend(this MonoBehaviour instance, string queueName, float timeDelay, Action callback)
    {
        return instance.ttAppend(queueName, timeDelay, null, callback, null, false);
    }


    /// <summary>
    /// Appends a timed callback into a queue.
    /// </summary>
    public static MonoBehaviour ttAppend(this MonoBehaviour instance, string queueName, float timeDelay, Action<ttHandler> callback)
    {
        return instance.ttAppend(queueName, timeDelay, null, null, callback, false);
    }


    /// <summary>
    /// Appends a timed callback into a queue.
    /// </summary>
    public static MonoBehaviour ttAppend(this MonoBehaviour instance, string queueName, YieldInstruction yieldToWait, Action callback)
    {
        return instance.ttAppend(queueName, 0, yieldToWait, callback, null, false);
    }


    /// <summary>
    /// Appends a timed callback into a queue.
    /// </summary>
    public static MonoBehaviour ttAppend(this MonoBehaviour instance, string queueName, YieldInstruction yieldToWait, Action<ttHandler> callback)
    {
        return instance.ttAppend(queueName, 0, yieldToWait, null, callback, false);
    }


    /// <summary>
    /// Appends a timed callback into the last used queue (or default).
    /// </summary>
    public static MonoBehaviour ttAppend(this MonoBehaviour instance, float timeDelay, Action callback)
    {
        PrepareInstanceLastQueueName(instance);

        return instance.ttAppend(lastQueueName[instance], timeDelay, null, callback, null, false);
    }


    /// <summary>
    /// Appends a timed callback into the last used queue (or default).
    /// </summary>
    public static MonoBehaviour ttAppend(this MonoBehaviour instance, float timeDelay, Action<ttHandler> callback)
    {
        PrepareInstanceLastQueueName(instance);

        return instance.ttAppend(lastQueueName[instance], timeDelay, null, null, callback, false);
    }


    /// <summary>
    /// Appends a timed callback into the last used queue (or default).
    /// </summary>
    public static MonoBehaviour ttAppend(this MonoBehaviour instance, YieldInstruction yieldToWait, Action callback)
    {
        PrepareInstanceLastQueueName(instance);

        return instance.ttAppend(lastQueueName[instance], 0, yieldToWait, callback, null, false);
    }


    /// <summary>
    /// Appends a timed callback into the last used queue (or default).
    /// </summary>
    public static MonoBehaviour ttAppend(this MonoBehaviour instance, YieldInstruction yieldToWait, Action<ttHandler> callback)
    {
        PrepareInstanceLastQueueName(instance);

        return instance.ttAppend(lastQueueName[instance], 0, yieldToWait, null, callback, false);
    }


    /// <summary>
    /// Appends a time interval into a queue.
    /// </summary>
    public static MonoBehaviour ttAppend(this MonoBehaviour instance, string queueName, float interval)
    {
        return instance.ttAppend(queueName, interval, null, null, null, false);
    }


    /// <summary>
    /// Appends a time interval into the last used queue (or default).
    /// </summary>
    public static MonoBehaviour ttAppend(this MonoBehaviour instance, float interval)
    {
        PrepareInstanceLastQueueName(instance);

        return instance.ttAppend(lastQueueName[instance], interval, null, null, null, false);
    }


    /// <summary>
    /// Appends a timed callback into a queue.
    /// </summary>
    public static MonoBehaviour ttAppend(this MonoBehaviour instance, string queueName, Action callback)
    {
        return instance.ttAppend(queueName, 0, null, callback, null, false);
    }


    /// <summary>
    /// Appends a timed callback into a queue.
    /// </summary>
    public static MonoBehaviour ttAppend(this MonoBehaviour instance, string queueName, Action<ttHandler> callback)
    {
        return instance.ttAppend(queueName, 0, null, null, callback, false);
    }


    /// <summary>
    /// Appends a timed callback into the last used queue (or default).
    /// </summary>
    public static MonoBehaviour ttAppend(this MonoBehaviour instance, Action callback)
    {
        PrepareInstanceLastQueueName(instance);

        return instance.ttAppend(lastQueueName[instance], 0, null, callback, null, false);
    }


    /// <summary>
    /// Appends a timed callback into the last used queue (or default).
    /// </summary>
    public static MonoBehaviour ttAppend(this MonoBehaviour instance, Action<ttHandler> callback)
    {
        PrepareInstanceLastQueueName(instance);

        return instance.ttAppend(lastQueueName[instance], 0, null, null, callback, false);
    }


    /// <summary>
    /// Appends a callback that runs frame by frame (until his exit is forced) into a queue.
    /// </summary>
    public static MonoBehaviour ttAppendLoop(this MonoBehaviour instance, string queueName, float duration, Action<ttHandler> callback)
    {
        return instance.ttAppend(queueName, duration, null, null, callback, true);
    }


    /// <summary>
    /// Appends a callback that runs frame by frame (until his exit is forced) into a queue.
    /// </summary>
    public static MonoBehaviour ttAppendLoop(this MonoBehaviour instance, string queueName, Action<ttHandler> callback)
    {
        return instance.ttAppend(queueName, 0, null, null, callback, true);
    }


    /// <summary>
    /// Appends a callback that runs frame by frame for his duration into the last used queue.
    /// </summary>
    public static MonoBehaviour ttAppendLoop(this MonoBehaviour instance, float duration, Action<ttHandler> callback)
    {
        PrepareInstanceLastQueueName(instance);

        return instance.ttAppend(lastQueueName[instance], duration, null, null, callback, true);
    }


    /// <summary>
    /// Appends a callback that runs frame by frame (until his exit is forced) into the last used queue.
    /// </summary>
    public static MonoBehaviour ttAppendLoop(this MonoBehaviour instance, Action<ttHandler> callback)
    {
        PrepareInstanceLastQueueName(instance);

        return instance.ttAppend(lastQueueName[instance], 0, null, null, callback, true);
    }


    /// <summary>
    /// Executes a timed callback ignoring queues.
    /// </summary>
    private static MonoBehaviour ttInvoke(this MonoBehaviour instance, float timeDelay, YieldInstruction yieldToWait, Action callback)
    {
        instance.StartCoroutine(ExecuteOnce(timeDelay, yieldToWait, callback, null));

        return instance;
    }


    /// <summary>
    /// Executes a timed callback ignoring queues.
    /// </summary>
    public static MonoBehaviour ttInvoke(this MonoBehaviour instance, float timeDelay, Action callback)
    {
        return instance.ttInvoke(timeDelay, null, callback);
    }


    /// <summary>
    /// Executes a timed callback ignoring queues.
    /// </summary>
    public static MonoBehaviour ttInvoke(this MonoBehaviour instance, YieldInstruction yieldToWait, Action callback)
    {
        return instance.ttInvoke(0, yieldToWait, callback);
    }


    /// <summary>
    /// Locks the current queue (no more appends) until all his callbacks are done.
    /// </summary>
    public static MonoBehaviour ttLock(this MonoBehaviour instance)
    {
        PrepareInstanceQueue(instance);
        PrepareInstanceLastQueueName(instance);

        // Ignore if the queue is empty
        if (queue[instance].ContainsKey(lastQueueName[instance]) == false ||
            queue[instance][lastQueueName[instance]].Count < 1)
            return instance;

        // Adds the lock
        if (IsLocked(instance, lastQueueName[instance]) == false)
            lockedQueue[instance].Add(lastQueueName[instance]);

        return instance;
    }


    /// <summary>
    /// Execute all timed callbacks and loops for the instance queue.
    /// </summary>
    private static IEnumerator ExecuteQueue(MonoBehaviour instance, string queueName)
    {
        // Ignore if empty
        if (queue.ContainsKey(instance) == false)
            yield break;

        if (queue[instance].ContainsKey(queueName) == false)
            yield break;

        // Create a runner list for the instance
        if (currentlyRunning == null)
            currentlyRunning = new Dictionary<MonoBehaviour, List<string>>();

        if (currentlyRunning.ContainsKey(instance) == false)
            currentlyRunning.Add(instance, new List<string>());

        // Ignore if already running
        if (currentlyRunning.ContainsKey(instance) && currentlyRunning[instance].Contains(queueName))
            yield break;

        // Locks the queue
        currentlyRunning[instance].Add(queueName);

        // Run until depleted (over a clone)
        List<TeaTask> batch = new List<TeaTask>();
        batch.AddRange(queue[instance][queueName]);

        foreach (TeaTask c in batch)
        {
            // Select, execute & remove tasks
            if (c.isLoop)
            {
                if (c.time > 0)
                {
                    yield return instance.StartCoroutine(ExecuteLoop(c.time, c.callbackWithHandler));
                }
                else
                {
                    yield return instance.StartCoroutine(ExecuteInfiniteLoop(c.callbackWithHandler));
                }
            }
            else
            {
                yield return instance.StartCoroutine(ExecuteOnce(c.time, c.yieldInstruction, c.callback, c.callbackWithHandler));
            }

            queue[instance][queueName].Remove(c);
        }

        // Unlocks the queue
        currentlyRunning[instance].Remove(queueName);

        // Try again is there are new items, else, remove the lock
        if (queue[instance][queueName].Count > 0)
        {
            instance.StartCoroutine(ExecuteQueue(instance, queueName));
        }
        else
        {
            if (IsLocked(instance, queueName))
                lockedQueue[instance].Remove(queueName);
        }
    }


    /// <summary>
    /// Executes a timed callback.
    /// </summary>
    private static IEnumerator ExecuteOnce(float timeToWait, YieldInstruction yieldToWait,
        Action callback, Action<ttHandler> callbackWithHandler)
    {
        // The minimun safe time to wait
        // #bug To avoid an Append faster than his previous AppendLoop.
        if (timeToWait < Time.deltaTime)
            yield return null;

        // Wait until
        if (timeToWait > 0)
            yield return new WaitForSeconds(timeToWait);

        if (yieldToWait != null)
            yield return yieldToWait;

        // Executes the normal handler
        if (callback != null)
            callback();

        // Executes the callback with handler (and waits his yield)
        if (callbackWithHandler != null)
        {
            ttHandler t = new ttHandler();
            callbackWithHandler(t);

            if (t.yieldToWait != null)
                yield return t.yieldToWait;
        }

        yield return null;
    }


    /// <summary>
    /// Executes a callback inside a loop until time.
    /// </summary>
    private static IEnumerator ExecuteLoop(float time, Action<ttHandler> callback)
    {
        float t = 0f;
        float rate = 0f;

        // Only for positive values
        if (time > 0)
        {
            rate = 1f / time;
        }
        else
        {
            yield break;
        }

        ttHandler loopHandler = new ttHandler();

        // Run until t is 1 again
        while (t < 1)
        {
            if (loopHandler.isBroken)
                break;

            // t will return the delta value of the linear interpolation based in the duration time
            // but if there is no duration the t value sent will be the time since start
            t += Time.deltaTime * rate;
            loopHandler.t = t;
            loopHandler.timeSinceStart += Time.deltaTime;

            // Execute
            if (callback != null)
                callback(loopHandler);

            // Yields once and resets
            if (loopHandler.yieldToWait != null)
            {
                yield return loopHandler.yieldToWait;
                loopHandler.yieldToWait = null;
            }

            yield return null;
        }
    }


    /// <summary>
    /// Executes a callback inside an infinite loop.
    /// </summary>
    private static IEnumerator ExecuteInfiniteLoop(Action<ttHandler> callback)
    {
        ttHandler loopHandler = new ttHandler();

        // Infinite
        while (true)
        {
            if (loopHandler.isBroken)
                break;

            //loopHandler.t = Time.deltaTime;
            loopHandler.timeSinceStart += Time.deltaTime;

            // Execute
            if (callback != null)
                callback(loopHandler);

            // Yields once and resets
            if (loopHandler.yieldToWait != null)
            {
                yield return loopHandler.yieldToWait;
                loopHandler.yieldToWait = null;
            }

            yield return null;
        }
    }
}