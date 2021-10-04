using UnityEngine;

public class TweenDeltaOrT : MonoBehaviour
{
    void Update()
    {
        TeaTime deltaTween = this.tt("DeltaTween").Loop(2.5f, (TeaHandler t) =>
            {
                // t.deltaTime is the exact ammount to complete the lerp in 2.5 // seconds.
                transform.localPosition = Vector3.Lerp(
                    transform.localPosition,
                    new Vector3(0, 1f, 0),
                    t.deltaTime);
            })
            .Loop(2.5f, t =>
            {
                // t.t goes from 0 to 1 in 3 seconds.
                transform.localPosition = Vector3.Lerp(
                    new Vector3(0, 1f, 0),
                    new Vector3(0, -1, 0),
                    Easef.Smootherstep(t.t));
            })
            .Immutable();
    }
}