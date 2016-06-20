
// TeaTime Tween Extensions!
// #experimental

// @matnesis
// 2016/05/14 01:00 AM


using UnityEngine;
using matnesis.TeaTime;

public static class ttTween
{
    public static TeaTime ttMove(this MonoBehaviour self, Transform target, Vector3 toPosition, float duration)
    {
        Vector3 initial = target.position;

        return self.tt(target.GetInstanceID() + "@ttMove").Reset().Loop(duration, (ttHandler t) =>
        {
            target.position = Vector3.Lerp(
                initial,
                toPosition,
                t.t * t.t * (3f - 2f * t.t) // Smothstep
            );
        });
    }


    public static TeaTime ttRotate(this MonoBehaviour self, Transform target, Vector3 euler, float duration)
    {
        Vector3 initial = target.eulerAngles;

        return self.tt(target.GetInstanceID() + "@ttRotate").Reset().Loop(duration, (ttHandler t) =>
        {
            target.eulerAngles = new Vector3(
                Mathf.LerpAngle(initial.x, euler.x, t.t * t.t * (3f - 2f * t.t)), // Smothstep
                Mathf.LerpAngle(initial.y, euler.y, t.t * t.t * (3f - 2f * t.t)),
                Mathf.LerpAngle(initial.z, euler.z, t.t * t.t * (3f - 2f * t.t))
            );
        });
    }


    public static TeaTime ttScale(this MonoBehaviour self, Transform target, Vector3 scale, float duration)
    {
        Vector3 initial = target.localScale;

        return self.tt(target.GetInstanceID() + "@ttScale").Reset().Loop(duration, (ttHandler t) =>
        {
            target.localScale = Vector3.Lerp(
                initial,
                scale,
                t.t * t.t * (3f - 2f * t.t) // Smothstep
            );
        });
    }
}
