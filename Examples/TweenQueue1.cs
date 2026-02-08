// A queue that continues adding and executing new callbacks.

using System.Collections.Generic;
using UnityEngine;

public class TweenQueue1 : MonoBehaviour
{
    private enum TweenKind
    {
        Color,
        Scale,
    }

    private class TweenRecord
    {
        public string label;
        public TweenKind kind;

        public Color targetColor;
        public Vector3 targetScale;

        public bool started = false;
        public bool completed = false;

        public float startedAt = -1f;
        public float completedAt = -1f;
        public float maxTimeSinceStart = 0f;

        public int frameCount = 0;
        public bool sawPositiveDelta = false;

        public Color observedColor;
        public Vector3 observedScale;
        public bool finalValueCaptured = false;
    }

    private const int HarnessTweenCount = 4;
    private const int ExpectedChecks = 27;
    private const float TweenDuration = 1f;
    private const float HarnessTimeoutSeconds = 12f;

    public Transform t;
    public Renderer r;

    private TeaTime queue;

    private readonly List<TweenRecord> harnessRecords = new List<TweenRecord>();

    private int completedHarnessTweens = 0;
    private bool pendingEvaluation = false;
    private bool evaluated = false;

    private float harnessStartedAt = 0f;
    private float firstTweenStartedAt = -1f;
    private float lastTweenCompletedAt = -1f;

    private int checksDone = 0;
    private int checksPassed = 0;

    public void Start()
    {
        Debug.Log("[TweenQueue1Test] Starting timing harness...");

        if (r == null)
            r = GetComponent<Renderer>();

        if (t == null)
            t = r != null ? r.transform : transform;

        queue = new TeaTime(this);

        if (r != null)
        {
            r.material.color = Color.white;
            r.transform.localScale = Vector3.one;
        }

        if (t != null)
            t.localScale = Vector3.one;

        harnessStartedAt = Time.time;

        ScheduleHarnessScenario();
    }

    public void Update()
    {
        if (evaluated)
            return;

        if (pendingEvaluation && queue != null && queue.IsCompleted)
        {
            EvaluateAndPrint(false);
            return;
        }

        if (Time.time - harnessStartedAt >= HarnessTimeoutSeconds)
        {
            Debug.LogWarning(
                $"[TweenQueue1Test] Timeout after {HarnessTimeoutSeconds:0.0}s. Evaluating partial results."
            );
            EvaluateAndPrint(true);
        }
    }

    private void ScheduleHarnessScenario()
    {
        QueueColorTween(
            new Color(0.90f, 0.15f, 0.30f, 1f),
            CreateHarnessRecord("Color #1", TweenKind.Color)
        );

        QueueScaleTween(
            new Vector3(1.60f, 0.80f, 1.30f),
            CreateHarnessRecord("Scale #1", TweenKind.Scale)
        );

        QueueColorTween(
            new Color(0.10f, 0.70f, 0.25f, 1f),
            CreateHarnessRecord("Color #2", TweenKind.Color)
        );

        QueueScaleTween(
            new Vector3(0.65f, 1.75f, 1.15f),
            CreateHarnessRecord("Scale #2", TweenKind.Scale)
        );
    }

    public void RandomColor()
    {
        Color randomColor = new Color(Random.value, Random.value, Random.value, Random.value);

        QueueColorTween(randomColor, null);
    }

    public void RandomScale()
    {
        Vector3 randomScale = new Vector3(
            Random.Range(0.5f, 2f),
            Random.Range(0.5f, 2f),
            Random.Range(0.5f, 2f)
        );

        QueueScaleTween(randomScale, null);
    }

    private TweenRecord CreateHarnessRecord(string label, TweenKind kind)
    {
        var record = new TweenRecord();
        record.label = label;
        record.kind = kind;

        harnessRecords.Add(record);
        return record;
    }

    private void QueueColorTween(Color targetColor, TweenRecord record)
    {
        if (record != null)
            record.targetColor = targetColor;

        queue
            .Loop(
                TweenDuration,
                (TeaHandler handler) =>
                {
                    if (record != null)
                        TrackRecordFrame(record, handler);

                    if (r != null)
                    {
                        r.material.color = Color.Lerp(
                            r.material.color,
                            targetColor,
                            handler.deltaTime
                        );

                        if (record != null)
                            record.observedColor = r.material.color;
                    }
                }
            )
            .Add(() =>
            {
                if (record != null)
                    CompleteHarnessRecord(record);
            });
    }

    private void QueueScaleTween(Vector3 targetScale, TweenRecord record)
    {
        if (record != null)
            record.targetScale = targetScale;

        queue
            .Loop(
                TweenDuration,
                (TeaHandler handler) =>
                {
                    if (record != null)
                        TrackRecordFrame(record, handler);

                    if (r != null)
                    {
                        Vector3 sourceScale = t != null ? t.localScale : r.transform.localScale;

                        r.transform.localScale = Vector3.Lerp(
                            sourceScale,
                            targetScale,
                            handler.deltaTime
                        );

                        if (record != null)
                            record.observedScale = r.transform.localScale;
                    }
                }
            )
            .Add(() =>
            {
                if (record != null)
                    CompleteHarnessRecord(record);
            });
    }

    private void TrackRecordFrame(TweenRecord record, TeaHandler handler)
    {
        if (!record.started)
        {
            record.started = true;
            record.startedAt = Time.time;

            if (firstTweenStartedAt < 0f)
                firstTweenStartedAt = record.startedAt;

            Debug.Log($"[TweenQueue1Test] {record.label} started at {record.startedAt:0.000}s");
        }

        record.frameCount += 1;
        record.maxTimeSinceStart = Mathf.Max(record.maxTimeSinceStart, handler.timeSinceStart);

        if (handler.deltaTime > 0f)
            record.sawPositiveDelta = true;
    }

    private void CompleteHarnessRecord(TweenRecord record)
    {
        if (record.completed)
            return;

        record.completed = true;
        record.completedAt = Time.time;
        record.finalValueCaptured = r != null;

        if (record.kind == TweenKind.Color && r != null)
            record.observedColor = r.material.color;

        if (record.kind == TweenKind.Scale && r != null)
            record.observedScale = r.transform.localScale;

        completedHarnessTweens += 1;
        lastTweenCompletedAt = Mathf.Max(lastTweenCompletedAt, record.completedAt);

        Debug.Log($"[TweenQueue1Test] {record.label} completed at {record.completedAt:0.000}s");

        if (completedHarnessTweens >= HarnessTweenCount)
            pendingEvaluation = true;
    }

    private void EvaluateAndPrint(bool timedOut)
    {
        if (evaluated)
            return;

        evaluated = true;

        bool queueCompleted = queue != null && queue.IsCompleted;

        if (queue != null)
            queue.Stop();

        for (int i = 0; i < harnessRecords.Count; i++)
            EvaluateRecord(harnessRecords[i]);

        CheckCondition(
            "Expected tween completions observed",
            completedHarnessTweens == HarnessTweenCount,
            $"Completed {completedHarnessTweens}/{HarnessTweenCount} tweens."
        );

        CheckCondition(
            "Queue reached completed state",
            !timedOut && queueCompleted,
            timedOut ? "Queue did not complete before timeout." : "Queue.IsCompleted was false."
        );

        if (firstTweenStartedAt >= 0f && lastTweenCompletedAt >= 0f)
        {
            float total = lastTweenCompletedAt - firstTweenStartedAt;
            CheckApprox(
                "Queued tween sequence duration",
                HarnessTweenCount * TweenDuration,
                total,
                1.00f
            );
        }
        else
        {
            CheckCondition(
                "Queued tween sequence duration",
                false,
                "Missing start/completion timestamps."
            );
        }

        PrintSummary();
    }

    private void EvaluateRecord(TweenRecord record)
    {
        CheckCondition(
            $"{record.label} started",
            record.started,
            "Loop callback was never called."
        );

        CheckCondition(
            $"{record.label} had multiple frames",
            record.frameCount > 1,
            $"Observed {record.frameCount} frame(s)."
        );

        if (record.started && record.completed)
        {
            float wallDuration = record.completedAt - record.startedAt;
            CheckApprox($"{record.label} wall duration", TweenDuration, wallDuration, 0.40f);
        }
        else
        {
            CheckCondition(
                $"{record.label} wall duration",
                false,
                "Tween did not complete with both timestamps available."
            );
        }

        if (record.frameCount > 0)
        {
            CheckApprox(
                $"{record.label} handler duration",
                TweenDuration,
                record.maxTimeSinceStart,
                0.40f
            );
        }
        else
        {
            CheckCondition($"{record.label} handler duration", false, "No loop frames observed.");
        }

        CheckCondition(
            $"{record.label} positive deltaTime",
            record.sawPositiveDelta,
            "No positive deltaTime observed."
        );

        if (!record.finalValueCaptured)
        {
            CheckCondition(
                $"{record.label} reached target",
                false,
                "Final value was not captured (Renderer missing)."
            );
            return;
        }

        if (record.kind == TweenKind.Color)
        {
            float colorError = MaxColorComponentDelta(record.observedColor, record.targetColor);
            CheckApprox($"{record.label} final color error", 0f, colorError, 0.10f);
        }
        else
        {
            float scaleError = Vector3.Distance(record.observedScale, record.targetScale);
            CheckApprox($"{record.label} final scale error", 0f, scaleError, 0.15f);
        }
    }

    private float MaxColorComponentDelta(Color a, Color b)
    {
        return Mathf.Max(
            Mathf.Abs(a.r - b.r),
            Mathf.Abs(a.g - b.g),
            Mathf.Abs(a.b - b.b),
            Mathf.Abs(a.a - b.a)
        );
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
            $"[TweenQueue1Test] {verdict} {label}: expected {expectedSeconds:0.000}, "
            + $"actual {actualSeconds:0.000}, tolerance ±{tolerance:0.000}";

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
            Debug.Log($"[TweenQueue1Test] PASS {label}");
        else
            Debug.LogWarning($"[TweenQueue1Test] FAIL {label}: {failReason}");
    }

    private void PrintSummary()
    {
        if (checksDone != ExpectedChecks)
        {
            Debug.LogWarning(
                $"[TweenQueue1Test] Summary check mismatch: expected {ExpectedChecks}, got {checksDone}."
            );
        }

        int failed = checksDone - checksPassed;

        if (failed == 0)
        {
            Debug.Log($"[TweenQueue1Test] COMPLETE: {checksPassed}/{checksDone} checks passed.");
        }
        else
        {
            Debug.LogWarning(
                $"[TweenQueue1Test] COMPLETE: {checksPassed}/{checksDone} checks passed, {failed} failed."
            );
        }
    }
}

// 2015/10/05 05:54:55 PM
