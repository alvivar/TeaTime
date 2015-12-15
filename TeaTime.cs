
// TeaTime v0.7.3 beta

// TeaTime is a fast & simple queue for timed callbacks, focused on solving
// common coroutines patterns in Unity games.

// By Andrés Villalobos ^ twitter.com/matnesis ^ andresalvivar@gmail.com
// Created 2014/12/26 12:21 am ^ Rewritten 2015/09/15 12:28 pm


// Copyright (c) 2014/12/26 andresalvivar@gmail.com

// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.


namespace matnesis.TeaTime
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;


	/// <summary>
	/// Timed task node.
	/// </summary>
	public class ttTask
	{
		public bool isLoop = false;

		public float time = 0;
		public YieldInstruction yieldInstruction = null;

		public Action callback = null;
		public Action<ttHandler> callbackWithHandler = null;
	}


	/// <summary>
	/// Special handler on callbacks.
	/// </summary>
	public class ttHandler
	{
		public TeaTime self;

		public bool isActive = true;

		public float t = 0;
		public float deltaTime = 0;
		public float timeSinceStart = 0;

		public List<YieldInstruction> yieldsToWait = null;


		/// <summary>
		/// Breaks the current loop (isActive = false).
		/// </summary>
		public void Break()
		{
			isActive = false;
		}


		/// <summary>
		/// Appends a delay after the current callback execution.
		/// </summary>
		public void WaitFor(YieldInstruction yi)
		{
			if (yieldsToWait == null)
				yieldsToWait = new List<YieldInstruction>();

			yieldsToWait.Add(yi);
		}


		/// <summary>
		/// Appends a delay after the current callback execution.
		/// </summary>
		public void WaitFor(float time)
		{
			WaitFor(new WaitForSeconds(time));
		}
	}


	/// <summary>
	/// Special TeaTime extensions.
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
		/// access queues without a formal definition. Dark magic.
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
	/// TeaTime is a fast & simple queue for timed callbacks, focused on
	/// solving common coroutines patterns in Unity games.
	/// </summary>
	public class TeaTime
	{
		// Queue
		private List<ttTask> _tasks = new List<ttTask>(); // Tasks list used as a queue
		private int _currentTask = 0; // Current task mark (to be executed)


		// Dependencies
		private MonoBehaviour _instance = null; // Required to access .StartCoroutine( for the
		private Coroutine _currentCoroutine = null; // Coroutine that holds the queue execution


		// States
		private bool _isPlaying = false; // True while queue execution
		private bool _isPaused = false; // On .Pause()
		private bool _isWaiting = false; // On .Wait() mode
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
			get { return _currentTask >= _tasks.Count && !_isPlaying; }
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
		/// A TeaTime queue requires a MonoBehaviour instance to access his
		/// coroutine fuctions.
		/// </summary>
		public TeaTime(MonoBehaviour instance)
		{
			_instance = instance;
		}


		// ^
		// ADD


		/// <summary>
		/// Appends a new ttTask.
		/// </summary>
		private TeaTime Add(float timeDelay, YieldInstruction yi, Action callback, Action<ttHandler> callbackWithHandler)
		{
			// Ignores appends on Wait mode
			if (!_isWaiting)
			{
				ttTask newTask = new ttTask();
				newTask.time = timeDelay;
				newTask.yieldInstruction = yi;
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
		public TeaTime Add(YieldInstruction yi, Action callback)
		{
			return Add(0, yi, callback, null);
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
		public TeaTime Add(YieldInstruction yi, Action<ttHandler> callback)
		{
			return Add(0, yi, null, callback);
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
		public TeaTime Add(YieldInstruction yi)
		{
			return Add(0, yi, null, null);
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


		// ^
		// LOOP


		/// <summary>
		/// Appends a callback loop (if duration is less than 0,
		/// the loop runs infinitely).
		/// </summary>
		public TeaTime Loop(float duration, Action<ttHandler> callback)
		{
			// Ignores appends on Wait mode
			if (!_isWaiting)
			{
				ttTask newTask = new ttTask();
				newTask.isLoop = true;
				newTask.time = duration;
				newTask.callbackWithHandler = callback;

				_tasks.Add(newTask);
			}


			// Autoplay if not paused or playing
			return _isPaused || _isPlaying ? this : this.Play();
		}


		/// <summary>
		/// Appends an infinite callback loop.
		/// </summary>
		public TeaTime Loop(Action<ttHandler> callback)
		{
			return Loop(-1, callback);
		}


		// ^
		// QUEUE MODES


		/// <summary>
		/// Enables Wait mode, the queue will ignore new appends (Add, Loop, If).
		/// </summary>
		public TeaTime Wait()
		{
			_isWaiting = true;

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
		/// Disables all modes (Wait, Repeat, Consume).
		/// </summary>
		public TeaTime Unlock()
		{
			_isWaiting = _isRepeating = _isConsuming = false;

			return this;
		}


		// ^
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

			_isPlaying = false;
			_currentTask = 0;

			return this;
		}


		/// <summary>
		/// Starts / Resumes the queue execution.
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


		// ^
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


		// ^
		// DESTRUCTION


		/// <summary>
		/// Stops and cleans the queue.
		/// </summary>
		public TeaTime Reset()
		{
			if (_currentCoroutine != null)
				_instance.StopCoroutine(_currentCoroutine);

			_tasks.Clear();
			_currentTask = 0;

			_isPlaying = false;
			_isPaused = false;
			_isWaiting = false;
			_isRepeating = false;

			return this;
		}


		// ^
		// THE COROUTINE


		/// <summary>
		/// This is the main algorithm. Executes all tasks, one after the
		/// other, calling their callbacks according to type, time and queue
		/// config.
		/// </summary>
		IEnumerator ExecuteQueue()
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
				yield return new WaitForEndOfFrame();


				// It's a loop
				if (currentTask.isLoop)
				{
					// Nothing to do, skip
					if (currentTask.time == 0)
						continue;


					// Loops always need a handler
					ttHandler loopHandler = new ttHandler();
					loopHandler.self = this;

					// Negative time means the loop is infinite
					bool isInfinite = currentTask.time < 0;

					// T quotient
					float tRate = isInfinite ? 0 : 1 / currentTask.time;

					// While active and, until time or infinite
					while (loopHandler.isActive && loopHandler.t <= 1)
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
						    : 1 / (currentTask.time - loopHandler.timeSinceStart) * unityDeltaTime;

						loopHandler.timeSinceStart += unityDeltaTime;


						// Pause?
						while (_isPaused)
							yield return null;


						// Loops always have a callback with a handler
						currentTask.callbackWithHandler(loopHandler);


						// Handler .WaitFor(
						if (loopHandler.yieldsToWait != null)
						{
							foreach (YieldInstruction yi in loopHandler.yieldsToWait)
								yield return yi;

							loopHandler.yieldsToWait.Clear();
						}


						// Minimum sane delay
						if (loopHandler.yieldsToWait == null)
							yield return null;
					}
				}
				// It's a timed callback
				else
				{
					// Time delay
					if (currentTask.time > 0)
						yield return new WaitForSeconds(currentTask.time);


					// Yield delay
					if (currentTask.yieldInstruction != null)
						yield return currentTask.yieldInstruction;


					// Pause?
					while (_isPaused)
						yield return null;


					// Normal callback
					if (currentTask.callback != null)
						currentTask.callback();


					// Callback with handler
					ttHandler handler = new ttHandler();
					handler.self = this;

					if (currentTask.callbackWithHandler != null)
					{
						handler.isActive = true;
						handler.t = 1;
						handler.timeSinceStart = currentTask.time;
						handler.deltaTime = Time.deltaTime;

						currentTask.callbackWithHandler(handler);


						// Handler WaitFor
						if (handler.yieldsToWait != null)
						{
							foreach (YieldInstruction yi in handler.yieldsToWait)
								yield return yi;

							handler.yieldsToWait.Clear();
						}
					}


					// Minimum sane delay
					if (currentTask.time <= 0 && handler.yieldsToWait == null)
						yield return null;
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
