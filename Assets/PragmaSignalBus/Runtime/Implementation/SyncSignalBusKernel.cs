using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;

namespace PragmaSignalBus
{
    [Preserve]
    internal class SyncSignalBusKernel : SignalBusKernel
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
            Register(typeof(TSignal), signal, sortOptions, token);
        }

        public void Register<TSignal>(Action signal, object token, SortOptions sortOptions = null)
        {
            Register(typeof(TSignal), signal, sortOptions, token);
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
            if(!TryGetSubscriptions(signalType, out var subscriptions))
            {
                return;
            }

            var buffer = RentBuffer(subscriptions);

            try
            {
                Send(signal, buffer);
            }
            finally
            {
                ReleaseBuffer(buffer);
            }
        }

        public void SendUnsafe<TSignal>(TSignal signal)
        {
            var signalType = typeof(TSignal);

            if(!TryGetSubscriptions(signalType, out var subscriptions))
            {
                return;
            }

            Send(signal, subscriptions);
        }

        private void Send<TSignal>(TSignal signal, List<SignalSubscription> subscriptions)
        {
            var cachedCount = subscriptions.Count;

            for (var i = 0; i < cachedCount; i++)
            {
                var sourceDelegate = subscriptions[i].SourceDelegate;

                switch (sourceDelegate)
                {
                    case Action<TSignal> argAction:
                    {
                        argAction.Invoke(signal);
                        break;
                    }
                    case Action action:
                    {
                        action.Invoke();
                        break;
                    }
                    default:
                    {
                        configuration?.Logger?.Invoke(LogType.Log, $"Dynamic invocation. Signal Type : {typeof(TSignal)}, Delegate Type : {sourceDelegate.GetType()}");
                        sourceDelegate?.DynamicInvoke(signal);
                        break;
                    }
                }
            }
        }
    }
}
