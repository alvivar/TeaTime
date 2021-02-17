// Wait!

using UnityEngine;

public class WaitForFunc : MonoBehaviour
{
    public bool something = false;

    void Start()
    {
        TeaTime untilTrueFunc = this.tt()
            .Wait(() => something, 0.1f)
            .Add(() =>
            {
                Debug.Log($"Prelude at {Time.time}");
            })
            .Loop(0.1f, t =>
            {
                Debug.Log($"Action! at {Time.time}");
            });

        // Both are equivalent. Wait( is syntactic sugar.

        TeaTime waitIsSyntacticSugar = this.tt()
            .Loop((ttHandler t) =>
            {
                if (something)
                    t.Break();

                t.Wait(0.1f);
            })
            .Add(() =>
            {
                Debug.Log($"Prelude at {Time.time}");
            })
            .Loop(0.1f, t =>
            {
                Debug.Log($"Action! at {Time.time}");
            });
    }
}

// 2021.02.17 12.52 am