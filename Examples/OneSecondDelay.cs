// If every Debug.Log follows in order one after the other, with 1 second
// between them, then everything is fine!

// It's a different test on reverse.

using UnityEngine;

public class OneSecondDelay : MonoBehaviour
{
    TeaTime queue;

    void Start()
    {
        queue = this.tt().Pause()
            .Add(() =>
            {
                Debug.Log($"Start 0 {Time.time}");
            })
            .Add(1, () =>
            {
                Debug.Log("Step 1 " + Time.time);
            })
            .Add(() => 1, () =>
            {
                Debug.Log("Step 2 " + Time.time);
            })
            .Add(1, (TeaHandler t) =>
            {
                Debug.Log("Step 3 " + Time.time);
                t.Wait(1);
            })
            .Add(() =>
            {
                Debug.Log("Step 4 " + Time.time);
            })
            .Loop(1, (TeaHandler t) =>
            {

            })
            .Add(() =>
            {
                Debug.Log("Step 5 " + Time.time);
            })
            .Loop((TeaHandler t) =>
            {
                if (t.timeSinceStart >= 1)
                    t.Break();
            })
            .Add(() =>
            {
                Debug.Log("Step 6 " + Time.time);
            })
            .Loop(0, (TeaHandler t) =>
            {
                // Ignorable loop
            })
            .Add(1, () =>
            {
                Debug.Log("Step 7 " + Time.time);
            })
            .Add((TeaHandler t) =>
            {
                // WaitFor a Loop and an Add
                t.Wait(this.tt()
                    .Loop(0.5f, (TeaHandler) => { })
                    .Add(0.5f, () =>
                    {
                        Debug.Log($"Step 8 {Time.time}");
                    })
                    .WaitForCompletion());
            })
            .Add(1, () =>
            {
                Debug.Log($"Step 9 {Time.time}");
            })
            .Loop(t =>
            {
                Debug.Log($"End 0 {Time.time}");
                t.Break();
            })
            .Immutable();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            queue.Consume();
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            queue.Reverse();
        }

        if (Input.GetKeyDown(KeyCode.Y))
        {
            queue.Reset();
        }

        if (Input.GetKeyDown(KeyCode.U))
        {
            queue.Restart();
        }

        if (Input.GetKeyDown(KeyCode.I))
        {
            queue.Play();
        }

        if (Input.GetKeyDown(KeyCode.O))
        {
            queue.Pause();
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            queue.Stop();
        }
    }
}

// 2015/09/15 12:47:29 PM