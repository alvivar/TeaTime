###TeaTime v0.6 alpha 

_[DOWNLOAD](http://github.com/alvivar/TeaTime/raw/master/TeaTime.zip)_


TeaTime is a fast & simple queue for timed callbacks, fashioned as a
MonoBehaviour extension set, focused on solving common coroutines patterns in
Unity games.

Just put 'TeaTime.cs' somewhere in your project and call it inside any
MonoBehaviour using 'this.tt' (and trust the autocomplete).


	this.tt("QueueName").ttAdd(2, () =>
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
to learn. (*More to come!*)


By **[Andrés Villalobos](http://twitter.com/matnesis)**, special thanks to
**[Antonio Zamora](http://twitter.com/tzamora)** for the loop idea and
testing.

> 2014/12/26 12:21 am
