
// 2015/09/15 12:47:29 PM


using UnityEngine;


public class Example1 : MonoBehaviour
{
    private TeaTime2 queue;


    void Start()
    {
        queue = this.TeaTime2();
        queue.Add(1, () =>
        {
            Debug.Log("time " + Time.time);
            Debug.Log("delta " + Time.deltaTime);
        })
        .Add(() =>
        {
            Debug.Log("end 1");
        })
        .Add(1, () =>
        {
            Debug.Log("time " + Time.time);
            Debug.Log("delta " + Time.deltaTime);
        })
        .Add(() =>
        {
            Debug.Log("end");
        })
        .Repeat();
    }


    void Update()
    {
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

        if (Input.GetKeyDown(KeyCode.L))
        {
            queue.Reset();
        }
    }
}
