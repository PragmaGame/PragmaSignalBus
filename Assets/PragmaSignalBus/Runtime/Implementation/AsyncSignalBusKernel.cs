using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Scripting;

namespace PragmaSignalBus
{
    [Preserve]
    internal class AsyncSignalBusKernel : SignalBusKernel
    {
        private readonly Stack<List<UniTask>> _taskListPool;

        public AsyncSignalBusKernel(SignalBusConfiguration configuration = null) : base(configuration)
        {
            _taskListPool = new Stack<List<UniTask>>();
        }

        private List<UniTask> RentTaskBuffer(int size)
        {
            if (_taskListPool.Count > 0)
            {
                return _taskListPool.Pop();
            }

            return new List<UniTask>(size);
        }

        private void ReleaseTaskBuffer(List<UniTask> list)
        {
            list.Clear();
            _taskListPool.Push(list);
        }

        public object Register<TSignal>(Func<TSignal, CancellationToken, UniTask> signal, SortOptions sortOptions = null)
        {
            var extraToken = GetToken();
            Register<TSignal>(signal, extraToken, sortOptions);
            return extraToken;
        }

        public object Register<TSignal>(Func<CancellationToken, UniTask> signal, SortOptions sortOptions = null)
        {
            var extraToken = GetToken();
            Register<TSignal>(signal, extraToken, sortOptions);
            return extraToken;
        }

        public void Register<TSignal>(Func<TSignal, CancellationToken, UniTask> signal, object token, SortOptions sortOptions = null)
        {
            Register(typeof(TSignal), signal, sortOptions, token);
        }

        public void Register<TSignal>(Func<CancellationToken, UniTask> signal, object token, SortOptions sortOptions = null)
        {
            Register(typeof(TSignal), signal, sortOptions, token);
        }

        public void Deregister<TSignal>(Func<CancellationToken, UniTask> signal)
        {
            Deregister(typeof(TSignal), signal);
        }

        public void Deregister<TSignal>(Func<TSignal, CancellationToken, UniTask> signal)
        {
            Deregister(typeof(TSignal), signal);
        }

        public UniTask Send<TSignal>(TSignal signal, CancellationToken token = default, AsyncSendInvocationType asyncSendInvocationType = AsyncSendInvocationType.Sequence)
        {
            return Send(typeof(TSignal), signal, token, asyncSendInvocationType);
        }

        public UniTask Send<TSignal>(CancellationToken token = default, AsyncSendInvocationType asyncSendInvocationType = AsyncSendInvocationType.Sequence)
        {
            return Send<TSignal>(typeof(TSignal), default, token, asyncSendInvocationType);
        }

        public async UniTask Send<TSignal>(Type signalType, TSignal signal, CancellationToken token, AsyncSendInvocationType asyncSendInvocationType = AsyncSendInvocationType.Sequence)
        {
            if(!TryGetSubscriptions(signalType, out var subscriptions))
            {
                return;
            }

            var buffer = RentBuffer(subscriptions);

            try
            {
                switch (asyncSendInvocationType)
                {
                    case AsyncSendInvocationType.Sequence:
                    {
                        await SequenceSend(buffer, signal, token);
                        break;
                    }
                    case AsyncSendInvocationType.Parallel:
                    {
                        await ParallelSend(buffer, signal, token);
                        break;
                    }
                    default:
                    {
                        throw new ArgumentOutOfRangeException(nameof(asyncSendInvocationType),
                            asyncSendInvocationType, null);
                    }
                }
            }
            finally
            {
                ReleaseBuffer(buffer);
            }
        }

        public UniTask SendAbstract(object signal, CancellationToken token, AsyncSendInvocationType asyncSendInvocationType = AsyncSendInvocationType.Sequence)
        {
            if (signal == null)
            {
                configuration.Logger?.Invoke(LogType.Log, $"SendAbstract : Signal is null");
                return UniTask.CompletedTask;
            }

            return Send(signal.GetType(), signal, token, asyncSendInvocationType);
        }

        private async UniTask SequenceSend<TSignal>(List<SignalSubscription> subscriptions, TSignal signal, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            foreach (var subscription in subscriptions)
            {
                token.ThrowIfCancellationRequested();

                switch (subscription.SourceDelegate)
                {
                    case Func<TSignal, CancellationToken, UniTask> typed:
                    {
                        await typed.Invoke(signal, token);
                        break;
                    }
                    case Func<CancellationToken, UniTask> action:
                    {
                        await action.Invoke(token);
                        break;
                    }
                    default:
                    {
                        if (subscription.SourceDelegate.DynamicInvoke(signal, token) is UniTask task)
                        {
                            configuration.Logger?.Invoke(LogType.Log, $"Dynamic invocation. Signal Type : {typeof(TSignal)}, Delegate Type : {subscription.SourceDelegate.GetType()}");
                            await task;
                        }
                        
                        break;
                    }
                }
            }
        }

        private async UniTask ParallelSend<TSignal>(List<SignalSubscription> subscriptions, TSignal signal, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            var tasks = RentTaskBuffer(subscriptions.Count);

            try
            {
                foreach (var subscription in subscriptions)
                {
                    UniTask task;

                    switch (subscription.SourceDelegate)
                    {
                        case Func<TSignal, CancellationToken, UniTask> typed:
                        {
                            task = typed.Invoke(signal, token);
                            break;
                        }
                        case Func<CancellationToken, UniTask> action:
                        {
                            task = action.Invoke(token);
                            break;
                        }
                        default:
                        {
                            if (subscription.SourceDelegate.DynamicInvoke(signal, token) is UniTask t)
                            {
                                configuration.Logger?.Invoke(LogType.Log, $"Dynamic invocation. Signal Type : {typeof(TSignal)}, Delegate Type : {subscription.SourceDelegate.GetType()}");
                                task = t;
                            }
                            else
                            {
                                continue;
                            }

                            break;
                        }
                    }

                    tasks.Add(task);
                }

                await UniTask.WhenAll(tasks);
            }
            finally
            {
                ReleaseTaskBuffer(tasks);
            }
        }
    }
}