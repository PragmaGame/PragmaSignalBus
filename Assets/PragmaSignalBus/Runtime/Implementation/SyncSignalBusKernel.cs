using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using UnityEngine.Scripting;

namespace PragmaSignalBus
{
    [Preserve]
    internal class SyncSignalBusKernel : SignalBusKernel<Action<object>>
    {
        [RequiredMember]
        public SyncSignalBusKernel(SignalBusConfiguration configuration = null) : base(configuration)
        {
        }

        public object Register<TSignal>(Action<TSignal> signal, SortOptions sortOptions = null)
        {
            var extraToken = GetToken();

            Register(signal, extraToken, sortOptions);

            return extraToken;
        }

        public object Register<TSignal>(Action signal, SortOptions sortOptions = null)
        {
            var extraToken = GetToken();

            Register<TSignal>(signal, extraToken, sortOptions);

            return extraToken;
        }

        public void Register<TSignal>(Action<TSignal> signal, object token, SortOptions sortOptions = null)
        {
            Register(typeof(TSignal), WrapperSignal, signal, sortOptions, token);

            return;

            void WrapperSignal(object args) => signal((TSignal)args);
        }

        public void Register<TSignal>(Action signal, object token, SortOptions sortOptions = null)
        {
            Register(typeof(TSignal), WrapperAction, signal, sortOptions, token);

            return;

            void WrapperAction(object _) => signal();
        }

        public void Deregister<TSignal>(Action signal)
        {
            Deregister(typeof(TSignal), signal);
        }

        public void Deregister<TSignal>(Action<TSignal> signal)
        {
            Deregister(typeof(TSignal), signal);
        }

        public void Send<TSignal>(TSignal signal)
        {
            Send(typeof(TSignal), signal);
        }

        public void Send<TSignal>()
        {
            Send<TSignal>(typeof(TSignal), default);
        }

        public void Send<TSignal>(Type signalType, TSignal signal)
        {
            if (!subscriptions.TryGetValue(signalType, out var value))
            {
                TryThrowException($"Dont find Subscription. Signal Type : {signalType}");
                return;
            }

            var buffer = RentBuffer(value);
            
            Send(signal, value);

            ReleaseBuffer(buffer);
        }

        public void SendUnsafe<TSignal>(Type signalType, TSignal signal)
        {
            if (!subscriptions.TryGetValue(signalType, out var value))
            {
                TryThrowException($"Dont find Subscription. Signal Type : {signalType}");
                return;
            }
            
            Send(signal, value);
        }
        
        private void Send<TSignal>(TSignal signal, List<SignalSubscription<Action<object>>> subscriptions)
        {
            var cachedCount = subscriptions.Count;

            for (var i = 0; i < cachedCount; i++)
            {
                if (subscriptions[i].Handler is not Action<TSignal> typedHandler)
                {
                    return;
                }
                
                typedHandler.Invoke(signal);
            }
        }
    }
}
