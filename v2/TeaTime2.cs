
// 2015/09/15 12:28:33 PM

// A new try on the same concept, but a lot better.


using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class TeaTask2
{
	public bool isLoop = false;

	public float time = 0;
	public YieldInstruction yieldInstruction = null;
	public Action callback = null;
	public Action<TeaHandler2> callbackWithHandler = null;
}


public class TeaHandler2
{
	public bool isActive = false;

	public float t = 0;
	public float deltaTime = 0;
	public float timeSinceStart = 0;

	public List<YieldInstruction> yieldsToWait;
	public List<IEnumerator> ienumsToWait;


	public void Break()
	{
		isActive = false;
	}


	public void WaitFor(YieldInstruction yi)
	{
		if (yieldsToWait == null)
			yieldsToWait = new List<YieldInstruction>();

		yieldsToWait.Add(yi);
	}


	public void WaitFor(IEnumerator ie)
	{
		if (ienumsToWait == null)
			ienumsToWait = new List<IEnumerator>();

		ienumsToWait.Add(ie);
	}


	public void WaitFor(float time)
	{
		WaitFor(new WaitForSeconds(time));
	}
}


public class TeaTime2
{
	private List<TeaTask2> tasks = new List<TeaTask2>();
	private int nextTask = 0;

	private MonoBehaviour instance = null;
	private Coroutine currentCoroutine = null;

	private bool _isPlaying;
	private bool _isPaused;
	private bool _isWaiting;
	private bool _isRepeating;


	public bool isPlaying
	{
		get { return _isPlaying; }
	}


	public TeaTime2(MonoBehaviour instance)
	{
		this.instance = instance;
	}


	// >
	// ADD


	private TeaTime2 Add(float timeDelay, Action callback, Action<TeaHandler2> callbackWithHandler)
	{
		// Ignore during Wait or Repeat mode
		if (_isWaiting || _isRepeating)
			return this;


		TeaTask2 newTask = new TeaTask2();
		newTask.isLoop = false;
		newTask.time = timeDelay;
		newTask.callback = callback;
		newTask.callbackWithHandler = callbackWithHandler;

		tasks.Add(newTask);

		return this;
	}


	public TeaTime2 Add(float timeDelay, Action callback)
	{
		return this.Add(timeDelay, callback, null);
	}

	public TeaTime2 Add(float timeDelay, Action<TeaHandler2> callback)
	{
		return this.Add(timeDelay, null, callback);
	}

	public TeaTime2 Add(float timeDelay)
	{
		return this.Add(timeDelay, null, null);
	}

	public TeaTime2 Add(Action callback)
	{
		return this.Add(0, callback, null);
	}

	public TeaTime2 Add(Action<TeaHandler2> callback)
	{
		return this.Add(0, null, callback);
	}


	// >
	// LOOP


	public TeaTime2 Loop(float duration, Action callback)
	{
		// Ignore during Wait or Repeat mode
		if (_isWaiting || _isRepeating)
			return this;


		TeaTask2 newTask = new TeaTask2();
		newTask.isLoop = true;
		newTask.time = duration;
		newTask.callback = callback;

		tasks.Add(newTask);

		return this;
	}


	// >
	// PLAY MODES


	public TeaTime2 Wait()
	{
		_isWaiting = true;

		return this;
	}

	public TeaTime2 Repeat()
	{
		_isRepeating = true;

		return this;
	}

	public TeaTime2 Unlock()
	{
		_isWaiting = _isRepeating = false;

		return this;
	}


	// >
	// CONTROL


	public TeaTime2 Pause()
	{
		_isPaused = true;

		return this;
	}


	public TeaTime2 Stop()
	{
		if (currentCoroutine != null)
			instance.StopCoroutine(currentCoroutine);

		_isPlaying = false;
		_isPaused = true;

		nextTask = 0;

		return this;
	}


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


	public TeaTime2 Restart()
	{
		// Alias
		return this.Stop().Play();
	}


	// >
	// DESTRUCTION


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


	IEnumerator ExecuteQueue()
	{
		_isPlaying = true;


		while (nextTask < tasks.Count)
		{
			// Current
			TeaTask2 currentTask = tasks[nextTask];


			// LOOP
			if (currentTask.isLoop)
			{
				// Loops always have a handler
				TeaHandler2 loopHandler = new TeaHandler2();
				loopHandler.isActive = true;


				// While active and until time
				float tRate = 1 / currentTask.time;
				while (loopHandler.isActive && loopHandler.t <= 1)
				{
					float unityDeltatime = Time.deltaTime;

					// Completion rate from 0 to 1
					loopHandler.t += tRate * unityDeltatime;

					// Customized time delta representing the loop duration
					loopHandler.deltaTime = 1 / (currentTask.time - loopHandler.timeSinceStart) * unityDeltatime;

					loopHandler.timeSinceStart += unityDeltatime;


					// Pause?
					while (_isPaused)
						yield return null;


					// Loops always have a callback
					currentTask.callbackWithHandler(loopHandler);


					// Wait for
					if (loopHandler.yieldsToWait != null)
					{
						foreach (YieldInstruction yi in loopHandler.yieldsToWait)
							yield return yi;

						loopHandler.yieldsToWait.Clear();
					}

					if (loopHandler.ienumsToWait != null)
					{
						foreach (IEnumerator ie in loopHandler.ienumsToWait)
							yield return instance.StartCoroutine(ie);

						loopHandler.ienumsToWait.Clear();
					}


					yield return null;
				}
			}

			// NON LOOP
			else
			{
				// Time delay
				if (currentTask.time > 0)
					yield return new WaitForSeconds(currentTask.time);


				// Pause?
				while (_isPaused)
					yield return null;


				// Normal callbacks
				if (currentTask.callback != null)
					currentTask.callback();


				// Callbacks with handlers
				if (currentTask.callbackWithHandler != null)
				{
					TeaHandler2 handler = new TeaHandler2();

					handler.isActive = true;
					handler.t = 1;
					handler.timeSinceStart = currentTask.time;
					handler.deltaTime = Time.deltaTime;

					currentTask.callbackWithHandler(handler);


					// Wait for
					if (handler.yieldsToWait != null)
					{
						foreach (YieldInstruction yi in handler.yieldsToWait)
							yield return yi;

						handler.yieldsToWait.Clear();
					}

					if (handler.ienumsToWait != null)
					{
						foreach (IEnumerator ie in handler.ienumsToWait)
							yield return instance.StartCoroutine(ie);

						handler.ienumsToWait.Clear();
					}
				}


				// Wait frame
				if (currentTask.time <= 0)
					yield return null;
			}


			// Next
			nextTask += 1;
		}


		// Repeat if repeat mode
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


public static class TeaTimeExtensions
{
	public static TeaTime2 TeaTime2(this MonoBehaviour instance)
	{
		return new TeaTime2(instance);
	}
}
