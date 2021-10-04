// Check the profiler!

using UnityEngine;

public class StressTest : MonoBehaviour
{
    public float addFrameCount = 0;
    public float loopFrameCount = 0;

    void Start()
    {
        TeaTime queue = this.tt();

        // Every second
        this.tt().Add(1, () =>
            {
                // Append lots of Adds & Loops
                for (int i = 0; i < 10000; i++)
                {
                    // Call them quick
                    queue.Add(0.10f, (TeaHandler t) =>
                        {
                            addFrameCount += 1;
                        })
                        .Loop(0.10f, (TeaHandler t) =>
                        {
                            loopFrameCount += 1;
                        });
                }
            })
            // Forever
            .Repeat();
    }
}

// 2016/04/26 04:37 PM