###TeaTime v0.5.8.4 alpha 

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
		// ttLoop runs frame by frame for all his duration (3s) and his handler have a
		// customized delta (loop.deltaTime) that represents the precise loop duration.
		Camera.main.backgroundColor 
			= Color.Lerp(Camera.main.backgroundColor, Color.white, loop.deltaTime);
	})
	this.ttAdd("delegate(ttHandler t)
	{
		Sequence myTween = DOTween.Sequence();
		myTween.Append(transform.DOMoveX(5, 2.5f));
		myTween.Append(transform.DOMoveX(-5, 2.5f));

		// WaitFor waits for a time or YieldInstruction after the current callback is
		// done and before the next queued callback.		
		t.WaitFor(myTween.WaitForCompletion());
	})
	.ttAdd(() =>
	{
		Debug.Log("10 seconds since QueueName started " + Time.time);
	})
	.ttWait(); 
	// ttWait locks the current queue, ignoring new appends until all his callbacks
	// are done.

	// And finally, TeaTime.Reset( let you stop and clean a running queue or all
	// queues from an instance, and there is a TeaTime.ResetAll() that cleans
	// everything.
	TeaTime.Reset(this, "QueueName");

Check out
**[Examples.cs](http://github.com/alvivar/TeaTime/blob/master/Examples.cs)**
for a more depth explanation. *(More patterns and examples to come)*


**Details to remember**
- Execution starts immediately
- Queues are unique to his MonoBehaviour (this is an extension after all)
- Below the sugar, everything runs on Unity coroutines!

**Tips**
- Always name your queue if you want to use more than one queue with safety 
- You can use a YieldInstruction instead of time (e.g. WaitForEndOfFrame)
- You can create tween-like behaviours by mixing loops, ttHandler properties, and lerp functions
- ttWait ensures a complete and safe run during continuous calls

**About ttHandler**
- ttHandler adds special control features to your callbacks
- .deltaTime contains a customized deltaTime that represents the precise loop duration
- .t contains the completion percentage expresed from 0 to 1 based on the loop duration
- .waitFor( applies only once and at the end of the current callback

And that's it!


By **[Andrés Villalobos](http://twitter.com/matnesis)**, special thanks to
**[Antonio Zamora](http://twitter.com/tzamora)** for the loop idea and
testing.

> 2014/12/26 12:21 am
