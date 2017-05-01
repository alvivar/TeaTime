
// Experimental TeaTime prototype to research a better core algorithm.

// @matnesis
// 2016/11/12 07:31 PM


using System;
using System.Collections.Generic;

namespace matnesis.TeaCup
{
    public class TeaCup
    {
        public TeaCupTaskList tasks = new TeaCupTaskList();

        public float elapsedTime = 0; // Time that has been running during the current execution
        public float totalElapsedTime = 0; // Total time that has been running since the beginning
        public int executedCount = 0; // Times that has been executed completely

        public bool hasRepeatMode = false; // On true, the TeaCup will repeat again at the end
        public bool hasUnscaledTime = false; // Use Time.unscaledDeltaTime instead of Time.delta to calculations.
        public bool hasReverseMode = false; // Run backwards


        /// <summary>
        /// Adds everything.
        /// </summary>
        private TeaCup Add(float timeDelay, float loopDuration, Action<TeaCupHandler> action)
        {
            tasks.Add(timeDelay, loopDuration, action);

            return TeaCupCore.Init(this);
        }

        /// <summary>
        /// Appends an Action to be executed after a delay.
        /// </summary>
        public TeaCup Add(float timeDelay, Action<TeaCupHandler> action)
        {
            return Add(timeDelay, 0, action);
        }


        /// <summary>
        /// Appends an Action that itself constantly for a certain duration.
        /// </summary>
        public TeaCup Loop(float duration, Action<TeaCupHandler> action)
        {
            return Add(0, duration, action);
        }


        /// <summary>
        /// [MODE] Repeat again at the end.
        /// </summary>
        public TeaCup Repeat(bool value = true)
        {
            hasRepeatMode = value;

            return this;
        }


        /// <summary>
        /// [MODE] Use Time.unscaledDeltaTime instead of Time.delta to calculations.
        /// </summary>
        public TeaCup UnscaledTime(bool value = true)
        {
            hasUnscaledTime = value;

            return this;
        }


        /// <summary>
        /// [MODE] Run backwards.
        /// </summary>
        public TeaCup Reverse(bool value = true)
        {
            hasReverseMode = value;

            return this;
        }

    }
}