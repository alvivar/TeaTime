using DG.Tweening; // If you don't have DOTween, feel free to remove this line and his example.
using UnityEngine;


public class Examples : MonoBehaviour
{
    void Update()
    {
        Test();
    }


    void Test()
    {
        // 'ttAdd(' appends a timed callback into a queue, execution starts immediately and
        // all 'ttAdd(' without a name will be appended in the last used queue (or default)
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
        .ttWait();
        // 'ttWait()' locks the current queue ignoring new appends until all his
        // current callbacks are completed. That's why they are safe to run during Update
        // without over-appending, they just keep repeating themselves in order.


        // 'ttLoop(' executes his callback frame by frame for all his duration. It
        // requires 'ttHandler' to work, where you can access special properties, like a
        // custom delta for interpolations during the loop duration.

        // 'ttAdd(' and 'ttLoop(' can be mixed. In this example, a 3 seconds 'ttLoop(' will
        // run a Lerp using a special 'deltaTime' from 'ttHandler', customized to
        // represent the loop duration, then a 'ttAdd(' (0s) marks the loop end.
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
            Camera.main.backgroundColor = Color.Lerp(Color.black, Color.white, loop.t);
        })
        .ttAdd(() =>
        {
            Debug.Log("White, +3 secs " + Time.time);
        })
        .ttWait();
        // In the second 'Lerp' loop, instead of 'loop.deltaTime' we are using 'loop.t',
        // because 't' contains the completion percentage from 0 to 1 based on duration.
        // This is the precise value required when using a constant in the 'Lerp' 'from'.


        // You can also use 'ttHandler' in a normal 'ttAdd(' for extra features. In this
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
        .ttWait();


        // If you call 'ttLoop(' without time (or negative) the loop will be infinite. In
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
        .ttWait();


        // Alternatively to create or change your current queue by using the name
        // parameter in 'ttAdd(' or 'ttLoop(', there is also 'tt(' that does the same.
        this.tt("Queue named by tt").ttAdd(3, delegate()
        {
            // But, If you use 'tt()' without name, TeaTime will use an unknown and unique
            // identifier instead. The current queue will be anonymous and untrackable
            // (immune to 'ttWait'). Pretty useful to create simple timers.
            this.tt().ttAdd(4, () =>
            {
                Debug.Log("Out of sync timer, step 1 +4 " + Time.time);
            })
            .ttAdd(1, () =>
            {
                Debug.Log("Step 2 +1 " + Time.time);
            });
        })
        .ttWait();


        // And finally, 'TeaTime.Reset(' let you stop and clean a running queue or all
        // queues from an instance, and there is a 'TeaTime.ResetAll()' that cleans
        // everything.
        TeaTime.Reset(this, "QueueName");


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
        // - tt( allows to change the current queue, reset it or create an anonymous queue
        // - ttWait() ensures a complete and safe run of the current queue (waits for completion)
        // - TeaTime.Reset( stops and resets queues and instances, TeaTime.ResetAll( resets everything

        // And that's it!


        // By Andrés Villalobos [andresalvivar@gmail.com twitter.com/matnesis]
        // Special thanks to Antonio Zamora [twitter.com/tzamora] for the loop idea and testing.
        // Created 2014/12/26 12:21 am
    }
}
