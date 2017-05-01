
// Data structure to handle timed Actions on a timeline that goes forward and
// backward.

// @matnesis
// 2016/11/13 09:51 PM


using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace matnesis.TeaCup
{
    public class TeaCupTaskList
    {
        public TeaCupTask first;
        public TeaCupTask last;
        public int count;

        public float time;
        public TeaCupTask current;


        public void Add(float timeDelay, float loopDuration, Action<TeaCupHandler> action)
        {
            var newTask = new TeaCupTask(
                timeDelay,
                loopDuration,
                action
            );


            if (first == null)
            {
                current = first = last = newTask;
            }
            else
            {
                var current = last;
                current.next = newTask;

                newTask.previous = current;
                newTask.delay += current.delay;

                last = newTask;
            }


            count += 1;
        }


        public Action<TeaCupHandler> Next(float delta)
        {
            if (current == null)
                return null;


            time += delta;


            if (time >= current.delay)
            {
                var chosen = current;
                current = current.next;

                // #todo Instead of returning only the next Action, we need to
                // return a list of all of those that can be fired at this
                // precise time.
                return chosen.action;
            }


            return null;
        }

    }
}
