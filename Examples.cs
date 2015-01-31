using DG.Tweening; // If you don't have DOTween, feel free to remove this line and his example.
using System.Collections;
using UnityEngine;


public class Examples : MonoBehaviour
{
    void Start()
    {
        // 'ttNow' executes a timed callback ignoring queues. It's the fast bullet.
        this.ttNow(10, () =>
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
        // 'ttAdd' appends a timed callback into a queue, execution starts immediately and
        // all 'ttAdd' without a name will be appended in the last used queue (or default)
        // to be executed one after another.

        // In this example, the first callback is called after 1 second, and the second
        // callback 4 seconds after the first callback is completed (a chain of timed
        // callbacks).
        this.ttAdd("Seconds", 1, () =>
        {
            Debug.Log("+1 second " + Time.time);
        })
        .ttAdd(4, () =>
        {
            Debug.Log("+1 +4 seconds " + Time.time);
        })
        .ttWaitForCompletion();

        // 'ttWaitForCompletion' locks the current queue ignoring new appends until all his
        // current callbacks are completed. That's why they are safe to run during Update
        // without over-appending, they just keep repeating themselves in order.


        // 'ttLoop' executes his callback frame by frame for all his duration. It
        // requires 'ttHandler' to work, where you can access special properties, like a
        // custom delta for interpolations during the loop duration.

        // 'ttAdd' and 'ttLoop' can be mixed. In this example, a 3 seconds 'ttLoop' will
        // run using Lerp and the custom 'deltaTime' to change a color, then a 'ttAdd'
        // without time (0) marks the loop end. After that the same but backwards.
        this.ttLoop("Background change", 3, delegate(ttHandler loop)
        {
            Camera.main.backgroundColor = Color.Lerp(Camera.main.backgroundColor, Color.black, loop.deltaTime);
        })
        .ttAdd(() =>
        {
            Debug.Log("Black, +3 secs " + Time.time);
        })
        .ttLoop(3, delegate(ttHandler loop)
        {
            Camera.main.backgroundColor = Color.Lerp(Camera.main.backgroundColor, Color.white, loop.deltaTime);
        })
        .ttAdd(() =>
        {
            Debug.Log("White, +3 secs " + Time.time);
        })
        .ttWaitForCompletion();


        // You can also use 'ttHandler' in a normal 'ttAdd' for extra features. In this
        // example we are using 'WaitFor(' to wait for a YieldInstruction (e.g DOTween,
        // WaitForSeconds) after the callback is done and before the next queued callback.
        this.ttAdd("DOTween example", delegate(ttHandler t)
        {
            Sequence myTween = DOTween.Sequence();
            myTween.Append(transform.DOMoveX(5, 2.5f));
            myTween.Append(transform.DOMoveX(-5, 2.5f));

            t.WaitFor(myTween.WaitForCompletion());
        })
        .ttAdd(() =>
        {
            Debug.Log("myTween end, +5 secs " + Time.time);
        })
        .ttWaitForCompletion();


        // If you call 'ttLoop' without time (or negative) the loop will be infinite. In
        // this case you can use 'timeSinceStart' and 'Break(' from ttHandler to control
        // the loop.
        this.ttLoop("Timed by break", delegate(ttHandler t)
        {
            if (t.timeSinceStart > 2)
                t.Break();
        })
        .ttAdd(() =>
        {
            Debug.Log("Break Loop, +2 " + Time.time);
        })
        .ttWaitForCompletion();


        // Some important details:
        // - Execution starts immediately
        // - Queues are unique to his MonoBehaviour (this is an extension after all)
        // - Naming your queue is recommended if you want to use more than one queue with safety
        // - You can use a YieldInstruction instead of time (i.e. WaitForEndOfFrame)
        // - ttWaitForCompletion ensures a complete and safe run during continuous calls
        // - ttHandler adds special control features to your callbacks
        // - You can create tween-like behaviours mixing loops, ttHandler.deltaTime and Lerp functions
        // - ttHandler.waitFor applies only once and at the end of the current callback
        // - ttNow will always ignore queues (it's inmune to ttWaitForCompletion)
        // - Below the sugar, everything runs on coroutines!
    }
}
