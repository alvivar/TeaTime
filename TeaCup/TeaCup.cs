
// The most awesome Action Timed Queue I can create!

// @matnesis
// 2016/11/12 07:31 PM


using System;
using System.Collections.Generic;

namespace matnesis.TeaCup
{
    public class TeaCup
    {
        public TeaCupQueue queue = new TeaCupQueue();


        /// <summary>
        /// Appends everything.
        /// </summary>
        private TeaCup Add(float timeDelay, float loopDuration, Action<TeaCupHandler> action)
        {
            queue.Add(timeDelay, loopDuration, action);

            return TeaCupServer.Init(this);
        }

        /// <summary>
        /// Appends an Action to be executed after a delay.
        /// </summary>
        public TeaCup Add(float timeDelay, Action<TeaCupHandler> action)
        {
            return Add(timeDelay, 0, action);
        }


        /// <summary>
        /// Appends an Action that loops itself constantly for a certain duration.
        /// </summary>
        public TeaCup Loop(float duration, Action<TeaCupHandler> action)
        {
            return Add(0, duration, action);
        }


        /// <summary>
        /// [MODE] Use Time.unscaledDeltaTime instead of Time.delta to calculations.
        /// </summary>
        public TeaCup UnscaledTime(bool value = true)
        {
            queue.isUnscaled = value;

            return this;
        }


        /// <summary>
        /// [MODE] Repeat again at the end.
        /// </summary>
        public TeaCup Repeat(bool value = true)
        {
            queue.isRepeatable = value;

            return this;
        }


        /// <summary>
        /// [MODE] Run backwards.
        /// </summary>
        public TeaCup Reverse(bool value = true)
        {
            queue.isReversed = value;

            return this;
        }
    }
}