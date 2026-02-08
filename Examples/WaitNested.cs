// A TeaTime that waits nested TeaTimes.

using UnityEngine;

public class WaitNested : MonoBehaviour
{
    private const int PlannedCycleCount = 3;
    private const int ExpectedChecks = 10;
    private const float HarnessTimeoutSeconds = 12f;

    private static readonly float[] PlannedDelays = { 1f, 2f, 3f };
    private static readonly string[] PlannedQueueNames = { "@1", "@2", "@3" };

    private TeaTime queue;

    private readonly float[] cycleStartTimes = new float[PlannedCycleCount];
    private readonly float[] nestedTimes = new float[PlannedCycleCount];
    private readonly bool[] cycleNestedSeen = new bool[PlannedCycleCount];
    private readonly string[] cycleQueueNames = new string[PlannedCycleCount];

    private int startedCycles = 0;
    private int nestedCallbacks = 0;

    private int activeCycle = -1;
    private int invalidNestedCallbacks = 0;

    private int checksDone = 0;
    private int checksPassed = 0;

    private bool pendingEvaluation = false;
    private bool evaluated = false;
    private float harnessStartedAt = 0f;

    void Start()
    {
        Debug.Log("[WaitNestedTest] Starting timing harness...");

        for (int i = 0; i < PlannedCycleCount; i++)
        {
            cycleStartTimes[i] = -1f;
            nestedTimes[i] = -1f;
        }

        queue = this.tt("@master")
            .Add(
                (TeaHandler t) =>
                {
                    // Stop after the planned deterministic sequence.
                    if (startedCycles >= PlannedCycleCount)
                    {
                        pendingEvaluation = true;
                        t.self.Stop();
                        return;
                    }

                    int cycleIndex = startedCycles;
                    float plannedDelay = PlannedDelays[cycleIndex];
                    string plannedQueueName = PlannedQueueNames[cycleIndex];

                    cycleStartTimes[cycleIndex] = Time.time;
                    cycleQueueNames[cycleIndex] = plannedQueueName;
                    startedCycles += 1;

                    activeCycle = cycleIndex;

                    TeaTime chosen = this.tt(plannedQueueName)
                        .Add(
                            plannedDelay,
                            (TeaHandler t2) =>
                            {
                                OnNestedCallback(plannedQueueName, plannedDelay);
                            }
                        )
                        .Immutable();

                    Debug.Log(
                        $"[WaitNestedTest] Cycle {cycleIndex + 1}: waiting {plannedDelay:0.000}s via {plannedQueueName} at {cycleStartTimes[cycleIndex]:0.000}s"
                    );

                    t.Wait(chosen);
                }
            )
            .Repeat();

        harnessStartedAt = Time.time;
    }

    void Update()
    {
        if (evaluated)
            return;

        if (pendingEvaluation && queue != null && !queue.IsPlaying)
        {
            EvaluateAndPrint(false);
            return;
        }

        if (Time.time - harnessStartedAt >= HarnessTimeoutSeconds)
        {
            Debug.LogWarning(
                $"[WaitNestedTest] Timeout after {HarnessTimeoutSeconds:0.0}s. Evaluating partial results."
            );
            EvaluateAndPrint(true);
        }
    }

    private void OnNestedCallback(string queueName, float plannedDelay)
    {
        float now = Time.time;

        if (activeCycle < 0 || activeCycle >= PlannedCycleCount)
        {
            invalidNestedCallbacks += 1;
            Debug.LogWarning(
                $"[WaitNestedTest] Unexpected nested callback from {queueName} at {now:0.000}s"
            );
            return;
        }

        int cycleIndex = activeCycle;

        if (cycleNestedSeen[cycleIndex])
        {
            invalidNestedCallbacks += 1;
            Debug.LogWarning(
                $"[WaitNestedTest] Duplicate nested callback for cycle {cycleIndex + 1} via {queueName} at {now:0.000}s"
            );
            return;
        }

        cycleNestedSeen[cycleIndex] = true;
        nestedTimes[cycleIndex] = now;
        nestedCallbacks += 1;

        Debug.Log(
            $"[WaitNestedTest] Nested callback cycle {cycleIndex + 1} via {queueName} at {now:0.000}s (planned {plannedDelay:0.000}s)"
        );

        activeCycle = -1;
    }

    private void EvaluateAndPrint(bool timedOut)
    {
        if (evaluated)
            return;

        evaluated = true;

        bool queueCompleted = queue != null && queue.IsCompleted;

        if (queue != null)
            queue.Stop();

        CheckCondition(
            "Started expected number of cycles",
            startedCycles == PlannedCycleCount,
            $"Started {startedCycles}/{PlannedCycleCount}."
        );

        CheckCondition(
            "Observed expected nested callbacks",
            nestedCallbacks == PlannedCycleCount,
            $"Observed {nestedCallbacks}/{PlannedCycleCount}."
        );

        CheckCondition(
            "No invalid nested callbacks",
            invalidNestedCallbacks == 0,
            $"Invalid nested callback count = {invalidNestedCallbacks}."
        );

        bool queueOrderOk = true;
        for (int i = 0; i < PlannedCycleCount; i++)
        {
            if (cycleQueueNames[i] != PlannedQueueNames[i])
            {
                queueOrderOk = false;
                break;
            }
        }

        CheckCondition(
            "Nested queue order is @1 -> @2 -> @3",
            queueOrderOk,
            $"Observed: {FormatObservedQueueOrder()}"
        );

        for (int i = 0; i < PlannedCycleCount; i++)
        {
            CheckCycleWaitDuration(i);
        }

        for (int i = 0; i < PlannedCycleCount - 1; i++)
        {
            CheckCycleStartInterval(i, i + 1);
        }

        if (cycleStartTimes[0] >= 0f && nestedTimes[PlannedCycleCount - 1] >= 0f)
        {
            float total = nestedTimes[PlannedCycleCount - 1] - cycleStartTimes[0];
            CheckApprox("Total planned nested duration", 6f, total, 0.90f);
        }
        else
        {
            CheckCondition(
                "Total planned nested duration",
                false,
                "Missing first cycle start or last nested callback timestamp."
            );
        }

        if (timedOut)
        {
            Debug.LogWarning("[WaitNestedTest] Evaluation was triggered by timeout.");
        }
        else if (!queueCompleted)
        {
            Debug.LogWarning(
                "[WaitNestedTest] Queue did not report IsCompleted before stop (expected with Stop semantics)."
            );
        }

        PrintSummary();
    }

    private void CheckCycleWaitDuration(int cycleIndex)
    {
        string label = $"Cycle {cycleIndex + 1} nested wait duration";

        if (cycleStartTimes[cycleIndex] < 0f || nestedTimes[cycleIndex] < 0f)
        {
            CheckCondition(label, false, "Missing cycle start or nested callback timestamp.");
            return;
        }

        float actual = nestedTimes[cycleIndex] - cycleStartTimes[cycleIndex];
        float expected = PlannedDelays[cycleIndex];

        CheckApprox(label, expected, actual, 0.35f);
    }

    private void CheckCycleStartInterval(int fromCycle, int toCycle)
    {
        string label = $"Cycle {fromCycle + 1} -> {toCycle + 1} start interval";

        if (cycleStartTimes[fromCycle] < 0f || cycleStartTimes[toCycle] < 0f)
        {
            CheckCondition(label, false, "Missing cycle start timestamps.");
            return;
        }

        float actual = cycleStartTimes[toCycle] - cycleStartTimes[fromCycle];
        float expected = PlannedDelays[fromCycle];

        CheckApprox(label, expected, actual, 0.40f);
    }

    private string FormatObservedQueueOrder()
    {
        string s = "";

        for (int i = 0; i < PlannedCycleCount; i++)
        {
            if (i > 0)
                s += " -> ";

            string name = string.IsNullOrEmpty(cycleQueueNames[i])
                ? "(missing)"
                : cycleQueueNames[i];
            s += name;
        }

        return s;
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
            $"[WaitNestedTest] {verdict} {label}: expected {expected:0.000}s, "
            + $"actual {actual:0.000}s, tolerance ±{tolerance:0.000}s";

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
            Debug.Log($"[WaitNestedTest] PASS {label}");
        else
            Debug.LogWarning($"[WaitNestedTest] FAIL {label}: {failReason}");
    }

    private void PrintSummary()
    {
        if (checksDone != ExpectedChecks)
        {
            Debug.LogWarning(
                $"[WaitNestedTest] Summary check mismatch: expected {ExpectedChecks}, got {checksDone}."
            );
        }

        int failed = checksDone - checksPassed;

        if (failed == 0)
        {
            Debug.Log($"[WaitNestedTest] COMPLETE: {checksPassed}/{checksDone} checks passed.");
        }
        else
        {
            Debug.LogWarning(
                $"[WaitNestedTest] COMPLETE: {checksPassed}/{checksDone} checks passed, {failed} failed."
            );
        }
    }
}

// 2017/03/04 01:37 PM
