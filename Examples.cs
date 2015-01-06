using DG.Tweening; // This example requires DOTween.
using UnityEngine;


public class Examples : MonoBehaviour
{
    void Start()
    {
        // ttInvoke executes a timed callback ignoring queues. It's the fast bullet.
        this.ttInvoke(10, () =>
        {
            Debug.Log("ttInvoke +10 secs " + Time.time);
        });


        Test();
    }


    void Update()
    {
        Test();
    }


    void Test()
    {
        // ttAppend adds a timed callback into a queue, execution starts immediately and
        // all appends without name will be added in the last used queue (or default) to
        // be executed one after another.

        // In this example, the first callback is called after 1 second, and the second
        // callback 4 seconds after the first callback is completed (a chain of timed
        // callbacks).
        this.ttAppend("Seconds", 1, () =>
        {
            Debug.Log("+1 second " + Time.time);
        })
        .ttAppend(4, () =>
        {
            Debug.Log("+1 +4 seconds " + Time.time);
        })
        .ttLock();

        // All queues here, in Test(), are locked (ttLock). That's why they are safe to
        // run during Update. ttLock ensures that all new appends in the last used queue
        // are ignored until all his callbacks are completed.


        // AppendLoop executes his callback frame by frame for all his duration (like
        // some sort of controlled update). It requires ttHandler to work, where you can
        // access special properties, like 't', a custom delta for interpolations.

        // Append and AppendLoop can be mixed. In this example, a 3 second AppendLoop
        // will run using Lerp and 't' to change a color, then an Append without time (0)
        // marks the loop end. After that the same but backwards.
        this.ttAppendLoop("Background change", 3, delegate(ttHandler loop)
        {
            Camera.main.backgroundColor = Color.Lerp(Color.white, Color.black, loop.t);
        })
        .ttAppend(() =>
        {
            Debug.Log("Black, +3 secs " + Time.time);
        })
        .ttAppendLoop(3, delegate(ttHandler loop)
        {
            Camera.main.backgroundColor = Color.Lerp(Color.black, Color.white, loop.t);
        })
        .ttAppend(() =>
        {
            Debug.Log("White, +3 secs " + Time.time);
        })
        .ttLock();


        // You can also use ttHandler in a normal Append for the extra features. In this
        // example we are using 'WaitFor(' to wait for a YieldInstruction (e.g DOTween,
        // WaitForSeconds) after the callback is done, before the next queued callback.
        this.ttAppend("DOTween example", delegate(ttHandler tt)
        {
            Sequence myTween = DOTween.Sequence();
            myTween.Append(transform.DOMoveX(5, 2.5f));
            myTween.Append(transform.DOMoveX(-5, 2.5f));

            tt.WaitFor(myTween.WaitForCompletion());
        })
        .ttAppend(() =>
        {
            Debug.Log("myTween end, +5 secs " + Time.time);
        })
        .ttLock();


        // If you call an AppendLoop without time (or negative) the loop will be
        // infinite. In this case you can use 'timeSinceStart' and 'Break()' from
        // ttHandler to control the loop.
        this.ttAppendLoop("Timed by break", delegate(ttHandler tt)
        {
            if (tt.timeSinceStart > 2)
                tt.Break();
        })
        .ttAppend(() =>
        {
            Debug.Log("Break Loop, +2 " + Time.time);
        })
        .ttLock();


        // Some important details
        // - Execution starts immediately
        // - Locking a queue ensures a safe run during continuous calls
        // - Naming a queue is highly recommended (but optional)
        // - You can use a YieldInstruction instead of time in ttAppend (Dotween!)
        // - ttHandler adds special control features to your callbacks
        // - Queues are unique to his MonoBehaviour
    }
}