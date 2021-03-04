// Useful TeaTime patterns!

// 2021.03.03 09.55 pm

using UnityEngine;

public class AdvancedPatterns : MonoBehaviour
{
    void Update()
    {
        // Both TeaTimes below can lock their execution for some time, even when
        // called multiple times (like inside Update).

        // One second time lock.
        var timeLock1 = this.tt().Add(() =>
            {
                Debug.Log($"timeLock: Call first, then wait {Time.time}");
            })
            .Add(1, t => t.self.Reset())
            .Immutable();

        // Wait one second.
        var timeLock2 = this.tt().Add(1, t =>
            {
                Debug.Log($"timeLock 2: Wait first, then executes {Time.time}");
                t.self.Reset();
            })
            .Immutable();

        // Immutable makes sure they don't change. Reset cleans the TeaTime, so
        // it can be rebuilt and played again when their execution is completed.
    }
}