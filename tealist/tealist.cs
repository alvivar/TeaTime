// Andrés Villalobos * twitter.com/matnesis * andresalvivar@gmail.com
// 2018/05/26 03:18 pm

using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

public class tnode
{
    public float start;
    public float end;

    public System.Action call;
}

public class tlist
{
    public List<tnode> nodes = new List<tnode>();
    public tnode current = null;
    public float time = 0;
    public float duration = 0;

    public tlist Add(float delay, float duration, System.Action call)
    {
        var node = new tnode();
        node.start = this.duration + delay;
        node.end = this.duration + delay + duration;
        node.call = call;
        nodes.Add(node);

        this.duration += delay + duration;

        return this;
    }

    public tlist Add(float delay, System.Action call)
    {
        return this.Add(delay, 0, call);
    }

    public void Execute(float delta)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            var start = nodes[i].start;
            var end = nodes[i].end;
            var call = nodes[i].call;

            if (start == end) // Just one frame kind of loop
                end += delta;

            if (time > end)
                continue;

            if (time >= start && time <= end)
            {
                if (call != null)
                {
                    call();
                    break; // One callback at the time
                }
            }
        }

        time += delta;
    }
}