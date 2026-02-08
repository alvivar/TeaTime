// If every callback follows in order and roughly 1 second apart,
// then everything is fine.

using UnityEngine;

public class OneSecondDelay : MonoBehaviour
{
    private const int StepStart0 = 0;
    private const int Step1 = 1;
    private const int Step2 = 2;
    private const int Step3 = 3;
    private const int Step4 = 4;
    private const int Step5 = 5;
    private const int Step6 = 6;
    private const int Step7 = 7;
    private const int Step8 = 8;
    private const int Step9 = 9;
    private const int StepEnd0 = 10;

    private const int StepCount = 11;
    private const int ExpectedChecks = 13;
    private const float HarnessTimeoutSeconds = 20f;

    private static readonly string[] StepNames =
    {
        "Start 0",
        "Step 1",
        "Step 2",
        "Step 3",
        "Step 4",
        "Step 5",
        "Step 6",
        "Step 7",
        "Step 8",
        "Step 9",
        "End 0",
    };

    private TeaTime queue;

    private readonly float[] stepTimes = new float[StepCount];
    private readonly bool[] stepSeen = new bool[StepCount];

    private int stepSeenCount = 0;
    private int nextExpectedStep = 0;
    private bool orderOk = true;

    private int checksDone = 0;
    private int checksPassed = 0;

    private float harnessStartedAt = 0f;
    private bool evaluated = false;

    void Start()
    {
        for (int i = 0; i < StepCount; i++)
            stepTimes[i] = -1f;

        Debug.Log("[OneSecondDelayTest] Starting timing harness...");

        queue = this.tt()
            .Pause()
            .Add(() => MarkStep(StepStart0))
            .Add(1, () => MarkStep(Step1))
            .Add(() => 1, () => MarkStep(Step2))
            .Add(
                1,
                (TeaHandler t) =>
                {
                    MarkStep(Step3);
                    t.Wait(1);
                }
            )
            .Add(() => MarkStep(Step4))
            .Loop(1, (TeaHandler t) => { })
            .Add(() => MarkStep(Step5))
            .Loop(
                (TeaHandler t) =>
                {
                    if (t.timeSinceStart >= 1)
                        t.Break();
                }
            )
            .Add(() => MarkStep(Step6))
            .Loop(
                0,
                (TeaHandler t) => {
                    // Ignorable loop
                }
            )
            .Add(1, () => MarkStep(Step7))
            .Add(
                (TeaHandler t) =>
                {
                    // WaitFor a Loop and an Add
                    t.Wait(
                        this.tt()
                            .Loop(0.5f, (TeaHandler) => { })
                            .Add(0.5f, () => MarkStep(Step8))
                            .WaitForCompletion()
                    );
                }
            )
            .Add(1, () => MarkStep(Step9))
            .Loop(t =>
            {
                MarkStep(StepEnd0);
                t.Break();
            })
            .Immutable();

        harnessStartedAt = Time.time;
        queue.Play();
    }

    void Update()
    {
        if (!evaluated && Time.time - harnessStartedAt >= HarnessTimeoutSeconds)
        {
            Debug.LogWarning(
                $"[OneSecondDelayTest] Timeout after {HarnessTimeoutSeconds:0.0}s. Evaluating partial results."
            );
            EvaluateAndPrint();
        }
    }

    private void MarkStep(int stepIndex)
    {
        if (evaluated)
            return;

        float now = Time.time;

        if (stepSeen[stepIndex])
        {
            orderOk = false;
            Debug.LogWarning(
                $"[OneSecondDelayTest] Duplicate step: {StepNames[stepIndex]} at {now:0.000}s"
            );
            return;
        }

        stepSeen[stepIndex] = true;
        stepTimes[stepIndex] = now;
        stepSeenCount += 1;

        if (stepIndex != nextExpectedStep)
        {
            orderOk = false;

            string expected = nextExpectedStep < StepCount ? StepNames[nextExpectedStep] : "(none)";

            Debug.LogWarning(
                $"[OneSecondDelayTest] Order mismatch: expected '{expected}', got '{StepNames[stepIndex]}' at {now:0.000}s"
            );
        }

        while (nextExpectedStep < StepCount && stepSeen[nextExpectedStep])
            nextExpectedStep += 1;

        Debug.Log($"[OneSecondDelayTest] Mark {StepNames[stepIndex]} at {now:0.000}s");

        if (stepIndex == StepEnd0)
            EvaluateAndPrint();
    }

    private void EvaluateAndPrint()
    {
        if (evaluated)
            return;

        evaluated = true;

        CheckCondition(
            "All expected steps observed",
            stepSeenCount == StepCount,
            $"Observed {stepSeenCount}/{StepCount} steps."
        );

        CheckCondition(
            "Strict step order",
            orderOk,
            "Callbacks were not executed in the expected order."
        );

        CheckIntervalApprox("Start 0 -> Step 1", StepStart0, Step1, 1f, 0.45f);
        CheckIntervalApprox("Step 1 -> Step 2", Step1, Step2, 1f, 0.45f);
        CheckIntervalApprox("Step 2 -> Step 3", Step2, Step3, 1f, 0.45f);
        CheckIntervalApprox("Step 3 -> Step 4", Step3, Step4, 1f, 0.45f);
        CheckIntervalApprox("Step 4 -> Step 5", Step4, Step5, 1f, 0.50f);
        CheckIntervalApprox("Step 5 -> Step 6", Step5, Step6, 1f, 0.50f);
        CheckIntervalApprox("Step 6 -> Step 7", Step6, Step7, 1f, 0.45f);
        CheckIntervalApprox("Step 7 -> Step 8", Step7, Step8, 1f, 0.55f);
        CheckIntervalApprox("Step 8 -> Step 9", Step8, Step9, 1f, 0.45f);

        // End loop should run immediately after Step 9.
        CheckIntervalApprox("Step 9 -> End 0", Step9, StepEnd0, 0f, 0.20f);

        // Whole sequence should be roughly 9 seconds.
        CheckIntervalApprox("Start 0 -> End 0 total", StepStart0, StepEnd0, 9f, 1.00f);

        PrintSummary();
    }

    private void CheckIntervalApprox(
        string label,
        int fromStep,
        int toStep,
        float expectedSeconds,
        float tolerance
    )
    {
        if (!stepSeen[fromStep] || !stepSeen[toStep])
        {
            CheckCondition(
                label,
                false,
                $"Missing marks: '{StepNames[fromStep]}' or '{StepNames[toStep]}'."
            );
            return;
        }

        float actual = stepTimes[toStep] - stepTimes[fromStep];
        CheckApprox(label, expectedSeconds, actual, tolerance);
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
            $"[OneSecondDelayTest] {verdict} {label}: expected {expectedSeconds:0.000}s, "
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
        {
            Debug.Log($"[OneSecondDelayTest] PASS {label}");
        }
        else
        {
            Debug.LogWarning($"[OneSecondDelayTest] FAIL {label}: {failReason}");
        }
    }

    private void PrintSummary()
    {
        if (checksDone != ExpectedChecks)
        {
            Debug.LogWarning(
                $"[OneSecondDelayTest] Summary check mismatch: expected {ExpectedChecks}, got {checksDone}."
            );
        }

        int failed = checksDone - checksPassed;

        if (failed == 0)
        {
            Debug.Log($"[OneSecondDelayTest] COMPLETE: {checksPassed}/{checksDone} checks passed.");
        }
        else
        {
            Debug.LogWarning(
                $"[OneSecondDelayTest] COMPLETE: {checksPassed}/{checksDone} checks passed, {failed} failed."
            );
        }
    }
}

// 2015/09/15 12:47:29 PM
