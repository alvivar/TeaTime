// Wait for a dynamic delay.

using UnityEngine;

public class WaitForFunc : MonoBehaviour
{
    private const int ExpectedChecks = 14;
    private const float WaitTick = 0.1f;
    private const float TriggerDelay = 0.55f;
    private const float HarnessTimeoutSeconds = 8f;

    public bool dynamicDelay = false;

    private TeaTime untilTrueFunc;
    private TeaTime waitIsSyntacticSugar;
    private TeaTime dynamicFlip;

    private float harnessStartedAt = 0f;
    private bool evaluated = false;

    private float dynamicDelaySetAt = -1f;

    private float preludeAAt = -1f;
    private float preludeBAt = -1f;

    private int actionACount = 0;
    private int actionBCount = 0;

    private float firstActionAAt = -1f;
    private float firstActionBAt = -1f;

    private int checksDone = 0;
    private int checksPassed = 0;

    void Start()
    {
        Debug.Log("[WaitForFuncTest] Starting timing harness...");

        dynamicDelay = false;
        harnessStartedAt = Time.time;

        dynamicFlip = this.tt("@dynamicFlip")
            .Add(
                TriggerDelay,
                () =>
                {
                    dynamicDelay = true;
                    dynamicDelaySetAt = Time.time;
                    Debug.Log($"[WaitForFuncTest] dynamicDelay=true at {dynamicDelaySetAt:0.000}s");
                }
            )
            .Immutable();

        untilTrueFunc = this.tt()
            .Wait(() => dynamicDelay, WaitTick)
            .Add(() =>
            {
                preludeAAt = Time.time;
                Debug.Log($"[WaitForFuncTest] Prelude A at {preludeAAt:0.000}s");
            })
            .Loop(
                0.1f,
                t =>
                {
                    actionACount += 1;

                    if (firstActionAAt < 0f)
                    {
                        firstActionAAt = Time.time;
                        Debug.Log(
                            $"[WaitForFuncTest] Action A first frame at {firstActionAAt:0.000}s"
                        );
                    }
                }
            );

        // Both are equivalent. Wait( is syntactic sugar of a Break inside a
        // loop.

        waitIsSyntacticSugar = this.tt()
            .Loop(
                (TeaHandler t) =>
                {
                    if (dynamicDelay)
                        t.Break();

                    t.Wait(WaitTick);
                }
            )
            .Add(() =>
            {
                preludeBAt = Time.time;
                Debug.Log($"[WaitForFuncTest] Prelude B at {preludeBAt:0.000}s");
            })
            .Loop(
                0.1f,
                t =>
                {
                    actionBCount += 1;

                    if (firstActionBAt < 0f)
                    {
                        firstActionBAt = Time.time;
                        Debug.Log(
                            $"[WaitForFuncTest] Action B first frame at {firstActionBAt:0.000}s"
                        );
                    }
                }
            );
    }

    void Update()
    {
        if (evaluated)
            return;

        bool ready =
            dynamicDelaySetAt >= 0f
            && preludeAAt >= 0f
            && preludeBAt >= 0f
            && actionACount > 0
            && actionBCount > 0
            && untilTrueFunc != null
            && waitIsSyntacticSugar != null
            && untilTrueFunc.IsCompleted
            && waitIsSyntacticSugar.IsCompleted;

        if (ready)
        {
            EvaluateAndPrint(false);
            return;
        }

        if (Time.time - harnessStartedAt >= HarnessTimeoutSeconds)
        {
            Debug.LogWarning(
                $"[WaitForFuncTest] Timeout after {HarnessTimeoutSeconds:0.0}s. Evaluating partial results."
            );
            EvaluateAndPrint(true);
        }
    }

    private void EvaluateAndPrint(bool timedOut)
    {
        if (evaluated)
            return;

        evaluated = true;

        bool queueACompleted = untilTrueFunc != null && untilTrueFunc.IsCompleted;
        bool queueBCompleted = waitIsSyntacticSugar != null && waitIsSyntacticSugar.IsCompleted;

        if (dynamicFlip != null)
            dynamicFlip.Stop();

        if (untilTrueFunc != null)
            untilTrueFunc.Stop();

        if (waitIsSyntacticSugar != null)
            waitIsSyntacticSugar.Stop();

        CheckCondition(
            "dynamicDelay was flipped to true",
            dynamicDelaySetAt >= 0f,
            "No flip event."
        );

        CheckCondition(
            "Wait() queue completed",
            !timedOut && queueACompleted,
            timedOut ? "Queue A did not complete before timeout." : "Queue A IsCompleted was false."
        );

        CheckCondition(
            "Syntactic-sugar queue completed",
            !timedOut && queueBCompleted,
            timedOut ? "Queue B did not complete before timeout." : "Queue B IsCompleted was false."
        );

        CheckCondition("Prelude A observed", preludeAAt >= 0f, "Prelude A callback never fired.");
        CheckCondition("Prelude B observed", preludeBAt >= 0f, "Prelude B callback never fired.");

        CheckCondition("Action A observed", actionACount > 0, "Action A loop never ran.");
        CheckCondition("Action B observed", actionBCount > 0, "Action B loop never ran.");

        if (dynamicDelaySetAt >= 0f && preludeAAt >= 0f)
        {
            float latencyA = preludeAAt - dynamicDelaySetAt;

            CheckCondition(
                "Prelude A happened after dynamicDelay=true",
                latencyA >= 0f,
                $"Latency was negative ({latencyA:0.000}s)."
            );

            CheckRange("Prelude A latency is reasonable", latencyA, 0f, 0.30f);
        }
        else
        {
            CheckCondition(
                "Prelude A happened after dynamicDelay=true",
                false,
                "Missing timestamps to compute latency."
            );

            CheckCondition(
                "Prelude A latency is reasonable",
                false,
                "Missing timestamps to compute latency."
            );
        }

        if (dynamicDelaySetAt >= 0f && preludeBAt >= 0f)
        {
            float latencyB = preludeBAt - dynamicDelaySetAt;

            CheckCondition(
                "Prelude B happened after dynamicDelay=true",
                latencyB >= 0f,
                $"Latency was negative ({latencyB:0.000}s)."
            );

            CheckRange("Prelude B latency is reasonable", latencyB, 0f, 0.30f);
        }
        else
        {
            CheckCondition(
                "Prelude B happened after dynamicDelay=true",
                false,
                "Missing timestamps to compute latency."
            );

            CheckCondition(
                "Prelude B latency is reasonable",
                false,
                "Missing timestamps to compute latency."
            );
        }

        if (preludeAAt >= 0f && preludeBAt >= 0f)
        {
            CheckApprox(
                "Prelude A vs Prelude B timing",
                0f,
                Mathf.Abs(preludeAAt - preludeBAt),
                0.08f
            );
        }
        else
        {
            CheckCondition("Prelude A vs Prelude B timing", false, "Missing prelude timestamps.");
        }

        if (dynamicDelaySetAt >= 0f && preludeAAt >= 0f && preludeBAt >= 0f)
        {
            float latencyA = preludeAAt - dynamicDelaySetAt;
            float latencyB = preludeBAt - dynamicDelaySetAt;

            CheckApprox(
                "Wait() and sugar latency match",
                0f,
                Mathf.Abs(latencyA - latencyB),
                0.05f
            );
        }
        else
        {
            CheckCondition(
                "Wait() and sugar latency match",
                false,
                "Missing timestamps to compare latency."
            );
        }

        if (firstActionAAt >= 0f && firstActionBAt >= 0f)
        {
            CheckApprox(
                "Action A/B first-frame timing",
                0f,
                Mathf.Abs(firstActionAAt - firstActionBAt),
                0.08f
            );
        }
        else
        {
            CheckCondition(
                "Action A/B first-frame timing",
                false,
                "Missing action first-frame timestamps."
            );
        }

        PrintSummary();
    }

    private void CheckRange(string label, float value, float minInclusive, float maxInclusive)
    {
        bool pass = value >= minInclusive && value <= maxInclusive;

        checksDone += 1;
        if (pass)
            checksPassed += 1;

        if (pass)
        {
            Debug.Log(
                $"[WaitForFuncTest] PASS {label}: {value:0.000}s in [{minInclusive:0.000}, {maxInclusive:0.000}]"
            );
        }
        else
        {
            Debug.LogWarning(
                $"[WaitForFuncTest] FAIL {label}: {value:0.000}s not in [{minInclusive:0.000}, {maxInclusive:0.000}]"
            );
        }
    }

    private void CheckApprox(string label, float expected, float actual, float tolerance)
    {
        float error = Mathf.Abs(actual - expected);
        bool pass = error <= tolerance;

        checksDone += 1;
        if (pass)
            checksPassed += 1;

        string verdict = pass ? "PASS" : "FAIL";
        string msg =
            $"[WaitForFuncTest] {verdict} {label}: expected {expected:0.000}, "
            + $"actual {actual:0.000}, tolerance ±{tolerance:0.000}";

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
            Debug.Log($"[WaitForFuncTest] PASS {label}");
        else
            Debug.LogWarning($"[WaitForFuncTest] FAIL {label}: {failReason}");
    }

    private void PrintSummary()
    {
        if (checksDone != ExpectedChecks)
        {
            Debug.LogWarning(
                $"[WaitForFuncTest] Summary check mismatch: expected {ExpectedChecks}, got {checksDone}."
            );
        }

        int failed = checksDone - checksPassed;

        if (failed == 0)
        {
            Debug.Log($"[WaitForFuncTest] COMPLETE: {checksPassed}/{checksDone} checks passed.");
        }
        else
        {
            Debug.LogWarning(
                $"[WaitForFuncTest] COMPLETE: {checksPassed}/{checksDone} checks passed, {failed} failed."
            );
        }
    }
}

// 2021.02.17 12.52 am
