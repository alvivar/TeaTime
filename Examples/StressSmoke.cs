// Deterministic stress smoke test for TeaTime.
// Focus: append-while-running, pause/resume churn, and finite completion.

using UnityEngine;

public class StressSmoke : MonoBehaviour
{
    private const int WorkerCount = 24;
    private const float WorkerLoopDuration = 2f;

    private const float AppendPhaseDuration = 2f;
    private const float AppendTick = 0.05f;
    private const int AppendBatchSize = 2;
    private const float AppendedTaskDelay = 0.02f;

    private const int MinLoopFramesPerWorker = 25;

    private const float HarnessTimeoutSeconds = 15f;
    private const int ExpectedChecks = 9;

    private TeaTime[] workers;
    private int[] loopFrames;
    private bool[] workerStarted;
    private bool[] workerCompleted;

    private int workerStartedCount = 0;
    private int workerCompletedCount = 0;

    private TeaTime producerQueue;
    private TeaTime churnQueue;

    private bool producerDone = false;
    private bool pauseIssued = false;
    private bool resumeIssued = false;

    private int appendedTasksScheduled = 0;
    private int appendedTaskCallbacks = 0;

    private float harnessStartedAt = 0f;
    private bool evaluated = false;

    private int checksDone = 0;
    private int checksPassed = 0;

    void Start()
    {
        Debug.Log("[StressSmokeTest] Starting deterministic stress smoke test...");

        workers = new TeaTime[WorkerCount];
        loopFrames = new int[WorkerCount];
        workerStarted = new bool[WorkerCount];
        workerCompleted = new bool[WorkerCount];

        for (int i = 0; i < WorkerCount; i++)
        {
            int workerIndex = i;

            workers[i] = new TeaTime(this)
                .Add(() =>
                {
                    if (!workerStarted[workerIndex])
                    {
                        workerStarted[workerIndex] = true;
                        workerStartedCount += 1;
                    }
                })
                .Loop(
                    WorkerLoopDuration,
                    (TeaHandler t) =>
                    {
                        loopFrames[workerIndex] += 1;
                    }
                )
                .Add(() =>
                {
                    if (!workerCompleted[workerIndex])
                    {
                        workerCompleted[workerIndex] = true;
                        workerCompletedCount += 1;
                    }
                });
        }

        float nextAppendAt = 0f;
        int roundRobinWorker = 0;

        producerQueue = new TeaTime(this)
            .Loop(
                AppendPhaseDuration,
                (TeaHandler t) =>
                {
                    while (t.timeSinceStart >= nextAppendAt)
                    {
                        for (int b = 0; b < AppendBatchSize; b++)
                        {
                            int workerIndex = roundRobinWorker % WorkerCount;
                            roundRobinWorker += 1;

                            appendedTasksScheduled += 1;

                            workers[workerIndex]
                                .Add(
                                    AppendedTaskDelay,
                                    () =>
                                    {
                                        appendedTaskCallbacks += 1;
                                    }
                                );
                        }

                        nextAppendAt += AppendTick;
                    }
                }
            )
            .Add(() =>
            {
                producerDone = true;
                Debug.Log(
                    $"[StressSmokeTest] Append phase done. Scheduled={appendedTasksScheduled}"
                );
            });

        churnQueue = new TeaTime(this)
            .Add(
                0.30f,
                () =>
                {
                    pauseIssued = true;
                    PauseEvenWorkers();
                    Debug.Log("[StressSmokeTest] Pause issued for even-index workers.");
                }
            )
            .Add(
                0.20f,
                () =>
                {
                    resumeIssued = true;
                    ResumeEvenWorkers();
                    Debug.Log("[StressSmokeTest] Resume issued for even-index workers.");
                }
            );

        harnessStartedAt = Time.time;
    }

    void Update()
    {
        if (evaluated)
            return;

        if (AllQueuesCompleted())
        {
            EvaluateAndPrint(false);
            return;
        }

        if (Time.time - harnessStartedAt >= HarnessTimeoutSeconds)
        {
            Debug.LogWarning(
                $"[StressSmokeTest] Timeout after {HarnessTimeoutSeconds:0.0}s. Evaluating partial results."
            );
            EvaluateAndPrint(true);
        }
    }

    private void PauseEvenWorkers()
    {
        for (int i = 0; i < WorkerCount; i += 2)
        {
            if (workers[i] != null && workers[i].IsPlaying)
                workers[i].Pause();
        }
    }

    private void ResumeEvenWorkers()
    {
        for (int i = 0; i < WorkerCount; i += 2)
        {
            // Important: guard with IsPlaying to avoid replaying completed queues.
            if (workers[i] != null && workers[i].IsPlaying)
                workers[i].Play();
        }
    }

    private bool AllQueuesCompleted()
    {
        if (!producerDone)
            return false;

        if (producerQueue == null || !producerQueue.IsCompleted)
            return false;

        if (churnQueue == null || !churnQueue.IsCompleted)
            return false;

        for (int i = 0; i < WorkerCount; i++)
        {
            if (workers[i] == null || !workers[i].IsCompleted)
                return false;
        }

        return true;
    }

    private void EvaluateAndPrint(bool timedOut)
    {
        if (evaluated)
            return;

        evaluated = true;

        bool producerCompleted = producerQueue != null && producerQueue.IsCompleted;
        bool churnCompleted = churnQueue != null && churnQueue.IsCompleted;

        bool allWorkersCompleted = true;
        int totalLoopFrames = 0;
        int minLoopFrames = int.MaxValue;

        for (int i = 0; i < WorkerCount; i++)
        {
            if (workers[i] == null || !workers[i].IsCompleted)
                allWorkersCompleted = false;

            int frames = loopFrames[i];
            totalLoopFrames += frames;
            minLoopFrames = Mathf.Min(minLoopFrames, frames);
        }

        float avgLoopFrames = totalLoopFrames / (float)WorkerCount;
        float totalDuration = Time.time - harnessStartedAt;

        if (producerQueue != null)
            producerQueue.Stop();

        if (churnQueue != null)
            churnQueue.Stop();

        for (int i = 0; i < WorkerCount; i++)
        {
            if (workers[i] != null)
                workers[i].Stop();
        }

        CheckCondition(
            "Producer queue completed",
            !timedOut && producerCompleted,
            timedOut
                ? "Producer did not complete before timeout."
                : "Producer IsCompleted was false."
        );

        CheckCondition(
            "Pause/resume churn queue completed",
            !timedOut && churnCompleted,
            timedOut
                ? "Churn queue did not complete before timeout."
                : "Churn IsCompleted was false."
        );

        CheckCondition(
            "All workers started",
            workerStartedCount == WorkerCount,
            $"Started {workerStartedCount}/{WorkerCount} workers."
        );

        CheckCondition(
            "All workers completed",
            allWorkersCompleted && workerCompletedCount == WorkerCount,
            $"Completed {workerCompletedCount}/{WorkerCount} workers."
        );

        CheckCondition(
            $"Workers produced enough loop frames (min={minLoopFrames}, avg={avgLoopFrames:0.0})",
            minLoopFrames >= MinLoopFramesPerWorker,
            $"Expected at least {MinLoopFramesPerWorker} loop frames per worker."
        );

        CheckCondition(
            "Append phase scheduled tasks",
            appendedTasksScheduled > 0,
            "No appended tasks were scheduled."
        );

        CheckCondition(
            "All appended callbacks executed",
            appendedTaskCallbacks == appendedTasksScheduled,
            $"Executed {appendedTaskCallbacks}/{appendedTasksScheduled} appended callbacks."
        );

        CheckCondition(
            "Pause/resume commands were issued",
            pauseIssued && resumeIssued,
            $"pauseIssued={pauseIssued}, resumeIssued={resumeIssued}."
        );

        CheckCondition(
            "Total run duration stayed in expected window",
            totalDuration >= 1.5f && totalDuration <= 8f,
            $"Observed duration {totalDuration:0.000}s (expected 1.5s..8.0s)."
        );

        PrintSummary();
    }

    private void CheckCondition(string label, bool pass, string failReason)
    {
        checksDone += 1;
        if (pass)
            checksPassed += 1;

        if (pass)
            Debug.Log($"[StressSmokeTest] PASS {label}");
        else
            Debug.LogWarning($"[StressSmokeTest] FAIL {label}: {failReason}");
    }

    private void PrintSummary()
    {
        if (checksDone != ExpectedChecks)
        {
            Debug.LogWarning(
                $"[StressSmokeTest] Summary check mismatch: expected {ExpectedChecks}, got {checksDone}."
            );
        }

        int failed = checksDone - checksPassed;

        if (failed == 0)
        {
            Debug.Log($"[StressSmokeTest] COMPLETE: {checksPassed}/{checksDone} checks passed.");
        }
        else
        {
            Debug.LogWarning(
                $"[StressSmokeTest] COMPLETE: {checksPassed}/{checksDone} checks passed, {failed} failed."
            );
        }
    }
}
