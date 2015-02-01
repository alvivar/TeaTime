###TeaTime v0.5.7 alpha 

_[DOWNLOAD](http://github.com/alvivar/TeaTime/raw/master/TeaTime.zip)_


TeaTime is a fast & simple queue for timed callbacks, fashioned as a
MonoBehaviour extension set, focused on solving common coroutines patterns in
Unity games.

Just put **'TeaTime.cs'** somewhere in your project and call it inside any
MonoBehaviour using **'this.tt'**.


	this.ttAdd("QueueName", 2, () =>
	{
		Debug.Log("2 seconds since QueueName started " + Time.time);
	})
	.ttLoop(3, delegate(ttHandler loop)
	{
		// This loop will run frame by frame for all his duration (3s) using
		// loop.deltaTime (a custom delta) for a timed interpolation.
		Camera.main.backgroundColor 
			= Color.Lerp(Camera.main.backgroundColor, Color.white, loop.deltaTime);
	})
	this.ttAdd("delegate(ttHandler t)
	{
		Sequence myTween = DOTween.Sequence();
		myTween.Append(transform.DOMoveX(5, 2.5f));
		myTween.Append(transform.DOMoveX(-5, 2.5f));

		// WaitFor waits for a time or YieldInstruction after the current
		// callback is done and before the next queued callback.
		t.WaitFor(myTween.WaitForCompletion());
	})
	.ttAdd(() =>
	{
		Debug.Log("10 seconds since QueueName started " + Time.time);
	})
	.ttNow(1, () =>
	{
		Debug.Log("ttNow is arbitrary and ignores the queue order " + Time.time);
	})
	.ttWait(); 
	// ttWait locks the current queue, ignoring new appends until all callbacks are
	// done.

	// And finally, ttReset let you stop a running queue, and just like ttNow,
	// is immediate and ignores the queue order.
	this.ttReset("QueueName");


Check out
**[Examples.cs](http://github.com/alvivar/TeaTime/blob/master/Examples.cs)**
for a more depth explanation. *(More patterns and examples to come)*

Important details to remember:
- Execution starts immediately
- Queues are unique to his MonoBehaviour (this is an extension after all)
- Naming your queue is recommended if you want to use more than one queue with safety
- You can use a YieldInstruction instead of time (i.e. WaitForEndOfFrame)
- ttWait ensures a complete and safe run during continuous calls
- ttHandler adds special control features to your callbacks
- You can create tween-like behaviours mixing loops, ttHandler.deltaTime and lerp functions
- ttHandler.waitFor applies only once and at the end of the current callback
- Both ttNow & ttReset runs immediately (ignoring the queue order)
- Below the sugar, everything runs on Unity coroutines!

And that's it!

By **[Andrés Villalobos](http://twitter.com/matnesis)**, special thanks to
**[Antonio Zamora](http://twitter.com/tzamora)**.

> 2014/12/26 12:21 am
