using UnityEngine;

public static class Easef
{
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
        return t * t;
    }

    public static float EaseOut(float t)
    {
        return Mathf.Sin(t * Mathf.PI * 0.5f);
    }

    public static float EaseIn(float t)
    {
        return 1f - Mathf.Cos(t * Mathf.PI * 0.5f);
    }
}

// 2016/06/18 01:17 AM