// A TeaTime that waits nested TeaTimes.

using UnityEngine;

public class WaitNested : MonoBehaviour
{
    TeaTime queue;

    void Start()
    {
        queue = this.tt("@master").Pause().Add(t =>
            {
                TeaTime chosen = null;

                switch (Random.Range(0, 3))
                {
                    case 0:
                        chosen = this.tt("@1").Add(1, t2 =>
                            {
                                Debug.Log("Waits 1 " + Time.time);
                            })
                            .Immutable();
                        break;
                    case 1:
                        chosen = this.tt("@2").Add(2, t2 =>
                            {
                                Debug.Log("Waits 2 " + Time.time);
                            })
                            .Immutable();
                        break;
                    case 2:
                        chosen = this.tt("@3").Add(3, t2 =>
                            {
                                Debug.Log("Waits 3 " + Time.time);
                            })
                            .Immutable();
                        break;
                }

                t.Wait(chosen);
                Debug.Log("New cycle " + Time.time);
            })
            .Repeat();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Y))
        {
            Debug.Log("Reset " + Time.time);
            queue.Reset();
        }

        if (Input.GetKeyDown(KeyCode.U))
        {
            Debug.Log("Restart " + Time.time);
            queue.Restart();
        }

        if (Input.GetKeyDown(KeyCode.I))
        {
            Debug.Log("Play " + Time.time);
            queue.Play();
        }

        if (Input.GetKeyDown(KeyCode.O))
        {
            Debug.Log("Pause " + Time.time);
            queue.Pause();
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log("Stop " + Time.time);
            queue.Stop();
        }
    }
}

// 2017/03/04 01:37 PM