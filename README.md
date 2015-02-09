###TeaTime v0.5.9 alpha 

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
			= Color.Lerp(Camera.main.backgroundColor, Color.black, loop.deltaTime);
	})
	.ttAdd(() =>
	{
		Debug.Log("5 seconds since QueueName started " + Time.time);
	})


Check out
**[Examples.cs](http://github.com/alvivar/TeaTime/blob/master/Examples.cs)**
for all the features and explained examples. (*More to come!*)


**Details to remember**
- Execution starts immediately
- Queues are unique to his MonoBehaviour (this is an extension after all)
- Below the sugar, everything runs on Unity coroutines!

**Tips**
- You can create tween-like behaviours with loops and lerp functions
- Always name your queue if you want to use more than one queue with safety 
- You can use a YieldInstruction instead of time (e.g. WaitForEndOfFrame)

**About ttHandler**
- ttHandler adds special control features to all your callbacks
- ttHandler.deltaTime contains a customized deltaTime that represents the precise loop duration
- ttHandler.t contains the completion percentage expressed from 0 to 1 based on the loop duration
- ttHandler.waitFor( applies a wait interval once, at the end of the current callback

**Moar**
- tt( allows to change the current queue, reset it or create an anonymous queue
- ttWait() ensures a complete and safe run of the current queue (waits for completion)
- TeaTime.Reset( stops and resets queues and instances, TeaTime.ResetAll( resets everything

And that's it!


By **[Andrés Villalobos](http://twitter.com/matnesis)**, special thanks to
**[Antonio Zamora](http://twitter.com/tzamora)** for the loop idea and
testing.

> 2014/12/26 12:21 am
