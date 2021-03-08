using System;
using System.Collections.Generic;
using System.Threading;

namespace Dan200.Core.Computer
{
    public class TaskThread : IDisposable
    {
        private bool m_over;
        private AutoResetEvent m_queueEvent;
        private Queue<Action> m_queue;
        private Thread m_thread;

        public bool IsBusy
        {
            get
            {
                if (!m_over)
                {
                    lock (m_queue)
                    {
                        return m_queue.Count > 0;
                    }
                }
                return false;
            }
        }

        public TaskThread()
        {
            m_over = false;
            m_queueEvent = new AutoResetEvent(false);
            m_queue = new Queue<Action>();
            m_thread = new Thread(Run);
            m_thread.Start();
        }

        public void Dispose()
        {
            if (!m_over)
            {
                m_over = true;
                m_queueEvent.Set();
                m_thread.Join();
            }
            ((IDisposable)m_queueEvent).Dispose();
        }

        public void Enqueue(Action task)
        {
            if (!m_over)
            {
                lock (m_queue)
                {
                    m_queue.Enqueue(task);
                }
                m_queueEvent.Set();
            }
        }

        private bool TryDequeue(out Action o_task)
        {
            lock (m_queue)
            {
                if (m_queue.Count > 0)
                {
                    o_task = m_queue.Dequeue();
                    return true;
                }
            }
            o_task = null;
            return false;
        }

        private void Run()
        {
            try
            {
                while (true)
                {
                    Action task;
                    if (TryDequeue(out task))
                    {
                        if (!m_over)
                        {
                            try
                            {
                                task.Invoke();
                            }
                            catch (Exception e)
                            {
                                LogError("Error invoking task: " + e.ToString());
                            }
                        }
                    }
                    else
                    {
                        if (m_over)
                        {
                            break;
                        }
                        else
                        {
                            m_queueEvent.WaitOne();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                LogError(e.ToString());
            }
            finally
            {
                m_over = true;
            }
        }

        private void LogError(string error)
        {
#if UNITY_5
			UnityEngine.Debug.LogError( error );
#else
            Console.Error.WriteLine(error);
#endif
        }
    }
}
