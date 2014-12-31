// TeaTimer v0.4 Alpha

// By Andrés Villalobos > andresalvivar@gmail.com > twitter.com/matnesis
// In collaboration with Antonio Zamora > tzamora@gmail.com > twitter.com/tzamora
// Created 2014/12/26 12:21 am

// TeaTimer is a fast & simple queue for timed callbacks, designed as a
// MonoBehaviour extension set, focused on solving common coroutines patterns.

// Just put 'TeaTimer.cs' somewhere in your folders and call it inside any
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

// Some details
// - Execution starts inmediatly
// - Locking a queue ensures a safe run during continuous calls
// - Naming a queue is recommended, but optional
// - You can use a YieldInstruction instead of time in ttAppend (Dotween!)
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
    public YieldInstruction y = null;
    public Action callback = null;
    public bool isLoop = false;
    public Action<LoopHandler> loopCallback = null;


    public TeaTask(float time, YieldInstruction y, Action callback)
    {
        this.time = time;
        this.y = y;
        this.callback = callback;
        this.isLoop = false;
    }


    public TeaTask(float duration, Action<LoopHandler> callback)
    {
        this.time = duration;
        this.y = null;
        this.isLoop = true;
        this.loopCallback = callback;
    }
}


/// <summary>
/// Handler for appended loops.
/// </summary>
public class LoopHandler
{
    public float t = 0f;
    public float timeSinceStart = 0f;
    public bool exitLoop = false;


    public void Exit()
    {
        exitLoop = true;
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

        // Is locked?
        if (lockedQueue[instance].Contains(queueName))
            return true;

        return false;
    }


    /// <summary>
    /// Appends a timed callback into a queue.
    /// </summary>
    private static MonoBehaviour ttAppend(this MonoBehaviour instance, string queueName, float timeDelay, YieldInstruction yieldDelay, Action callback)
    {
        // Ignore locked queues
        if (IsLocked(instance, queueName))
            return instance;
        else
            Debug.Log(queueName);

        PrepareInstanceQueue(instance);
        PrepareInstanceLastQueueName(instance);

        // Adds callback list & last queue name 
        lastQueueName[instance] = queueName;
        if (queue[instance].ContainsKey(queueName) == false)
            queue[instance].Add(queueName, new List<TeaTask>());

        // Appends a new task
        List<TeaTask> taskList = queue[instance][queueName];
        taskList.Add(new TeaTask(timeDelay, yieldDelay, callback));

        // Execute queue
        instance.StartCoroutine(ExecuteQueue(instance, queueName));

        return instance;
    }


    /// <summary>
    /// Appends a callback that runs frame by frame for his duration into a queue.
    /// </summary>
    public static MonoBehaviour ttAppendLoop(this MonoBehaviour instance, string queueName, float duration, Action<LoopHandler> callback)
    {
        // Ignore locked queues
        if (IsLocked(instance, queueName))
            return instance;
        else
            Debug.Log(queueName);

        PrepareInstanceQueue(instance);
        PrepareInstanceLastQueueName(instance);

        // Adds callback list & last queue name 
        lastQueueName[instance] = queueName;
        if (queue[instance].ContainsKey(queueName) == false)
            queue[instance].Add(queueName, new List<TeaTask>());

        // Append a new task
        List<TeaTask> taskList = queue[instance][queueName];
        taskList.Add(new TeaTask(duration, callback));

        // Execute queue
        instance.StartCoroutine(ExecuteQueue(instance, queueName));

        return instance;
    }


    /// <summary>
    /// Appends a timed callback into a queue.
    /// </summary>
    public static MonoBehaviour ttAppend(this MonoBehaviour instance, string queueName, float timeDelay, Action callback)
    {
        return instance.ttAppend(queueName, timeDelay, null, callback);
    }


    /// <summary>
    /// Appends a timed callback into a queue.
    /// </summary>
    public static MonoBehaviour ttAppend(this MonoBehaviour instance, string queueName, YieldInstruction yieldToWait, Action callback)
    {
        return instance.ttAppend(queueName, 0, yieldToWait, callback);
    }


    /// <summary>
    /// Appends a timed callback into the last used queue (or default).
    /// </summary>
    public static MonoBehaviour ttAppend(this MonoBehaviour instance, float timeDelay, Action callback)
    {
        PrepareInstanceLastQueueName(instance);

        return instance.ttAppend(lastQueueName[instance], timeDelay, null, callback);
    }


    /// <summary>
    /// Appends a timed callback into the last used queue (or default).
    /// </summary>
    public static MonoBehaviour ttAppend(this MonoBehaviour instance, YieldInstruction yieldToWait, Action callback)
    {
        PrepareInstanceLastQueueName(instance);

        return instance.ttAppend(lastQueueName[instance], 0, yieldToWait, callback);
    }


    /// <summary>
    /// Appends a timed interval into a queue.
    /// </summary>
    public static MonoBehaviour ttAppend(this MonoBehaviour instance, string queueName, float duration)
    {
        return instance.ttAppend(queueName, duration, null, null);
    }


    /// <summary>
    /// Appends a timed interval into the last used queue (or default).
    /// </summary>
    public static MonoBehaviour ttAppend(this MonoBehaviour instance, float duration)
    {
        PrepareInstanceLastQueueName(instance);

        return instance.ttAppend(lastQueueName[instance], duration, null, null);
    }


    /// <summary>
    /// Appends a timed callback into a queue.
    /// </summary>
    public static MonoBehaviour ttAppend(this MonoBehaviour instance, string queueName, Action callback)
    {
        return instance.ttAppend(queueName, 0, null, callback);
    }


    /// <summary>
    /// Appends a timed callback into the last used queue (or default).
    /// </summary>
    public static MonoBehaviour ttAppend(this MonoBehaviour instance, Action callback)
    {
        PrepareInstanceLastQueueName(instance);

        return instance.ttAppend(lastQueueName[instance], 0, null, callback);
    }


    /// <summary>
    /// Appends a callback that runs frame by frame (until his exit is forced) into a queue.
    /// </summary>
    public static MonoBehaviour ttAppendLoop(this MonoBehaviour instance, string queueName, Action<LoopHandler> callback)
    {
        return instance.ttAppendLoop(queueName, 0, callback);
    }


    /// <summary>
    /// Appends a callback that runs frame by frame for his duration into the last used queue.
    /// </summary>
    public static MonoBehaviour ttAppendLoop(this MonoBehaviour instance, float duration, Action<LoopHandler> callback)
    {
        PrepareInstanceLastQueueName(instance);

        return instance.ttAppendLoop(lastQueueName[instance], duration, callback);
    }


    /// <summary>
    /// Appends a callback that runs frame by frame (until his exit is forced) into the last used queue.
    /// </summary>
    public static MonoBehaviour ttAppendLoop(this MonoBehaviour instance, Action<LoopHandler> callback)
    {
        PrepareInstanceLastQueueName(instance);

        return instance.ttAppendLoop(lastQueueName[instance], 0, callback);
    }


    /// <summary>
    /// Executes a timed callback ignoring queues.
    /// </summary>
    private static MonoBehaviour ttInvoke(this MonoBehaviour instance, float timeDelay, YieldInstruction yieldToWait, Action callback)
    {
        instance.StartCoroutine(ExecuteOnce(timeDelay, yieldToWait, callback));

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
            queue[instance][lastQueueName[instance]].Count <= 0)
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
                    yield return instance.StartCoroutine(ExecuteLoop(c.time, c.loopCallback));
                }
                else
                {
                    yield return instance.StartCoroutine(ExecuteInfiniteLoop(c.loopCallback));
                }
            }
            else
            {
                yield return instance.StartCoroutine(ExecuteOnce(c.time, c.y, c.callback));
            }

            queue[instance][queueName].Remove(c);
        }

        // Unlocks the queue
        currentlyRunning[instance].Remove(queueName);

        // Try again is there are new items,
        // else, remove the lock
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
    private static IEnumerator ExecuteOnce(float timeToWait, YieldInstruction yieldToWait, Action callback)
    {
        // Wait until
        if (timeToWait > 0)
            yield return new WaitForSeconds(timeToWait);

        if (yieldToWait != null)
            yield return yieldToWait;

        // Task
        if (callback != null)
            callback();
    }


    /// <summary>
    /// Executes a callback inside a loop during time.
    /// </summary>
    private static IEnumerator ExecuteLoop(float time, Action<LoopHandler> callback)
    {
        float t = 0f;

        float rate = 0;

        float timeSinceStart = 0f;

        // if time is 0 the execute loop is infinite

        if (time > 0)
        {
            rate = 1f / time;
        }

        LoopHandler loopHandler = new LoopHandler();

        while (t < 1)
        {
            if (loopHandler.exitLoop)
            {
                break;
            }

            timeSinceStart += Time.deltaTime;

            if (time > 0)
            {
                t += Time.deltaTime * rate;

                loopHandler.t = t;
            }
            else
            {
                loopHandler.t = timeSinceStart;
            }

            // t will return the delta value of the linear interpolation based in the duration time
            // but if there is no duration the t value sent will be the time since start

            if (callback != null)
            {
                callback(loopHandler);
            }

            yield return null;
        }
    }


    /// <summary>
    /// Executes a callback inside an infinite loop.
    /// </summary>
    private static IEnumerator ExecuteInfiniteLoop(Action<LoopHandler> callback)
    {
        float timeSinceStart = 0;

        LoopHandler loopHandler = new LoopHandler();

        while (true)
        {
            if (loopHandler.exitLoop)
            {
                break;
            }

            timeSinceStart += Time.deltaTime;

            loopHandler.timeSinceStart = timeSinceStart;

            // t will return the delta value of the linear interpolation based in the duration time
            // but if there is no duration the t value sent will be the time since start

            if (callback != null)
            {
                callback(loopHandler);
            }

            yield return null;
        }
    }
}