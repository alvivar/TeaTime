###TeaTime v0.7 alpha

_[DOWNLOAD](http://github.com/alvivar/TeaTime/raw/master/TeaTime.zip)_


TeaTime is a fast & simple queue for timed callbacks, focused on solving
common coroutines patterns in Unity games.


	// In MonoBehaviours
	TeaTime queue = this.tt().Add(1, () =>
	{
		Debug.Log("Once second later! " + Time.time);
	})
	.Loop(3, (ttHandler loop) =>
	{
		// This will repeat per frame during the loop duration
		transform.position =
			Vector3.Lerp(
				transform.position,
				transform.position + Random.insideUnitSphere,
				loop.deltaTime // deltaTime sincronized with the loop duration
			);
	})
	.Add(() =>
	{
		Debug.Log("The loop is done! " + Time.time);
	})
	.Repeat(); // Repeat forever!

	// And
	queue.Pause();
	queue.Play();
	queue.Stop();
	queue.Reset();


By **[Andrés Villalobos](http://twitter.com/matnesis)**.

> Created 2014/12/26 12:21 am ^ Rewritten 2015/09/15 12:28 pm
