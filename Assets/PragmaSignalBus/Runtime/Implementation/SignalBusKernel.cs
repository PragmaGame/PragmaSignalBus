using System;
using System.Collections.Generic;
using UnityEngine.Scripting;

namespace PragmaSignalBus
{
    [Preserve]
    internal abstract class SignalBusKernel<TSignal>
    {
        protected readonly Dictionary<Type, List<SignalSubscription<TSignal>>> subscriptions;
        protected readonly SignalBusConfiguration configuration;

        protected SignalBusKernel(SignalBusConfiguration configuration)
        {
            this.configuration = configuration ?? new SignalBusConfiguration();

            subscriptions = new Dictionary<Type, List<SignalSubscription<TSignal>>>();
        }
        
        protected abstract bool IsAlreadySend(Type type, out AlreadySendSignalInfo<TSignal> signalInfo);
        protected abstract bool IsAnyAlreadySend();

        protected virtual object GetToken()
        {
            return Guid.NewGuid();
        }

        protected void Register(Type signalType, TSignal signal, object token, SortOptions sortOptions = null, object extraToken = null)
        {
            var subscription = new SignalSubscription<TSignal>(signal, token, extraToken, sortOptions);

            if (this.subscriptions.TryGetValue(signalType, out var subscriptions))
            {
                if (IsAlreadySend(signalType, out var eventInfo))
                {
                    OnAddedSubscriptionsToRegister(eventInfo, subscription);
                }
                else
                {
                    subscriptions.Add(subscription);

                    if (sortOptions == null)
                    {
                        return;
                    }

                    SubscriptionTopologicalSorter<TSignal>.Sort(subscriptions, false);
                }
            }
            else
            {
                this.subscriptions.Add(signalType, new List<SignalSubscription<TSignal>>() { subscription });
            }
        }
        
        protected void Deregister(Type signalType, object @event)
        {
            if (!this.subscriptions.TryGetValue(signalType, out var subscriptions))
            {
                TryThrowException($"Don't find SignalType. SignalType : {signalType}");

                return;
            }

            var subscriptionToRemove = subscriptions.FindIndex(subscription => subscription.Token.GetHashCode() == @event.GetHashCode());

            if (subscriptionToRemove == -1)
            {
                TryThrowException($"Don't find Subscription. SignalType : {signalType}");
                return;
            }

            if (IsAlreadySend(signalType, out var eventInfo))
            {
                OnAddedSubscriptionsToDeregister(eventInfo, subscriptions[subscriptionToRemove]);
            }
            else
            {
                subscriptions.RemoveAt(subscriptionToRemove);
            }
        }
        
        protected virtual void OnAddedSubscriptionsToDeregister(AlreadySendSignalInfo<TSignal> signalInfo, SignalSubscription<TSignal> subscription)
        {
            signalInfo.SubscriptionsToDeregister.Add(subscription);
        }

        protected virtual void OnAddedSubscriptionsToRegister(AlreadySendSignalInfo<TSignal> signalInfo, SignalSubscription<TSignal> subscription)
        {
            signalInfo.SubscriptionsToRegister.Add(subscription);
        }

        protected void RefreshSubscriptions(Type type, AlreadySendSignalInfo<TSignal> signalInfo)
        {
            var eventSubscriptions = subscriptions[type];
            var isNeedSort = false;

            foreach (var subscription in signalInfo.SubscriptionsToDeregister)
            {
                eventSubscriptions.Remove(subscription);
            }
            
            signalInfo.SubscriptionsToDeregister.Clear();

            foreach (var subscription in signalInfo.SubscriptionsToRegister)
            {
                eventSubscriptions.Add(subscription);
                
                if (subscription.SortOptions != null)
                {
                    isNeedSort = true;
                }
            }

            if (isNeedSort)
            {
                SubscriptionTopologicalSorter<TSignal>.Sort(eventSubscriptions, false);
            }
            
            signalInfo.SubscriptionsToRegister.Clear();
        }
        
        public void Deregister(object token)
        {
            var tokenHash = token.GetHashCode();
            var removeCount = 0;

            if (IsAnyAlreadySend())
            {
                foreach (var key in subscriptions.Keys)
                {
                    var subscriptions = this.subscriptions[key];
                    var isCurrentPublish = IsAlreadySend(key, out var eventInfo);

                    for (var i = 0; i < subscriptions.Count; i++)
                    {
                        if (subscriptions[i].ExtraToken.GetHashCode() != tokenHash)
                        {
                            continue;
                        }

                        removeCount++;

                        if (isCurrentPublish)
                        {
                            OnAddedSubscriptionsToDeregister(eventInfo, subscriptions[i]);
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
                foreach (var subscriptions in subscriptions.Values)
                {
                    removeCount += subscriptions.RemoveAll(subscription => subscription.ExtraToken.GetHashCode() == tokenHash);
                }
            }

            if (removeCount == 0)
            {
                TryThrowException($"Don't find Subscription. Token : {token}");
            }
        }
        
        protected void TryThrowException(string message)
        {
            if (configuration.IsThrowException)
            {
                throw new Exception(message);
            }
        }
        
        public void ClearSubscriptions()
        {
            subscriptions.Clear();
        }
    }
}