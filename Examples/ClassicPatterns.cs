using UnityEngine;

public class ClassicPatterns : MonoBehaviour
{
    private const int ExpectedChecks = 5;

    private int checksDone = 0;
    private int checksPassed = 0;
    private bool summaryPrinted = false;

    void Start()
    {
        Debug.Log("[ClassicPatternsTest] Starting timing harness...");

        // A simple 2 seconds delay.
        float simpleDelayStartedAt = Time.time;
        TeaTime simpleDelay = this.tt()
            .Add(
                2,
                () =>
                {
                    float elapsed = Time.time - simpleDelayStartedAt;
                    CheckApprox("simpleDelay delay", 2f, elapsed, 0.35f);
                }
            );

        // Something that repeats itself every 3 seconds.
        float repeatLastTick = -1f;
        float repeatIntervalSum = 0f;
        int repeatIntervals = 0;

        TeaTime repeatDelay = this.tt()
            .Add(
                (TeaHandler t) =>
                {
                    float now = Time.time;

                    if (repeatLastTick >= 0f)
                    {
                        float interval = now - repeatLastTick;
                        repeatIntervalSum += interval;
                        repeatIntervals += 1;

                        Debug.Log(
                            $"[ClassicPatternsTest] repeatDelay interval #{repeatIntervals}: {interval:0.000}s"
                        );
                    }

                    repeatLastTick = now;

                    // Measure two intervals (three callback hits), then stop.
                    if (repeatIntervals >= 2)
                    {
                        float averageInterval = repeatIntervalSum / repeatIntervals;
                        CheckApprox("repeatDelay interval", 3f, averageInterval, 0.35f);
                        t.self.Stop();
                    }
                }
            )
            .Add(3)
            .Repeat();

        // A controlled frame by frame loop (update-like) with 1 second duration!
        float updateLikeStartedAt = -1f;
        TeaTime updateLike = this.tt()
            .Add(() =>
            {
                updateLikeStartedAt = Time.time;
                Debug.Log($"[ClassicPatternsTest] updateLike start at {updateLikeStartedAt:0.000}");
            })
            .Loop(
                1f,
                (TeaHandler loop) => {
                    // Keep callback active to preserve the same update-like structure.
                }
            )
            .Add(() =>
            {
                float elapsed = Time.time - updateLikeStartedAt;
                CheckApprox("updateLike loop duration", 1f, elapsed, 0.30f);
            });

        // A simple delay without autoplay.
        float somethingForLaterPlayedAt = -1f;
        TeaTime somethingForLater = this.tt()
            .Pause()
            .Add(
                3,
                () =>
                {
                    float elapsed = Time.time - somethingForLaterPlayedAt;
                    CheckApprox("somethingForLater from Play()", 3f, elapsed, 0.35f);
                }
            );

        somethingForLaterPlayedAt = Time.time;
        somethingForLater.Play();

        // A tween-like with before and after setup.
        float tweenLikeStartedAt = -1f;
        TeaTime tweenLike = this.tt()
            .Add(() =>
            {
                tweenLikeStartedAt = Time.time;
                transform.position = new Vector3(999, 999, 999);
            })
            .Loop(
                4,
                (TeaHandler loop) =>
                {
                    transform.position = Vector3.Lerp(
                        transform.position,
                        Vector3.zero,
                        loop.deltaTime
                    );
                }
            )
            .Add(() =>
            {
                float elapsed = Time.time - tweenLikeStartedAt;
                CheckApprox("tweenLike loop duration", 4f, elapsed, 0.45f);
            });
    }

    private void CheckApprox(
        string label,
        float expectedSeconds,
        float actualSeconds,
        float tolerance
    )
    {
        float error = Mathf.Abs(actualSeconds - expectedSeconds);
        bool pass = error <= tolerance;

        checksDone += 1;
        if (pass)
            checksPassed += 1;

        string verdict = pass ? "PASS" : "FAIL";
        string msg =
            $"[ClassicPatternsTest] {verdict} {label}: expected {expectedSeconds:0.000}s, "
            + $"actual {actualSeconds:0.000}s, tolerance ±{tolerance:0.000}s";

        if (pass)
            Debug.Log(msg);
        else
            Debug.LogWarning(msg);

        TryPrintSummary();
    }

    private void TryPrintSummary()
    {
        if (summaryPrinted || checksDone < ExpectedChecks)
            return;

        summaryPrinted = true;

        int failed = checksDone - checksPassed;
        if (failed == 0)
        {
            Debug.Log(
                $"[ClassicPatternsTest] COMPLETE: {checksPassed}/{checksDone} checks passed."
            );
        }
        else
        {
            Debug.LogWarning(
                $"[ClassicPatternsTest] COMPLETE: {checksPassed}/{checksDone} checks passed, {failed} failed."
            );
        }
    }
}

// 2016/03/18 06:15 PM
