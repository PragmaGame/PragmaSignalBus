using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.Scripting;

namespace PragmaSignalBus
{
    [Preserve]
    internal class AsyncSignalBusKernel : SignalBusKernel<Func<object, CancellationToken, UniTask>>
    {
        private readonly Dictionary<Type, AlreadySendSignalInfo<Func<object, CancellationToken, UniTask>>> _alreadySendSignals;
        private readonly Stack<AlreadySendSignalInfo<Func<object, CancellationToken, UniTask>>> _poolSendSignals;

        public AsyncSignalBusKernel(SignalBusConfiguration configuration = null) : base(configuration)
        {
            _alreadySendSignals = new Dictionary<Type, AlreadySendSignalInfo<Func<object, CancellationToken, UniTask>>>();
            _poolSendSignals = new Stack<AlreadySendSignalInfo<Func<object, CancellationToken, UniTask>>>();
        }
        
        private AlreadySendSignalInfo<Func<object, CancellationToken, UniTask>> GetSignalInfo()
        {
            if (_poolSendSignals.Count > 0)
            {
                return _poolSendSignals.Pop();
            }

            return new AlreadySendSignalInfo<Func<object, CancellationToken, UniTask>>();
        }

        private void ReleaseSignalInfo(AlreadySendSignalInfo<Func<object, CancellationToken, UniTask>> eventInfo)
        {
            _poolSendSignals.Push(eventInfo);
        }

        protected override bool IsAnyAlreadySend()
        {
            return _alreadySendSignals.Count > 0;
        }

        protected override bool IsAlreadySend(Type type, out AlreadySendSignalInfo<Func<object, CancellationToken, UniTask>> signalInfo)
        {
            return _alreadySendSignals.TryGetValue(type, out signalInfo);
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
            return Send(typeof(TSignal), null, token, asyncSendInvocationType);
        }
        
        private async UniTask Send(Type signalType, object signal, CancellationToken token, AsyncSendInvocationType asyncSendInvocationType = AsyncSendInvocationType.Sequence)
        {
            if (_alreadySendSignals.ContainsKey(signalType))
            {
                TryThrowException($"Recursion detected for signal type: {signalType}");
                return;
            }
            
            if (!subscriptions.TryGetValue(signalType, out var signalSubscriptions))
            {
                TryThrowException($"Dont find Subscription. Signal Type : {signalType}");
                return;
            }

            var alreadySendSignalInfo = GetSignalInfo();
            _alreadySendSignals.Add(signalType, alreadySendSignalInfo);
            
            var cachedCount = signalSubscriptions.Count;

            try
            {
                switch (asyncSendInvocationType)
                {
                    case AsyncSendInvocationType.Sequence:
                    {
                        for (var i = 0; i < cachedCount; i++)
                        {
                            await signalSubscriptions[i].Action.Invoke(signal, token);
                        }

                        break;
                    }
                    case AsyncSendInvocationType.Parallel:
                    {
                        await UniTask.WhenAll(signalSubscriptions.Select(subscription =>
                            subscription.Action(signal, token)));

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
                if (alreadySendSignalInfo.IsDirtySubscriptions)
                {
                    RefreshSubscriptions(signalType, alreadySendSignalInfo);
                }
            
                _alreadySendSignals.Remove(signalType);
                ReleaseSignalInfo(alreadySendSignalInfo);
            }
        }
    }
}