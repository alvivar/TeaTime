using DG.Tweening;
using UnityEngine;


public class Examples : MonoBehaviour
{
    private Sequence myTween;


    void Start()
    {
        myTween = DOTween.Sequence();

        this.ttInvoke(10, () =>
        {
            Debug.Log("ttInvoke +10 secs " + Time.time);
        });

        Test();
    }


    void Update()
    {
        Test();
    }


    void Test()
    {
        this.ttAppend("> Every second", 1, () =>
        {
            Debug.Log("+1 second " + Time.time);
        })
        .ttLock();


        this.ttAppendLoop("> Background change", 3, delegate(LoopHandler loop)
        {
            Camera.main.backgroundColor = Color.Lerp(Color.white, Color.black, loop.t);
        })
        .ttAppend(() =>
        {
            Debug.Log("Black, +3 secs " + Time.time);
        })
        .ttAppendLoop(3, delegate(LoopHandler loop)
        {
            Camera.main.backgroundColor = Color.Lerp(Color.black, Color.white, loop.t);
        })
        .ttAppend(() =>
        {
            Debug.Log("White, +3 secs " + Time.time);
        })
        .ttLock();


        this.ttAppend("> myTween", myTween.WaitForCompletion(), () =>
        {
            myTween = DOTween.Sequence();
            myTween.Append(transform.DOMoveX(10, 2.5f));
            myTween.Append(transform.DOMoveX(-10, 2.5f));
        })
        .ttAppend(() =>
        {
            Debug.Log("myTween end, +5 secs " + Time.time);
        })
        .ttLock();


        this.ttAppend("> Every 4 secs", 4, delegate()
        {
            Debug.Log("+4 secs " + Time.time);
        })
        .ttLock();
    }
}