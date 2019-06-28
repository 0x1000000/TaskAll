using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace TaskAll
{
    public static class TaskAllExtensions
    {
        private const string NotResolvedParamErrorText = "Object reference not set to an instance of an object. Check that not yet resolved parameter is not passed to a parallel task.";

        public static ParallelTaskWrapper<T> AsParallel<T>(this Task<T> task)
        {
            return new ParallelTaskWrapper<T>(task);
        }

        public static SequentialTaskWrapper<T> AsSequential<T>(this Task<T> task)
        {
            return new SequentialTaskWrapper<T>(task);
        }

        public static ParallelTaskWrapper<TRes> SelectMany<TCur, TNext, TRes>(this ParallelTaskWrapper<TCur> source, Func<TCur, ParallelTaskWrapper<TNext>> exec, Func<TCur, TNext, TRes> mapper)
        {
            ParallelTaskWrapper<TNext> nextWrapper;
            try
            {
                nextWrapper = exec(default(TCur));
            }
            catch (NullReferenceException e)
            {
                throw new Exception(NotResolvedParamErrorText, e);
            }

            async Task<TRes> GetResult()
            {
                var arg1 = await source.Task;
                var arg2 = await nextWrapper.Task;
                return mapper(arg1, arg2);
            }


            return new ParallelTaskWrapper<TRes>(GetResult());
        }

        public static ParallelTaskWrapper<TRes> SelectMany<TCur, TNext, TRes>(this ParallelTaskWrapper<TCur> source, Func<TCur, SequentialTaskWrapper<TNext>> exec, Func<TCur, TNext, TRes> mapper)
        {
            async Task<TRes> GetResult()
            {
                var arg1 = await source;
                var arg2 = await exec(arg1).Task;
                return mapper(arg1, arg2);
            }

            return new ParallelTaskWrapper<TRes>(GetResult());
        }

        public static ParallelTaskWrapper<TRes> Select<TCur, TRes>(this ParallelTaskWrapper<TCur> source, Func<TCur, TRes> mapper)
        {
            return new ParallelTaskWrapper<TRes>(source.Task.ContinueWith(t=> mapper(t.Result)));
        }

        public static TaskAwaiter<T> GetAwaiter<T>(this ParallelTaskWrapper<T> source)
            => source.Task.GetAwaiter();

        public static TaskAwaiter<T> GetAwaiter<T>(this SequentialTaskWrapper<T> source)
            => source.Task.GetAwaiter();
    }
}
