
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
	public Action callback = null;
}


public class TeaHandler2
{
	public bool isActive = true;
	public float t = 0;
	public float deltaTime = 0;
	public float timeSinceStart = 0;
}


public class TeaTime2
{
	private List<TeaTask2> tasks = new List<TeaTask2>();
	private int nextTask = 0;

	private MonoBehaviour instance = null;
	private Coroutine currentCoroutine = null;

	private bool _isPlaying;
	private bool _isWaiting;
	private bool _isRepeating;
	private bool _isPaused;


	public bool isPlaying
	{
		get { return _isPlaying; }
	}


	public TeaTime2(MonoBehaviour instance)
	{
		this.instance = instance;
	}


	// >
	// QUEUE


	public TeaTime2 Add(float timeDelay, Action callback)
	{
		// Ignore during Wait or Repeat mode
		if (_isWaiting || _isRepeating)
			return this;


		TeaTask2 newTask = new TeaTask2();
		newTask.isLoop = false;
		newTask.time = timeDelay;
		newTask.callback = callback;

		tasks.Add(newTask);

		return this;
	}

	public TeaTime2 Add(float timeDelay)
	{
		return this.Add(timeDelay, null);
	}

	public TeaTime2 Add(Action callback)
	{
		return this.Add(0, callback);
	}


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
		{
			instance.StopCoroutine(currentCoroutine);
			currentCoroutine = null;
		}

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


		// Execute
		currentCoroutine = instance.StartCoroutine(ExecuteQueue());


		return this;
	}


	public TeaTime2 Restart()
	{
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
		_isWaiting = false;
		_isRepeating = false;
		_isPaused = false;

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
				TeaHandler2 loopHandler = new TeaHandler2();
				loopHandler.isActive = true;

				// Completion rate [0..1]
				float tRate = 1 / currentTask.time;


				while (loopHandler.isActive && tRate <= 1)
				{
					// deltaTime
					float unityDeltatime = Time.deltaTime;

					// Completion % from 0 to 1
					loopHandler.t += tRate * unityDeltatime;

					// Customized time delta representing the loop duration
					loopHandler.deltaTime = 1 / (currentTask.time - loopHandler.timeSinceStart) * unityDeltatime;
					loopHandler.timeSinceStart += unityDeltatime;


					// Loops will always have a callback
					currentTask.callback();


					// Pause?
					while (_isPaused)
						yield return null;


					yield return null;
				}
			}

			// NON LOOP
			else
			{
				// Time delay
				if (currentTask.time > 0)
				{
					yield return new WaitForSeconds(currentTask.time);
				}


				// Pause?
				while (_isPaused)
					yield return null;


				// Normal callbacks
				if (currentTask.callback != null)
					currentTask.callback();


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
