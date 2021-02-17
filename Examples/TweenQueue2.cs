// A queue of tweens.

// 2015/10/05 05:54:55 PM

using UnityEngine;

public class TweenQueue2 : MonoBehaviour
{
    public Transform cube;
    public Renderer cubeRen;

    TeaTime queue;

    void Start()
    {
        queue = new TeaTime(this);

        // Adds a one second callback loop that lerps to a random color.
        queue.Loop(1f, (ttHandler t) =>
        {
            cubeRen.material.color = Color.Lerp(
                cubeRen.material.color,
                new Color(Random.value, Random.value, Random.value, Random.value),
                t.deltaTime); // t.deltaTime is a custom delta that represents the loop duration
        });

        // Adds a one second callback loop that lerps to a random scale.
        queue.Loop(1f, (ttHandler t) =>
        {
            cubeRen.transform.localScale = Vector3.Lerp(
                cube.localScale,
                new Vector3(Random.Range(0.5f, 2), Random.Range(0.5f, 2), Random.Range(0.5f, 2)),
                t.deltaTime);
        });
    }
}