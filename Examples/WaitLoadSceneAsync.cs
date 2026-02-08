using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WaitLoadSceneAsync : MonoBehaviour
{
    private const string DefaultSceneReference = "Assets/Game/Scenes/SampleScene.unity";
    private const float PollTick = 0.02f;
    private const float HarnessTimeoutSeconds = 20f;
    private const int ExpectedChecks = 11;

    [Tooltip("Scene name or scene path to load additively during the test.")]
    public string sceneReference = DefaultSceneReference;

    private static bool primaryInstanceExists = false;

    private bool isPrimaryInstance = false;

    private TeaTime queue;

    private AsyncOperation loadOperation;
    private AsyncOperation unloadOperation;

    private bool loadRequested = false;
    private bool loadOperationCreated = false;
    private bool loadCompleted = false;

    private bool unloadRequested = false;
    private bool unloadOperationCreated = false;
    private bool unloadCompleted = false;

    private string resolvedSceneReference = "";
    private string resolvedSceneName = "";

    private string loadError = "";
    private string unloadError = "";

    private readonly HashSet<int> sceneHandlesBeforeLoad = new HashSet<int>();
    private Scene loadedScene;
    private bool loadedSceneResolved = false;
    private bool loadedSceneWasLoadedAfterLoadPhase = false;

    private int loadPollCount = 0;
    private int unloadPollCount = 0;

    private float harnessStartedAt = 0f;
    private float loadStartedAt = -1f;
    private float loadCompletedAt = -1f;
    private float unloadStartedAt = -1f;
    private float unloadCompletedAt = -1f;

    private int checksDone = 0;
    private int checksPassed = 0;

    private bool evaluated = false;

    void Start()
    {
        if (primaryInstanceExists)
        {
            Debug.Log(
                "[WaitLoadSceneAsyncTest] Secondary instance detected. Disabling duplicate harness."
            );
            enabled = false;
            return;
        }

        primaryInstanceExists = true;
        isPrimaryInstance = true;

        Debug.Log("[WaitLoadSceneAsyncTest] Starting load/unload async harness...");

        queue = this.tt("@LoadUnload")
            .Add(BeginLoad)
            .Loop(PollLoad)
            .Add(FinishLoad)
            .Add(BeginUnload)
            .Loop(PollUnload)
            .Add(FinishUnload)
            .Immutable();

        harnessStartedAt = Time.time;
    }

    void Update()
    {
        if (evaluated)
            return;

        if (queue != null && queue.IsCompleted)
        {
            EvaluateAndPrint(false);
            return;
        }

        if (Time.time - harnessStartedAt >= HarnessTimeoutSeconds)
        {
            Debug.LogWarning(
                $"[WaitLoadSceneAsyncTest] Timeout after {HarnessTimeoutSeconds:0.0}s. Evaluating partial results."
            );
            EvaluateAndPrint(true);
        }
    }

    void OnDestroy()
    {
        if (isPrimaryInstance)
            primaryInstanceExists = false;
    }

    private void BeginLoad()
    {
        loadRequested = true;
        loadStartedAt = Time.time;

        CaptureSceneHandlesBeforeLoad();

        string sceneRef = string.IsNullOrWhiteSpace(sceneReference)
            ? DefaultSceneReference
            : sceneReference;

        loadOperation = TryStartLoad(
            sceneRef,
            out resolvedSceneReference,
            out resolvedSceneName,
            out loadError
        );

        loadOperationCreated = loadOperation != null;

        if (loadOperationCreated)
        {
            Debug.Log(
                $"[WaitLoadSceneAsyncTest] Load requested ({resolvedSceneReference}) at {loadStartedAt:0.000}s"
            );
        }
        else
        {
            Debug.LogWarning(
                $"[WaitLoadSceneAsyncTest] Load request failed for '{sceneRef}'. {loadError}"
            );
        }
    }

    private void PollLoad(TeaHandler t)
    {
        loadPollCount += 1;

        if (loadOperation == null)
        {
            t.Break();
            return;
        }

        if (loadOperation.isDone)
        {
            t.Break();
            return;
        }

        t.Wait(PollTick);
    }

    private void FinishLoad()
    {
        loadCompleted = loadOperation != null && loadOperation.isDone;

        if (loadCompleted)
            loadCompletedAt = Time.time;

        loadedScene = ResolveLoadedScene();
        loadedSceneResolved = loadedScene.IsValid();
        loadedSceneWasLoadedAfterLoadPhase = loadedSceneResolved && loadedScene.isLoaded;

        if (loadedSceneResolved)
        {
            Debug.Log(
                $"[WaitLoadSceneAsyncTest] Load completed. Scene='{loadedScene.name}', handle={loadedScene.handle}, loaded={loadedScene.isLoaded}"
            );
        }
        else
        {
            Debug.LogWarning(
                "[WaitLoadSceneAsyncTest] Could not resolve newly loaded scene from scene delta."
            );
        }
    }

    private void BeginUnload()
    {
        unloadRequested = true;
        unloadStartedAt = Time.time;

        if (!loadedSceneResolved || !loadedScene.isLoaded)
        {
            unloadError = "No valid loaded scene available to unload.";
            Debug.LogWarning($"[WaitLoadSceneAsyncTest] Unload skipped. {unloadError}");
            return;
        }

        unloadOperation = TryStartUnload(loadedScene, out unloadError);
        unloadOperationCreated = unloadOperation != null;

        if (unloadOperationCreated)
        {
            Debug.Log(
                $"[WaitLoadSceneAsyncTest] Unload requested for '{loadedScene.name}' at {unloadStartedAt:0.000}s"
            );
        }
        else
        {
            Debug.LogWarning(
                $"[WaitLoadSceneAsyncTest] Unload request failed for '{loadedScene.name}'. {unloadError}"
            );
        }
    }

    private void PollUnload(TeaHandler t)
    {
        unloadPollCount += 1;

        if (unloadOperation == null)
        {
            t.Break();
            return;
        }

        if (unloadOperation.isDone)
        {
            t.Break();
            return;
        }

        t.Wait(PollTick);
    }

    private void FinishUnload()
    {
        unloadCompleted = unloadOperation != null && unloadOperation.isDone;

        if (unloadCompleted)
            unloadCompletedAt = Time.time;

        Debug.Log(
            $"[WaitLoadSceneAsyncTest] Unload phase finished. completed={unloadCompleted}, sceneLoadedNow={(loadedSceneResolved ? loadedScene.isLoaded.ToString() : "(unknown)")}"
        );
    }

    private void CaptureSceneHandlesBeforeLoad()
    {
        sceneHandlesBeforeLoad.Clear();

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene s = SceneManager.GetSceneAt(i);
            sceneHandlesBeforeLoad.Add(s.handle);
        }
    }

    private Scene ResolveLoadedScene()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene s = SceneManager.GetSceneAt(i);

            if (!sceneHandlesBeforeLoad.Contains(s.handle))
                return s;
        }

        if (!string.IsNullOrEmpty(resolvedSceneName))
        {
            Scene byName = SceneManager.GetSceneByName(resolvedSceneName);
            if (byName.IsValid())
                return byName;
        }

        return default(Scene);
    }

    private AsyncOperation TryStartLoad(
        string requestedRef,
        out string usedRef,
        out string usedSceneName,
        out string error
    )
    {
        usedRef = requestedRef;
        usedSceneName = SceneNameFromReference(requestedRef);
        error = "";

        AsyncOperation op = TryLoadInternal(requestedRef, out error);

        if (op != null)
            return op;

        string fallbackName = SceneNameFromReference(requestedRef);
        if (
            !string.IsNullOrEmpty(fallbackName)
            && !string.Equals(fallbackName, requestedRef, StringComparison.Ordinal)
        )
        {
            string fallbackError;
            AsyncOperation fallbackOp = TryLoadInternal(fallbackName, out fallbackError);

            if (fallbackOp != null)
            {
                usedRef = fallbackName;
                usedSceneName = fallbackName;
                error = "";
                return fallbackOp;
            }

            error = $"{error} | Fallback '{fallbackName}' failed: {fallbackError}";
        }

        return null;
    }

    private AsyncOperation TryLoadInternal(string reference, out string error)
    {
        error = "";

        try
        {
            return SceneManager.LoadSceneAsync(reference, LoadSceneMode.Additive);
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return null;
        }
    }

    private AsyncOperation TryStartUnload(Scene scene, out string error)
    {
        error = "";

        try
        {
            return SceneManager.UnloadSceneAsync(scene);
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return null;
        }
    }

    private string SceneNameFromReference(string reference)
    {
        if (string.IsNullOrWhiteSpace(reference))
            return "";

        string name = Path.GetFileNameWithoutExtension(reference);
        return string.IsNullOrEmpty(name) ? reference : name;
    }

    private bool IsLoadedSceneUnloaded()
    {
        if (!loadedSceneResolved)
            return false;

        return !loadedScene.isLoaded;
    }

    private void EvaluateAndPrint(bool timedOut)
    {
        if (evaluated)
            return;

        evaluated = true;

        bool queueCompleted = queue != null && queue.IsCompleted;

        if (queue != null)
            queue.Stop();

        CheckCondition("Load request was issued", loadRequested, "BeginLoad callback did not run.");

        CheckCondition(
            "Load AsyncOperation was created",
            loadOperationCreated,
            string.IsNullOrEmpty(loadError) ? "Load operation was null." : loadError
        );

        CheckCondition(
            "Load phase completed",
            loadCompleted,
            "Load operation was not completed when FinishLoad executed."
        );

        CheckCondition(
            "Loaded scene was resolved",
            loadedSceneResolved,
            "Could not resolve loaded scene after load completion."
        );

        CheckCondition(
            "Loaded scene reported isLoaded=true after load phase",
            loadedSceneWasLoadedAfterLoadPhase,
            loadedSceneResolved
                ? "Scene resolved but was not loaded at the end of load phase."
                : "Scene was not resolved."
        );

        CheckCondition(
            "Unload request was issued",
            unloadRequested,
            "BeginUnload callback did not run."
        );

        CheckCondition(
            "Unload AsyncOperation was created",
            unloadOperationCreated,
            string.IsNullOrEmpty(unloadError) ? "Unload operation was null." : unloadError
        );

        CheckCondition(
            "Unload phase completed",
            unloadCompleted,
            "Unload operation was not completed when FinishUnload executed."
        );

        CheckCondition(
            "Loaded scene was unloaded",
            IsLoadedSceneUnloaded(),
            "Scene still reports isLoaded=true after unload phase."
        );

        CheckCondition(
            "Load/Unload polling loops executed",
            loadPollCount > 0 && unloadPollCount > 0,
            $"loadPolls={loadPollCount}, unloadPolls={unloadPollCount}."
        );

        bool timelineOk =
            loadStartedAt >= 0f
            && loadCompletedAt >= loadStartedAt
            && unloadStartedAt >= loadCompletedAt
            && unloadCompletedAt >= unloadStartedAt;

        CheckCondition(
            "Timeline order is valid (load -> unload)",
            timelineOk,
            $"loadStart={loadStartedAt:0.000}, loadDone={loadCompletedAt:0.000}, unloadStart={unloadStartedAt:0.000}, unloadDone={unloadCompletedAt:0.000}"
        );

        if (timedOut)
        {
            Debug.LogWarning("[WaitLoadSceneAsyncTest] Evaluation triggered by timeout.");
        }
        else if (!queueCompleted)
        {
            Debug.LogWarning(
                "[WaitLoadSceneAsyncTest] Queue did not report IsCompleted before stop (possible Stop() completion semantics)."
            );
        }

        PrintSummary();
    }

    private void CheckCondition(string label, bool pass, string failReason)
    {
        checksDone += 1;
        if (pass)
            checksPassed += 1;

        if (pass)
            Debug.Log($"[WaitLoadSceneAsyncTest] PASS {label}");
        else
            Debug.LogWarning($"[WaitLoadSceneAsyncTest] FAIL {label}: {failReason}");
    }

    private void PrintSummary()
    {
        if (checksDone != ExpectedChecks)
        {
            Debug.LogWarning(
                $"[WaitLoadSceneAsyncTest] Summary check mismatch: expected {ExpectedChecks}, got {checksDone}."
            );
        }

        int failed = checksDone - checksPassed;

        if (failed == 0)
        {
            Debug.Log(
                $"[WaitLoadSceneAsyncTest] COMPLETE: {checksPassed}/{checksDone} checks passed."
            );
        }
        else
        {
            Debug.LogWarning(
                $"[WaitLoadSceneAsyncTest] COMPLETE: {checksPassed}/{checksDone} checks passed, {failed} failed."
            );
        }
    }
}

// 2021/10/03 04:10 pm
