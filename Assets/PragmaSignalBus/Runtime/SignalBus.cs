using System;
using System.Collections.Generic;

namespace Pragma.SignalBus
{
    public class SignalBus : ISignalBus
    {
        private readonly Dictionary<Type, List<Subscription>> _subscriptions;

        private List<ISignalBus> _children; 

        private List<Subscription> _subscriptionsToRegister;
        private List<Subscription> _subscriptionsToDeregister;

        private bool _isAlreadyInvoked;
        private Type _currentInvokedType;
        private bool _isDirtySubscriptions;

        public SignalBus()
        {
            _subscriptions = new Dictionary<Type, List<Subscription>>();

            _children = new List<ISignalBus>();

            _subscriptionsToDeregister = new List<Subscription>();
            _subscriptionsToRegister = new List<Subscription>();
        }

        private bool IsAlreadyInvoked(Type type) => _isAlreadyInvoked && _currentInvokedType == type;
        
        public void AddChildren(ISignalBus signalBus)
        {
            _children.Add(signalBus);
        }

        public void RemoveChildren(ISignalBus signalBus)
        {
            _children.Remove(signalBus);
        }

        public void Register<TSignal>(Action<TSignal> action, int order = int.MaxValue, object extraToken = null) where TSignal : class
        {
            Action<object> wrapperAction = args => action((TSignal)args);
            
            Register(typeof(TSignal), wrapperAction, action, order, extraToken);
        }
        
        public void Register<TSignal>(Action action, int order = int.MaxValue, object extraToken = null) where TSignal : class
        {
            Action<object> wrapperAction = _ => action();
            
            Register(typeof(TSignal), wrapperAction, action, order, extraToken);
        }

        private void Register(Type signalType, Action<object> action, object token, int order = int.MaxValue, object extraToken = null)
        {
            var subscription = new Subscription(action, token, order, extraToken);

            if (_subscriptions.TryGetValue(signalType, out var subscriptions))
            {
                if (IsAlreadyInvoked(signalType))
                {
                    _subscriptionsToRegister.Add(subscription);
                    _isDirtySubscriptions = true;
                }
                else
                {
                    InsertSubscription(subscriptions, subscription);
                }
            }
            else
            {
                _subscriptions.Add(signalType, new List<Subscription>() {subscription});
            }
        }

        private void InsertSubscription(List<Subscription> subscriptions, Subscription subscription)
        {
            if (subscription.order == int.MaxValue)
            {
                subscriptions.Add(subscription);
                return;
            }

            for (var i = 0; i < subscriptions.Count; i++)
            {
                if (subscriptions[i].order <= subscription.order)
                {
                    continue;
                }
                
                subscriptions.Insert(i, subscription);
                return;
            }
            
            subscriptions.Add(subscription);
        }

        public void Deregister<TSignal>(Action action) where TSignal : class
        {
            Deregister(typeof(TSignal), action);
        }
        
        public void Deregister<TSignal>(Action<TSignal> action) where TSignal : class
        {
            Deregister(typeof(TSignal), action);
        }

        private void Deregister(Type signalType, object token)
        {
            if (!_subscriptions.ContainsKey(signalType))
            {
                return;
            }

            var subscriptions = _subscriptions[signalType];

            var subscriptionToRemove = subscriptions.FindIndex(subscription => subscription.token.GetHashCode() == token.GetHashCode());

            if (subscriptionToRemove == -1)
            {
                return;
            }

            if (IsAlreadyInvoked(signalType))
            {
                _subscriptionsToDeregister.Add(subscriptions[subscriptionToRemove]);
                _isDirtySubscriptions = true;
            }
            else
            {
                subscriptions.RemoveAt(subscriptionToRemove);
            }
        }

        public void DeregisterByExtraToken(object extraToken)
        {
            foreach (var subscriptions in _subscriptions.Values)
            {
                subscriptions.RemoveAll(subscription => subscription.extraToken == extraToken);
            }
        }
        
        public void ClearSubscriptions()
        {
            _subscriptions.Clear();
        }

        public void Invoke<TSignal>(TSignal signal) where TSignal : class
        {
            Invoke(typeof(TSignal), signal);
        }

        public void Invoke<TSignal>() where TSignal : class
        {
            Invoke(typeof(TSignal),null);
        }

        public TSignal InvokeWithCreateInstance<TSignal>() where TSignal : class
        {
            var instance = Activator.CreateInstance<TSignal>();
            
            Invoke(typeof(TSignal), instance);

            return instance;
        }
        
        public void InvokeWithBroadcast<TSignal>(TSignal signal) where TSignal : class
        {
            InvokeWithBroadcast(typeof(TSignal), signal);
        }
        
        public void InvokeWithBroadcast<TSignal>() where TSignal : class
        {
            InvokeWithBroadcast(typeof(TSignal), null);
        }
        
        public void InvokeWithBroadcast(Type signalType, object signal)
        {
            Invoke(signalType, signal);
            
            Broadcast(signalType, signal);
        }

        private void Broadcast(Type signalType, object signal)
        {
            foreach (var signalBus in _children)
            {
                signalBus.InvokeWithBroadcast(signalType, signal);
            }
        }
        
        private void Invoke(Type signalType, object signal)
        {
            if (!_subscriptions.TryGetValue(signalType, out var subscriptions))
            {
                return;
            }

            _isAlreadyInvoked = true;
            _currentInvokedType = signalType;
            
            var cachedCount = subscriptions.Count;

            for (var i = 0; i < cachedCount; i++)
            {
                subscriptions[i].action.Invoke(signal);
            }
            
            _isAlreadyInvoked = false;

            if (_isDirtySubscriptions)
            {
                RefreshSubscriptions();

                _isDirtySubscriptions = false;
            }
        }

        private void RefreshSubscriptions()
        {
            var subscriptions = _subscriptions[_currentInvokedType];

            foreach (var subscription in _subscriptionsToDeregister)
            {
                subscriptions.Remove(subscription);
            }
            
            _subscriptionsToDeregister.Clear();

            foreach (var subscription in _subscriptionsToRegister)
            {
                InsertSubscription(subscriptions, subscription);
            }
            
            _subscriptionsToRegister.Clear();
        }
    }
}
