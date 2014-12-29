// TeaTimer v0.3
// Unity Extension Set for a quick coroutine/callback timer in MonoBehaviours.

// #author Andrés Villalobos
// #contact andresalvivar@gmail.com - twitter.com/matnesis
// #created 2014/12/26 12:21 am

// #usage Just put TeaTimer.cs somewhere in your folders and call it in a MonoBehaviour 
// using 'this'.

// ttAppend
// Appends a timed callback into a queue to be executed in order.
// this.ttAppend("SomeQueue", 1, () => Debug.Log("SomeQueue " + Time.time)); // Prints 1
// this.ttAppend("SomeQueue", 2, () => Debug.Log("SomeQueue " + Time.time)); // Prints 3

// When called without a queue name, 
// the task is appended to the last named queue (or default).
// this.ttAppend(2, () => Debug.Log("SomeQueue " + Time.time));  // Prints 5

// ttLock
// Locks the current queue until all his previous callbacks are done.
// Useful during cycles (e.g. Update) to avoid over appending callbacks.
// void Update() 
// {
//     this.ttAppend("LockedQueue", 3, () => Debug.Log("LockedQueue " + Time.time));
//     this.ttAppend("LockedQueue", 3, () => Debug.Log("LockedQueue " + Time.time));
//     this.ttLock(); // 'LockedQueue' will run as a safe permanent timer.
// }

// ttNow
// Executes a timed callback ignoring queues.
// this.ttNow(3, () => Debug.Log("ttNow " + Time.time)); // Prints 3

// Some details
// #1 Execution starts inmediatly
// #2 You can chain methods
// #3 You can use a YieldInstruction instead of time (e.g Dotween!)
// #4 Locking a queue ensures a safe timer during continuous calls
// #5 Queues are unique to his MonoBehaviour
// #6 If you never name a queue, an internal default will be used

// A classic chain example.
// this.ttAppend("AnotherQueue", 1, () =>
// {
//     Debug.Log("AnotherQueue " + Time.time); // Prints 1
// })
// .ttNow(1, () =>
// {
//     Debug.Log("AnotherQueue ttNow ignores queues " + Time.time); // Prints 1
// })
// .ttAppend(0, () =>
// {
//     Debug.Log("AnotherQueue " + Time.time); // Prints 1
// })
// .ttAppend(transform.DOMoveX(1, 3).WaitForCompletion(), () => // Dotween WaitFor instead of time
// {
//     Debug.Log("AnotherQueue YieldInstruction " + Time.time); // Prints 3
// })

// append loop

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Timed task data.
/// </summary>
public class TeaTask
{
    public float atTime = 0;
    public YieldInstruction atYield = null;
    public Action callback = null;


    public TeaTask(float atTime, YieldInstruction atYield, Action callback)
    {
        this.atTime = atTime;
        this.atYield = atYield;
        this.callback = callback;
    }
}


/// <summary>
// Unity Extension Set for a quick coroutine/callback timer in MonoBehaviours.
/// </summary>
public static class TeaTimer
{
    /// <summary>
    /// Main queue for all the timed callbacks.
    /// </summary>
    private static Dictionary<MonoBehaviour, Dictionary<string, List<TeaTask>>> queue;

    /// <summary>
    /// Holds the running queues by instance.
    /// </summary>
    private static Dictionary<MonoBehaviour, List<string>> currentlyRunning;

    /// <summary>
    /// Holds the last queue name used by instance.
    /// </summary>
    private static Dictionary<MonoBehaviour, string> lastQueueName;

    /// <summary>
    /// Holds the locked queues by instance.
    /// </summary>
    private static Dictionary<MonoBehaviour, List<string>> lockedQueue;


    /// <summary>
    /// Prepares the main queue for the instance.
    /// </summary>
    private static void InitInstanceQueue(MonoBehaviour instance)
    {
        if (queue == null)
            queue = new Dictionary<MonoBehaviour, Dictionary<string, List<TeaTask>>>();

        if (queue.ContainsKey(instance) == false)
            queue.Add(instance, new Dictionary<string, List<TeaTask>>());
    }


    /// <summary>
    /// Prepares the last queue name dictionary for the instance.
    /// </summary>
    private static void InitInstanceLastQueueName(MonoBehaviour instance)
    {
        if (lastQueueName == null)
            lastQueueName = new Dictionary<MonoBehaviour, string>();

        // Default name
        if (lastQueueName.ContainsKey(instance) == false)
            lastQueueName[instance] = "TEATIMER_DEFAULT_QUEUE_NAME";
    }


    /// <summary>
    /// Prepares the locked queue dictionary for the instance.
    /// </summary>
    private static void InitInstanceLockedQueue(MonoBehaviour instance)
    {
        if (lockedQueue == null)
            lockedQueue = new Dictionary<MonoBehaviour, List<string>>();

        if (lockedQueue.ContainsKey(instance) == false)
            lockedQueue.Add(instance, new List<string>());
    }


    /// <summary>
    /// Returns true if the queue is locked in the instance.
    /// </summary>
    /// <returns></returns>
    private static bool IsLocked(MonoBehaviour instance, string queueName)
    {
        InitInstanceLockedQueue(instance);

        // Is locked?
        if (lockedQueue[instance].Contains(queueName))
            return true;

        return false;
    }


    /// <summary>
    /// Appends a timed task into a default queue to be executed in order.
    /// </summary>
    private static MonoBehaviour ttAppend(this MonoBehaviour instance, string queueName, float atTime, YieldInstruction atYield, Action callback)
    {
        // Ignore locked queues
        if (IsLocked(instance, queueName))
            return instance;

        InitInstanceQueue(instance);
        InitInstanceLastQueueName(instance);

        // Adds callback list & last queue name 
        lastQueueName[instance] = queueName;
        if (queue[instance].ContainsKey(queueName) == false)
            queue[instance].Add(queueName, new List<TeaTask>());

        // Append a new task
        List<TeaTask> taskList = queue[instance][queueName];
        taskList.Add(new TeaTask(atTime, atYield, callback));

        // Execute queue
        instance.StartCoroutine(ExecuteQueue(instance, queueName));

        return instance;
    }


    /// <summary>
    /// Appends a timed task into a default queue to be executed in order.
    /// </summary>
    public static MonoBehaviour ttAppend(this MonoBehaviour instance, string queueName, float atTime, Action callback)
    {
        return instance.ttAppend(queueName.Trim(), atTime, null, callback);
    }


    /// <summary>
    /// Appends a timed task into a default queue to be executed in order.
    /// </summary>
    public static MonoBehaviour ttAppend(this MonoBehaviour instance, string queueName, YieldInstruction atYield, Action callback)
    {
        return instance.ttAppend(queueName.Trim(), 0, atYield, callback);
    }


    /// <summary>
    /// Appends a timed task into a default queue to be executed in order.
    /// </summary>
    public static MonoBehaviour ttAppend(this MonoBehaviour instance, float atTime, Action callback)
    {
        InitInstanceLastQueueName(instance);

        return instance.ttAppend(lastQueueName[instance], atTime, null, callback);
    }


    /// <summary>
    /// Appends a timed task into a default queue to be executed in order.
    /// </summary>
    public static MonoBehaviour ttAppend(this MonoBehaviour instance, YieldInstruction atYield, Action callback)
    {
        InitInstanceLastQueueName(instance);

        return instance.ttAppend(lastQueueName[instance], 0, atYield, callback);
    }


    /// <summary>
    /// Appends a timed task into a default queue to be executed in order.
    /// </summary>
    public static MonoBehaviour ttAppend(this MonoBehaviour instance, float atTime)
    {
        InitInstanceLastQueueName(instance);

        return instance.ttAppend(lastQueueName[instance], atTime, null, null);
    }


    /// <summary>
    /// Appends a timed task into a default queue to be executed in order.
    /// </summary>
    public static MonoBehaviour ttAppend(this MonoBehaviour instance, Action callback)
    {
        InitInstanceLastQueueName(instance);

        return instance.ttAppend(lastQueueName[instance], 0, null, callback);
    }


    /// <summary>
    /// Executes a timed callback ignoring queues.
    /// </summary>
    private static MonoBehaviour ttNow(this MonoBehaviour instance, float atTime, YieldInstruction atYield, Action callback)
    {
        instance.StartCoroutine(ExecuteOnce(atTime, atYield, callback));

        return instance;
    }


    /// <summary>
    /// Executes a timed callback ignoring queues.
    /// </summary>
    public static MonoBehaviour ttNow(this MonoBehaviour instance, float atTime, Action callback)
    {
        return instance.ttNow(atTime, null, callback);
    }


    /// <summary>
    /// Executes a timed callback ignoring queues.
    /// </summary>
    public static MonoBehaviour ttNow(this MonoBehaviour instance, YieldInstruction atYield, Action callback)
    {
        return instance.ttNow(0, atYield, callback);
    }


    /// <summary>
    /// Locks the current queue (it can't be used again until all current task are done).
    /// </summary>
    public static MonoBehaviour ttLock(this MonoBehaviour instance)
    {
        InitInstanceQueue(instance);
        InitInstanceLastQueueName(instance);

        // Ignore if the queue is empty
        if (queue[instance].ContainsKey(lastQueueName[instance]) == false ||
            queue[instance][lastQueueName[instance]].Count <= 0)
            return instance;

        // Adds the lock
        if (!IsLocked(instance, lastQueueName[instance]))
            lockedQueue[instance].Add(lastQueueName[instance]);

        return instance;
    }


    /// <summary>
    /// Execute all callbacks in the instance queue.
    /// </summary>
    private static IEnumerator ExecuteQueue(MonoBehaviour instance, string queueName)
    {
        // Ignore empty task
        if (queue.ContainsKey(instance) == false)
            yield break;

        if (queue[instance].ContainsKey(queueName) == false)
            yield break;

        // Create runners list for the instace
        if (currentlyRunning == null)
            currentlyRunning = new Dictionary<MonoBehaviour, List<string>>();

        if (currentlyRunning.ContainsKey(instance) == false)
            currentlyRunning.Add(instance, new List<string>());

        // Ignore if already running
        if (currentlyRunning.ContainsKey(instance) && currentlyRunning[instance].Contains(queueName))
            yield break;

        // Run a clone list of tasks until depleted
        List<TeaTask> batch = new List<TeaTask>();
        batch.AddRange(queue[instance][queueName]);

        currentlyRunning[instance].Add(queueName);
        foreach (TeaTask c in batch)
        {
            // Execute & remove tasks
            yield return instance.StartCoroutine(ExecuteOnce(c.atTime, c.atYield, c.callback));
            queue[instance][queueName].Remove(c);
        }
        currentlyRunning[instance].Remove(queueName);

        // Try again is there are new items,
        // or remove any current locks for the queue
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
    /// Executes a timed coroutine.
    /// </summary>
    private static IEnumerator ExecuteOnce(float atTime, YieldInstruction atYield, Action callback)
    {
        // Wait until
        if (atTime > 0)
            yield return new WaitForSeconds(atTime);

        if (atYield != null)
            yield return atYield;

        // Task
        if (callback != null)
            callback();
    }
}