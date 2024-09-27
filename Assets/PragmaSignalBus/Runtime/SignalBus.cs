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
        
        private bool _isAlreadySend;
        private Type _currentSendType;
        private bool _isDirtySubscriptions;

        private List<ISignalBus> _childrenToAdded;
        private List<ISignalBus> _childrenToRemoved;

        private bool _isAlreadyBroadcast;
        private bool _isDirtyChildren;
        
        protected Configuration configuration;

        public SignalBus(Configuration configuration = null)
        {
            this.configuration = configuration ?? new Configuration();
            
            _subscriptions = new Dictionary<Type, List<Subscription>>();
            
            _subscriptionsToDeregister = new List<Subscription>();
            _subscriptionsToRegister = new List<Subscription>();

            _children = new List<ISignalBus>();

            _childrenToAdded = new List<ISignalBus>();
            _childrenToRemoved = new List<ISignalBus>();
        }

        private bool IsAlreadySend(Type type) => _isAlreadySend && _currentSendType == type;
        
        public void AddChildren(ISignalBus signalBus)
        {
            if (_isAlreadyBroadcast)
            {
                _childrenToAdded.Add(signalBus);
                _isDirtyChildren = true;
            }
            else
            {
                _children.Add(signalBus);
            }
        }

        public void RemoveChildren(ISignalBus signalBus)
        {
            if (_isAlreadyBroadcast)
            {
                _childrenToRemoved.Add(signalBus);
                _isDirtyChildren = true;
            }
            else
            {
                _children.Remove(signalBus);
            }
        }
        
        public object Register<TSignal>(Action<TSignal> action, int order = int.MaxValue) where TSignal : class
        {
            var token = GetDefaultToken();
            Register(action, token, order);
            return token;
        }
        
        public object Register<TSignal>(Action action, int order = int.MaxValue) where TSignal : class
        {
            var token = GetDefaultToken();
            Register<TSignal>(action, token, order);
            return token;
        }

        public void Register<TSignal>(Action<TSignal> action, object token, int order = int.MaxValue) where TSignal : class
        {
            Action<object> wrapperAction = args => action((TSignal)args);
            
            Register(typeof(TSignal), wrapperAction, action, order, token);
        }
        
        public void Register<TSignal>(Action action, object token, int order = int.MaxValue) where TSignal : class
        {
            Action<object> wrapperAction = _ => action();
            
            Register(typeof(TSignal), wrapperAction, action, order, token);
        }

        private void Register(Type signalType, Action<object> action, object token, int order = int.MaxValue, object extraToken = null)
        {
            var subscription = new Subscription(action, token, order, extraToken);

            if (_subscriptions.TryGetValue(signalType, out var subscriptions))
            {
                if (IsAlreadySend(signalType))
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

        protected virtual void InsertSubscription(List<Subscription> subscriptions, Subscription subscription)
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
                TryThrowException($"Dont find EventTyp. Signal Type : {signalType}");
                return;
            }

            var subscriptions = _subscriptions[signalType];

            var subscriptionToRemove = subscriptions.FindIndex(subscription => subscription.token.GetHashCode() == token.GetHashCode());

            if (subscriptionToRemove == -1)
            {
                TryThrowException($"Dont find Subscription. Signal Type : {signalType}");
                return;
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
        }

        public void Deregister(object token)
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

            if (removeCount == 0)
            {
                TryThrowException($"Dont find Subscription. Token : {token}");
            }
        }
        
        public void ClearSubscriptions()
        {
            _subscriptions.Clear();
        }

        public void Send<TSignal>(TSignal signal) where TSignal : class
        {
            Send(typeof(TSignal), signal);
        }

        public void Send<TSignal>() where TSignal : class
        {
            Send(typeof(TSignal),null);
        }

        public void Broadcast<TSignal>(TSignal signal) where TSignal : class
        {
            Broadcast(typeof(TSignal), signal);
        }
        
        public void Broadcast<TSignal>() where TSignal : class
        {
            Broadcast(typeof(TSignal), null);
        }
        
        public void Broadcast(Type signalType, object signal)
        {
            Send(signalType, signal);
            
            BroadcastInternal(signalType, signal);
        }

        private void BroadcastInternal(Type signalType, object signal)
        {
            _isAlreadyBroadcast = true;
            
            foreach (var signalBus in _children)
            {
                signalBus.Broadcast(signalType, signal);
            }

            _isAlreadyBroadcast = false;

            if (_isDirtyChildren)
            {
                RefreshChildren();
                
                _isDirtyChildren = false;
            }
        }
        
        public void Send(Type signalType, object signal)
        {
            if (!_subscriptions.TryGetValue(signalType, out var subscriptions))
            {
                TryThrowException($"Dont find Subscription. Signal Type : {signalType}");
                return;
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
                InsertSubscription(subscriptions, subscription);
            }
            
            _subscriptionsToRegister.Clear();
        }
        
        private void RefreshChildren()
        {
            foreach (var signalBus in _childrenToRemoved)
            {
                _children.Remove(signalBus);
            }
            
            _childrenToRemoved.Clear();

            foreach (var signalBus in _childrenToAdded)
            {
                _children.Add(signalBus);
            }
            
            _childrenToAdded.Clear();
        }
        
        protected virtual void TryThrowException(string message)
        {
            if (configuration.IsThrowException)
            {
                throw new Exception(message);
            }
        }

        protected virtual object GetDefaultToken()
        {
            return Guid.NewGuid();
        }
    }
}
