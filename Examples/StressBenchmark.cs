// Metrics-oriented TeaTime benchmark.
// Focus: sustained callback throughput, frame stability, append backlog, and GC drift.

using System;
using System.Text;
using UnityEngine;

public class StressBenchmark : MonoBehaviour
{
    [Header("Benchmark window")]
    public float benchmarkDuration = 10f;
    public float drainDuration = 2f;

    [Header("Worker load")]
    public int workerQueueCount = 24;
    public float workerLoopDuration = 0.5f;

    [Header("Append mutation load")]
    public bool includeAppendMutations = true;
    public float appendTick = 0.10f;
    public int appendBatchSize = 2;
    public float appendedTaskDelay = 0.02f;

    private TeaTime[] workers;
    private int[] workerLoopCallbacks;
    private int[] workerLoopCompletions;

    private long totalLoopCallbacks = 0;
    private long totalAddCallbacks = 0;

    private int appendedScheduled = 0;
    private int appendRoundRobin = 0;
    private float nextAppendAt = 0f;
    private int maxPendingAppends = 0;

    private int frameSamples = 0;
    private float frameTimeSum = 0f;
    private float frameTimeMin = float.MaxValue;
    private float frameTimeMax = 0f;

    private int gc0Start = 0;
    private int gc1Start = 0;
    private int gc2Start = 0;
    private long managedMemoryStart = 0;

    private float startedAtRealtime = 0f;
    private bool enteredDrainPhase = false;
    private bool reportPrinted = false;

    void Start()
    {
        benchmarkDuration = Mathf.Max(0.25f, benchmarkDuration);
        drainDuration = Mathf.Max(0f, drainDuration);

        workerQueueCount = Mathf.Max(1, workerQueueCount);
        workerLoopDuration = Mathf.Max(0.05f, workerLoopDuration);

        appendTick = Mathf.Max(0.01f, appendTick);
        appendBatchSize = Mathf.Max(1, appendBatchSize);
        appendedTaskDelay = Mathf.Max(0f, appendedTaskDelay);

        workers = new TeaTime[workerQueueCount];
        workerLoopCallbacks = new int[workerQueueCount];
        workerLoopCompletions = new int[workerQueueCount];

        for (int i = 0; i < workerQueueCount; i++)
        {
            int workerIndex = i;

            workers[i] = new TeaTime(this)
                .Loop(
                    workerLoopDuration,
                    (TeaHandler t) =>
                    {
                        totalLoopCallbacks += 1;
                        workerLoopCallbacks[workerIndex] += 1;
                    }
                )
                .Add(() =>
                {
                    workerLoopCompletions[workerIndex] += 1;
                })
                .Repeat();
        }

        gc0Start = GC.CollectionCount(0);
        gc1Start = GC.CollectionCount(1);
        gc2Start = GC.CollectionCount(2);
        managedMemoryStart = GC.GetTotalMemory(false);

        startedAtRealtime = Time.realtimeSinceStartup;

        Debug.Log(
            "[StressBenchmark] Started: "
                + $"workers={workerQueueCount}, workerLoopDuration={workerLoopDuration:0.000}s, "
                + $"benchmark={benchmarkDuration:0.00}s, drain={drainDuration:0.00}s, "
                + $"appendMutations={(includeAppendMutations ? "on" : "off")}."
        );
    }

    void Update()
    {
        if (reportPrinted)
            return;

        float elapsed = Time.realtimeSinceStartup - startedAtRealtime;

        SampleFrame();

        if (includeAppendMutations && elapsed <= benchmarkDuration)
            RunAppendMutations(elapsed);

        int pending = appendedScheduled - (int)totalAddCallbacks;
        maxPendingAppends = Mathf.Max(maxPendingAppends, pending);

        if (!enteredDrainPhase && elapsed > benchmarkDuration)
        {
            enteredDrainPhase = true;
            Debug.Log(
                $"[StressBenchmark] Entering drain phase ({drainDuration:0.00}s). Pending appends={pending}."
            );
        }

        if (elapsed >= benchmarkDuration + drainDuration)
            CompleteBenchmark();
    }

    private void SampleFrame()
    {
        float dt = Time.unscaledDeltaTime;

        frameSamples += 1;
        frameTimeSum += dt;

        if (dt < frameTimeMin)
            frameTimeMin = dt;

        if (dt > frameTimeMax)
            frameTimeMax = dt;
    }

    private void RunAppendMutations(float elapsed)
    {
        while (elapsed >= nextAppendAt)
        {
            for (int b = 0; b < appendBatchSize; b++)
            {
                int workerIndex = appendRoundRobin % workerQueueCount;
                appendRoundRobin += 1;

                appendedScheduled += 1;

                workers[workerIndex]
                    .Add(
                        appendedTaskDelay,
                        () =>
                        {
                            totalAddCallbacks += 1;
                        }
                    );
            }

            nextAppendAt += appendTick;
        }
    }

    private void CompleteBenchmark()
    {
        if (reportPrinted)
            return;

        reportPrinted = true;

        float endedAtRealtime = Time.realtimeSinceStartup;
        float elapsed = endedAtRealtime - startedAtRealtime;

        int gc0Delta = GC.CollectionCount(0) - gc0Start;
        int gc1Delta = GC.CollectionCount(1) - gc1Start;
        int gc2Delta = GC.CollectionCount(2) - gc2Start;

        long managedMemoryEnd = GC.GetTotalMemory(false);
        long managedMemoryDelta = managedMemoryEnd - managedMemoryStart;

        int minWorkerCallbacks = int.MaxValue;
        int maxWorkerCallbacks = 0;
        int minWorkerCompletions = int.MaxValue;
        int maxWorkerCompletions = 0;

        long sumWorkerCallbacks = 0;
        long sumWorkerCompletions = 0;

        int totalQueueCount = 0;
        int totalQueueCurrent = 0;
        int totalQueueExecuted = 0;
        int playingQueueCount = 0;

        for (int i = 0; i < workerQueueCount; i++)
        {
            int callbacks = workerLoopCallbacks[i];
            int completions = workerLoopCompletions[i];

            minWorkerCallbacks = Mathf.Min(minWorkerCallbacks, callbacks);
            maxWorkerCallbacks = Mathf.Max(maxWorkerCallbacks, callbacks);

            minWorkerCompletions = Mathf.Min(minWorkerCompletions, completions);
            maxWorkerCompletions = Mathf.Max(maxWorkerCompletions, completions);

            sumWorkerCallbacks += callbacks;
            sumWorkerCompletions += completions;

            TeaTime q = workers[i];
            if (q != null)
            {
                totalQueueCount += q.Count;
                totalQueueCurrent += q.Current;
                totalQueueExecuted += q.ExecutedCount;

                if (q.IsPlaying)
                    playingQueueCount += 1;
            }
        }

        for (int i = 0; i < workerQueueCount; i++)
        {
            if (workers[i] != null)
                workers[i].Stop();
        }

        float avgFrame = frameSamples > 0 ? frameTimeSum / frameSamples : 0f;
        float avgFps = avgFrame > 0f ? 1f / avgFrame : 0f;

        double totalCallbacks = totalLoopCallbacks + totalAddCallbacks;
        double loopRate = elapsed > 0f ? totalLoopCallbacks / elapsed : 0d;
        double addRate = elapsed > 0f ? totalAddCallbacks / elapsed : 0d;
        double totalRate = elapsed > 0f ? totalCallbacks / elapsed : 0d;

        float avgWorkerCallbacks =
            workerQueueCount > 0 ? (float)sumWorkerCallbacks / workerQueueCount : 0f;

        float avgWorkerCompletions =
            workerQueueCount > 0 ? (float)sumWorkerCompletions / workerQueueCount : 0f;

        int pendingAppends = appendedScheduled - (int)totalAddCallbacks;

        StringBuilder sb = new StringBuilder();

        sb.AppendLine("[StressBenchmark] COMPLETE");
        sb.AppendLine(
            $"Duration: active={benchmarkDuration:0.00}s, drain={drainDuration:0.00}s, total={elapsed:0.00}s"
        );

        sb.AppendLine($"Workers: {workerQueueCount} (loopDuration={workerLoopDuration:0.000}s)");

        sb.AppendLine(
            $"Callbacks: loop={totalLoopCallbacks}, add={totalAddCallbacks}, total={(long)totalCallbacks}"
        );

        sb.AppendLine(
            $"Callback rate: loop={loopRate:0.0}/s, add={addRate:0.0}/s, total={totalRate:0.0}/s"
        );

        sb.AppendLine(
            "Worker distribution (loop callbacks): "
                + $"min={minWorkerCallbacks}, avg={avgWorkerCallbacks:0.0}, max={maxWorkerCallbacks}"
        );

        sb.AppendLine(
            "Worker loop completions: "
                + $"min={minWorkerCompletions}, avg={avgWorkerCompletions:0.0}, max={maxWorkerCompletions}"
        );

        if (includeAppendMutations)
        {
            sb.AppendLine(
                "Appends: "
                    + $"scheduled={appendedScheduled}, executed={totalAddCallbacks}, pending={pendingAppends}, maxPending={maxPendingAppends}, "
                    + $"tick={appendTick:0.000}s, batch={appendBatchSize}, delay={appendedTaskDelay:0.000}s"
            );
        }
        else
        {
            sb.AppendLine("Appends: disabled");
        }

        sb.AppendLine(
            "Frame time (unscaled): "
                + $"avg={avgFrame * 1000f:0.00}ms, min={frameTimeMin * 1000f:0.00}ms, max={frameTimeMax * 1000f:0.00}ms, "
                + $"approxFPS={avgFps:0.0}"
        );

        sb.AppendLine($"GC collections delta: gen0={gc0Delta}, gen1={gc1Delta}, gen2={gc2Delta}");

        sb.AppendLine(
            "Managed memory delta: "
                + $"{FormatBytes(managedMemoryDelta)} (start {FormatBytes(managedMemoryStart)}, end {FormatBytes(managedMemoryEnd)})"
        );

        sb.AppendLine(
            "Queue snapshot before stop: "
                + $"playing={playingQueueCount}/{workerQueueCount}, totalCount={totalQueueCount}, totalCurrent={totalQueueCurrent}, totalExecuted={totalQueueExecuted}"
        );

        Debug.Log(sb.ToString());

        if (includeAppendMutations && pendingAppends > workerQueueCount * 4)
        {
            Debug.LogWarning(
                $"[StressBenchmark] Pending appended callbacks remained high ({pendingAppends}). Consider larger drainDuration."
            );
        }

        if (gc2Delta > 0)
        {
            Debug.LogWarning(
                $"[StressBenchmark] Gen2 collections occurred ({gc2Delta}) during the benchmark."
            );
        }

        if (frameTimeMax > 0.10f)
        {
            Debug.LogWarning(
                $"[StressBenchmark] Max frame time spike detected: {frameTimeMax * 1000f:0.00}ms"
            );
        }
    }

    private string FormatBytes(long bytes)
    {
        long abs = Math.Abs(bytes);

        if (abs >= 1024L * 1024L)
            return $"{bytes / (1024f * 1024f):0.00} MB";

        if (abs >= 1024L)
            return $"{bytes / 1024f:0.00} KB";

        return $"{bytes} B";
    }
}
