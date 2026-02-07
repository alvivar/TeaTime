using UnityEngine;

public static class Easef
{
    public static float Linear(float t)
    {
        return t;
    }

    public static float Quad(float t)
    {
        return t * t;
    }

    public static float QuadOut(float t)
    {
        return -t * (t - 2f);
    }

    public static float Cubic(float t)
    {
        return t * t * t;
    }

    public static float CubicOut(float t)
    {
        t = t - 1f;
        return t * t * t + 1f;
    }

    public static float CubicInOut(float t)
    {
        if (t < 0.5f)
        {
            return 4f * t * t * t;
        }
        else
        {
            t = t - 1f;
            return 4f * t * t * t + 1f;
        }
    }

    public static float EaseIn(float t)
    {
        return 1f - Mathf.Cos(t * Mathf.PI * 0.5f);
    }

    public static float EaseOut(float t)
    {
        return Mathf.Sin(t * Mathf.PI * 0.5f);
    }

    public static float EaseInOut(float t)
    {
        return t < 0.5f ? EaseIn(t * 2f) * 0.5f : EaseOut((t - 0.5f) * 2f) * 0.5f + 0.5f;
    }

    public static float Smoothstep(float t)
    {
        return t * t * (3f - 2f * t);
    }

    public static float Smootherstep(float t)
    {
        return t * t * t * (t * (6f * t - 15f) + 10f);
    }

    public static float Exponential(float t)
    {
        return t == 0f ? 0f : Mathf.Pow(2f, 10f * (t - 1f));
    }

    public static float Back(float t)
    {
        float s = 1.70158f;
        return t * t * ((s + 1f) * t - s);
    }

    public static float BackOut(float t)
    {
        float s = 1.70158f;
        t = t - 1f;
        return (t * t * ((s + 1f) * t + s) + 1f);
    }

    public static float Elastic(float t)
    {
        if (t == 0f)
            return 0f;
        if (t == 1f)
            return 1f;
        return -Mathf.Pow(2f, 10f * (t - 1f)) * Mathf.Sin((t - 1.1f) * (2f * Mathf.PI) / 0.4f);
    }

    public static float Bounce(float t)
    {
        return 1f - BounceOut(1f - t);
    }

    public static float BounceOut(float t)
    {
        const float n1 = 7.5625f;
        const float d1 = 2.75f;

        if (t < 1f / d1)
            return n1 * t * t;
        if (t < 2f / d1)
        {
            t -= 1.5f / d1;
            return n1 * t * t + 0.75f;
        }
        if (t < 2.5f / d1)
        {
            t -= 2.25f / d1;
            return n1 * t * t + 0.9375f;
        }

        t -= 2.625f / d1;
        return n1 * t * t + 0.984375f;
    }
}



// 2016/06/18 01:17 AM, Created.
// 2025/05/21 10:58 PM, Updated.
