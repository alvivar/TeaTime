
// Useful TeaTime patterns!

// 2016/03/18 06:15 PM


using UnityEngine;
using matnesis.TeaTime;

public class TeaTimePatterns : MonoBehaviour
{
    void Start()
    {
        // @
        // A simple delay
        TeaTime simpleDelay = this.tt().Add(1, () =>
        {
            Debug.Log("simpleDelay: One second later, once" + Time.time);
        });


        // @
        // A simple observer
        TeaTime observeSomething = this.tt().Add(() =>
        {
            Debug.Log("observeSomething: Every 3 seconds! " + Time.time);
        })
        .Add(3).Repeat();


        // More to come! Soon!
    }
}
