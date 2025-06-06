using System;
using System.Collections.Generic;
using UnityEngine.Scripting;

namespace Pragma.SignalBus
{
    public class SignalBus : ISignalBus
    {
        private readonly Dictionary<Type, List<Subscription>> _subscriptions;

        private List<Subscription> _subscriptionsToRegister;
        private List<Subscription> _subscriptionsToDeregister;
        private HashSet<Type> _subscriptionsToSort;

        private bool _isAlreadySend;
        private Type _currentSendType;
        private bool _isDirtySubscriptions;

        protected Configuration configuration;

        [RequiredMember]
        public SignalBus(Configuration configuration = null)
        {
            this.configuration = configuration ?? new Configuration();

            _subscriptions = new Dictionary<Type, List<Subscription>>();

            _subscriptionsToDeregister = new List<Subscription>();
            _subscriptionsToRegister = new List<Subscription>();
            _subscriptionsToSort = new HashSet<Type>();
        }

        private bool IsAlreadySend(Type type) => _isAlreadySend && _currentSendType == type;

        public object Register<TSignal>(
            Action<TSignal> action,
            Type owner = null,
            Type[] beforeOrder = null,
            Type[] afterOrder = null,
            bool isLazySorted = true) where TSignal : class
        {
            var token = GetDefaultToken();
            Register(action, token, owner, beforeOrder, afterOrder, isLazySorted);
            return token;
        }

        public object Register<TSignal>(
            Action action,
            Type owner = null,
            Type[] beforeOrder = null,
            Type[] afterOrder = null,
            bool isLazySorted = true) where TSignal : class
        {
            var token = GetDefaultToken();
            Register<TSignal>(action, token, owner, beforeOrder, afterOrder, isLazySorted);
            return token;
        }

        public void Register<TSignal>(
            Action action,
            object token,
            Type owner = null,
            Type[] beforeOrder = null,
            Type[] afterOrder = null,
            bool isLazySorted = true) where TSignal : class
        {
            Action<object> wrapperAction = _ => action();
            Register(typeof(TSignal), wrapperAction, action, token, owner, beforeOrder, afterOrder, isLazySorted);
        }

        public void Register<TSignal>(
            Action<TSignal> action,
            object token,
            Type owner = null,
            Type[] beforeOrder = null,
            Type[] afterOrder = null,
            bool isLazySorted = true) where TSignal : class
        {
            Action<object> wrapperAction = args => action((TSignal)args);
            Register(typeof(TSignal), wrapperAction, action, token, owner, beforeOrder, afterOrder, isLazySorted);
        }

        private void Register(
            Type signalType,
            Action<object> action,
            object token,
            object extraToken,
            Type owner,
            Type[] beforeOrder,
            Type[] afterOrder,
            bool isLazySorted = true)
        {
            var subscription = new Subscription(action, token, extraToken, owner, beforeOrder, afterOrder);

            if (_subscriptions.TryGetValue(signalType, out var subscriptions))
            {
                if (IsAlreadySend(signalType))
                {
                    _subscriptionsToRegister.Add(subscription);
                    _isDirtySubscriptions = true;
                }
                else
                {
                    subscriptions.Add(subscription);

                    if (owner == null)
                    {
                        return;
                    }
                    
                    if (isLazySorted && !_subscriptionsToSort.Contains(signalType))
                    {
                        _subscriptionsToSort.Add(signalType);
                    }
                    else
                    {
                        SubscriptionTopologicalSorter.Sort(subscriptions);
                    }
                }
            }
            else
            {
                _subscriptions.Add(signalType, new List<Subscription>() { subscription });
            }
        }

        private void SortSubscriptions(Type type)
        {
            SubscriptionTopologicalSorter.Sort(_subscriptions[type]);
        }

        public void SortSubscriptions()
        {
            foreach (var type in _subscriptionsToSort)
            {
                SortSubscriptions(type);
            }
            
            _subscriptionsToSort.Clear();
        }

        public bool Deregister<TSignal>(Action action) where TSignal : class
        {
            return Deregister(typeof(TSignal), action);
        }
        
        public bool Deregister<TSignal>(Action<TSignal> action) where TSignal : class
        {
            return Deregister(typeof(TSignal), action);
        }

        private bool Deregister(Type signalType, object token)
        {
            if (!_subscriptions.ContainsKey(signalType))
            {
                return false;
            }

            var subscriptions = _subscriptions[signalType];

            var subscriptionToRemove = subscriptions.FindIndex(subscription => subscription.token.GetHashCode() == token.GetHashCode());

            if (subscriptionToRemove == -1)
            {
                return false;
            }

            if (IsAlreadySend(signalType))
            {
                _subscriptionsToDeregister.Add(subscriptions[subscriptionToRemove]);
                _isDirtySubscriptions = true;
            }
            else
            {
                subscriptions.RemoveAt(subscriptionToRemove);
            }

            return true;
        }

        public int Deregister(object token)
        {
            var hashToken = token.GetHashCode();
            var removeCount = 0;
            
            if (_isAlreadySend)
            {
                foreach (var key in _subscriptions.Keys)
                {
                    var subscriptions = _subscriptions[key];
                    var isCurrentPublish = key == _currentSendType;

                    for (var i = 0; i < subscriptions.Count; i++)
                    {
                        if (subscriptions[i].extraToken.GetHashCode() != hashToken)
                        {
                            continue;
                        }

                        removeCount++;
                        
                        if (isCurrentPublish)
                        {
                            _subscriptionsToDeregister.Add(subscriptions[i]);
                            _isDirtySubscriptions = true;
                        }
                        else
                        {
                            subscriptions.RemoveAt(i);
                        }
                    }
                }
            }
            else
            {
                foreach (var subscriptions in _subscriptions.Values)
                {
                    removeCount += subscriptions.RemoveAll(subscription => subscription.extraToken.GetHashCode() == hashToken);
                }
            }

            return removeCount;
        }
        
        public void ClearSubscriptions()
        {
            _subscriptions.Clear();
        }
        
        public void SendFromPool<TSignal>(Action<TSignal> setter) where TSignal : class
        {
            var signal = SignalPool.Rent<TSignal>();
            setter.Invoke(signal);
            Send(typeof(TSignal), signal);
        }
        
        public SignalWrapper<TSignal> FromPool<TSignal>() where TSignal : class
        {
            return new SignalWrapper<TSignal>(this, SignalPool.Rent<TSignal>());
        }

        public void Send<TSignal>(TSignal signal) where TSignal : class
        {
            Send(typeof(TSignal), signal);
        }

        public void Send<TSignal>() where TSignal : class
        {
            Send(typeof(TSignal),null);
        }

        public void Send(Type signalType, object signal)
        {
            if (!_subscriptions.TryGetValue(signalType, out var subscriptions))
            {
                return;
            }
            
            if(_subscriptionsToSort.Count > 0 && _subscriptionsToSort.Contains(signalType))
            {
                SubscriptionTopologicalSorter.Sort(subscriptions);
                _subscriptionsToSort.Remove(signalType);
            }

            _isAlreadySend = true;
            _currentSendType = signalType;
            
            var cachedCount = subscriptions.Count;

            for (var i = 0; i < cachedCount; i++)
            {
                subscriptions[i].action.Invoke(signal);
            }
            
            _isAlreadySend = false;

            if (_isDirtySubscriptions)
            {
                RefreshSubscriptions();

                _isDirtySubscriptions = false;
            }
        }

        private void RefreshSubscriptions()
        {
            var subscriptions = _subscriptions[_currentSendType];

            foreach (var subscription in _subscriptionsToDeregister)
            {
                subscriptions.Remove(subscription);
            }
            
            _subscriptionsToDeregister.Clear();

            foreach (var subscription in _subscriptionsToRegister)
            {
                subscriptions.Add(subscription);
            }

            if (_subscriptionsToRegister.Count > 0)
            {
                SubscriptionTopologicalSorter.Sort(subscriptions);
            }
            
            _subscriptionsToRegister.Clear();
        }

        protected virtual object GetDefaultToken()
        {
            return Guid.NewGuid();
        }
    }
}
