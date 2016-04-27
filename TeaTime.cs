
// @
// TeaTime v0.7.8 beta

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


namespace matnesis.TeaTime
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;


    /// <summary>
    /// Timed Task node.
    /// </summary>
    internal class ttTask
    {
        public bool isLoop = false;

        public float time = 0;
        public Func<float> timeByFunc = null;

        public Action callback = null;
        public Action<ttHandler> callbackWithHandler = null;
    }


    /// <summary>
    /// TeaTime handler for callbacks.
    /// </summary>
    public class ttHandler
    {
        public TeaTime self; // Current TeaTime instance

        public bool isLooping = false;

        public float t = 0;
        public float deltaTime = 0;
        public float timeSinceStart = 0;

        public List<YieldInstruction> yieldsToWait = null;


        /// <summary>
        /// Ends the current loop (.isLooping = false).
        /// </summary>
        public void EndLoop()
        {
            isLooping = false;
        }


        /// <summary>
        /// Appends a delay to wait after the current callback execution.
        /// </summary>
        public void WaitFor(YieldInstruction yi)
        {
            if (yieldsToWait == null)
                yieldsToWait = new List<YieldInstruction>();

            yieldsToWait.Add(yi);
        }


        /// <summary>
        /// Appends a delay to wait after the current callback execution.
        /// </summary>
        public void WaitFor(float time)
        {
            WaitFor(new WaitForSeconds(time));
        }


        /// <summary>
        /// Appends a delay to wait after the current callback execution.
        /// </summary>
        public void WaitFor(TeaTime tt)
        {
            WaitFor(tt.WaitForCompletion());
        }
    }


    /// <summary>
    /// TeaTime extensions (Dark magic).
    /// </summary>
    public static class TeaTimeExtensions
    {
        private static Dictionary<MonoBehaviour, Dictionary<string, TeaTime>> ttRegister; // Queues bounded by 'tt(string)'


        /// <summary>
        /// Returns a new TeaTime queue ready to be used. This is basically a
        /// shorcut to 'new TeaTime(this);' in MonoBehaviours.
        /// </summary>
        public static TeaTime tt(this MonoBehaviour instance)
        {
            return new TeaTime(instance);
        }


        /// <summary>
        /// Returns a TeaTime queue bounded to his name, unique per
        /// MonoBehaviour instance, new on the first call. This allows you to
        /// access queues without a formal definition. Dark magic, be careful.
        /// </summary>
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


    /// <summary>
    /// TeaTime custom yield that waits for completion (currently unused).
    /// </summary>
    internal class ttWaitForCompletion : CustomYieldInstruction
    {
        private TeaTime tt;

        public override bool keepWaiting { get { return tt.IsCompleted; } }

        public ttWaitForCompletion(TeaTime tt) { this.tt = tt; }
    }


    /// <summary>
    /// YieldInstruction static cache!
    /// Found here http://forum.unity3d.com/threads/c-coroutine-waitforseconds-garbage-collection-tip.224878/
    /// </summary>
    public static class ttYield
    {
        class FloatComparer : IEqualityComparer<float>
        {
            bool IEqualityComparer<float>.Equals(float x, float y)
            {
                return x == y;
            }

            int IEqualityComparer<float>.GetHashCode(float obj)
            {
                return obj.GetHashCode();
            }
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


    /// <summary>
    /// TeaTime is a fast & simple queue for timed callbacks, focused on solving
    /// common coroutines patterns in Unity games.
    /// </summary>
    public class TeaTime
    {
        // Queue
        private List<ttTask> _tasks = new List<ttTask>(); // Tasks list used as a queue
        private int _currentTask = 0; // Current task mark (to be executed)
        private int _executedCount = 0; // Executed task count


        // Dependencies
        private MonoBehaviour _instance = null; // Required to access Unity coroutine fuctions
        private Coroutine _currentCoroutine = null; // Coroutine that holds the queue execution


        // States
        private bool _isPlaying = false; // True while queue execution
        private bool _isPaused = false; // On .Pause()
        private bool _isImmutable = false; // On .Immutable() mode
        private bool _isRepeating = false; // On .Repeat() mode
        private bool _isConsuming = false; // On .Consume() mode


        /// <summary>
        /// True while the queue is being executed.
        /// </summary>
        public bool IsPlaying
        {
            get { return _isPlaying; }
        }

        /// <summary>
        /// True if the queue execution is done.
        /// </summary>
        public bool IsCompleted
        {
            get { return _currentTask > _tasks.Count && !_isPlaying; }
        }

        /// <summary>
        /// Queue count.
        /// </summary>
        public int Count
        {
            get { return _tasks.Count; }
        }

        /// <summary>
        /// Current queue position to be executed.
        /// </summary>
        public int Current
        {
            get { return _currentTask; }
        }


        /// <summary>
        /// Executed callback count.
        /// </summary>
        public int ExecutedCount
        {
            get { return _executedCount; }
        }


        /// <summary>
        /// A TeaTime queue requires a MonoBehaviour instance to access his
        /// coroutine fuctions.
        /// </summary>
        public TeaTime(MonoBehaviour instance)
        {
            _instance = instance;
        }


        // @
        // ADD


        /// <summary>
        /// Appends a new ttTask.
        /// </summary>
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


        /// <summary>
        /// Appends a timed callback.
        /// </summary>
        public TeaTime Add(float timeDelay, Action callback)
        {
            return Add(timeDelay, null, callback, null);
        }


        /// <summary>
        /// Appends a timed callback.
        /// </summary>
        public TeaTime Add(Func<float> timeByFunc, Action callback)
        {
            return Add(0, timeByFunc, callback, null);
        }


        /// <summary>
        /// Appends a timed callback.
        /// </summary>
        public TeaTime Add(float timeDelay, Action<ttHandler> callback)
        {
            return Add(timeDelay, null, null, callback);
        }


        /// <summary>
        /// Appends a timed callback.
        /// </summary>
        public TeaTime Add(Func<float> timeByFunc, Action<ttHandler> callback)
        {
            return Add(0, timeByFunc, null, callback);
        }


        /// <summary>
        /// Appends a time delay.
        /// </summary>
        public TeaTime Add(float timeDelay)
        {
            return Add(timeDelay, null, null, null);
        }


        /// <summary>
        /// Appends a time delay.
        /// </summary>
        public TeaTime Add(Func<float> timeByFunc)
        {
            return Add(0, timeByFunc, null, null);
        }


        /// <summary>
        /// Appends a callback.
        /// </summary>
        public TeaTime Add(Action callback)
        {
            return Add(0, null, callback, null);
        }


        /// <summary>
        /// Appends a callback.
        /// </summary>
        public TeaTime Add(Action<ttHandler> callback)
        {
            return Add(0, null, null, callback);
        }


        // @
        // LOOP


        /// <summary>
        /// Appends a callback loop (if duration is less than 0, the loop runs
        /// infinitely).
        /// </summary>
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


        /// <summary>
        /// Appends a callback loop (if duration is less than 0,
        /// the loop runs infinitely).
        /// </summary>
        public TeaTime Loop(float duration, Action<ttHandler> callback)
        {
            return Loop(duration, null, callback);
        }


        /// <summary>
        /// Appends a callback loop (if duration is less than 0,
        /// the loop runs infinitely).
        /// </summary>
        public TeaTime Loop(Func<float> durationByFunc, Action<ttHandler> callback)
        {
            return Loop(0, durationByFunc, callback);
        }


        /// <summary>
        /// Appends an infinite callback loop.
        /// </summary>
        public TeaTime Loop(Action<ttHandler> callback)
        {
            return Loop(-1, null, callback);
        }


        // @
        // QUEUE MODES


        /// <summary>
        /// Enables Immutable mode, the queue will ignore new appends (Add,
        /// Loop, If).
        /// </summary>
        public TeaTime Immutable()
        {
            _isImmutable = true;

            return this;
        }


        /// <summary>
        /// Enables Repeat mode, the queue will always be restarted on
        /// completion.
        /// </summary>
        public TeaTime Repeat()
        {
            _isRepeating = true;

            return this;
        }


        /// <summary>
        /// Enables Consume mode, the queue will remove each callback after
        /// execution.
        /// </summary>
        public TeaTime Consume()
        {
            _isConsuming = true;

            return this;
        }


        /// <summary>
        /// Disables all modes (Immutable, Repeat, Consume).
        /// </summary>
        public TeaTime Release()
        {
            _isImmutable = _isRepeating = _isConsuming = false;

            return this;
        }


        // @
        // CONTROL


        /// <summary>
        /// Pauses the queue execution (use .Play() to resume).
        /// </summary>
        public TeaTime Pause()
        {
            _isPaused = true;

            return this;
        }


        /// <summary>
        /// Stops the queue execution (use .Play() to start over).
        /// </summary>
        public TeaTime Stop()
        {
            if (_currentCoroutine != null)
                _instance.StopCoroutine(_currentCoroutine);

            _currentTask = 0;
            _isPlaying = false;


            return this;
        }


        /// <summary>
        /// Starts and resumes the queue execution.
        /// </summary>
        public TeaTime Play()
        {
            // Unpause always
            _isPaused = false;


            // Ignore if currently playing
            if (_isPlaying)
                return this;


            // Restart if already finished
            if (_currentTask >= _tasks.Count)
                _currentTask = 0;


            // Execute!
            _currentCoroutine = _instance.StartCoroutine(ExecuteQueue());


            return this;
        }


        /// <summary>
        /// Restarts the queue execution (Stop + Play).
        /// </summary>
        public TeaTime Restart()
        {
            // Alias
            return this.Stop().Play();
        }


        // @
        // DESTRUCTION


        /// <summary>
        /// Stops and cleans the queue, turning off all modes (Immutable,
        /// Repeat, Consume). Just like new.
        /// </summary>
        public TeaTime Reset()
        {
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

            return this;
        }


        // @
        // SPECIAL


        /// <summary>
        /// Appends a boolean condition that stops the queue when isn't
        /// fullfiled. On Repeat mode the queue is restarted. The interruption
        /// also affects Consume mode (no execution, no removal).
        /// </summary>
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


        // @
        // CUSTOM YIELDS


        /// <summary>
        /// IEnumerator that waits the completion of a TeaTime.
        /// </summary>
        private IEnumerator WaitForCompletion(TeaTime tt)
        {
            while (!tt.IsCompleted) yield return null;
        }

        /// <summary>
        /// Returns a YieldInstruction that waits until the queue is completed.
        /// </summary>
        public YieldInstruction WaitForCompletion()
        {
            return _instance.StartCoroutine(WaitForCompletion(this));
        }


        // @
        // THE COROUTINE


        /// <summary>
        /// This is the main algorithm. Executes all tasks, one after the
        /// other, calling their callbacks according to type, time and queue
        /// config.
        /// </summary>
        private IEnumerator ExecuteQueue()
        {
            _isPlaying = true;


            while (_currentTask < _tasks.Count)
            {
                // Current
                ttTask currentTask = _tasks[_currentTask];

                // Next
                _currentTask += 1;


                // Let's wait
                // 1 For secuencial Adds or Loops before their first execution
                // 2 Maybe a callback is trying to modify his own queue
                yield return ttYield.EndOfFrame;


                // It's a loop
                if (currentTask.isLoop)
                {
                    // Holds the duration
                    float loopDuration = currentTask.time;

                    // Func<float> is added
                    if (currentTask.timeByFunc != null)
                        loopDuration += currentTask.timeByFunc();

                    // Nothing to do, skip
                    if (loopDuration == 0)
                        continue;


                    // Loops will always need a handler
                    ttHandler loopHandler = new ttHandler();
                    loopHandler.self = this;
                    loopHandler.isLooping = true;


                    // Negative time means the loop is infinite
                    bool isInfinite = loopDuration < 0;

                    // T quotient
                    float tRate = isInfinite ? 0 : 1 / loopDuration;

                    // While looping and, until time or infinite
                    while (loopHandler.isLooping && loopHandler.t <= 1)
                    {
                        float unityDeltaTime = Time.deltaTime;

                        // Completion % from 0 to 1
                        if (!isInfinite)
                            loopHandler.t += tRate * unityDeltaTime;

                        // On finite loops this .deltaTime is sincronized with
                        // the exact loop duration
                        loopHandler.deltaTime =
                            isInfinite
                            ? unityDeltaTime
                            : 1 / (loopDuration - loopHandler.timeSinceStart) * unityDeltaTime;

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
                }
                // It's a timed callback
                else
                {
                    // Holds the delay
                    float delayDuration = currentTask.time;

                    // Func<float> is added
                    if (currentTask.timeByFunc != null)
                        delayDuration += currentTask.timeByFunc();

                    // Time delay
                    if (delayDuration > 0)
                        yield return ttYield.Seconds(delayDuration);


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
                }


                // Consume mode removes the task after execution
                if (_isConsuming)
                {
                    _currentTask -= 1;
                    _tasks.Remove(currentTask);
                }
            }


            // Repeats on Repeat mode (if needed)
            if (_isRepeating && _tasks.Count > 0)
            {
                _currentTask = 0;
                _currentCoroutine = _instance.StartCoroutine(ExecuteQueue());
            }
            // Done!
            else
            {
                _isPlaying = false;
            }


            yield return null;
        }
    }
}
