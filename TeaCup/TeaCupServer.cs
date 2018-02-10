
// Handles all TeaCups!

// @matnesis
// 2016/11/12 07:25 PM


using UnityEngine;
using System.Collections.Generic;
using matnesis.TeaCup;

public class TeaCupServer : MonoBehaviour
{
    public List<TeaCup> running = new List<TeaCup>();


    public static TeaCupServer self = null;
    public static TeaCup Init(TeaCup t)
    {
        // Be
        if (self == null)
            self = FindObjectOfType<TeaCupServer>();

        if (self == null)
        {
            GameObject god = new GameObject("[TeaCup]");
            self = god.AddComponent<TeaCupServer>();
        }


        // Auto run
        if (!self.running.Contains(t))
            self.running.Add(t);


        // This is not a singleton, it's the same TeaCup
        return t;
    }


    void Update()
    {
        // For each TeaCup running
        for (int runId = 0, runCount = running.Count; runId < runCount; runId++)
        {
            TeaCup teaCup = running[runId];


            // The next frame
            float delta = 0;

            // Scaled v Unscaled
            if (teaCup.queue.isUnscaled) delta = Time.unscaledDeltaTime;
            else delta = Time.deltaTime;


            // Reverse
            if (teaCup.queue.isReversed) delta *= -1;


            // One action at the time
            var handler = new TeaCupHandler();
            handler.self = teaCup.queue;

            var node = teaCup.queue.Update(delta, handler);
            if (node != null)
            {
                node.action(handler);
            }
        }
    }
}
