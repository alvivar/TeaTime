using UnityEngine;

public class ImmutableUpdate : MonoBehaviour
{
    void Update()
    {
        // Both TeaTimes below can lock their execution for some time, even when
        // called multiple times (like inside Update).

        // One second time lock.
        var timeLock1 = this.tt("timeLock1").Add(() =>
            {
                Debug.Log($"timeLock #1 Call first, then wait {Time.time}");
            })
            .Add(1, t => t.self.Reset())
            .Immutable();

        // Wait one second.
        var timeLock2 = this.tt("timeLock2")
            .Add(() => Debug.Log($"timeLock #2 A: Call {Time.time}"))
            .Add(() => Debug.Log($"timeLock #2 B: Call {Time.time}"))
            .Add(() => Debug.Log($"timeLock #2 C: Call {Time.time}"))
            .Add(1, t =>
            {
                Debug.Log($"timeLock #2 D: Waited {Time.time}");
                t.self.Reset();
            })
            .Immutable();

        // Immutable makes sure they don't change. Reset could clean the TeaTime
        // if needed, so it can be rebuilt and played again when their execution
        // is completed.
    }
}

// 2021/03/03 09:55 pm