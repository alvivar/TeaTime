using UnityEngine;


public class Examples : MonoBehaviour
{
    void Start()
    {
        this.ttAppend("Queue name", 2, () =>
        {
            Debug.Log("2 second since start " + Time.time);
        })
        .ttAppendLoop(3, delegate(LoopHandler loop)
        {
            // An append loop will run frame by frame for all his duration.
            // loop.t holds the fraction of time by frame.
            Camera.main.backgroundColor = Color.Lerp(Color.black, Color.white, loop.t);
        })
        .ttAppend(2, () =>
        {
            Debug.Log("The append loop started 5 seconds ago  " + Time.time);
        })
        .ttInvoke(1, () =>
        {
            Debug.Log("ttInvoke is arbitrary and ignores the queue " + Time.time);
        })
        .ttLock(); // Locks the queue, ignoring new appends until all callbacks are done.

        // And that's it!

        // Some details
        // - Execution starts inmediatly
        // - Locking a queue ensures a safe run during continuous calls
        // - Naming a queue is recommended, but optional
        // - You can use a YieldInstruction instead of time in ttAppend (Dotween!)
        // - Queues are unique to his MonoBehaviour
    }
}