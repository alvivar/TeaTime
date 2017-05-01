
// I use this to test TeaCup as I experiment with it.

// @matnesis
// 2016/11/12 07:34 PM


using UnityEngine;
using matnesis.TeaCup;

public class TeaCupTest : MonoBehaviour
{
    void Start()
    {
        TeaCup test1 = new TeaCup().Add(1, t =>
        {
            Debug.Log("Test1 a " + Time.time);
        })
        .Add(0, t =>
        {
            Debug.Log("Test1 b " + Time.time);
        })
        .Add(2, t =>
        {
            Debug.Log("Test1 c " + Time.time);
        })
        .Add(1, t =>
        {
            Debug.Log("Test1 d " + Time.time);
        });
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
        // })
        // .Repeat();
    }
}
