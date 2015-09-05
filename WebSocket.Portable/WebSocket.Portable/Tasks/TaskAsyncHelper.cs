using System;
using System.Linq;
using System.Threading.Tasks;

namespace WebSocket.Portable.Tasks
{
    public static class TaskAsyncHelper
    {
        private static readonly Task _emptyTask = MakeTask<object>(null);
        private static readonly Task<bool> _trueTask = MakeTask(true);
        private static readonly Task<bool> _falseTask = MakeTask(false);

        private static Task<T> MakeTask<T>(T value)
        {
            return FromResult(value);
        }

        public static Task Empty
        {
            get
            {
                return _emptyTask;
            }
        }

        public static Task<bool> True
        {
            get
            {
                return _trueTask;
            }
        }

        public static Task<bool> False
        {
            get
            {
                return _falseTask;
            }
        }

        public static Task OrEmpty(this Task task)
        {
            return task ?? Empty;
        }

        public static Task<T> OrEmpty<T>(this Task<T> task)
        {
            return task ?? TaskCache<T>.EmptyTask;
        }

        public static Task FromAsync(Func<AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, object state)
        {
            try
            {
                return Task.Factory.FromAsync(beginMethod, endMethod, state);
            }
            catch (Exception ex)
            {
                return TaskAsyncHelper.FromError(ex);
            }
        }

        public static Task<T> FromAsync<T>(Func<AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, T> endMethod, object state)
        {
            try
            {
                return Task.Factory.FromAsync(beginMethod, endMethod, state);
            }
            catch (Exception ex)
            {
                return TaskAsyncHelper.FromError<T>(ex);
            }
        }

        public static Task Series(Func<object, Task>[] tasks, object[] state)
        {
            var finalTask = TaskAsyncHelper.Empty;

            for (var i = 0; i < tasks.Length; i++)
            {
                var prev = finalTask;
                finalTask = prev.Then(tasks[i], state[i]);
            }

            return finalTask;
        }

        public static TTask Catch<TTask>(this TTask task) where TTask : Task
        {
            return Catch(task, ex => { });
        }

        public static TTask Catch<TTask>(this TTask task, Action<AggregateException, object> handler, object state) where TTask : Task
        {
            if (task != null && task.Status != TaskStatus.RanToCompletion)
            {
                if (task.Status == TaskStatus.Faulted)
                {
                    ExecuteOnFaulted(handler, state, task.Exception);
                }
                else
                {
                    AttachFaultedContinuation(task, handler, state);
                }
            }

            return task;
        }

        private static void AttachFaultedContinuation<TTask>(TTask task, Action<AggregateException, object> handler, object state) where TTask : Task
        {
            task.ContinueWith(innerTask => ExecuteOnFaulted(handler, state, innerTask.Exception),
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
        }

        private static void ExecuteOnFaulted(Action<AggregateException, object> handler, object state, AggregateException exception)
        {
            handler(exception, state);
        }

        public static TTask Catch<TTask>(this TTask task, Action<AggregateException> handler) where TTask : Task
        {
            return task.Catch((ex, state) => ((Action<AggregateException>)state).Invoke(ex),
                              handler);
        }

        public static Task ContinueWithNotComplete(this Task task, Action action)
        {
            switch (task.Status)
            {
                case TaskStatus.Faulted:
                case TaskStatus.Canceled:
                    try
                    {
                        action();
                        return task;
                    }
                    catch (Exception e)
                    {
                        return FromError(e);
                    }
                case TaskStatus.RanToCompletion:
                    return task;
                default:
                    var tcs = new TaskCompletionSource<object>();

                    task.ContinueWith(t =>
                    {
                        if (t.IsFaulted || t.IsCanceled)
                        {
                            try
                            {
                                action();

                                if (t.IsFaulted)
                                {
                                    tcs.TrySetUnwrappedException(t.Exception);
                                }
                                else
                                {
                                    tcs.TrySetCanceled();
                                }
                            }
                            catch (Exception e)
                            {
                                tcs.TrySetException(e);
                            }
                        }
                        else
                        {
                            tcs.TrySetResult(null);
                        }
                    },
                    TaskContinuationOptions.ExecuteSynchronously);

                    return tcs.Task;
            }
        }

        public static void ContinueWithNotComplete(this Task task, TaskCompletionSource<object> tcs)
        {
            task.ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    tcs.SetUnwrappedException(t.Exception);
                }
                else if (t.IsCanceled)
                {
                    tcs.SetCanceled();
                }
            },
            TaskContinuationOptions.NotOnRanToCompletion);
        }

        public static void ContinueWith(this Task task, TaskCompletionSource<object> tcs)
        {
            task.ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    tcs.TrySetUnwrappedException(t.Exception);
                }
                else if (t.IsCanceled)
                {
                    tcs.TrySetCanceled();
                }
                else
                {
                    tcs.TrySetResult(null);
                }
            },
            TaskContinuationOptions.ExecuteSynchronously);
        }

        public static void ContinueWith<T>(this Task<T> task, TaskCompletionSource<T> tcs)
        {
            task.ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    tcs.TrySetUnwrappedException(t.Exception);
                }
                else if (t.IsCanceled)
                {
                    tcs.TrySetCanceled();
                }
                else
                {
                    tcs.TrySetResult(t.Result);
                }
            });
        }

        public static Task Return(this Task[] tasks)
        {
            return Then(tasks, () => { });
        }

        public static Task Then(this Task task, Action successor)
        {
            switch (task.Status)
            {
                case TaskStatus.Faulted:
                case TaskStatus.Canceled:
                    return task;

                case TaskStatus.RanToCompletion:
                    return FromMethod(successor);

                default:
                    return RunTask(task, successor);
            }
        }

        public static Task<TResult> Then<TResult>(this Task task, Func<TResult> successor)
        {
            switch (task.Status)
            {
                case TaskStatus.Faulted:
                    return FromError<TResult>(task.Exception);

                case TaskStatus.Canceled:
                    return Canceled<TResult>();

                case TaskStatus.RanToCompletion:
                    return FromMethod(successor);

                default:
                    return TaskRunners<object, TResult>.RunTask(task, successor);
            }
        }

        public static Task Then(this Task[] tasks, Action successor)
        {
            if (tasks.Length == 0)
            {
                return FromMethod(successor);
            }

            var tcs = new TaskCompletionSource<object>();
            Task.Factory.ContinueWhenAll(tasks, completedTasks =>
            {
                var faulted = completedTasks.FirstOrDefault(t => t.IsFaulted);
                if (faulted != null)
                {
                    tcs.SetUnwrappedException(faulted.Exception);
                    return;
                }
                var cancelled = completedTasks.FirstOrDefault(t => t.IsCanceled);
                if (cancelled != null)
                {
                    tcs.SetCanceled();
                    return;
                }

                successor();
                tcs.SetResult(null);
            });

            return tcs.Task;
        }

        public static Task Then<T1>(this Task task, Action<T1> successor, T1 arg1)
        {
            switch (task.Status)
            {
                case TaskStatus.Faulted:
                case TaskStatus.Canceled:
                    return task;

                case TaskStatus.RanToCompletion:
                    return FromMethod(successor, arg1);

                default:
                    return GenericDelegates<object, object, T1, object>.ThenWithArgs(task, successor, arg1);
            }
        }

        public static Task Then<T1, T2>(this Task task, Action<T1, T2> successor, T1 arg1, T2 arg2)
        {
            switch (task.Status)
            {
                case TaskStatus.Faulted:
                case TaskStatus.Canceled:
                    return task;

                case TaskStatus.RanToCompletion:
                    return FromMethod(successor, arg1, arg2);

                default:
                    return GenericDelegates<object, object, T1, T2>.ThenWithArgs(task, successor, arg1, arg2);
            }
        }

        public static Task Then<T1>(this Task task, Func<T1, Task> successor, T1 arg1)
        {
            switch (task.Status)
            {
                case TaskStatus.Faulted:
                case TaskStatus.Canceled:
                    return task;

                case TaskStatus.RanToCompletion:
                    return FromMethod(successor, arg1);

                default:
                    return GenericDelegates<object, Task, T1, object>.ThenWithArgs(task, successor, arg1)
                                                                     .FastUnwrap();
            }
        }

        public static Task Then<T1, T2>(this Task task, Func<T1, T2, Task> successor, T1 arg1, T2 arg2)
        {
            switch (task.Status)
            {
                case TaskStatus.Faulted:
                case TaskStatus.Canceled:
                    return task;

                case TaskStatus.RanToCompletion:
                    return FromMethod(successor, arg1, arg2);

                default:
                    return GenericDelegates<object, Task, T1, T2>.ThenWithArgs(task, successor, arg1, arg2)
                                                                 .FastUnwrap();
            }
        }

        public static Task<TResult> Then<T, TResult>(this Task<T> task, Func<T, Task<TResult>> successor)
        {
            switch (task.Status)
            {
                case TaskStatus.Faulted:
                    return FromError<TResult>(task.Exception);

                case TaskStatus.Canceled:
                    return Canceled<TResult>();

                case TaskStatus.RanToCompletion:
                    return FromMethod(successor, task.Result);

                default:
                    return TaskRunners<T, Task<TResult>>.RunTask(task, t => successor(t.Result))
                                                        .FastUnwrap();
            }
        }

        public static Task<TResult> Then<T, TResult>(this Task<T> task, Func<T, TResult> successor)
        {
            switch (task.Status)
            {
                case TaskStatus.Faulted:
                    return FromError<TResult>(task.Exception);

                case TaskStatus.Canceled:
                    return Canceled<TResult>();

                case TaskStatus.RanToCompletion:
                    return FromMethod(successor, task.Result);

                default:
                    return TaskRunners<T, TResult>.RunTask(task, t => successor(t.Result));
            }
        }

        public static Task<TResult> Then<T, T1, TResult>(this Task<T> task, Func<T, T1, TResult> successor, T1 arg1)
        {
            switch (task.Status)
            {
                case TaskStatus.Faulted:
                    return FromError<TResult>(task.Exception);

                case TaskStatus.Canceled:
                    return Canceled<TResult>();

                case TaskStatus.RanToCompletion:
                    return FromMethod(successor, task.Result, arg1);

                default:
                    return GenericDelegates<T, TResult, T1, object>.ThenWithArgs(task, successor, arg1);
            }
        }

        public static Task Then(this Task task, Func<Task> successor)
        {
            switch (task.Status)
            {
                case TaskStatus.Faulted:
                case TaskStatus.Canceled:
                    return task;

                case TaskStatus.RanToCompletion:
                    return FromMethod(successor);

                default:
                    return TaskRunners<object, Task>.RunTask(task, successor)
                                                    .FastUnwrap();
            }
        }

        public static Task<TResult> Then<TResult>(this Task task, Func<Task<TResult>> successor)
        {
            switch (task.Status)
            {
                case TaskStatus.Faulted:
                    return FromError<TResult>(task.Exception);

                case TaskStatus.Canceled:
                    return Canceled<TResult>();

                case TaskStatus.RanToCompletion:
                    return FromMethod(successor);

                default:
                    return TaskRunners<object, Task<TResult>>.RunTask(task, successor)
                                                             .FastUnwrap();
            }
        }

        public static Task Then<TResult>(this Task<TResult> task, Action<TResult> successor)
        {
            switch (task.Status)
            {
                case TaskStatus.Faulted:
                case TaskStatus.Canceled:
                    return task;

                case TaskStatus.RanToCompletion:
                    return FromMethod(successor, task.Result);

                default:
                    return TaskRunners<TResult, object>.RunTask(task, successor);
            }
        }

        public static Task Then<TResult>(this Task<TResult> task, Func<TResult, Task> successor)
        {
            switch (task.Status)
            {
                case TaskStatus.Faulted:
                case TaskStatus.Canceled:
                    return task;

                case TaskStatus.RanToCompletion:
                    return FromMethod(successor, task.Result);

                default:
                    return TaskRunners<TResult, Task>.RunTask(task, t => successor(t.Result))
                                                     .FastUnwrap();
            }
        }

        public static Task<TResult> Then<TResult, T1>(this Task<TResult> task, Func<Task<TResult>, T1, Task<TResult>> successor, T1 arg1)
        {
            switch (task.Status)
            {
                case TaskStatus.Faulted:
                case TaskStatus.Canceled:
                    return task;

                case TaskStatus.RanToCompletion:
                    return FromMethod(successor, task, arg1);

                default:
                    return GenericDelegates<TResult, Task<TResult>, T1, object>
                        .ThenWithArgs(task, successor, arg1)
                        .FastUnwrap();
            }
        }

        public static Task Finally(this Task task, Action<object> next, object state)
        {
            try
            {
                switch (task.Status)
                {
                    case TaskStatus.Faulted:
                    case TaskStatus.Canceled:
                        next(state);
                        return task;
                    case TaskStatus.RanToCompletion:
                        return FromMethod(next, state);

                    default:
                        return RunTaskSynchronously(task, next, state, onlyOnSuccess: false);
                }
            }
            catch (Exception ex)
            {
                return FromError(ex);
            }
        }

        public static Task RunSynchronously(this Task task, Action successor)
        {
            switch (task.Status)
            {
                case TaskStatus.Faulted:
                case TaskStatus.Canceled:
                    return task;

                case TaskStatus.RanToCompletion:
                    return FromMethod(successor);

                default:
                    return RunTaskSynchronously(task, state => ((Action)state).Invoke(), successor);
            }
        }

        public static Task FastUnwrap(this Task<Task> task)
        {
            var innerTask = (task.Status == TaskStatus.RanToCompletion) ? task.Result : null;
            return innerTask ?? task.Unwrap();
        }

        public static Task<T> FastUnwrap<T>(this Task<Task<T>> task)
        {
            var innerTask = (task.Status == TaskStatus.RanToCompletion) ? task.Result : null;
            return innerTask ?? task.Unwrap();
        }

        public static Task FromMethod(Action func)
        {
            try
            {
                func();
                return Empty;
            }
            catch (Exception ex)
            {
                return FromError(ex);
            }
        }

        public static Task FromMethod<T1>(Action<T1> func, T1 arg)
        {
            try
            {
                func(arg);
                return Empty;
            }
            catch (Exception ex)
            {
                return FromError(ex);
            }
        }

        public static Task FromMethod<T1, T2>(Action<T1, T2> func, T1 arg1, T2 arg2)
        {
            try
            {
                func(arg1, arg2);
                return Empty;
            }
            catch (Exception ex)
            {
                return FromError(ex);
            }
        }

        public static Task FromMethod(Func<Task> func)
        {
            try
            {
                return func();
            }
            catch (Exception ex)
            {
                return FromError(ex);
            }
        }

        public static Task<TResult> FromMethod<TResult>(Func<Task<TResult>> func)
        {
            try
            {
                return func();
            }
            catch (Exception ex)
            {
                return FromError<TResult>(ex);
            }
        }

        public static Task<TResult> FromMethod<TResult>(Func<TResult> func)
        {
            try
            {
                return FromResult(func());
            }
            catch (Exception ex)
            {
                return FromError<TResult>(ex);
            }
        }

        public static Task FromMethod<T1>(Func<T1, Task> func, T1 arg)
        {
            try
            {
                return func(arg);
            }
            catch (Exception ex)
            {
                return FromError(ex);
            }
        }

        public static Task FromMethod<T1, T2>(Func<T1, T2, Task> func, T1 arg1, T2 arg2)
        {
            try
            {
                return func(arg1, arg2);
            }
            catch (Exception ex)
            {
                return FromError(ex);
            }
        }

        public static Task<TResult> FromMethod<T1, TResult>(Func<T1, Task<TResult>> func, T1 arg)
        {
            try
            {
                return func(arg);
            }
            catch (Exception ex)
            {
                return FromError<TResult>(ex);
            }
        }

        public static Task<TResult> FromMethod<T1, TResult>(Func<T1, TResult> func, T1 arg)
        {
            try
            {
                return FromResult(func(arg));
            }
            catch (Exception ex)
            {
                return FromError<TResult>(ex);
            }
        }

        public static Task<TResult> FromMethod<T1, T2, TResult>(Func<T1, T2, Task<TResult>> func, T1 arg1, T2 arg2)
        {
            try
            {
                return func(arg1, arg2);
            }
            catch (Exception ex)
            {
                return FromError<TResult>(ex);
            }
        }

        public static Task<TResult> FromMethod<T1, T2, TResult>(Func<T1, T2, TResult> func, T1 arg1, T2 arg2)
        {
            try
            {
                return FromResult(func(arg1, arg2));
            }
            catch (Exception ex)
            {
                return FromError<TResult>(ex);
            }
        }

        public static Task<T> FromResult<T>(T value)
        {
            var tcs = new TaskCompletionSource<T>();
            tcs.SetResult(value);
            return tcs.Task;
        }

        public static Task FromError(Exception e)
        {
            return FromError<object>(e);
        }

        public static Task<T> FromError<T>(Exception e)
        {
            var tcs = new TaskCompletionSource<T>();
            tcs.SetUnwrappedException(e);
            return tcs.Task;
        }

        internal static void SetUnwrappedException<T>(this TaskCompletionSource<T> tcs, Exception e)
        {
            var aggregateException = e as AggregateException;
            if (aggregateException != null)
            {
                tcs.SetException(aggregateException.InnerExceptions);
            }
            else
            {
                tcs.SetException(e);
            }
        }

        internal static bool TrySetUnwrappedException<T>(this TaskCompletionSource<T> tcs, Exception e)
        {
            var aggregateException = e as AggregateException;
            return aggregateException != null ? tcs.TrySetException(aggregateException.InnerExceptions) : tcs.TrySetException(e);
        }

        private static Task<T> Canceled<T>()
        {
            var tcs = new TaskCompletionSource<T>();
            tcs.SetCanceled();
            return tcs.Task;
        }

        private static Task RunTask(Task task, Action successor)
        {
            var tcs = new TaskCompletionSource<object>();
            task.ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    tcs.SetUnwrappedException(t.Exception);
                }
                else if (t.IsCanceled)
                {
                    tcs.SetCanceled();
                }
                else
                {
                    try
                    {
                        successor();
                        tcs.SetResult(null);
                    }
                    catch (Exception ex)
                    {
                        tcs.SetUnwrappedException(ex);
                    }
                }
            });

            return tcs.Task;
        }

        private static Task RunTaskSynchronously(Task task, Action<object> next, object state, bool onlyOnSuccess = true)
        {
            var tcs = new TaskCompletionSource<object>();
            task.ContinueWith(t =>
            {
                try
                {
                    if (t.IsFaulted)
                    {
                        if (!onlyOnSuccess)
                        {
                            next(state);
                        }

                        tcs.SetUnwrappedException(t.Exception);
                    }
                    else if (t.IsCanceled)
                    {
                        if (!onlyOnSuccess)
                        {
                            next(state);
                        }

                        tcs.SetCanceled();
                    }
                    else
                    {
                        next(state);
                        tcs.SetResult(null);
                    }
                }
                catch (Exception ex)
                {
                    tcs.SetUnwrappedException(ex);
                }
            },
            TaskContinuationOptions.ExecuteSynchronously);

            return tcs.Task;
        }

        private static class TaskRunners<T, TResult>
        {
            internal static Task RunTask(Task<T> task, Action<T> successor)
            {
                var tcs = new TaskCompletionSource<object>();
                task.ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        tcs.SetUnwrappedException(t.Exception);
                    }
                    else if (t.IsCanceled)
                    {
                        tcs.SetCanceled();
                    }
                    else
                    {
                        try
                        {
                            successor(t.Result);
                            tcs.SetResult(null);
                        }
                        catch (Exception ex)
                        {
                            tcs.SetUnwrappedException(ex);
                        }
                    }
                });

                return tcs.Task;
            }

            internal static Task<TResult> RunTask(Task task, Func<TResult> successor)
            {
                var tcs = new TaskCompletionSource<TResult>();
                task.ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        tcs.SetUnwrappedException(t.Exception);
                    }
                    else if (t.IsCanceled)
                    {
                        tcs.SetCanceled();
                    }
                    else
                    {
                        try
                        {
                            tcs.SetResult(successor());
                        }
                        catch (Exception ex)
                        {
                            tcs.SetUnwrappedException(ex);
                        }
                    }
                });

                return tcs.Task;
            }

            internal static Task<TResult> RunTask(Task<T> task, Func<Task<T>, TResult> successor)
            {
                var tcs = new TaskCompletionSource<TResult>();
                task.ContinueWith(t =>
                {
                    if (task.IsFaulted)
                    {
                        tcs.SetUnwrappedException(t.Exception);
                    }
                    else if (task.IsCanceled)
                    {
                        tcs.SetCanceled();
                    }
                    else
                    {
                        try
                        {
                            tcs.SetResult(successor(t));
                        }
                        catch (Exception ex)
                        {
                            tcs.SetUnwrappedException(ex);
                        }
                    }
                });

                return tcs.Task;
            }
        }

        private static class GenericDelegates<T, TResult, T1, T2>
        {
            internal static Task ThenWithArgs(Task task, Action<T1> successor, T1 arg1)
            {
                return RunTask(task, () => successor(arg1));
            }

            internal static Task ThenWithArgs(Task task, Action<T1, T2> successor, T1 arg1, T2 arg2)
            {
                return RunTask(task, () => successor(arg1, arg2));
            }

            internal static Task<TResult> ThenWithArgs(Task<T> task, Func<T, T1, TResult> successor, T1 arg1)
            {
                return TaskRunners<T, TResult>.RunTask(task, t => successor(t.Result, arg1));
            }

            internal static Task<Task> ThenWithArgs(Task task, Func<T1, Task> successor, T1 arg1)
            {
                return TaskRunners<object, Task>.RunTask(task, () => successor(arg1));
            }

            internal static Task<Task> ThenWithArgs(Task task, Func<T1, T2, Task> successor, T1 arg1, T2 arg2)
            {
                return TaskRunners<object, Task>.RunTask(task, () => successor(arg1, arg2));
            }

            internal static Task<Task<T>> ThenWithArgs(Task<T> task, Func<Task<T>, T1, Task<T>> successor, T1 arg1)
            {
                return TaskRunners<T, Task<T>>.RunTask(task, t => successor(t, arg1));
            }
        }

        private static class TaskCache<T>
        {
            public static readonly Task<T> EmptyTask = MakeTask(default(T));
        }
    }
}
