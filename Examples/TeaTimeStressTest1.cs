
// Some day this will be a good stress test for TeaTime.

// 2016/04/26 04:37 PM


using UnityEngine;
using matnesis.TeaTime;

public class TeaTimeStressTest1 : MonoBehaviour
{
    void Start()
    {
        TeaTime queue = this.tt();

        for (int i = 0; i < 10000; i++)
        {
            queue.Add(0.10f, (ttHandler t) =>
            {
                Debug.Log(Time.time);
            });
        }

        queue.Add(0.10f, (ttHandler t) =>
        {
            Debug.Log("END " + Time.time);
        });
    }
}
