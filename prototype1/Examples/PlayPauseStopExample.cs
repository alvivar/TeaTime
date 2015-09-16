using UnityEngine;


// Example: Activation / Deactivation

// Let's say you have a heavy calculation that you want to activate only when
// the gameObject is being observed by a camera.

// Usually this kind of behaviours are created during Update() and their
// execution is limited by flags, TeaTime gives you more control for this
// pattern.


public class PlayPauseStopExample : MonoBehaviour
{
	public float currentTime = 0;


	void Start()
	{
		this.tt("HeavyCalculation").ttStop().ttLoop((ttHandler loop) =>
		{
			currentTime += Time.time; // Please imagine some heavy stuff here :)
		})
		.ttWait();

		// + We are using .tt with name, this way we can identify this queue
		//   later.

		// + .ttStop() stops and pauses the queue. We are using it just after
		//   the name because the default behaviour of TeaTime is automatic
		//   execution, and we don't want that this time.

		// + .ttLoop() with just the callback is an infinite loop, we are
		//   putting our "HeavyCalculation" here.
	}


	void OnBecameVisible()
	{
		this.tt("HeavyCalculation").ttPlay();

		// + .ttPlay() will resume the current queue if paused.

		// REMEMBER OnBecameVisible() is called once every time the gameObject
		// starts being observed by the camera.
	}


	void OnBecameInvisible()
	{
		this.tt("HeavyCalculation").ttStop();

		// + .ttStop() stops and pauses the queue, just when

		// TIP Let's say you don't want to stop, that you don't want to
		// restart the queue, that you want a pause, well, you can use
		// .ttPause() just for that :)

		// REMEMBER OnBecameInvisible() is called once every time the
		// gameObject stops being observed by the camera.
	}
}
