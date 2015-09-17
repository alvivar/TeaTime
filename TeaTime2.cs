
// TeaTime v0.7 alpha

// TeaTime is a fast & simple queue for timed callbacks, focused on solving
// common coroutines patterns in Unity games.

// By Andrés Villalobos ^ twitter.com/matnesis ^ andresalvivar@gmail.com
// Created 2014/12/26 12:21 AM ^ Rewritten 2015/09/15 12:28 PM


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


namespace TT2
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;


	/// <summary>
	/// Timed task node.
	/// </summary>
	public class TeaTask2
	{
		public bool isLoop = false;

		public float time = 0;
		public YieldInstruction yieldInstruction = null;
		public Action callback = null;
		public Action<TeaHandler2> callbackWithHandler = null;
	}


	/// <summary>
	/// Special handler on callbacks.
	/// </summary>
	public class TeaHandler2
	{
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
	/// Special TeaTime related extensions.
	/// </summary>
	public static class TeaTimeExtensions
	{
		/// <summary>
		/// Returns a new TeaTime queue ready to be used. This is basically a
		/// shorcut to 'new TeaTime2(this);'.
		/// </summary>
		public static TeaTime2 TeaTime2(this MonoBehaviour instance)
		{
			return new TeaTime2(instance);
		}
	}


	/// <summary>
	/// TeaTime is a fast & simple queue for timed callbacks, focused on
	/// solving coroutines patterns in Unity games.
	/// </summary>
	public class TeaTime2
	{
		// Queue
		private List<TeaTask2> tasks = new List<TeaTask2>(); 	// Tasks list used as a queue
		private int nextTask = 0; 								// Current task marker (to be executed)


		// Dependencies
		private MonoBehaviour instance = null; 		// Required to access .StartCoroutine( for the
		private Coroutine currentCoroutine = null; 	// Coroutine that holds the queue execution


		// States
		private bool _isPlaying; 	// True while executing the queue
		private bool _isPaused; 	// On Pause()
		private bool _isWaiting; 	// On Wait()
		private bool _isRepeating; 	// On Repeat()


		// Info
		public bool isPlaying
		{
			get { return _isPlaying; }
		}

		public bool isCompleted
		{
			get { return nextTask >= tasks.Count; }
		}


		/// <summary>
		/// A TeaTime queue requires a MonoBehaviour instance to use Coroutines.
		/// </summary>
		public TeaTime2(MonoBehaviour instance)
		{
			this.instance = instance;
		}


		// ADD CALLBACK
		// >


		/// <summary>
		/// Appends a new task.
		/// </summary>
		private TeaTime2 Add(float timeDelay, YieldInstruction yi, Action callback, Action<TeaHandler2> callbackWithHandler)
		{
			// Ignore during Wait or Repeat mode
			if (_isWaiting || _isRepeating)
				return this;


			TeaTask2 newTask = new TeaTask2();
			newTask.time = timeDelay;
			newTask.yieldInstruction = yi;
			newTask.callback = callback;
			newTask.callbackWithHandler = callbackWithHandler;

			tasks.Add(newTask);

			return this;
		}


		/// <summary>
		/// Appends a timed callback.
		/// </summary>
		public TeaTime2 Add(float timeDelay, Action callback)
		{
			return Add(timeDelay, null, callback, null);
		}


		/// <summary>
		/// Appends a timed callback.
		/// </summary>
		public TeaTime2 Add(YieldInstruction yi, Action callback)
		{
			return Add(0, yi, callback, null);
		}


		/// <summary>
		/// Appends a timed callback.
		/// </summary>
		public TeaTime2 Add(float timeDelay, Action<TeaHandler2> callback)
		{
			return Add(timeDelay, null, null, callback);
		}


		/// <summary>
		/// Appends a timed callback.
		/// </summary>
		public TeaTime2 Add(YieldInstruction yi, Action<TeaHandler2> callback)
		{
			return Add(0, yi, null, callback);
		}


		/// <summary>
		/// Appends a time delay.
		/// </summary>
		public TeaTime2 Add(float timeDelay)
		{
			return Add(timeDelay, null, null, null);
		}


		/// <summary>
		/// Appends a time delay.
		/// </summary>
		public TeaTime2 Add(YieldInstruction yi)
		{
			return Add(0, yi, null, null);
		}


		/// <summary>
		/// Appends a callback.
		/// </summary>
		public TeaTime2 Add(Action callback)
		{
			return Add(0, null, callback, null);
		}


		/// <summary>
		/// Appends a callback.
		/// </summary>
		public TeaTime2 Add(Action<TeaHandler2> callback)
		{
			return Add(0, null, null, callback);
		}


		// LOOP
		// >


		/// <summary>
		/// Appends a callback loop (if duration is less than 0,
		/// the loop will be infinite).
		/// </summary>
		public TeaTime2 Loop(float duration, Action<TeaHandler2> callback)
		{
			// Ignore during Wait or Repeat mode
			if (_isWaiting || _isRepeating)
				return this;


			TeaTask2 newTask = new TeaTask2();
			newTask.isLoop = true;
			newTask.time = duration;
			newTask.callbackWithHandler = callback;

			tasks.Add(newTask);

			return this;
		}


		/// <summary>
		/// Appends an infinite callback loop.
		/// </summary>
		public TeaTime2 Loop(Action<TeaHandler2> callback)
		{
			return Loop(-1, callback);
		}


		// PLAY MODES
		// >


		/// <summary>
		/// Enables Wait mode, ignoring new appends (Add, Loop) until
		/// everything is completed.
		/// </summary>
		public TeaTime2 Wait()
		{
			_isWaiting = true;

			return this;
		}


		/// <summary>
		/// Enables Repeat mode, the queue will always be restarted on
		/// completion.
		/// </summary>
		public TeaTime2 Repeat()
		{
			_isRepeating = true;

			return this;
		}


		/// <summary>
		/// Disables both Wait and Repeat mode.
		/// </summary>
		public TeaTime2 Unlock()
		{
			_isWaiting = _isRepeating = false;

			return this;
		}


		// CONTROL
		// >


		/// <summary>
		/// Pauses the queue execution (use Play to resume).
		/// </summary>
		public TeaTime2 Pause()
		{
			_isPaused = true;

			return this;
		}


		/// <summary>
		/// Stops the queue execution (use Play to start over).
		/// </summary>
		public TeaTime2 Stop()
		{
			if (currentCoroutine != null)
				instance.StopCoroutine(currentCoroutine);

			_isPlaying = false;
			_isPaused = true;

			nextTask = 0;

			return this;
		}


		/// <summary>
		/// Starts / Resumes the queue execution.
		/// </summary>
		public TeaTime2 Play()
		{
			// Unpause always
			_isPaused = false;


			// Ignore if currently playing
			if (_isPlaying)
				return this;


			// Restart if already finished
			if (nextTask >= tasks.Count)
				nextTask = 0;


			// Execute!
			currentCoroutine = instance.StartCoroutine(ExecuteQueue());


			return this;
		}


		/// <summary>
		/// Restart the queue execution (Stop + Play).
		/// </summary>
		public TeaTime2 Restart()
		{
			// Alias
			return this.Stop().Play();
		}


		// >
		// DESTRUCTION


		/// <summary>
		/// Stops and cleans the queue.
		/// </summary>
		public TeaTime2 Reset()
		{
			if (currentCoroutine != null)
				instance.StopCoroutine(currentCoroutine);

			tasks.Clear();
			nextTask = 0;

			_isPlaying = false;
			_isPaused = false;
			_isWaiting = false;
			_isRepeating = false;

			return this;
		}


		// >
		// COROUTINE


		/// <summary>
		/// This is the main algorithm. Executes all tasks, one after the
		/// other, calling their callbacks according to type, time and config.
		/// </summary>
		IEnumerator ExecuteQueue()
		{
			_isPlaying = true;


			while (nextTask < tasks.Count)
			{
				TeaTask2 currentTask = tasks[nextTask];


				if (currentTask.isLoop)
				{
					// Nothing to do, skip
					if (currentTask.time == 0)
					{
						nextTask += 1;
						continue;
					}


					// Loops always need a handler
					TeaHandler2 loopHandler = new TeaHandler2();

					// Negative time means the loop is infinite
					bool isInfinite = currentTask.time < 0;

					// T quotient
					float tRate = isInfinite ? 0 : 1 / currentTask.time;

					// While active and, until time or infinite
					while (loopHandler.isActive && loopHandler.t <= 1)
					{
						float unityDeltatime = Time.deltaTime;

						// Completion % from 0 to 1
						if (!isInfinite)
							loopHandler.t += tRate * unityDeltatime;

						// On finite loops this deltaTime represents the exact
						// loop duration
						loopHandler.deltaTime =
						    isInfinite
						    ? unityDeltatime
						    : 1 / (currentTask.time - loopHandler.timeSinceStart) * unityDeltatime;

						loopHandler.timeSinceStart += unityDeltatime;


						// Pause?
						while (_isPaused)
							yield return null;


						// Loops always have a callback
						currentTask.callbackWithHandler(loopHandler);


						// Handler WaitFor
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
					if (currentTask.callbackWithHandler != null)
					{
						TeaHandler2 handler = new TeaHandler2();

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
					if (currentTask.time <= 0)
						yield return null;
				}


				// Next task
				nextTask += 1;
			}


			// Repeat if Repeat mode
			if (_isRepeating)
			{
				nextTask = 0;
				currentCoroutine = instance.StartCoroutine(ExecuteQueue());
			}
			else
			{
				_isPlaying = false;
			}


			yield return null;
		}
	}
}
