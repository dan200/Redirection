using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dan200.Core.Async
{
    public abstract class Promise
    {
        public static Promise Sleep(float duration)
        {
            var result = new SimplePromise();
            Task.Factory.StartNew(delegate
           {
               Thread.Sleep(TimeSpan.FromSeconds(duration));
               result.Succeed();
           });
            return result;
        }

        public abstract Status Status { get; }
        public abstract string Error { get; }
    }

    public abstract class Promise<T> : Promise
    {
        public abstract T Result { get; }
    }
}
