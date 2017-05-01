
// Handles all TeaCups.

// @matnesis
// 2016/11/12 07:25 PM


using UnityEngine;
using System.Collections.Generic;
using matnesis.TeaCup;

public class TeaCupCore : MonoBehaviour
{
    public List<matnesis.TeaCup.TeaCup> running = new List<matnesis.TeaCup.TeaCup>();


    public static TeaCupCore self = null;

    public static TeaCup Init(matnesis.TeaCup.TeaCup t)
    {
        // Be
        if (self == null)
            self = FindObjectOfType<TeaCupCore>();

        if (self == null)
        {
            GameObject god = new GameObject("@TeaCup");
            self = god.AddComponent<TeaCupCore>();
        }


        // Auto run
        if (!self.running.Contains(t))
            self.running.Add(t);


        return t;
    }


    void Update()
    {
        // For each TeaCup running
        for (int runId = 0, runCount = running.Count; runId < runCount; runId++)
        {
            matnesis.TeaCup.TeaCup teaCup = running[runId];


            // Advance a frame
            float delta = 0;

            // Scaled v Unscaled
            if (teaCup.hasUnscaledTime) delta = Time.unscaledDeltaTime;
            else delta = Time.deltaTime;

            teaCup.elapsedTime += delta;
            teaCup.totalElapsedTime += delta;


            // And execute Actions in order when the time is right
            var tasksList = teaCup.tasks;
            var action = tasksList.Next(delta);

            if (action != null)
            {
                var handler = new TeaCupHandler(teaCup);
                action(handler);
            }
        }
    }
}
