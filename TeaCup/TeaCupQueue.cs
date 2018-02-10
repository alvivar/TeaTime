
// TeaCup Queue that returns itself when the time is right.

// @matnesis
// 2016/11/13 09:51 PM


using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace matnesis.TeaCup
{
    public class TeaCupQueue
    {
        public TeaCupNode first;
        public TeaCupNode last;
        public TeaCupNode current;
        public TeaCupNode previous;

        public int count;
        public float timeline;

        public float timeSinceStart = 0;
        public float lastLoopTime = 0;

        public bool isUnscaled = false;
        public bool isRepeatable = false;
        public bool isReversed = false;


        // Adds a new task at the end, calculating his time events.
        public void Add(float timeDelay, float loopDuration, Action<TeaCupHandler> action)
        {
            var newTask = new TeaCupNode(
                timeDelay,
                loopDuration,
                action
            );


            if (first == null)
            {
                newTask.beginDelay = 0;
                newTask.beginAction = newTask.beginDelay + newTask.delay;
                newTask.endAction = newTask.beginAction + newTask.loopTime;

                current = first = last = newTask;
            }
            else
            {
                last.next = newTask;
                newTask.previous = last;

                newTask.beginDelay = last.endAction;
                newTask.beginAction = newTask.beginDelay + newTask.delay;
                newTask.endAction = newTask.beginAction + newTask.loopTime;

                last = newTask;
            }


            count += 1;
        }


        // Moves the timeline forward or backward using the delta. Returns the
        // task that need to be executed.
        public TeaCupNode Update(float delta, TeaCupHandler handler)
        {
            // We reach the end, or we are empty
            if (current == null)
            {
                // When repeatable
                if (isRepeatable)
                {
                    // When time moves forward
                    if (delta > 0)
                    {
                        current = first;
                        timeline = 0;
                    }
                    // When time moves backward
                    else if (delta < 0)
                    {
                        current = last;
                        timeline = last.endAction;
                    }
                }

                // Nothing to do
                return null;
            }


            // When going in reverse and we are just beginning
            if (delta < 0 && timeline == 0)
            {
                current = last;
                timeline = last.endAction;
            }


            // DELTA
            timeline += delta;
            timeSinceStart += Mathf.Abs(delta);

            // Loop time
            if (current != previous)
            {
                lastLoopTime = 0;
                previous = current;
            }
            lastLoopTime += Mathf.Abs(delta);


            // The chosen
            if (delta > 0) // When time moves forward
            {
                if (timeline >= current.beginAction)
                {
                    var chosen = current;

                    if (timeline >= current.endAction)
                        current = current.next;

                    return chosen;
                }
            }
            else if (delta < 0) // When time moves backward
            {
                if (timeline + current.delay <= current.endAction)
                {
                    var chosen = current;

                    if (timeline + current.delay <= current.beginAction)
                        current = current.previous;

                    return chosen;
                }
            }


            return null;
        }
    }
}
