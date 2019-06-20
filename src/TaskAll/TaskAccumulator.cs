using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TaskAll
{
    public interface ITaskAccumulator<TRes>
    {
        Task<TRes> Result();
    }

    internal interface ITaskAccumulatorInternal<TRes> : ITaskAccumulator<TRes>
    {
        IEnumerable<Task> Tasks { get; }

        TRes ResultSync();
    }

    internal class TaskAccumulatorInitial<TPrev, TCur, TRes> : ITaskAccumulatorInternal<TRes>
    {
        public readonly Task<TPrev> Task1;

        public readonly Task<TCur> Task2;

        public readonly Func<TPrev, TCur, TRes> Mapper;

        public TaskAccumulatorInitial(Task<TPrev> task1, Task<TCur> task2, Func<TPrev, TCur, TRes> mapper)
        {
            this.Task1 = task1;
            this.Task2 = task2;
            this.Mapper = mapper;
        }

        public IEnumerable<Task> Tasks 
            => new Task[] { this.Task1, this.Task2 };

        public async Task<TRes> Result()
        {
            await Task.WhenAll(this.Tasks);

            return this.ResultSync();
        }

        public TRes ResultSync() 
            => this.Mapper(this.Task1.Result, this.Task2.Result);
    }

    internal class TaskAccumulator<TPrev, TCur, TRes> : ITaskAccumulatorInternal<TRes>
    {
        public readonly ITaskAccumulatorInternal<TPrev> Prev;

        public readonly Task<TCur> CurrentTask;

        public readonly Func<TPrev, TCur, TRes> Mapper;

        public TaskAccumulator(ITaskAccumulatorInternal<TPrev> prev, Task<TCur> currentTask, Func<TPrev, TCur, TRes> mapper)
        {
            this.Prev = prev;
            this.CurrentTask = currentTask;
            this.Mapper = mapper;
        }

        public IEnumerable<Task> Tasks => new Task[] { this.CurrentTask }.Concat(this.Prev.Tasks);

        public async Task<TRes> Result()
        {
            await Task.WhenAll(this.Tasks);
            return this.ResultSync();
        }

        public TRes ResultSync() 
            => this.Mapper(this.Prev.ResultSync(), this.CurrentTask.Result);
    }

    internal class SingleTask<T> : ITaskAccumulatorInternal<T>
    {
        private readonly Task<T> _task;

        private readonly Task[] _tasks;

        public SingleTask(Task<T> task)
        {
            this._task = task;
            this._tasks = new Task[] { task };
        }

        public Task<T> Result() => this._task;

        public IEnumerable<Task> Tasks => this._tasks;

        public T ResultSync() => this._task.Result;
    }
}