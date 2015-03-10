using DG.Tweening; // If you don't have DOTween, feel free to remove this line and his example.
using UnityEngine;


public class Examples : MonoBehaviour
{
    void Start()
    {
        // TeaTime is a fast & simple queue for timed callbacks, fashioned as a
        // MonoBehaviour extension set, focused on solving common coroutines patterns in
        // Unity games.

        // Just put 'TeaTime.cs' somewhere in your project and call it inside any
        // MonoBehaviour using 'this.tt' (and trust the autocomplete).


        // > tt(string queueName = null, bool resetQueue = false)
        // Creates or changes the current queue.
        // When used without name the queue will be anonymous and untrackable.
        // If 'resetQueue = true' the queue will be stopped and overwritten.

        // > ttAdd(float timeDelay, Action callback)
        // Appends a timed callback into the current queue.

        // EXAMPLE
        // Invoke-like Timer
        // 3 seconds callback timer.

        this.tt().ttAdd(3, delegate()
        {
            Debug.Log("3 seconds have passed since the queue started " + Time.time);
        });



        // > ttRepeat(int n = 1)
        // Repeats the current queue n times or infinite (n <= -1).

        // EXAMPLE
        // InvokeRepeating-like Timer
        // Repeats the callback every 9s, infinitely.

        this.tt().ttAdd(9, delegate()
        {
            Debug.Log("9 seconds have passed " + Time.time);
        })
        .ttRepeat(-1);



        // > ttLoop(float duration, Action<ttHandler> callback)
        // Appends into the current queue a callback that runs frame by frame for all his duration.

        // > ttHandler loop; loop.deltaTime
        // Special delta customized to represent the loop duration.

        // EXAMPLE
        // Tween-like Color Transition
        // Changes the camera background color to black in 3s, then to white in 4s, and repeats 2 more times.

        this.tt().ttLoop(3, delegate(ttHandler loop)
        {
            Camera.main.backgroundColor = Color.Lerp(Camera.main.backgroundColor, Color.black, loop.deltaTime);
        })
        .ttLoop(4, delegate(ttHandler loop)
        {
            Camera.main.backgroundColor = Color.Lerp(Camera.main.backgroundColor, Color.white, loop.deltaTime);
        })
        .ttRepeat(2).ttAdd(delegate()
        {
            Debug.Log("Background transitions end " + Time.time);
        });



        // **Details to remember**
        // - Execution starts immediately
        // - Queues are unique to his MonoBehaviour (this is an extension after all)
        // - Below the sugar, everything runs on Unity coroutines!

        // **Tips**
        // - You can create tween-like behaviours with loops and lerp functions
        // - Always name your queue if you want to use more than one queue with safety 
        // - You can use a YieldInstruction instead of time (e.g. WaitForEndOfFrame)

        // **About ttHandler**
        // - ttHandler adds special control features to all your callbacks
        // - ttHandler.deltaTime contains a customized deltaTime that represents the precise loop duration
        // - ttHandler.t contains the completion percentage expressed from 0 to 1 based on the loop duration
        // - ttHandler.waitFor( applies a wait interval once, at the end of the current callback

        // **Moar**
        // - tt( changes the current queue, reset it or create an anonymous queue
        // - ttWait() ensures a complete and safe run of the current queue (waits for completion)
        // - TeaTime.Reset( stops and resets queues and instances, TeaTime.ResetAll( resets everything

        // And that's it!
    }
}
