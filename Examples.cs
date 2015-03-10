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


        // >> tt(string queueName = null, bool resetQueue = false)
        // Creates or changes the current queue.
        // When used without name the queue will be anonymous and untrackable.
        // If 'resetQueue = true' the queue will be stopped and overwritten.

        // >> ttAdd(float timeDelay, Action callback)
        // Appends a timed callback into the current queue.

        // == Example: Invoke-like Timer ==
        // 3 seconds callback timer.

        this.tt().ttAdd(3, delegate()
        {
            Debug.Log("3 seconds have passed since the queue started " + Time.time);
        });



        // > ttRepeat(n)
        // Repeats the current queue n times or infinite (n < 0).

        // == InvokeRepeating-like Timer ==
        // Repeats the callback every 9s, infinitely.

        this.tt().ttAdd(9, delegate()
        {
            Debug.Log("9 seconds have passed " + Time.time);
        })
        .ttRepeat(-1);



        // > ttLoop(
        // Appends into the current queue a callback that runs frame by frame for all his duration.

        // > ttHandler .deltaTime
        // Special delta customized for the loop duration

        // == Tween-like Color Transition ==
        // Changes the camera background color to black in 3s, then to white in 4s, and repeat 2 more times.

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
            Debug.Log("Background transitions just ended " + Time.time);
        });
    }


    void Update()
    {

    }
}
