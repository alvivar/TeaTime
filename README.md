###TeaTime v0.5.2 alpha

TeaTime is a fast & simple queue for timed callbacks, fashioned as a
MonoBehaviour extension set, focused on solving common coroutines patterns in
Unity games.

Just put **'TeaTime.cs'** somewhere in your project and call it inside any
MonoBehaviour using **'this.tt'**.


	this.ttAdd("Queue name", 2, () =>
	{
		Debug.Log("2 seconds since start " + Time.time);
	})
	.ttLoop(3, delegate(ttHandler loop)
	{		
		// A loop will run frame by frame for all his duration. 
		// loop.t holds a custom delta for interpolation.

		Camera.main.backgroundColor = Color.Lerp(Color.black, Color.white, loop.t);
	})
	this.ttAdd("DOTween example", delegate(ttHandler tt)
	{
		Sequence myTween = DOTween.Sequence();
		myTween.Append(transform.DOMoveX(5, 2.5f));
		myTween.Append(transform.DOMoveX(-5, 2.5f));

		// Waits for a time or YieldInstruction after the current callback is
		// done and before the next queued callback.
		tt.WaitFor(myTween.WaitForCompletion());
	})
	.ttAdd(() =>
	{
		Debug.Log("myTween end, +5 secs " + Time.time);
	})
	.ttNow(1, () =>
	{
		Debug.Log("ttNow is arbitrary and ignores the queue " + Time.time);
	})
	.ttWaitForCompletion(); 
	// Locks the current queue, ignoring new appends until all callbacks are done.


Check out
**[Examples.cs](http://github.com/alvivar/TeaTime/blob/master/Examples.cs)**
for a more depth explanation. (*More patterns and examples to come.*)

Some important details:
- Execution starts immediately
- Queues are unique to his MonoBehaviour (this is an extension after all)
- Naming your queue is recommended if you want to use more than one queue with safety
- You can use a YieldInstruction instead of time (i.e. WaitForEndOfFrame)
- ttWaitForCompletion ensures a safe run during continuous calls
- ttHandler adds special control features to your callbacks
- waitFor from ttHandler applies only once at the end of the current callback
- ttNow will always ignore queues, it's a sexier Invoke(
- Below the sugar, everything runs on coroutines!

And that's it!

By **[Andrés Villalobos](http://twitter.com/matnesis)**, special thanks to
**[Antonio Zamora](http://twitter.com/tzamora)**.

> 2014/12/26 12:21 am
