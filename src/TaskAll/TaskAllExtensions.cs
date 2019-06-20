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

        public static ITaskAccumulator<TRes> SelectMany<TCur, TNext, TRes>(this ParallelTaskWrapper<TCur> source, Func<TCur, ParallelTaskWrapper<TNext>> exec, Func<TCur, TNext, TRes> mapper)
        {
            return new TaskAccumulatorInitial<TCur, TNext, TRes>(source.Task, exec(default(TCur)).Task, mapper);
        }

        public static ITaskAccumulator<TRes> SelectMany<TCur, TNext, TRes>(this ITaskAccumulator<TCur> source, Func<TCur, ParallelTaskWrapper<TNext>> exec, Func<TCur, TNext, TRes> mapper)
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
            return new TaskAccumulator<TCur, TNext, TRes>((ITaskAccumulatorInternal<TCur>)source, nextWrapper.Task, mapper);
        }

        public static ITaskAccumulator<TRes> SelectMany<TCur, TNext, TRes>(this ITaskAccumulator<TCur> source, Func<TCur, SequentialTaskWrapper<TNext>> exec, Func<TCur, TNext, TRes> mapper)
        {
            return new SingleTask<TRes>(BuildTask());

            async Task<TRes> BuildTask()
            {
                var arg1 = await source.Result();

                var arg2 = await exec(arg1).Task;

                return mapper(arg1, arg2);
            }
        }

        public static ITaskAccumulator<TRes> SelectMany<TCur, TNext, TRes>(this ITaskAccumulator<TCur> source, Func<TCur, ITaskAccumulator<TNext>> exec, Func<TCur, TNext, TRes> mapper)
        {
            return new SingleTask<TRes>(BuildTask());

            async Task<TRes> BuildTask()
            {
                var arg1 = await source.Result();

                var arg2 = await exec(arg1);

                return mapper(arg1, arg2);
            }
        }

        public static ITaskAccumulator<TRes> Select<TCur, TRes>(this ITaskAccumulator<TCur> source, Func<TCur, TRes> mapper)
        {
            return new SingleTask<TRes>(source.Result().ContinueWith(task=> mapper(task.Result)));
        }

        public static ParallelTaskWrapper<TRes> Select<TCur, TRes>(this ParallelTaskWrapper<TCur> source, Func<TCur, TRes> mapper)
        {
            return new ParallelTaskWrapper<TRes>(source.Task.ContinueWith(t=> mapper(t.Result)));
        }

        public static TaskAwaiter<T> GetAwaiter<T>(this ITaskAccumulator<T> source)
            => source.Result().GetAwaiter();
    }
}
