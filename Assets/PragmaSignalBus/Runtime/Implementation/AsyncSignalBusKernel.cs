using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Scripting;

namespace PragmaSignalBus
{
    [Preserve]
    internal class AsyncSignalBusKernel : SignalBusKernel<Func<object, CancellationToken, UniTask>>
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

            Register(signal, extraToken, sortOptions);

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
            Register(typeof(TSignal), WrapperSignal, signal, sortOptions, token);

            return;

            UniTask WrapperSignal(object args, CancellationToken ct) => signal((TSignal)args, ct);
        }

        public void Register<TSignal>(Func<CancellationToken, UniTask> signal, object token, SortOptions sortOptions = null)
        {
            Register(typeof(TSignal), WrapperAction, signal, sortOptions, token);

            return;

            UniTask WrapperAction(object _, CancellationToken ct) => signal(ct);
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
            if (!subscriptions.TryGetValue(signalType, out var signalSubscriptions))
            {
                configuration.Logger?.Invoke(LogType.Log, $"Dont find Subscription. Signal Type : {signalType}");
                return;
            }

            var buffer = RentBuffer(signalSubscriptions);

            try
            {
                switch (asyncSendInvocationType)
                {
                    case AsyncSendInvocationType.Sequence:
                    {
                        await SequenceSend(signalSubscriptions, signal, token);
                        break;
                    }
                    case AsyncSendInvocationType.Parallel:
                    {
                        await ParallelSend(signalSubscriptions, signal, token);
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
        
        private async UniTask SequenceSend<TEvent>(List<SignalSubscription<Func<object, CancellationToken, UniTask>>> publishList, TEvent eventData, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            
            foreach (var subscription in publishList)
            {
                token.ThrowIfCancellationRequested();
                
                if (subscription.SourceDelegate is Func<TEvent, CancellationToken, UniTask> typed)
                {
                    await typed.Invoke(eventData, token);
                }
                else
                {
                    await subscription.Handler.Invoke(eventData, token);
                }
            }
        }

        private async UniTask ParallelSend<TEvent>(List<SignalSubscription<Func<object, CancellationToken, UniTask>>> invocationList, TEvent eventData, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            var tasks = RentTaskBuffer(invocationList.Count);

            try
            {
                foreach (var subscription in invocationList)
                {
                    var task = subscription.SourceDelegate is Func<TEvent, CancellationToken, UniTask> typed
                        ? typed.Invoke(eventData, token)
                        : subscription.Handler.Invoke(eventData, token);

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