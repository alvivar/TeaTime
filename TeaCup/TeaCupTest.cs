
// I use this to test TeaCup as I create it.

// @matnesis
// 2016/11/12 07:34 PM


using UnityEngine;
using matnesis.TeaCup;

public class TeaCupTest : MonoBehaviour
{
    void Start()
    {
        // TeaCup test1 = new TeaCup().Add(1, t =>
        // {
        //     Debug.Log("Test1 a " + Time.time);
        // })
        // .Add(0, t =>
        // {
        //     Debug.Log("Test1 b " + Time.time);
        // })
        // .Add(2, t =>
        // {
        //     Debug.Log("Test1 c " + Time.time);
        //     // t.self.Reverse();
        // })
        // .Add(1, t =>
        // {
        //     Debug.Log("Test1 d " + Time.time);
        // });
        // .Repeat()
        // .Reverse();


        // TeaCup test2 = new TeaCup().Add(0, t =>
        // {
        //     Debug.Log("Test2 a " + Time.time);
        // })
        // .Add(3, t =>
        // {
        //     Debug.Log("Test2 b " + Time.time);
        // })
        // .Add(8, t =>
        // {
        //     Debug.Log("Test2 c " + Time.time);
        // });
        // .Reverse();


        // TeaCup test3 = new TeaCup().Add(0, t =>
        // {
        //     Debug.Log("Time " + Time.time);
        //     Debug.Log("Delta " + Time.deltaTime);
        // });
        // .Repeat();


        // Loop
        var time = 0f;
        TeaCup test4 = new TeaCup().Loop(0.5f, t =>
        {
            time += Time.deltaTime;
            Debug.Log("1 " + time + " | " + Time.deltaTime + " | " + t.self.lastLoopTime);
        })
        .Loop(0.5f, t =>
        {
            time += Time.deltaTime;
            Debug.Log("2 " + time + " | " + Time.deltaTime + " | " + t.self.lastLoopTime);
        })
        .Add(1, t => Debug.Log("Time " + time + " " + Time.time))
        .Reverse();


        // Testing if we cant go back between a loop
        // time = 0;
        // TeaCup test5 = new TeaCup()
        //     .Add(1, t => Debug.Log("A Time " + time + " " + Time.time))
        //     .Loop(0.5f, t =>
        //     {
        //         time += Time.deltaTime;
        //         Debug.Log("1 " + time + " | " + Time.deltaTime + " | " + t.self.lastLoopTime);
        //     })
        //     .Loop(3f, t =>
        //     {
        //         time += Time.deltaTime;
        //         Debug.Log("2 " + time + " | " + Time.deltaTime + " | " + t.self.lastLoopTime);

        //         if (t.self.lastLoopTime > 0.5f)
        //             t.self.isReversed = true;
        //     })
        //     .Add(1, t => Debug.Log("B Time " + time + " " + Time.time));
    }
}
