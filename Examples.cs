// TeaTimer v0.3 Alpha
// Unity Extension Set for a quick coroutine/callback timer in MonoBehaviours.

// #author Andrés Villalobos
// #contact andresalvivar@gmail.com - twitter.com/matnesis
// #created 2014/12/26 12:21 am

// #usage Just put TeaTimer.cs somewhere in your folders and call it in a MonoBehaviour using 'this'.

// ttAppend
// Appends a timed callback into a queue to be executed in order.
this.ttAppend("SomeQueue", 1, () => Debug.Log("SomeQueue " + Time.time)); // Prints 1
this.ttAppend("SomeQueue", 2, () => Debug.Log("SomeQueue " + Time.time)); // Prints 3

// When called without a queue name, 
// the task is appended to the last named queue (or default).
this.ttAppend(2, () => Debug.Log("SomeQueue " + Time.time));  // Prints 5

// ttLock
// Locks the current queue until all his previous callbacks are done.
// Useful during cycles (e.g. Update) to avoid over appending callbacks.
void Update() 
{
    this.ttAppend("LockedQueue", 3, () => Debug.Log("LockedQueue " + Time.time));
	this.ttAppend("LockedQueue", 3, () => Debug.Log("LockedQueue " + Time.time));
	this.ttLock(); // 'LockedQueue' will run as a safe 2 step timer.
}

// ttNow
// Executes a timed callback ignoring queues.
this.ttNow(3, () => Debug.Log("ttNow " + Time.time)); // Prints 3

// Some details
// #1 Execution starts inmediatly
// #2 You can chain methods
// #3 You can use a YieldInstruction instead of time (e.g Dotween!)
// #4 Locking a queue ensures a safe timer during continuous calls
// #5 Queues are unique to his MonoBehaviour
// #6 If you never name a queue, an internal default will be used

// A classic chain example.
this.ttAppend("AnotherQueue", 1, () =>
{
    Debug.Log("AnotherQueue " + Time.time); // Prints 1
})
.ttNow(1, () =>
{
    Debug.Log("AnotherQueue ttNow ignores queues " + Time.time); // Prints 1
})
.ttAppend(0, () =>
{
    Debug.Log("AnotherQueue " + Time.time); // Prints 1
})
.ttAppend(transform.DOMoveX(1, 3).WaitForCompletion(), () => // Dotween WaitFor instead of time
{
    Debug.Log("AnotherQueue YieldInstruction " + Time.time); // Prints 3
})