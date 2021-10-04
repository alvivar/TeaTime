// Wait for a dynamic delay.

using UnityEngine;

public class WaitForFunc : MonoBehaviour
{
    public bool dynamicDelay = false;

    void Start()
    {
        TeaTime untilTrueFunc = this.tt()
            .Wait(() => dynamicDelay, 0.1f)
            .Add(() =>
            {
                Debug.Log($"Prelude A at {Time.time}");
            })
            .Loop(0.1f, t =>
            {
                Debug.Log($"Action A at {Time.time}");
            });

        // Both are equivalent. Wait( is syntactic sugar of a Break inside a
        // loop.

        TeaTime waitIsSyntacticSugar = this.tt()
            .Loop((TeaHandler t) =>
            {
                if (dynamicDelay)
                    t.Break();

                t.Wait(0.1f);
            })
            .Add(() =>
            {
                Debug.Log($"Prelude B at {Time.time}");
            })
            .Loop(0.1f, t =>
            {
                Debug.Log($"Action B at {Time.time}");
            });
    }
}

// 2021.02.17 12.52 am