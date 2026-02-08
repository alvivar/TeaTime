using System.Collections.Generic;
using UnityEngine;

public class ReverseInfinite : MonoBehaviour
{
    private const int ReverseMarker = -1;
    private const int MaxEventsToSample = 15;
    private const int ExpectedChecks = 6;
    private const float ExpectedStepDelay = 1f;
    private const float DelayTolerance = 0.45f;
    private const float HarnessTimeoutSeconds = 25f;

    private TeaTime queue;

    private readonly List<int> events = new List<int>();
    private readonly List<float> eventTimes = new List<float>();

    private int reverseCount = 0;

    private int checksDone = 0;
    private int checksPassed = 0;

    private float harnessStartedAt = 0f;
    private bool evaluated = false;

    void Start()
    {
        Debug.Log("[ReverseInfiniteTest] Starting timing harness...");

        queue = this.tt("ReverseInfinite")
            .Add(1, () => MarkNumber(0))
            .Add(1, () => MarkNumber(1))
            .Add(1, () => MarkNumber(2))
            .Add(
                1,
                t =>
                {
                    MarkReverse();
                    t.self.Reverse();
                }
            )
            .Add(1, () => MarkNumber(3))
            .Add(1, () => MarkNumber(4))
            .Reverse()
            .Repeat();

        harnessStartedAt = Time.time;
    }

    void Update()
    {
        if (!evaluated && Time.time - harnessStartedAt >= HarnessTimeoutSeconds)
        {
            Debug.LogWarning(
                $"[ReverseInfiniteTest] Timeout after {HarnessTimeoutSeconds:0.0}s. Evaluating partial results."
            );
            EvaluateAndPrint();
        }
    }

    private void MarkNumber(int value)
    {
        MarkEvent(value);
    }

    private void MarkReverse()
    {
        reverseCount += 1;
        MarkEvent(ReverseMarker);
    }

    private void MarkEvent(int marker)
    {
        if (evaluated)
            return;

        float now = Time.time;

        events.Add(marker);
        eventTimes.Add(now);

        Debug.Log($"[ReverseInfiniteTest] Event #{events.Count}: {Label(marker)} at {now:0.000}s");

        if (events.Count >= MaxEventsToSample)
            EvaluateAndPrint();
    }

    private void EvaluateAndPrint()
    {
        if (evaluated)
            return;

        evaluated = true;

        if (queue != null)
            queue.Stop();

        CheckCondition(
            "Collected sample event count",
            events.Count >= MaxEventsToSample,
            $"Collected {events.Count}/{MaxEventsToSample} events."
        );

        bool rangeOk = true;
        for (int i = 0; i < events.Count; i++)
        {
            int m = events[i];
            if (m != ReverseMarker && (m < 0 || m > 4))
            {
                rangeOk = false;
                break;
            }
        }

        CheckCondition(
            "Numeric values remain in range 0..4",
            rangeOk,
            "Found numeric callback outside expected range."
        );

        CheckIntervals();

        bool initialPrefixOk =
            events.Count >= 3 && events[0] == 4 && events[1] == 3 && events[2] == ReverseMarker;

        CheckCondition(
            "Initial reversed prefix is 4 -> 3 -> REV",
            initialPrefixOk,
            $"Observed prefix: {Prefix(3)}"
        );

        CheckCondition(
            "Reverse callback executed at least twice",
            reverseCount >= 2,
            $"Reverse executed {reverseCount} time(s)."
        );

        int flips = CountDirectionFlipsAroundReverse();
        CheckCondition(
            "Direction flips around reverse callbacks",
            flips >= 2,
            $"Detected {flips} direction flip(s)."
        );

        PrintSummary();
    }

    private void CheckIntervals()
    {
        if (eventTimes.Count < 2)
        {
            CheckCondition(
                "Consecutive events are ~1 second apart",
                false,
                "Not enough events to compute intervals."
            );
            return;
        }

        bool pass = true;
        float avg = 0f;

        for (int i = 1; i < eventTimes.Count; i++)
        {
            float dt = eventTimes[i] - eventTimes[i - 1];
            avg += dt;

            if (Mathf.Abs(dt - ExpectedStepDelay) > DelayTolerance)
            {
                pass = false;
                Debug.LogWarning(
                    $"[ReverseInfiniteTest] Interval out of tolerance between event #{i} and #{i + 1}: {dt:0.000}s"
                );
            }
        }

        avg /= (eventTimes.Count - 1);

        CheckCondition(
            $"Consecutive events are ~1 second apart (avg {avg:0.000}s)",
            pass,
            $"One or more intervals were outside ±{DelayTolerance:0.000}s."
        );
    }

    private int CountDirectionFlipsAroundReverse()
    {
        int flips = 0;

        for (int i = 0; i < events.Count; i++)
        {
            if (events[i] != ReverseMarker)
                continue;

            int before = FindDirectionBeforeEvent(i);
            int after = FindDirectionAfterEvent(i);

            if (before != 0 && after != 0 && before == -after)
                flips += 1;
        }

        return flips;
    }

    private int FindDirectionBeforeEvent(int pivotEventIndex)
    {
        int newer = int.MinValue;

        for (int i = pivotEventIndex - 1; i >= 0; i--)
        {
            if (events[i] == ReverseMarker)
                continue;

            if (newer == int.MinValue)
            {
                newer = events[i];
                continue;
            }

            int dir = ComputeDirection(events[i], newer);
            if (dir != 0)
                return dir;

            newer = events[i];
        }

        return 0;
    }

    private int FindDirectionAfterEvent(int pivotEventIndex)
    {
        int older = int.MinValue;

        for (int i = pivotEventIndex + 1; i < events.Count; i++)
        {
            if (events[i] == ReverseMarker)
                continue;

            if (older == int.MinValue)
            {
                older = events[i];
                continue;
            }

            int dir = ComputeDirection(older, events[i]);
            if (dir != 0)
                return dir;

            older = events[i];
        }

        return 0;
    }

    private int ComputeDirection(int from, int to)
    {
        if (from == to)
            return 0;

        // Treat repeat boundary wraps as directional movement.
        if (from == 4 && to == 0)
            return 1;
        if (from == 0 && to == 4)
            return -1;

        return to > from ? 1 : -1;
    }

    private string Prefix(int length)
    {
        int count = Mathf.Min(length, events.Count);
        string result = "";

        for (int i = 0; i < count; i++)
        {
            if (i > 0)
                result += " -> ";

            result += Label(events[i]);
        }

        return result;
    }

    private string Label(int marker)
    {
        return marker == ReverseMarker ? "REV" : marker.ToString();
    }

    private void CheckCondition(string label, bool pass, string failReason)
    {
        checksDone += 1;
        if (pass)
            checksPassed += 1;

        if (pass)
            Debug.Log($"[ReverseInfiniteTest] PASS {label}");
        else
            Debug.LogWarning($"[ReverseInfiniteTest] FAIL {label}: {failReason}");
    }

    private void PrintSummary()
    {
        if (checksDone != ExpectedChecks)
        {
            Debug.LogWarning(
                $"[ReverseInfiniteTest] Summary check mismatch: expected {ExpectedChecks}, got {checksDone}."
            );
        }

        int failed = checksDone - checksPassed;
        if (failed == 0)
        {
            Debug.Log(
                $"[ReverseInfiniteTest] COMPLETE: {checksPassed}/{checksDone} checks passed."
            );
        }
        else
        {
            Debug.LogWarning(
                $"[ReverseInfiniteTest] COMPLETE: {checksPassed}/{checksDone} checks passed, {failed} failed."
            );
        }
    }
}

// 2021/10/03 02:04 am
