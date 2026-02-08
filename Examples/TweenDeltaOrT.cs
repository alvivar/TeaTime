using UnityEngine;

public class TweenDeltaOrT : MonoBehaviour
{
    private const int ExpectedChecks = 11;
    private const float LoopDuration = 2.5f;
    private const float HarnessTimeoutSeconds = 12f;

    private TeaTime queue;

    private int checksDone = 0;
    private int checksPassed = 0;
    private bool evaluated = false;

    private float harnessStartedAt = 0f;
    private float firstCallbackAt = -1f;
    private float completedAt = -1f;

    // First loop (deltaTime-driven lerp)
    private int deltaLoopFrames = 0;
    private float deltaLoopMaxTimeSinceStart = 0f;
    private bool deltaLoopSawPositiveDelta = false;
    private float deltaLoopEndY = 0f;
    private bool deltaLoopEndCaptured = false;

    // Second loop (t-driven lerp)
    private int tLoopFrames = 0;
    private float tLoopMaxTimeSinceStart = 0f;
    private float tLoopMinT = float.PositiveInfinity;
    private float tLoopMaxT = float.NegativeInfinity;
    private float tLoopLastT = float.NegativeInfinity;
    private bool tLoopMonotonic = true;
    private float tLoopEndY = 0f;
    private bool tLoopEndCaptured = false;

    void Start()
    {
        Debug.Log("[TweenDeltaOrTTest] Starting timing harness...");

        queue = this.tt("DeltaTween")
            .Add(() =>
            {
                firstCallbackAt = Time.time;
                transform.localPosition = Vector3.zero;
                Debug.Log($"[TweenDeltaOrTTest] Begin at {firstCallbackAt:0.000}s");
            })
            .Loop(
                LoopDuration,
                (TeaHandler t) =>
                {
                    // t.deltaTime should drive the lerp to target in exactly LoopDuration.
                    deltaLoopFrames += 1;
                    deltaLoopMaxTimeSinceStart = Mathf.Max(
                        deltaLoopMaxTimeSinceStart,
                        t.timeSinceStart
                    );

                    if (t.deltaTime > 0f)
                        deltaLoopSawPositiveDelta = true;

                    transform.localPosition = Vector3.Lerp(
                        transform.localPosition,
                        new Vector3(0, 1f, 0),
                        t.deltaTime
                    );
                }
            )
            .Add(() =>
            {
                deltaLoopEndCaptured = true;
                deltaLoopEndY = transform.localPosition.y;
                Debug.Log(
                    $"[TweenDeltaOrTTest] After delta loop: y={deltaLoopEndY:0.000}, maxTimeSinceStart={deltaLoopMaxTimeSinceStart:0.000}s"
                );
            })
            .Loop(
                LoopDuration,
                (TeaHandler t) =>
                {
                    // t should progress from 0 to 1 over LoopDuration.
                    tLoopFrames += 1;
                    tLoopMaxTimeSinceStart = Mathf.Max(tLoopMaxTimeSinceStart, t.timeSinceStart);

                    tLoopMinT = Mathf.Min(tLoopMinT, t.t);
                    tLoopMaxT = Mathf.Max(tLoopMaxT, t.t);

                    if (tLoopLastT > float.NegativeInfinity && t.t + 0.0001f < tLoopLastT)
                        tLoopMonotonic = false;

                    tLoopLastT = t.t;

                    transform.localPosition = Vector3.Lerp(
                        new Vector3(0, 1f, 0),
                        new Vector3(0, -1, 0),
                        Easef.Smootherstep(t.t)
                    );
                }
            )
            .Add(() =>
            {
                tLoopEndCaptured = true;
                tLoopEndY = transform.localPosition.y;
                completedAt = Time.time;

                Debug.Log(
                    $"[TweenDeltaOrTTest] After t loop: y={tLoopEndY:0.000}, minT={tLoopMinT:0.000}, maxT={tLoopMaxT:0.000}, maxTimeSinceStart={tLoopMaxTimeSinceStart:0.000}s"
                );

                EvaluateAndPrint(false);
            })
            .Immutable();

        harnessStartedAt = Time.time;
    }

    void Update()
    {
        if (!evaluated && Time.time - harnessStartedAt >= HarnessTimeoutSeconds)
        {
            Debug.LogWarning(
                $"[TweenDeltaOrTTest] Timeout after {HarnessTimeoutSeconds:0.0}s. Evaluating partial results."
            );
            EvaluateAndPrint(true);
        }
    }

    private void EvaluateAndPrint(bool timedOut)
    {
        if (evaluated)
            return;

        evaluated = true;

        if (queue != null)
            queue.Stop();

        bool completed = !timedOut && completedAt >= 0f;

        CheckCondition(
            "Queue completed",
            completed,
            timedOut
                ? "Queue did not complete before timeout."
                : "Completion callback was not observed."
        );

        CheckCondition(
            "delta loop produced frames",
            deltaLoopFrames > 1,
            $"Observed {deltaLoopFrames} frame(s)."
        );

        CheckApprox(
            "delta loop duration (timeSinceStart)",
            LoopDuration,
            deltaLoopMaxTimeSinceStart,
            0.35f
        );

        CheckCondition(
            "delta loop deltaTime was positive",
            deltaLoopSawPositiveDelta,
            "No positive deltaTime observed in first loop."
        );

        if (deltaLoopEndCaptured)
            CheckApprox("delta loop final y", 1f, deltaLoopEndY, 0.08f);
        else
            CheckCondition(
                "delta loop final y",
                false,
                "First loop completion callback was not observed."
            );

        CheckCondition(
            "t loop produced frames",
            tLoopFrames > 1,
            $"Observed {tLoopFrames} frame(s)."
        );

        CheckCondition(
            "t loop monotonic progression",
            tLoopMonotonic,
            "Observed t decreasing during second loop."
        );

        bool tCoverageOk = tLoopMinT <= 0.15f && tLoopMaxT >= 0.85f;
        CheckCondition(
            "t loop covered expected range",
            tCoverageOk,
            $"Observed minT={tLoopMinT:0.000}, maxT={tLoopMaxT:0.000}."
        );

        CheckApprox(
            "t loop duration (timeSinceStart)",
            LoopDuration,
            tLoopMaxTimeSinceStart,
            0.35f
        );

        if (tLoopEndCaptured)
            CheckApprox("t loop final y", -1f, tLoopEndY, 0.08f);
        else
            CheckCondition(
                "t loop final y",
                false,
                "Second loop completion callback was not observed."
            );

        if (completed && firstCallbackAt >= 0f)
            CheckApprox(
                "total sequence duration",
                LoopDuration * 2f,
                completedAt - firstCallbackAt,
                0.80f
            );
        else
            CheckCondition(
                "total sequence duration",
                false,
                "Missing begin/completion timestamps."
            );

        PrintSummary();
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
            $"[TweenDeltaOrTTest] {verdict} {label}: expected {expectedSeconds:0.000}s, "
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
            Debug.Log($"[TweenDeltaOrTTest] PASS {label}");
        else
            Debug.LogWarning($"[TweenDeltaOrTTest] FAIL {label}: {failReason}");
    }

    private void PrintSummary()
    {
        if (checksDone != ExpectedChecks)
        {
            Debug.LogWarning(
                $"[TweenDeltaOrTTest] Summary check mismatch: expected {ExpectedChecks}, got {checksDone}."
            );
        }

        int failed = checksDone - checksPassed;

        if (failed == 0)
        {
            Debug.Log($"[TweenDeltaOrTTest] COMPLETE: {checksPassed}/{checksDone} checks passed.");
        }
        else
        {
            Debug.LogWarning(
                $"[TweenDeltaOrTTest] COMPLETE: {checksPassed}/{checksDone} checks passed, {failed} failed."
            );
        }
    }
}
