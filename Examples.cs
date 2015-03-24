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
            Debug.Log("Invoke-like Timer: 3 seconds have passed " + Time.time);
        });



        // > ttRepeat(int n = 1)
        // Repeats the current queue n times or infinite (n <= -1).

        // EXAMPLE
        // InvokeRepeating-like Timer
        // Repeats the callback every 9s, infinitely.
        this.tt().ttAdd(9, delegate()
        {
            Debug.Log("InvokeRepeating-like Timer: 9 seconds have passed (repeating) " + Time.time);
        })
        .ttRepeat(-1); // Repeat infinitely



        // > ttLoop(float duration, Action<ttHandler> callback)
        // Appends into the current queue a callback that runs frame by frame for all his duration.

        // > ttHandler loop; loop.deltaTime
        // Customized delta that represents the loop duration.

        // EXAMPLE
        // Tween-like Color Transition
        // Changes the camera background color to black in 3s, then to gray in 4s, and repeats 2 more times.
        this.tt().ttLoop(3, delegate(ttHandler loop)
        {
            Camera.main.backgroundColor = Color.Lerp(Camera.main.backgroundColor, Color.black, loop.deltaTime);
        })
        .ttLoop(4, delegate(ttHandler loop)
        {
            Camera.main.backgroundColor = Color.Lerp(Camera.main.backgroundColor, Color.gray, loop.deltaTime);
        })
        .ttRepeat(2).ttAdd(delegate()
        {
            Debug.Log("Tween-like Color Transition: End " + Time.time);
        });
    }


    void Update()
    {
        // > ttWait()
        // Locks the current queue ignoring new appends until all his callbacks are completed (i.e. WaitForCompletion).
        // Wait for completion. Locks the current queue ignoring new appends until all his callbacks are completed.

        // EXAMPLE
        // Wait For Completion Lock
        // Executes a callback and waits 2s during continuous calls without over appending callbacks.
        if (Input.anyKey) // On any key hold
        {
            this.tt("QueueName").ttAdd(delegate()
            {
                Debug.Log("Wait For Completion Lock: " + Time.time);
            })
            .ttAdd(2) // 2s wait
            .ttWait();
            // ttWait() uses the queue name to set the lock, 
            // allowing tt("queueName") to wait for completion before appending new callbacks.
        }



        // EXAMPLE
        // Lerp Overwrite
        // Moves from Vector3.zero to Vector3(5, 5, 5) in 3 seconds, overwriting itselfs (reset) if recalled.
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // 'resetQueue' = true
            this.tt("LerpOverwrite", true).ttAdd(delegate()
            {
                transform.position = Vector3.zero;
            })
            .ttLoop(3, delegate(ttHandler loop)
            {
                transform.position = Vector3.Lerp(transform.position, new Vector3(5, 5, 5), loop.deltaTime);
            })
            .ttAdd(delegate()
            {
                Debug.Log("Lerp Overwrite: Done " + Time.time);
            });
        }
    }



    // TODO
    // Explain THIS with practical examples!

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
}
