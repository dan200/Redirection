namespace Dan200.Core.Async
{
    public class SimplePromise : Promise
    {
        private Status m_status;
        private string m_error;

        public override Status Status
        {
            get
            {
                lock (this)
                {
                    return m_status;
                }
            }
        }

        public override string Error
        {
            get
            {
                lock (this)
                {
                    return m_error;
                }
            }
        }

        public SimplePromise()
        {
            m_status = Status.Waiting;
            m_error = null;
        }

        public void Succeed()
        {
            lock (this)
            {
                m_status = Status.Complete;
                m_error = null;
            }
        }

        public void Fail(string error)
        {
            lock (this)
            {
                m_status = Status.Error;
                m_error = error;
            }
        }
    }

    public class SimplePromise<T> : Promise<T>
    {
        private Status m_status;
        private T m_result;
        private string m_error;

        public override Status Status
        {
            get
            {
                lock (this)
                {
                    return m_status;
                }
            }
        }

        public override T Result
        {
            get
            {
                lock (this)
                {
                    return m_result;
                }
            }
        }

        public override string Error
        {
            get
            {
                lock (this)
                {
                    return m_error;
                }
            }
        }

        public SimplePromise()
        {
            m_status = Status.Waiting;
            m_result = default(T);
            m_error = null;
        }

        public void Succeed(T result)
        {
            lock (this)
            {
                m_status = Status.Complete;
                m_result = result;
                m_error = null;
            }
        }

        public void Fail(string error)
        {
            lock (this)
            {
                m_status = Status.Error;
                m_result = default(T);
                m_error = error;
            }
        }
    }
}
