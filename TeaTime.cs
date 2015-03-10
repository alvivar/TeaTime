// TeaTime v0.5.9 alpha

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
//            = Color.Lerp(Camera.main.backgroundColor, Color.black, loop.deltaTime);
//    })
//    .ttAdd(() =>
//    {
//        Debug.Log("5 seconds since QueueName started " + Time.time);
//    })

// Check out Examples.cs [http://github.com/alvivar/TeaTime/blob/master/Examples.cs]
// for all the features and explained examples.


// **Details to remember**
// - Execution starts immediately
// - Queues are unique to his MonoBehaviour (this is an extension after all)
// - Below the sugar, everything runs on Unity coroutines!

// **Tips**
// - You can create tween-like behaviours with loops and lerp functions
// - Always name your queue if you want to use more than one queue with safety 
// - You can use a YieldInstruction instead of time (e.g. WaitForEndOfFrame)

// **About ttHandler**
// - ttHandler adds special control features to all your callbacks
// - ttHandler.deltaTime contains a customized deltaTime that represents the precise loop duration
// - ttHandler.t contains the completion percentage expressed from 0 to 1 based on the loop duration
// - ttHandler.waitFor( applies a wait interval once, at the end of the current callback

// **Moar**
// - tt( changes the current queue, reset it or create an anonymous queue
// - ttWait() ensures a complete and safe run of the current queue (waits for completion)
// - TeaTime.Reset( stops and resets queues and instances, TeaTime.ResetAll( resets everything

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
public class ttTask
{
    public float time = 0f;
    public YieldInstruction yieldInstruction = null;
    public Action callback = null;
    public Action<ttHandler> callbackWithHandler = null;
    public bool isLoop = false;


    public ttTask(float time, YieldInstruction yield, Action callback, Action<ttHandler> callbackWithHandler, bool isLoop)
    {
        this.time = time;
        this.yieldInstruction = yield;
        this.callback = callback;
        this.callbackWithHandler = callbackWithHandler;
        this.isLoop = isLoop;
    }
}


/// <summary>
/// ttTask handler.
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
    /// Default queue name.
    /// </summary>
    private const string DEFAULT_QUEUE_NAME = "DEFAULT_QUEUE_NAME";

    /// <summary>
    /// Main queue for all the timed callbacks.
    /// #todo This can be optimized by using blueprints, maybe.
    /// </summary>
    private static Dictionary<MonoBehaviour, Dictionary<string, List<ttTask>>> mainQueue = null;

    /// <summary>
    /// Contains a complete copy of every queue and their respective callbacks.
    /// </summary>
    private static Dictionary<MonoBehaviour, Dictionary<string, List<ttTask>>> blueprints = null;

    /// <summary>
    /// Queues currently running.
    /// </summary>
    private static Dictionary<MonoBehaviour, List<string>> runningQueues = null;

    /// <summary>
    /// Current queue.
    /// </summary>
    private static Dictionary<MonoBehaviour, string> currentQueueName = null;

    /// <summary>
    /// Locked queues, by ttLock().
    /// </summary>
    private static Dictionary<MonoBehaviour, List<string>> lockedQueues = null;

    /// <summary>
    /// Infinite queues, by ttRepeat(-1).
    /// </summary>
    private static Dictionary<MonoBehaviour, List<string>> infiniteQueues = null;

    /// <summary>
    /// Running coroutines in the instance, by queue.
    /// </summary>
    private static Dictionary<MonoBehaviour, Dictionary<string, List<IEnumerator>>> runningCoroutines = null;


    /// <summary>
    /// Prepares the main queue for the instance (and the blueprints registry).
    /// </summary>
    private static void PrepareMainQueue(MonoBehaviour instance, string queueName = null)
    {
        // Main queue
        if (mainQueue == null)
            mainQueue = new Dictionary<MonoBehaviour, Dictionary<string, List<ttTask>>>();

        if (mainQueue.ContainsKey(instance) == false)
            mainQueue.Add(instance, new Dictionary<string, List<ttTask>>());

        // Blueprints
        if (blueprints == null)
            blueprints = new Dictionary<MonoBehaviour, Dictionary<string, List<ttTask>>>();

        if (blueprints.ContainsKey(instance) == false)
            blueprints.Add(instance, new Dictionary<string, List<ttTask>>());

        // Task list for both
        if (queueName != null)
        {
            if (mainQueue[instance].ContainsKey(queueName) == false)
                mainQueue[instance].Add(queueName, new List<ttTask>());

            if (blueprints[instance].ContainsKey(queueName) == false)
                blueprints[instance].Add(queueName, new List<ttTask>());
        }
    }


    /// <summary>
    /// Prepares the dictionary for the queues running in the instance.
    /// </summary>
    private static void PrepareRunningQueues(MonoBehaviour instance)
    {
        if (runningQueues == null)
            runningQueues = new Dictionary<MonoBehaviour, List<string>>();

        if (runningQueues.ContainsKey(instance) == false)
            runningQueues.Add(instance, new List<string>());
    }


    /// <summary>
    /// Prepares the dictionary for the current queue (last used) in the instance.
    /// </summary>
    private static void PrepareCurrentQueueName(MonoBehaviour instance)
    {
        if (currentQueueName == null)
            currentQueueName = new Dictionary<MonoBehaviour, string>();

        // Default name
        if (currentQueueName.ContainsKey(instance) == false)
            currentQueueName[instance] = DEFAULT_QUEUE_NAME;
    }


    /// <summary>
    /// Prepares the dictionary for the queues locked in the instance.
    /// </summary>
    private static void PrepareLockedQueues(MonoBehaviour instance)
    {
        if (lockedQueues == null)
            lockedQueues = new Dictionary<MonoBehaviour, List<string>>();

        if (lockedQueues.ContainsKey(instance) == false)
            lockedQueues.Add(instance, new List<string>());
    }


    /// <summary>
    /// Prepares the dictionary for the infinite queues in the instance.
    /// </summary>
    private static void PrepareInfiniteQueues(MonoBehaviour instance)
    {
        if (infiniteQueues == null)
            infiniteQueues = new Dictionary<MonoBehaviour, List<string>>();

        if (infiniteQueues.ContainsKey(instance) == false)
            infiniteQueues.Add(instance, new List<string>());
    }


    /// <summary>
    /// Prepares the dictionary for the coroutines running in the instance.
    /// </summary>
    private static void PrepareRunningCoroutines(MonoBehaviour instance, string queueName = null)
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
        PrepareLockedQueues(instance);

        if (lockedQueues[instance].Contains(queueName))
            return true;

        return false;
    }


    /// <summary>
    /// Returns true if a queue is infinite.
    /// </summary>
    private static bool IsInfinite(MonoBehaviour instance, string queueName)
    {
        PrepareInfiniteQueues(instance);

        if (infiniteQueues[instance].Contains(queueName))
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
        // Always remember the name
        PrepareCurrentQueueName(instance);
        currentQueueName[instance] = queueName;

        // Ignore locked
        if (IsLocked(instance, queueName))
            return instance;

        PrepareMainQueue(instance, queueName);

        // Appends a new task (+ Blueprint clone)
        mainQueue[instance][queueName].Add(new ttTask(timeDelay, yieldDelay, callback, callbackWithHandler, isLoop));
        blueprints[instance][queueName].Add(new ttTask(timeDelay, yieldDelay, callback, callbackWithHandler, isLoop));

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
    /// Appends a timed callback into the current queue.
    /// </summary>
    public static MonoBehaviour ttAdd(this MonoBehaviour instance, float timeDelay, Action callback)
    {
        PrepareCurrentQueueName(instance);

        return instance.ttAdd(currentQueueName[instance], timeDelay, null, callback, null, false);
    }


    /// <summary>
    /// Appends a timed callback into the current queue.
    /// </summary>
    public static MonoBehaviour ttAdd(this MonoBehaviour instance, float timeDelay, Action<ttHandler> callback)
    {
        PrepareCurrentQueueName(instance);

        return instance.ttAdd(currentQueueName[instance], timeDelay, null, null, callback, false);
    }


    /// <summary>
    /// Appends a timed callback into the current queue.
    /// </summary>
    public static MonoBehaviour ttAdd(this MonoBehaviour instance, YieldInstruction yieldToWait, Action callback)
    {
        PrepareCurrentQueueName(instance);

        return instance.ttAdd(currentQueueName[instance], 0, yieldToWait, callback, null, false);
    }


    /// <summary>
    /// Appends a timed callback into the current queue.
    /// </summary>
    public static MonoBehaviour ttAdd(this MonoBehaviour instance, YieldInstruction yieldToWait, Action<ttHandler> callback)
    {
        PrepareCurrentQueueName(instance);

        return instance.ttAdd(currentQueueName[instance], 0, yieldToWait, null, callback, false);
    }


    /// <summary>
    /// Appends a time interval into a queue.
    /// </summary>
    public static MonoBehaviour ttAdd(this MonoBehaviour instance, string queueName, float interval)
    {
        return instance.ttAdd(queueName, interval, null, null, null, false);
    }


    /// <summary>
    /// Appends a time interval into the current queue.
    /// </summary>
    public static MonoBehaviour ttAdd(this MonoBehaviour instance, float interval)
    {
        PrepareCurrentQueueName(instance);

        return instance.ttAdd(currentQueueName[instance], interval, null, null, null, false);
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
    /// Appends a callback into the current queue.
    /// </summary>
    public static MonoBehaviour ttAdd(this MonoBehaviour instance, Action callback)
    {
        PrepareCurrentQueueName(instance);

        return instance.ttAdd(currentQueueName[instance], 0, null, callback, null, false);
    }


    /// <summary>
    /// Appends a callback into the current queue.
    /// </summary>
    public static MonoBehaviour ttAdd(this MonoBehaviour instance, Action<ttHandler> callback)
    {
        PrepareCurrentQueueName(instance);

        return instance.ttAdd(currentQueueName[instance], 0, null, null, callback, false);
    }


    /// <summary>
    /// Appends into a queue a callback that runs frame by frame for all his duration.
    /// </summary>
    public static MonoBehaviour ttLoop(this MonoBehaviour instance, string queueName, float duration, Action<ttHandler> callback)
    {
        return instance.ttAdd(queueName, duration, null, null, callback, true);
    }


    /// <summary>
    /// Appends into a queue a callback that runs frame by frame until ttHandler.Break().
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
        PrepareCurrentQueueName(instance);

        return instance.ttAdd(currentQueueName[instance], duration, null, null, callback, true);
    }


    /// <summary>
    /// Appends into the current queue a callback that runs frame by frame until ttHandler.Break().
    /// </summary>
    public static MonoBehaviour ttLoop(this MonoBehaviour instance, Action<ttHandler> callback)
    {
        PrepareCurrentQueueName(instance);

        return instance.ttAdd(currentQueueName[instance], 0, null, null, callback, true);
    }


    /// <summary>
    /// Locks the current queue ignoring new appends until all his callbacks are done (locks until completion).
    /// </summary>
    public static MonoBehaviour ttLock(this MonoBehaviour instance)
    {
        PrepareMainQueue(instance);
        PrepareCurrentQueueName(instance);

        // Ignore if the queue if empty
        if (mainQueue[instance].ContainsKey(currentQueueName[instance]) == false ||
            mainQueue[instance][currentQueueName[instance]].Count < 1)
            return instance;

        // Locks the queue
        if (IsLocked(instance, currentQueueName[instance]) == false)
            lockedQueues[instance].Add(currentQueueName[instance]);

        return instance;
    }


    /// <summary>
    /// Repeat the current queue n times or infinite (n <= -1).
    /// </summary>
    public static MonoBehaviour ttRepeat(this MonoBehaviour instance, int nTimes = 1)
    {
        PrepareCurrentQueueName(instance);

        // Ignore locked
        if (IsLocked(instance, currentQueueName[instance]))
            return instance;

        PrepareMainQueue(instance);

        // Ignore if the queue if empty
        if (mainQueue[instance].ContainsKey(currentQueueName[instance]) == false ||
            mainQueue[instance][currentQueueName[instance]].Count < 1)
            return instance;

        // If infinite
        if (nTimes < 0)
        {
            instance.tt(currentQueueName[instance]).ttLock();

            PrepareInfiniteQueues(instance);

            if (infiniteQueues[instance].Contains(currentQueueName[instance]) == false)
                infiniteQueues[instance].Add(currentQueueName[instance]);

            return instance;
        }

        // Repeat n 
        while (nTimes-- > 0)
        {
            mainQueue[instance][currentQueueName[instance]].AddRange(blueprints[instance][currentQueueName[instance]]);
        }

        return instance;
    }


    /// <summary>
    /// Creates or changes the current queue.
    /// When used without name the queue will be anonymous and untrackable.
    /// If 'resetQueue = true' the queue will be stopped and cleaned first, just like with 'TeaTime.Reset('.
    /// </summary>
    public static MonoBehaviour tt(this MonoBehaviour instance, string queueName = null, bool resetQueue = false)
    {
        if (queueName == null)
            queueName = DEFAULT_QUEUE_NAME + "_" + Time.time + "_" + UnityEngine.Random.Range(0, int.MaxValue);

        PrepareCurrentQueueName(instance);
        currentQueueName[instance] = queueName;

        if (resetQueue == true)
            Reset(instance, queueName);

        return instance;
    }


    /// <summary>
    /// Stops and resets a queue from an instance.
    /// </summary>
    public static void Reset(MonoBehaviour instance, string queueName)
    {
        // Initialize all
        PrepareMainQueue(instance, queueName);
        PrepareRunningQueues(instance);
        PrepareCurrentQueueName(instance);
        PrepareLockedQueues(instance);
        PrepareInfiniteQueues(instance);
        PrepareRunningCoroutines(instance, queueName);

        // Delete all
        mainQueue[instance][queueName].Clear();

        if (runningQueues[instance].Contains(queueName))
            runningQueues[instance].Remove(queueName);

        //currentQueue[instance] = queueName;

        if (lockedQueues[instance].Contains(queueName))
            lockedQueues[instance].Remove(queueName);

        if (infiniteQueues[instance].Contains(queueName))
            infiniteQueues[instance].Remove(queueName);

        // Stop coroutines & clean
        foreach (IEnumerator coroutine in runningCoroutines[instance][queueName])
        {
            instance.StopCoroutine(coroutine);
        }
        runningCoroutines[instance][queueName].Clear();
    }


    /// <summary>
    /// Stops and resets all queues from an instance.
    /// </summary>
    public static void Reset(MonoBehaviour instance)
    {
        // Initialize all
        PrepareMainQueue(instance);
        PrepareRunningQueues(instance);
        PrepareCurrentQueueName(instance);
        PrepareLockedQueues(instance);
        PrepareInfiniteQueues(instance);
        PrepareRunningCoroutines(instance);

        // Delete all
        foreach (KeyValuePair<string, List<ttTask>> taskList in mainQueue[instance])
            taskList.Value.Clear();

        runningQueues[instance].Clear();
        //currentQueue[instance] = DEFAULT_QUEUE_NAME;
        lockedQueues[instance].Clear();
        infiniteQueues[instance].Clear();

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
    /// Stops and resets all queues in all instances.
    /// </summary>
    public static void ResetAll()
    {
        // Main queue clear
        if (mainQueue != null)
        {
            foreach (KeyValuePair<MonoBehaviour, Dictionary<string, List<ttTask>>> instanceDict in mainQueue)
            {
                foreach (KeyValuePair<string, List<ttTask>> taskList in instanceDict.Value)
                {
                    taskList.Value.Clear();
                }
            }
        }

        // Running queues clear
        if (runningCoroutines != null)
        {
            foreach (KeyValuePair<MonoBehaviour, List<string>> runningList in runningQueues)
            {
                runningList.Value.Clear();
            }
        }

        // Queues names clear
        if (currentQueueName != null)
        {
            List<MonoBehaviour> keys = new List<MonoBehaviour>(currentQueueName.Keys);
            foreach (MonoBehaviour key in keys)
            {
                currentQueueName[key] = DEFAULT_QUEUE_NAME;
            }
        }

        // Locked queues clear
        if (lockedQueues != null)
        {
            foreach (KeyValuePair<MonoBehaviour, List<string>> lockedList in lockedQueues)
            {
                lockedList.Value.Clear();
            }
        }

        // Infinite queues clear
        if (infiniteQueues != null)
        {
            foreach (KeyValuePair<MonoBehaviour, List<string>> infiniteList in infiniteQueues)
            {
                infiniteList.Value.Clear();
            }
        }

        // Stop all coroutines & clean
        if (runningCoroutines != null)
        {
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
    }


    /// <summary>
    /// Executes all timed callbacks and loops for an instance queue.
    /// </summary>
    private static IEnumerator ExecuteQueue(MonoBehaviour instance, string queueName)
    {
        // Ignore if empty
        if (mainQueue.ContainsKey(instance) == false)
            yield break;

        if (mainQueue[instance].ContainsKey(queueName) == false)
            yield break;

        PrepareRunningQueues(instance);

        // Ignore if already running
        if (runningQueues.ContainsKey(instance) && runningQueues[instance].Contains(queueName))
            yield break;

        // Locks the queue
        runningQueues[instance].Add(queueName);

        // Coroutines registry
        PrepareRunningCoroutines(instance, queueName);
        IEnumerator coroutine = null;

        // Run until depleted (over a clone)
        List<ttTask> batch = new List<ttTask>();
        batch.AddRange(mainQueue[instance][queueName]);

        foreach (ttTask task in batch)
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

            // Register and execute coroutines
            runningCoroutines[instance][queueName].Add(coroutine);
            yield return instance.StartCoroutine(coroutine);
            runningCoroutines[instance][queueName].Remove(coroutine);

            // Done!
            mainQueue[instance][queueName].Remove(task);
        }

        // The queue has stopped
        runningQueues[instance].Remove(queueName);

        // Try again is there are new items, or
        if (mainQueue[instance][queueName].Count > 0)
        {
            instance.StartCoroutine(ExecuteQueue(instance, queueName));
        }
        else
        {
            // If empty, repeat if the queue is infinite
            if (IsInfinite(instance, queueName) == true)
            {
                mainQueue[instance][queueName].AddRange(blueprints[instance][queueName]);
                instance.StartCoroutine(ExecuteQueue(instance, queueName));
            }
            else
            {
                // Remove the lock on non infinite queues
                if (IsLocked(instance, queueName) == true)
                    lockedQueues[instance].Remove(queueName);
            }
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

        // Executes the normal callback
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
    /// Executes a callback inside a loop for all his duration, or until ttHandler.Break().
    /// </summary>
    private static IEnumerator ExecuteLoop(float duration, Action<ttHandler> callback)
    {
        // Only for positive values
        if (duration <= 0)
            yield break;

        // Handler data
        ttHandler loopHandler = new ttHandler();
        float tRate = 1 / duration;

        // Run while active until duration
        while (loopHandler.isActive && loopHandler.t < 1)
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
    /// Executes a callback inside an infinite loop until ttHandler.Break().
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
