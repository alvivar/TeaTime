
// TeaCup Node!

// @matnesis
// 2016/11/12 07:31 PM


using System;

namespace matnesis.TeaCup
{
    public class TeaCupNode
    {
        public float delay = 0; // TeaTime
        public float loopTime = 0;
        public Action<TeaCupHandler> action;

        public float beginDelay = 0; // This values will be updated when the node is created
        public float beginAction = 0;
        public float endAction = 0;

        public TeaCupNode next; // Linked list
        public TeaCupNode previous;


        public TeaCupNode(float delay, float loopTime, Action<TeaCupHandler> action)
        {
            this.delay = delay;
            this.loopTime = loopTime;
            this.action = action;
        }
    }
}