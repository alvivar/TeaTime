// TeaTime Reverse() Backward() Forward() & Yoyo() mode example! <3

// Thank you Xerios! http://github.com/alvivar/TeaTime/pull/8

// 2016/04/29 05:36 PM

using UnityEngine;

public class TeaTimeReverse1 : MonoBehaviour
{
    public Renderer cubeRen;

    private bool lastCompletion = false;

    // Declare your queue
    TeaTime queue;

    void Start()
    {
        // Instantiate
        queue = new TeaTime(this);
        // or you can use this shortcut: 'queue = this.tt();' (special
        // MonoBehaviour extension)

        // Adds a one second callback loop that lerps to a random color.
        queue
            .Add(() => Debug.Log("Queue Beginning " + Time.time))
            .Loop(1, (ttHandler t) =>
            {
                // From white to black, using .t (completion float from 0.0 to 1.0)
                cubeRen.material.color = Color.Lerp(
                    Color.white,
                    Color.black,
                    t.t);
            })
            .Loop(1, (ttHandler t) =>
            {
                cubeRen.transform.localScale = Vector3.Lerp(
                    new Vector3(1, 1, 1),
                    new Vector3(3, 3, 3),
                    t.t);
            })
            .Add(() => Debug.Log("Queue End " + Time.time))
            .Yoyo();
        // Yoyo mode will .Reverse() the queue execution order when the queue is
        // completed
    }

    void Update()
    {
        // Go Forward
        if (Input.GetKeyDown(KeyCode.K))
            queue.Forward().Play();

        // Go Backward
        if (Input.GetKeyDown(KeyCode.J))
            queue.Backward().Play();

        // .IsCompleted log
        if (lastCompletion != queue.IsCompleted)
        {
            Debug.Log("Is completed? " + (queue.IsCompleted ? "YES" : "NO"));
            lastCompletion = queue.IsCompleted;
        }
    }
}