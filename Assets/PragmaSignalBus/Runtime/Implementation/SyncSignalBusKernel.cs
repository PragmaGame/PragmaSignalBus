using System;
using UnityEngine.Scripting;

namespace PragmaSignalBus
{
    [Preserve]
    internal class SyncSignalBusKernel : SignalBusKernel<Action<object>>
    {
        private AlreadySendSignalInfo<Action<object>> _signalInfo;

        private bool _isAlreadySend;
        private Type _currentSendType;
        private bool _isDirtySubscriptions;

        [RequiredMember]
        public SyncSignalBusKernel(SignalBusConfiguration configuration = null) : base(configuration)
        {
            _signalInfo = new AlreadySendSignalInfo<Action<object>>();
        }
        
        protected override bool IsAlreadySend(Type type, out AlreadySendSignalInfo<Action<object>> signalInfo)
        {
            signalInfo = _signalInfo;
            return _isAlreadySend && _currentSendType == type;
        }

        protected override bool IsAnyAlreadySend() => _isAlreadySend;

        protected override void OnAddedSubscriptionsToRegister(AlreadySendSignalInfo<Action<object>> signalInfo, SignalSubscription<Action<object>> subscription)
        {
            base.OnAddedSubscriptionsToRegister(signalInfo, subscription);
            _isDirtySubscriptions = true;
        }

        protected override void OnAddedSubscriptionsToDeregister(AlreadySendSignalInfo<Action<object>> signalInfo, SignalSubscription<Action<object>> subscription)
        {
            base.OnAddedSubscriptionsToDeregister(signalInfo, subscription);
            _isDirtySubscriptions = true;
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
            Send(typeof(TSignal), null);
        }

        private void Send(Type signalType, object signal)
        {
            if (!subscriptions.TryGetValue(signalType, out var value))
            {
                TryThrowException($"Dont find Subscription. Signal Type : {signalType}");
                return;
            }

            _isAlreadySend = true;
            _currentSendType = signalType;

            var cachedCount = value.Count;

            for (var i = 0; i < cachedCount; i++)
            {
                value[i].Action.Invoke(signal);
            }

            _isAlreadySend = false;

            if (_isDirtySubscriptions)
            {
                RefreshSubscriptions(_currentSendType, _signalInfo);

                _isDirtySubscriptions = false;
            }
        }
    }
}
