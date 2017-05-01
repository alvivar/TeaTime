
// @matnesis
// 2016/11/12 07:31 PM


using System;

namespace matnesis.TeaCup
{
    public class TeaCupTask
    {
        public float delay;
        public float loop;
        public Action<TeaCupHandler> action;


        public TeaCupTask next;
        public TeaCupTask previous;


        public TeaCupTask(float delay, float loop, Action<TeaCupHandler> action)
        {
            this.delay = delay;
            this.loop = loop;
            this.action = action;
        }
    }
}