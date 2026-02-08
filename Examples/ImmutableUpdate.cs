using UnityEngine;

public class ImmutableUpdate : MonoBehaviour
{
    private const int ExpectedChecks = 4;

    private int checksDone = 0;
    private int checksPassed = 0;
    private bool summaryPrinted = false;

    private float timeLock1LastAt = -1f;
    private float timeLock1IntervalSum = 0f;
    private int timeLock1IntervalCount = 0;
    private bool timeLock1Checked = false;

    private int timeLock2ExpectedStep = 0;
    private bool timeLock2OrderOk = true;
    private int timeLock2CompletedCycles = 0;
    private bool timeLock2OrderChecked = false;

    private float timeLock2CAt = -1f;
    private bool timeLock2CDelayChecked = false;

    private float timeLock2LastDAt = -1f;
    private float timeLock2DIntervalSum = 0f;
    private int timeLock2DIntervalCount = 0;
    private bool timeLock2CycleChecked = false;

    void Start()
    {
        Debug.Log("[ImmutableUpdateTest] Starting immutable timing harness...");
    }

    void Update()
    {
        // Both TeaTimes below can lock their execution for some time, even when
        // called multiple times (like inside Update).

        // One second time lock.
        var timeLock1 = this.tt("timeLock1")
            .Add(() =>
            {
                float now = Time.time;

                if (timeLock1LastAt >= 0f)
                {
                    float interval = now - timeLock1LastAt;
                    timeLock1IntervalSum += interval;
                    timeLock1IntervalCount += 1;

                    Debug.Log(
                        $"[ImmutableUpdateTest] timeLock1 interval #{timeLock1IntervalCount}: {interval:0.000}s"
                    );

                    if (!timeLock1Checked && timeLock1IntervalCount >= 2)
                    {
                        float averageInterval = timeLock1IntervalSum / timeLock1IntervalCount;
                        CheckApprox("timeLock1 callback interval", 1f, averageInterval, 0.45f);
                        timeLock1Checked = true;
                    }
                }

                timeLock1LastAt = now;
            })
            .Add(1, t => t.self.Reset())
            .Immutable();

        // Wait one second.
        var timeLock2 = this.tt("timeLock2")
            .Add(() => OnTimeLock2Step(0))
            .Add(() => OnTimeLock2Step(1))
            .Add(() => OnTimeLock2Step(2))
            .Add(
                1,
                t =>
                {
                    OnTimeLock2Step(3);
                    t.self.Reset();
                }
            )
            .Immutable();

        // Immutable makes sure they don't change. Reset could clean the TeaTime
        // if needed, so it can be rebuilt and played again when their execution
        // is completed.
    }

    private void OnTimeLock2Step(int stepIndex)
    {
        string stepName =
            stepIndex == 0 ? "A"
            : stepIndex == 1 ? "B"
            : stepIndex == 2 ? "C"
            : "D";

        if (timeLock2ExpectedStep != stepIndex)
        {
            timeLock2OrderOk = false;
            Debug.LogWarning(
                $"[ImmutableUpdateTest] timeLock2 order mismatch: expected #{timeLock2ExpectedStep}, got {stepName}"
            );
        }

        timeLock2ExpectedStep = (stepIndex + 1) % 4;

        if (stepIndex == 2)
        {
            timeLock2CAt = Time.time;
            return;
        }

        if (stepIndex != 3)
            return;

        float now = Time.time;

        if (!timeLock2CDelayChecked && timeLock2CAt >= 0f)
        {
            float cToD = now - timeLock2CAt;
            CheckApprox("timeLock2 C->D delay", 1f, cToD, 0.35f);
            timeLock2CDelayChecked = true;
        }

        if (timeLock2LastDAt >= 0f)
        {
            float dToD = now - timeLock2LastDAt;
            timeLock2DIntervalSum += dToD;
            timeLock2DIntervalCount += 1;

            Debug.Log(
                $"[ImmutableUpdateTest] timeLock2 D->D interval #{timeLock2DIntervalCount}: {dToD:0.000}s"
            );

            if (!timeLock2CycleChecked && timeLock2DIntervalCount >= 2)
            {
                float averageInterval = timeLock2DIntervalSum / timeLock2DIntervalCount;
                CheckApprox("timeLock2 cycle interval (D->D)", 1f, averageInterval, 0.50f);
                timeLock2CycleChecked = true;
            }
        }

        timeLock2LastDAt = now;
        timeLock2CompletedCycles += 1;

        if (!timeLock2OrderChecked && timeLock2CompletedCycles >= 2)
        {
            CheckCondition(
                "timeLock2 order A->B->C->D",
                timeLock2OrderOk,
                "Callbacks were not executed in strict order."
            );
            timeLock2OrderChecked = true;
        }
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
            $"[ImmutableUpdateTest] {verdict} {label}: expected {expectedSeconds:0.000}s, "
            + $"actual {actualSeconds:0.000}s, tolerance ±{tolerance:0.000}s";

        if (pass)
            Debug.Log(msg);
        else
            Debug.LogWarning(msg);

        TryPrintSummary();
    }

    private void CheckCondition(string label, bool pass, string failReason)
    {
        checksDone += 1;
        if (pass)
            checksPassed += 1;

        if (pass)
        {
            Debug.Log($"[ImmutableUpdateTest] PASS {label}");
        }
        else
        {
            Debug.LogWarning($"[ImmutableUpdateTest] FAIL {label}: {failReason}");
        }

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
                $"[ImmutableUpdateTest] COMPLETE: {checksPassed}/{checksDone} checks passed."
            );
        }
        else
        {
            Debug.LogWarning(
                $"[ImmutableUpdateTest] COMPLETE: {checksPassed}/{checksDone} checks passed, {failed} failed."
            );
        }
    }
}

// 2021/03/03 09:55 pm
