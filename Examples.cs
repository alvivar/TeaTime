using UnityEngine;


public class Examples : MonoBehaviour
{
    void Start()
    {
        // TeaTime is a fast & simple queue for timed callbacks, fashioned as
        // a MonoBehaviour extension set, focused on solving common coroutines
        // patterns in Unity games.

        // Just put 'TeaTime.cs' somewhere in your project and call it inside
        // any MonoBehaviour using 'this.tt' (and trust the autocomplete).



        // EXAMPLE
        // Invoke-like Timer
        // 3 seconds callback timer.

        this.tt().ttAdd(3, delegate()
        {
            Debug.Log("Invoke-like Timer: 3 seconds have passed " + Time.time);
        });

        // > tt(string queueName = null) Creates or changes the current queue.
        // When used without queue name the queue will be anonymous and
        // untrackable.

        // > ttAdd(float timeDelay, Action callback) Appends a timed callback
        // into the current queue.



        // EXAMPLE
        // InvokeRepeating-like Timer
        // Repeats the callback every 9s, infinitely.

        this.tt().ttAdd(9, delegate()
        {
            Debug.Log("InvokeRepeating-like Timer: 9 seconds have passed (repeating) " + Time.time);
        })
        .ttRepeat(-1); // Repeat infinitely

        // > ttRepeat(int n = 1) Repeats the current queue n times or infinite
        // (n <= -1).



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

        // > ttLoop(float duration, Action<ttHandler> callback) Appends into
        // the current queue a callback that runs frame by frame for all his
        // duration, or until ttHandler.Break(). ttLoop always requires
        // ttHandler to work.

        // ttHandler adds extra functionality to your callbacks. In this
        // example .deltaTime is a customized delta that represents the exact
        // the loop duration.
    }


    void Update()
    {
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

        // > ttWait() Locks the current queue ignoring new appends until all
        // his callbacks are completed (i.e. WaitForCompletion). Wait for
        // completion. Locks the current queue ignoring new appends until all
        // his callbacks are completed.



        // EXAMPLE
        // Lerp Overwrite
        // Moves from Vector3.zero to Vector3(5, 5, 5) in 3 seconds, overwriting itselfs (reset) when recalled.
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // The queue is selected, then resetted by using ttReset() after.
            this.tt("LerpOverwrite").ttReset().ttAdd(delegate()
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

        // > ttReset() Stops and resets the current queue.
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
