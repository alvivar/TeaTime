// Thank you Xerios! http://github.com/alvivar/TeaTime/pull/8

using UnityEngine;

public class ReverseYoyo : MonoBehaviour
{
    private class LoopTracker
    {
        public readonly string name;

        public bool active = false;
        public bool activeReversed = false;
        public float lastTimeSinceStart = 0f;

        public int forwardRuns = 0;
        public int reverseRuns = 0;

        public float forwardDurationSum = 0f;
        public float reverseDurationSum = 0f;

        public float forwardMinT = 1f;
        public float forwardMaxT = 0f;
        public float reverseMinT = 1f;
        public float reverseMaxT = 0f;

        public LoopTracker(string loopName)
        {
            name = loopName;
        }
    }

    private const int ExpectedChecks = 9;
    private const float HarnessTimeoutSeconds = 12f;

    public Renderer renderr;

    private TeaTime queue;

    private readonly LoopTracker colorLoop = new LoopTracker("color");
    private readonly LoopTracker scaleLoop = new LoopTracker("scale");

    private int beginningCount = 0;
    private int endCount = 0;

    private bool sawPositiveDelta = false;
    private bool sawNegativeDelta = false;

    private float firstBeginningAt = -1f;
    private float completedAt = -1f;

    private int checksDone = 0;
    private int checksPassed = 0;

    private bool evaluated = false;
    private float harnessStartedAt = 0f;

    void Start()
    {
        Debug.Log("[ReverseYoyoTest] Starting timing harness...");

        if (renderr == null)
            renderr = GetComponent<Renderer>();

        queue = new TeaTime(this);

        // Adds one-second callback loops and verifies Yoyo behavior.
        queue
            .Add(() =>
            {
                beginningCount += 1;

                if (firstBeginningAt < 0f)
                    firstBeginningAt = Time.time;

                Debug.Log($"[ReverseYoyoTest] Beginning #{beginningCount} at {Time.time:0.000}s");
            })
            .Loop(
                1,
                (TeaHandler t) =>
                {
                    TrackLoop(colorLoop, t);

                    // From white to black, using .t (0 to 1 and back to 0 when reversed).
                    if (renderr != null)
                        renderr.material.color = Color.Lerp(Color.white, Color.black, t.t);
                }
            )
            .Loop(
                1,
                (TeaHandler t) =>
                {
                    TrackLoop(scaleLoop, t);

                    if (renderr != null)
                    {
                        renderr.transform.localScale = Vector3.Lerp(
                            new Vector3(1, 1, 1),
                            new Vector3(3, 3, 3),
                            t.t
                        );
                    }
                }
            )
            .Add(() =>
            {
                endCount += 1;
                Debug.Log($"[ReverseYoyoTest] End #{endCount} at {Time.time:0.000}s");
            })
            .Yoyo();

        // Yoyo mode will .Reverse() the queue execution order when the queue is
        // completed.

        harnessStartedAt = Time.time;
    }

    void Update()
    {
        if (evaluated)
            return;

        if (queue != null && queue.IsCompleted)
        {
            completedAt = Time.time;
            Debug.Log("[ReverseYoyoTest] Is completed? YES");
            EvaluateAndPrint(false);
            return;
        }

        if (Time.time - harnessStartedAt >= HarnessTimeoutSeconds)
        {
            Debug.LogWarning(
                $"[ReverseYoyoTest] Timeout after {HarnessTimeoutSeconds:0.0}s. Evaluating partial results."
            );
            EvaluateAndPrint(true);
        }
    }

    private void TrackLoop(LoopTracker tracker, TeaHandler t)
    {
        // Start run lazily.
        if (!tracker.active)
        {
            StartRun(tracker, t.isReversed, t.timeSinceStart);
        }
        else
        {
            // A new run begins when time resets or direction flips.
            bool timeReset = t.timeSinceStart + 0.0001f < tracker.lastTimeSinceStart;
            bool directionChanged = t.isReversed != tracker.activeReversed;

            if (timeReset || directionChanged)
            {
                CompleteRun(tracker);
                StartRun(tracker, t.isReversed, t.timeSinceStart);
            }
        }

        tracker.lastTimeSinceStart = t.timeSinceStart;

        if (t.isReversed)
        {
            tracker.reverseMinT = Mathf.Min(tracker.reverseMinT, t.t);
            tracker.reverseMaxT = Mathf.Max(tracker.reverseMaxT, t.t);
        }
        else
        {
            tracker.forwardMinT = Mathf.Min(tracker.forwardMinT, t.t);
            tracker.forwardMaxT = Mathf.Max(tracker.forwardMaxT, t.t);
        }

        if (t.deltaTime > 0f)
            sawPositiveDelta = true;
        else if (t.deltaTime < 0f)
            sawNegativeDelta = true;
    }

    private void StartRun(LoopTracker tracker, bool reversed, float initialTimeSinceStart)
    {
        tracker.active = true;
        tracker.activeReversed = reversed;
        tracker.lastTimeSinceStart = initialTimeSinceStart;
    }

    private void CompleteRun(LoopTracker tracker)
    {
        if (!tracker.active)
            return;

        float runDuration = tracker.lastTimeSinceStart;

        if (tracker.activeReversed)
        {
            tracker.reverseRuns += 1;
            tracker.reverseDurationSum += runDuration;
            Debug.Log(
                $"[ReverseYoyoTest] {tracker.name} reverse run #{tracker.reverseRuns} duration ~{runDuration:0.000}s"
            );
        }
        else
        {
            tracker.forwardRuns += 1;
            tracker.forwardDurationSum += runDuration;
            Debug.Log(
                $"[ReverseYoyoTest] {tracker.name} forward run #{tracker.forwardRuns} duration ~{runDuration:0.000}s"
            );
        }

        tracker.active = false;
    }

    private void CompleteOpenRuns()
    {
        CompleteRun(colorLoop);
        CompleteRun(scaleLoop);
    }

    private void EvaluateAndPrint(bool timedOut)
    {
        if (evaluated)
            return;

        evaluated = true;

        if (queue != null)
            queue.Stop();

        CompleteOpenRuns();

        bool completed = !timedOut && completedAt >= 0f;
        CheckCondition(
            "Queue completed (non-repeat yoyo)",
            completed,
            timedOut
                ? "Queue did not complete before timeout."
                : "Queue completion was not detected."
        );

        CheckCondition(
            "Beginning callback observed",
            beginningCount >= 1,
            "Beginning callback was never called."
        );

        CheckCondition("End callback observed", endCount >= 1, "End callback was never called.");

        CheckCondition(
            "Color loop ran forward and reverse",
            colorLoop.forwardRuns >= 1 && colorLoop.reverseRuns >= 1,
            $"Runs: forward={colorLoop.forwardRuns}, reverse={colorLoop.reverseRuns}."
        );

        CheckCondition(
            "Scale loop ran forward and reverse",
            scaleLoop.forwardRuns >= 1 && scaleLoop.reverseRuns >= 1,
            $"Runs: forward={scaleLoop.forwardRuns}, reverse={scaleLoop.reverseRuns}."
        );

        bool tCoverageOk =
            HasTRange(colorLoop.forwardMinT, colorLoop.forwardMaxT)
            && HasTRange(colorLoop.reverseMinT, colorLoop.reverseMaxT)
            && HasTRange(scaleLoop.forwardMinT, scaleLoop.forwardMaxT)
            && HasTRange(scaleLoop.reverseMinT, scaleLoop.reverseMaxT);

        CheckCondition(
            "Loop t covered full range in both directions",
            tCoverageOk,
            "Expected min<=0.15 and max>=0.85 for each forward/reverse loop run set."
        );

        CheckCondition(
            "deltaTime sign flipped with direction",
            sawPositiveDelta && sawNegativeDelta,
            "Expected at least one positive and one negative loop deltaTime."
        );

        float averageRunDuration;
        int durationSamples;
        ComputeAverageRunDuration(out averageRunDuration, out durationSamples);

        if (durationSamples > 0)
        {
            CheckApprox(
                $"Average loop run duration across {durationSamples} runs",
                1f,
                averageRunDuration,
                0.25f
            );
        }
        else
        {
            CheckCondition(
                "Average loop run duration across runs",
                false,
                "No loop run samples were recorded."
            );
        }

        if (completed && firstBeginningAt >= 0f)
        {
            float totalDuration = completedAt - firstBeginningAt;
            CheckApprox("Total yoyo sequence duration", 4f, totalDuration, 1.00f);
        }
        else
        {
            CheckCondition(
                "Total yoyo sequence duration",
                false,
                "Missing beginning/completion timestamps."
            );
        }

        PrintSummary();
    }

    private bool HasTRange(float minT, float maxT)
    {
        return minT <= 0.15f && maxT >= 0.85f;
    }

    private void ComputeAverageRunDuration(out float average, out int samples)
    {
        float sum = 0f;
        samples = 0;

        AddDurationSamples(
            colorLoop.forwardDurationSum,
            colorLoop.forwardRuns,
            ref sum,
            ref samples
        );
        AddDurationSamples(
            colorLoop.reverseDurationSum,
            colorLoop.reverseRuns,
            ref sum,
            ref samples
        );
        AddDurationSamples(
            scaleLoop.forwardDurationSum,
            scaleLoop.forwardRuns,
            ref sum,
            ref samples
        );
        AddDurationSamples(
            scaleLoop.reverseDurationSum,
            scaleLoop.reverseRuns,
            ref sum,
            ref samples
        );

        average = samples > 0 ? sum / samples : 0f;
    }

    private void AddDurationSamples(float durationSum, int count, ref float sum, ref int samples)
    {
        if (count <= 0)
            return;

        sum += durationSum;
        samples += count;
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
            $"[ReverseYoyoTest] {verdict} {label}: expected {expectedSeconds:0.000}s, "
            + $"actual {actualSeconds:0.000}s, tolerance ±{tolerance:0.000}s";

        if (pass)
            Debug.Log(msg);
        else
            Debug.LogWarning(msg);
    }

    private void CheckCondition(string label, bool pass, string failReason)
    {
        checksDone += 1;
        if (pass)
            checksPassed += 1;

        if (pass)
            Debug.Log($"[ReverseYoyoTest] PASS {label}");
        else
            Debug.LogWarning($"[ReverseYoyoTest] FAIL {label}: {failReason}");
    }

    private void PrintSummary()
    {
        if (checksDone != ExpectedChecks)
        {
            Debug.LogWarning(
                $"[ReverseYoyoTest] Summary check mismatch: expected {ExpectedChecks}, got {checksDone}."
            );
        }

        int failed = checksDone - checksPassed;
        if (failed == 0)
        {
            Debug.Log($"[ReverseYoyoTest] COMPLETE: {checksPassed}/{checksDone} checks passed.");
        }
        else
        {
            Debug.LogWarning(
                $"[ReverseYoyoTest] COMPLETE: {checksPassed}/{checksDone} checks passed, {failed} failed."
            );
        }
    }
}

// 2016/04/29 05:36 PM
