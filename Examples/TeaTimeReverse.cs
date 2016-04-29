
// Queue of tweens example 2

// 2015/10/05 05:54:55 PM


using UnityEngine;
using matnesis.TeaTime; // Add the namespace!

public class TeaTimeReverse : MonoBehaviour {
    public Transform cube;
    public Renderer cubeRen;


    // Declare your queue
    TeaTime queue;


    void Start() {
        // Instantiate
        queue = new TeaTime(this);
        // or you can use this shortcut: 'queue = this.tt();' (special MonoBehaviour extension)


        // Adds a one second callback loop that lerps to a random color.
        queue.Loop(0.25f, (ttHandler t) => {
            cubeRen.material.color = Color.Lerp(new Color(1, 1, 1, 1), new Color(1, 0, 0, 1), t.t);
            //Debug.Log("TEST 1");
        })
        .Loop(0.5f, (ttHandler t) => {
            cubeRen.transform.localScale = Vector3.Lerp(new Vector3(1, 1, 1), new Vector3(3, 3, 3), t.t);
            //Debug.Log("TEST 4");
        }).Pause();
        
    }

    void OnMouseEnter() {
        Debug.Log("ENTER");
        queue.ForwardPlayMode().Play();
    }
    void OnMouseExit() {
        Debug.Log("EXIT");
        queue.BackwardsPlayMode().Play();
    }
}
