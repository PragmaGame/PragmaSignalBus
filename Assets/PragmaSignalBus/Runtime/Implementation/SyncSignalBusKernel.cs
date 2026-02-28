using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using UnityEngine;
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

        public void SendAbstract(object signal)
        {
            if (signal == null)
            {
                configuration.Logger?.Invoke(LogType.Log, $"Signal is null");
                return;
            }
            
            Send(signal.GetType(), signal);
        }

        public void Send<TSignal>(Type signalType, TSignal signal)
        {
            if (!subscriptions.TryGetValue(signalType, out var value))
            {
                configuration.Logger?.Invoke(LogType.Log, $"Dont find Subscription. Signal Type : {signalType}");
                return;
            }

            var buffer = RentBuffer(value);

            try
            {
                Send(signal, value);
            }
            finally
            {
                ReleaseBuffer(buffer);
            }
        }

        public void SendUnsafe<TSignal>(TSignal signal)
        {
            var signalType = typeof(TSignal);
            
            if (!subscriptions.TryGetValue(signalType, out var value))
            {
                configuration.Logger?.Invoke(LogType.Log, $"Dont find Subscription. Signal Type : {signalType}");
                return;
            }
            
            Send(signal, value);
        }
        
        private void Send<TSignal>(TSignal signal, List<SignalSubscription<Action<object>>> invocationList)
        {
            var cachedCount = invocationList.Count;

            for (var i = 0; i < cachedCount; i++)
            {
                if (invocationList[i].SourceDelegate is Action<TSignal> typedDelegate)
                {
                    typedDelegate.Invoke(signal);
                }
                else
                {
                    invocationList[i].Handler.Invoke(signal);
                }
            }
        }
    }
}
