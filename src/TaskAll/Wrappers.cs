using System.Threading.Tasks;

namespace TaskAll
{
    public struct ParallelTaskWrapper<T>
    {
        public readonly Task<T> Task;

        internal ParallelTaskWrapper(Task<T> task) => this.Task = task;
    }

    public struct SequentialTaskWrapper<T>
    {
        public readonly Task<T> Task;

        public SequentialTaskWrapper(Task<T> task) => this.Task = task;
    }
}