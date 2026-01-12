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
        public AsyncSignalBusKernel(SignalBusConfiguration configuration = null) : base(configuration)
        {
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
            return Send(typeof(TSignal), true, signal, token, asyncSendInvocationType);
        }

        public UniTask Send<TSignal>(CancellationToken token = default, AsyncSendInvocationType asyncSendInvocationType = AsyncSendInvocationType.Sequence)
        {
            return Send<TSignal>(typeof(TSignal), false, default, token, asyncSendInvocationType);
        }
        
        public async UniTask Send<TSignal>(Type signalType, bool isHasValue, TSignal signal, CancellationToken token, AsyncSendInvocationType asyncSendInvocationType = AsyncSendInvocationType.Sequence)
        {
            if (!subscriptions.TryGetValue(signalType, out var signalSubscriptions))
            {
                TryThrowException($"Dont find Subscription. Signal Type : {signalType}");
                return;
            }

            var buffer = RentBuffer(signalSubscriptions);
            var cachedCount = signalSubscriptions.Count;

            try
            {
                switch (asyncSendInvocationType)
                {
                    case AsyncSendInvocationType.Sequence:
                    {
                        for (var i = 0; i < cachedCount; i++)
                        {
                            var subscription = buffer[i];
                
                            if (subscription.Handler is Func<TSignal, CancellationToken, UniTask> typed)
                            {
                                await typed.Invoke(signal, token);
                            }
                        }

                        break;
                    }
                    case AsyncSendInvocationType.Parallel:
                    {
                        await UniTask.WhenAll(signalSubscriptions.Select(subscription =>
                        {
                            if (subscription.Handler is Func<TSignal, CancellationToken, UniTask> typed)
                            {
                                return typed(signal, token);
                            }

                            return UniTask.CompletedTask;
                        }));
                        
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
    }
}