// Thank you Xerios! http://github.com/alvivar/TeaTime/pull/8

using UnityEngine;

public class ReverseYoyo : MonoBehaviour
{
    public Renderer renderr;

    private bool lastCompletion = false;
    private TeaTime queue;

    void Start()
    {
        queue = new TeaTime(this);

        // Adds a one second callback loop that lerps to a random color.
        queue
            .Add(() => Debug.Log($"Beginning {Time.time}"))
            .Loop(1, (TeaHandler t) =>
            {
                // From white to black, using .t (completion float from 0.0 to 1.0).
                renderr.material.color = Color.Lerp(
                    Color.white,
                    Color.black,
                    t.t);
            })
            .Loop(1, (TeaHandler t) =>
            {
                renderr.transform.localScale = Vector3.Lerp(
                    new Vector3(1, 1, 1),
                    new Vector3(3, 3, 3),
                    t.t);
            })
            .Add(() => Debug.Log($"End {Time.time}"))
            // .Repeat()
            .Yoyo();

        // Yoyo mode will .Reverse() the queue execution order when the queue is
        // completed.

        // Yoyo and Lerp works great by using TeaHandler.t because t goes from 0
        // to 1, and from 1 to 0 when reversed.
    }

    void Update()
    {
        // Here is where it gets interesting: Use K or J to change the direction
        // of the Yoyo realtime.

        // Go Backward
        if (Input.GetKeyDown(KeyCode.J))
            queue.Backward().Play();

        // Go Forward
        if (Input.GetKeyDown(KeyCode.K))
            queue.Forward().Play();

        // .IsCompleted log
        if (lastCompletion != queue.IsCompleted)
        {
            Debug.Log("Is completed? " + (queue.IsCompleted ? "YES" : "NO"));
            lastCompletion = queue.IsCompleted;
        }
    }
}

// 2016/04/29 05:36 PM