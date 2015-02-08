// TeaTime v0.5.8.4 alpha

// By Andrés Villalobos [andresalvivar@gmail.com twitter.com/matnesis]
// Special thanks to Antonio Zamora [twitter.com/tzamora] for the loop idea and testing.
// Created 2014/12/26 12:21 am

// TeaTime is a fast & simple queue for timed callbacks, fashioned as a
// MonoBehaviour extension set, focused on solving common coroutines patterns in
// Unity games.

// Just put 'TeaTime.cs' somewhere in your project and call it inside any
// MonoBehaviour using 'this.tt'.


//    this.ttAdd("QueueName", 2, () =>
//    {
//        Debug.Log("2 seconds since QueueName started " + Time.time);
//    })
//    .ttLoop(3, delegate(ttHandler loop)
//    {	
//        // ttLoop runs frame by frame for all his duration (3s) and his handler have a
//        // customized delta (loop.deltaTime) that represents the precise loop duration.
//        Camera.main.backgroundColor 
//            = Color.Lerp(Camera.main.backgroundColor, Color.white, loop.deltaTime);
//    })
//    this.ttAdd("delegate(ttHandler t)
//    {
//        Sequence myTween = DOTween.Sequence();
//        myTween.Append(transform.DOMoveX(5, 2.5f));
//        myTween.Append(transform.DOMoveX(-5, 2.5f));

//        // WaitFor waits for a time or YieldInstruction after the current callback is
//        // done and before the next queued callback.		
//        t.WaitFor(myTween.WaitForCompletion());
//    })
//    .ttAdd(() =>
//    {
//        Debug.Log("10 seconds since QueueName started " + Time.time);
//    })
//    .ttWait(); 
//    // ttWait locks the current queue, ignoring new appends until all his callbacks
//    // are done.

//    // And finally, TeaTime.Reset( let you stop and clean a running queue or all
//    // queues from an instance, and there is a TeaTime.ResetAll() that cleans
//    // everything.
//    TeaTime.Reset(this, "QueueName");

// Check out Examples.cs http://github.com/alvivar/TeaTime/blob/master/Examples.cs
// for a more depth explanation.


// Details to remember
// - Execution starts immediately
// - Queues are unique to his MonoBehaviour (this is an extension after all)
// - Below the sugar, everything runs on Unity coroutines!

// Tips
// - Always name your queue if you want to use more than one queue with safety 
// - You can use a YieldInstruction instead of time (e.g. WaitForEndOfFrame)
// - You can create tween-like behaviours by mixing loops, ttHandler properties, and lerp functions
// - ttWait ensures a complete and safe run during continuous calls

// About ttHandler
// - ttHandler adds special control features to your callbacks
// - .deltaTime contains a customized deltaTime that represents the precise loop duration
// - .t contains the completion percentage expresed from 0 to 1 based on the loop duration
// - .waitFor( applies only once and at the end of the current callback

// And that's it!


// Copyright (c) 2014/12/26 andresalvivar@gmail.com

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.

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


/// <summary>
/// TeaTime callback data.
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
/// TeaTask handler.
/// </summary>
public class ttHandler
{
    public bool isActive = true;
    public float t = 0f;
    public float deltaTime = 0f;
    public float timeSinceStart = 0f;
    public YieldInstruction yieldToWait = null;


    /// <summary>
    /// Breaks the current loop.
    /// </summary>
    public void Break()
    {
        this.isActive = false;
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
/// TeaTime is a fast & simple queue for timed callbacks, fashioned as a
/// MonoBehaviour extension set, focused on solving common coroutines patterns in
/// Unity games.
/// </summary>
public static class TeaTime
{
    /// <summary>
    /// Default name for anonymous queues.
    /// </summary>
    private const string DEFAULT_QUEUE_NAME = "TEATIME_DEFAULT_QUEUE_NAME";

    /// <summary>
    /// Main queue for all the timed callbacks.
    /// </summary>
    private static Dictionary<MonoBehaviour, Dictionary<string, List<TeaTask>>> mainQueue = null;

    /// <summary>
    /// Running queues in the instance.
    /// </summary>
    private static Dictionary<MonoBehaviour, List<string>> runningQueues = null;

    /// <summary>
    /// Current queue (last used) in the instance.
    /// </summary>
    private static Dictionary<MonoBehaviour, string> currentQueue = null;

    /// <summary>
    /// Locked Queues in the instance.
    /// </summary>
    private static Dictionary<MonoBehaviour, List<string>> lockedQueues = null;

    /// <summary>
    /// Running coroutines in the instance, by queue.
    /// </summary>
    private static Dictionary<MonoBehaviour, Dictionary<string, List<IEnumerator>>> runningCoroutines = null;


    /// <summary>
    /// Prepares the main queue for the instance.
    /// </summary>
    private static void PrepareInstanceMainQueue(MonoBehaviour instance, string queueName = null)
    {
        if (mainQueue == null)
            mainQueue = new Dictionary<MonoBehaviour, Dictionary<string, List<TeaTask>>>();

        if (mainQueue.ContainsKey(instance) == false)
            mainQueue.Add(instance, new Dictionary<string, List<TeaTask>>());

        if (queueName != null)
        {
            if (mainQueue[instance].ContainsKey(queueName) == false)
                mainQueue[instance].Add(queueName, new List<TeaTask>());
        }
    }


    /// <summary>
    /// Prepares the dictionary for the queues running in the instance.
    /// </summary>
    private static void PrepareInstanceRunningQueues(MonoBehaviour instance)
    {
        if (runningQueues == null)
            runningQueues = new Dictionary<MonoBehaviour, List<string>>();

        if (runningQueues.ContainsKey(instance) == false)
            runningQueues.Add(instance, new List<string>());
    }


    /// <summary>
    /// Prepares the dictionary for the current queue (last used) in the instance.
    /// </summary>
    private static void PrepareInstanceCurrentQueue(MonoBehaviour instance)
    {
        if (currentQueue == null)
            currentQueue = new Dictionary<MonoBehaviour, string>();

        // Default name
        if (currentQueue.ContainsKey(instance) == false)
            currentQueue[instance] = DEFAULT_QUEUE_NAME;
    }


    /// <summary>
    /// Prepares the dictionary for the queues locked in the instance.
    /// </summary>
    private static void PrepareInstanceLockedQueues(MonoBehaviour instance)
    {
        if (lockedQueues == null)
            lockedQueues = new Dictionary<MonoBehaviour, List<string>>();

        if (lockedQueues.ContainsKey(instance) == false)
            lockedQueues.Add(instance, new List<string>());
    }


    /// <summary>
    /// Prepares the dictionary for the coroutines running in the instance.
    /// </summary>
    private static void PrepareInstanceRunningCoroutines(MonoBehaviour instance, string queueName = null)
    {
        if (runningCoroutines == null)
            runningCoroutines = new Dictionary<MonoBehaviour, Dictionary<string, List<IEnumerator>>>();

        if (runningCoroutines.ContainsKey(instance) == false)
            runningCoroutines.Add(instance, new Dictionary<string, List<IEnumerator>>());

        if (queueName != null)
        {
            if (runningCoroutines[instance].ContainsKey(queueName) == false)
                runningCoroutines[instance].Add(queueName, new List<IEnumerator>());
        }
    }


    /// <summary>
    /// Returns true if a queue is locked.
    /// </summary>
    private static bool IsLocked(MonoBehaviour instance, string queueName)
    {
        PrepareInstanceLockedQueues(instance);

        if (lockedQueues[instance].Contains(queueName))
            return true;

        return false;
    }


    /// <summary>
    /// Appends a callback (timed or looped) into a queue.
    /// </summary>
    private static MonoBehaviour ttAdd(this MonoBehaviour instance, string queueName, float timeDelay, YieldInstruction yieldDelay,
        Action callback, Action<ttHandler> callbackWithHandler,
        bool isLoop)
    {
        // Ignore locked queues (but remember in his name)
        if (IsLocked(instance, queueName))
        {
            currentQueue[instance] = queueName;
            return instance;
        }

        PrepareInstanceMainQueue(instance, queueName);
        PrepareInstanceCurrentQueue(instance);

        // Sets the active queue
        currentQueue[instance] = queueName;

        // Appends a new task
        List<TeaTask> taskList = mainQueue[instance][queueName];
        taskList.Add(new TeaTask(timeDelay, yieldDelay, callback, callbackWithHandler, isLoop));

        // Execute queue
        instance.StartCoroutine(ExecuteQueue(instance, queueName));

        return instance;
    }


    /// <summary>
    /// Appends a timed callback into a queue.
    /// </summary>
    public static MonoBehaviour ttAdd(this MonoBehaviour instance, string queueName, float timeDelay, Action callback)
    {
        return instance.ttAdd(queueName, timeDelay, null, callback, null, false);
    }


    /// <summary>
    /// Appends a timed callback into a queue.
    /// </summary>
    public static MonoBehaviour ttAdd(this MonoBehaviour instance, string queueName, float timeDelay, Action<ttHandler> callback)
    {
        return instance.ttAdd(queueName, timeDelay, null, null, callback, false);
    }


    /// <summary>
    /// Appends a timed callback into a queue.
    /// </summary>
    public static MonoBehaviour ttAdd(this MonoBehaviour instance, string queueName, YieldInstruction yieldToWait, Action callback)
    {
        return instance.ttAdd(queueName, 0, yieldToWait, callback, null, false);
    }


    /// <summary>
    /// Appends a timed callback into a queue.
    /// </summary>
    public static MonoBehaviour ttAdd(this MonoBehaviour instance, string queueName, YieldInstruction yieldToWait, Action<ttHandler> callback)
    {
        return instance.ttAdd(queueName, 0, yieldToWait, null, callback, false);
    }


    /// <summary>
    /// Appends a timed callback into the current queue (or default).
    /// </summary>
    public static MonoBehaviour ttAdd(this MonoBehaviour instance, float timeDelay, Action callback)
    {
        PrepareInstanceCurrentQueue(instance);

        return instance.ttAdd(currentQueue[instance], timeDelay, null, callback, null, false);
    }


    /// <summary>
    /// Appends a timed callback into the current queue (or default).
    /// </summary>
    public static MonoBehaviour ttAdd(this MonoBehaviour instance, float timeDelay, Action<ttHandler> callback)
    {
        PrepareInstanceCurrentQueue(instance);

        return instance.ttAdd(currentQueue[instance], timeDelay, null, null, callback, false);
    }


    /// <summary>
    /// Appends a timed callback into the current queue (or default).
    /// </summary>
    public static MonoBehaviour ttAdd(this MonoBehaviour instance, YieldInstruction yieldToWait, Action callback)
    {
        PrepareInstanceCurrentQueue(instance);

        return instance.ttAdd(currentQueue[instance], 0, yieldToWait, callback, null, false);
    }


    /// <summary>
    /// Appends a timed callback into the current queue (or default).
    /// </summary>
    public static MonoBehaviour ttAdd(this MonoBehaviour instance, YieldInstruction yieldToWait, Action<ttHandler> callback)
    {
        PrepareInstanceCurrentQueue(instance);

        return instance.ttAdd(currentQueue[instance], 0, yieldToWait, null, callback, false);
    }


    /// <summary>
    /// Appends a time interval into a queue.
    /// </summary>
    public static MonoBehaviour ttAdd(this MonoBehaviour instance, string queueName, float interval)
    {
        return instance.ttAdd(queueName, interval, null, null, null, false);
    }


    /// <summary>
    /// Appends a time interval into the current queue (or default).
    /// </summary>
    public static MonoBehaviour ttAdd(this MonoBehaviour instance, float interval)
    {
        PrepareInstanceCurrentQueue(instance);

        return instance.ttAdd(currentQueue[instance], interval, null, null, null, false);
    }


    /// <summary>
    /// Appends a callback into a queue.
    /// </summary>
    public static MonoBehaviour ttAdd(this MonoBehaviour instance, string queueName, Action callback)
    {
        return instance.ttAdd(queueName, 0, null, callback, null, false);
    }


    /// <summary>
    /// Appends a callback into a queue.
    /// </summary>
    public static MonoBehaviour ttAdd(this MonoBehaviour instance, string queueName, Action<ttHandler> callback)
    {
        return instance.ttAdd(queueName, 0, null, null, callback, false);
    }


    /// <summary>
    /// Appends a callback into the current queue (or default).
    /// </summary>
    public static MonoBehaviour ttAdd(this MonoBehaviour instance, Action callback)
    {
        PrepareInstanceCurrentQueue(instance);

        return instance.ttAdd(currentQueue[instance], 0, null, callback, null, false);
    }


    /// <summary>
    /// Appends a callback into the current queue (or default).
    /// </summary>
    public static MonoBehaviour ttAdd(this MonoBehaviour instance, Action<ttHandler> callback)
    {
        PrepareInstanceCurrentQueue(instance);

        return instance.ttAdd(currentQueue[instance], 0, null, null, callback, false);
    }


    /// <summary>
    /// Appends into a queue a callback that runs frame by frame for all his duration.
    /// </summary>
    public static MonoBehaviour ttLoop(this MonoBehaviour instance, string queueName, float duration, Action<ttHandler> callback)
    {
        return instance.ttAdd(queueName, duration, null, null, callback, true);
    }


    /// <summary>
    /// Appends into a queue a callback that runs frame by frame until his exit is forced.
    /// </summary>
    public static MonoBehaviour ttLoop(this MonoBehaviour instance, string queueName, Action<ttHandler> callback)
    {
        return instance.ttAdd(queueName, 0, null, null, callback, true);
    }


    /// <summary>
    /// Appends into the current queue a callback that runs frame by frame for all his duration.
    /// </summary>
    public static MonoBehaviour ttLoop(this MonoBehaviour instance, float duration, Action<ttHandler> callback)
    {
        PrepareInstanceCurrentQueue(instance);

        return instance.ttAdd(currentQueue[instance], duration, null, null, callback, true);
    }


    /// <summary>
    /// Appends into the current queue a callback that runs frame by frame until his exit is forced.
    /// </summary>
    public static MonoBehaviour ttLoop(this MonoBehaviour instance, Action<ttHandler> callback)
    {
        PrepareInstanceCurrentQueue(instance);

        return instance.ttAdd(currentQueue[instance], 0, null, null, callback, true);
    }


    /// <summary>
    /// Create or select a queue as the current. When used without name the queue will be untrackable (immune to ttWait).
    /// </summary>
    public static MonoBehaviour tt(this MonoBehaviour instance, string queueName = null)
    {
        if (queueName == null)
            queueName = DEFAULT_QUEUE_NAME + "_" + Time.time + "_" + UnityEngine.Random.Range(int.MinValue, int.MaxValue);

        PrepareInstanceCurrentQueue(instance);
        currentQueue[instance] = queueName;

        return instance;
    }


    /// <summary>
    /// Waits for completion, locks the current queue ignoring new appends until all his callbacks are completed.
    /// </summary>
    public static MonoBehaviour ttWait(this MonoBehaviour instance)
    {
        PrepareInstanceMainQueue(instance);
        PrepareInstanceCurrentQueue(instance);

        // Ignore if the queue if empty
        if (mainQueue[instance].ContainsKey(currentQueue[instance]) == false ||
            mainQueue[instance][currentQueue[instance]].Count < 1)
            return instance;

        // Locks the queue
        if (IsLocked(instance, currentQueue[instance]) == false)
            lockedQueues[instance].Add(currentQueue[instance]);

        return instance;
    }


    /// <summary>
    /// Stop and reset a queue in an instance.
    /// </summary>
    public static void Reset(MonoBehaviour instance, string queueName)
    {
        // Initialize all
        PrepareInstanceMainQueue(instance, queueName);
        PrepareInstanceRunningQueues(instance);
        PrepareInstanceCurrentQueue(instance);
        PrepareInstanceLockedQueues(instance);
        PrepareInstanceRunningCoroutines(instance, queueName);

        // Delete all
        mainQueue[instance][queueName].Clear();

        if (runningQueues[instance].Contains(queueName))
            runningQueues[instance].Remove(queueName);

        currentQueue[instance] = queueName;

        if (lockedQueues[instance].Contains(queueName))
            lockedQueues[instance].Remove(queueName);

        // Stop coroutines & clean
        foreach (IEnumerator coroutine in runningCoroutines[instance][queueName])
        {
            instance.StopCoroutine(coroutine);
        }
        runningCoroutines[instance][queueName].Clear();
    }


    /// <summary>
    /// Stop and reset all queues in an instance.
    /// </summary>
    public static void Reset(MonoBehaviour instance)
    {
        // Initialize all
        PrepareInstanceMainQueue(instance);
        PrepareInstanceRunningQueues(instance);
        PrepareInstanceCurrentQueue(instance);
        PrepareInstanceLockedQueues(instance);
        PrepareInstanceRunningCoroutines(instance);

        // Delete all
        foreach (KeyValuePair<string, List<TeaTask>> taskList in mainQueue[instance])
            taskList.Value.Clear();

        runningQueues[instance].Clear();
        currentQueue[instance] = DEFAULT_QUEUE_NAME;
        lockedQueues[instance].Clear();

        // Stop coroutines & clean
        foreach (KeyValuePair<string, List<IEnumerator>> coroutineList in runningCoroutines[instance])
        {
            foreach (IEnumerator coroutine in coroutineList.Value)
            {
                instance.StopCoroutine(coroutine);
            }
            coroutineList.Value.Clear();
        }
        runningCoroutines[instance].Clear();
    }


    /// <summary>
    /// Stop and resets all queues in all instances.
    /// </summary>
    public static void ResetAll()
    {
        // Delete all

        // Main queue clear
        foreach (KeyValuePair<MonoBehaviour, Dictionary<string, List<TeaTask>>> instanceDict in mainQueue)
        {
            foreach (KeyValuePair<string, List<TeaTask>> taskList in instanceDict.Value)
            {
                taskList.Value.Clear();
            }
        }

        // Running queues clear
        foreach (KeyValuePair<MonoBehaviour, List<string>> runningList in runningQueues)
        {
            runningList.Value.Clear();
        }

        // Queues names clear
        List<MonoBehaviour> keys = new List<MonoBehaviour>(currentQueue.Keys);
        foreach (MonoBehaviour key in keys)
        {
            currentQueue[key] = DEFAULT_QUEUE_NAME;
        }

        // Locked queues clear
        foreach (KeyValuePair<MonoBehaviour, List<string>> lockedList in lockedQueues)
        {
            lockedList.Value.Clear();
        }

        // Stop all coroutines & clean
        foreach (KeyValuePair<MonoBehaviour, Dictionary<string, List<IEnumerator>>> instanceDict in runningCoroutines)
        {
            foreach (KeyValuePair<string, List<IEnumerator>> coroutineList in instanceDict.Value)
            {
                foreach (IEnumerator coroutine in coroutineList.Value)
                {
                    instanceDict.Key.StopCoroutine(coroutine);
                }
                coroutineList.Value.Clear();
            }
        }
        runningCoroutines.Clear();
    }


    /// <summary>
    /// Execute all timed callbacks and loops for an instance queue.
    /// </summary>
    private static IEnumerator ExecuteQueue(MonoBehaviour instance, string queueName)
    {
        // Ignore if empty
        if (mainQueue.ContainsKey(instance) == false)
            yield break;

        if (mainQueue[instance].ContainsKey(queueName) == false)
            yield break;

        PrepareInstanceRunningQueues(instance);

        // Ignore if already running
        if (runningQueues.ContainsKey(instance) && runningQueues[instance].Contains(queueName))
            yield break;

        // Locks the queue
        runningQueues[instance].Add(queueName);

        // Coroutines registry
        PrepareInstanceRunningCoroutines(instance, queueName);
        IEnumerator coroutine = null;

        // Run until depleted (over a clone)
        List<TeaTask> batch = new List<TeaTask>();
        batch.AddRange(mainQueue[instance][queueName]);

        foreach (TeaTask task in batch)
        {
            // Select and prepare
            if (task.isLoop)
            {
                if (task.time > 0)
                {
                    coroutine = ExecuteLoop(task.time, task.callbackWithHandler);
                }
                else
                {
                    coroutine = ExecuteInfiniteLoop(task.callbackWithHandler);
                }
            }
            else
            {
                coroutine = ExecuteOnce(task.time, task.yieldInstruction, task.callback, task.callbackWithHandler);
            }

            // Register and execute
            runningCoroutines[instance][queueName].Add(coroutine);
            yield return instance.StartCoroutine(coroutine);
            runningCoroutines[instance][queueName].Remove(coroutine);

            // Done
            mainQueue[instance][queueName].Remove(task);
        }

        // Unlocks the queue
        runningQueues[instance].Remove(queueName);

        // Try again is there are new items, else, remove the lock
        if (mainQueue[instance][queueName].Count > 0)
        {
            instance.StartCoroutine(ExecuteQueue(instance, queueName));
        }
        else
        {
            if (IsLocked(instance, queueName))
                lockedQueues[instance].Remove(queueName);
        }
    }


    /// <summary>
    /// Executes a timed callback.
    /// </summary>
    private static IEnumerator ExecuteOnce(float timeToWait, YieldInstruction yieldToWait,
        Action callback, Action<ttHandler> callbackWithHandler)
    {
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
    /// Executes a callback inside a loop for all his duration.
    /// </summary>
    private static IEnumerator ExecuteLoop(float duration, Action<ttHandler> callback)
    {
        // Only for positive values
        if (duration <= 0)
            yield break;

        ttHandler loopHandler = new ttHandler();
        float tRate = 1 / duration;

        // Run while active until duration
        while (loopHandler.isActive && loopHandler.timeSinceStart < duration)
        {
            float delta = Time.deltaTime;

            // Completion from 0 to 1
            loopHandler.t += tRate * delta;

            // Custom delta based on duration
            loopHandler.deltaTime = 1 / (duration - loopHandler.timeSinceStart) * delta;
            loopHandler.timeSinceStart += delta;

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

        // Run while active
        while (loopHandler.isActive)
        {
            float delta = Time.deltaTime;
            loopHandler.deltaTime = delta;
            loopHandler.timeSinceStart += delta;

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
