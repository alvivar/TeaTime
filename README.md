###TeaTime v0.5.1 alpha

TeaTime is a fast & simple queue for timed callbacks, fashioned as a
MonoBehaviour extension set, focused on solving common coroutines patterns in
Unity games.

Just put 'TeaTime.cs' somewhere in your project and call it inside any
MonoBehaviour using 'this.tt'.


	this.ttAppend("Queue name", 2, () =>
	{
		Debug.Log("2 seconds since start " + Time.time);
	})
	.ttAppendLoop(3, delegate(LoopHandler loop)
	{
		// An append loop will run frame by frame for all his duration.
		// loop.t holds a custom delta for interpolation.
		Camera.main.backgroundColor = Color.Lerp(Color.black, Color.white, loop.t);
	})
	this.ttAppend("DOTween example", delegate(ttHandler tt)
	{
		Sequence myTween = DOTween.Sequence();
		myTween.Append(transform.DOMoveX(5, 2.5f));
		myTween.Append(transform.DOMoveX(-5, 2.5f));

		// Waits after this callback is done and before the next append
		// for a time or YieldInstruction.
		tt.WaitFor(myTween.WaitForCompletion());
	})
	.ttAppend(() =>
	{
		Debug.Log("myTween end, +5 secs " + Time.time);
	})
	.ttInvoke(1, () =>
	{
		Debug.Log("ttInvoke is arbitrary and ignores the queue " + Time.time);
	})
	.ttLock(); // Locks the queue, ignoring new appends until all callbacks are done.


Check out
[Examples.cs](http://github.com/alvivar/TeaTime/blob/master/Examples.cs) for a
more depth explanation. (*More patterns and examples to come.*)

Some important details:
- Execution starts immediately
- Naming a queue is highly recommended (but optional)
- Locking a queue ensures a safe run during continuous calls
- ttHandler adds special control features to your callbacks
- You can use a YieldInstruction instead of time (i.e. WaitForEndOfFrame)
- Queues are unique to his MonoBehaviour
- Below the sugar, everything runs with coroutines

And that's it!

By [Andrés Villalobos](http://twitter.com/matnesis) in collaboration with
[Antonio Zamora](http://twitter.com/tzamora).

> 2014/12/26 12:21 am
