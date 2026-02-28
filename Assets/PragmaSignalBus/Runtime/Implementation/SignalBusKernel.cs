using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;

namespace PragmaSignalBus
{
    [Preserve]
    internal abstract class SignalBusKernel
    {
        protected readonly Dictionary<Type, List<SignalSubscription>> subscriptionsMap;
        protected readonly SignalBusConfiguration configuration;
        protected readonly Stack<List<SignalSubscription>> poolBuffers;

        protected SignalBusKernel(SignalBusConfiguration configuration)
        {
            this.configuration = configuration ?? new SignalBusConfiguration();

            subscriptionsMap = new Dictionary<Type, List<SignalSubscription>>();
            poolBuffers = new Stack<List<SignalSubscription>>();
        }

        protected List<SignalSubscription> RentBuffer(List<SignalSubscription> source)
        {
            List<SignalSubscription> buffer;

            var sourceCount = source.Count;

            if (poolBuffers.Count > 0)
            {
                buffer = poolBuffers.Pop();
            }
            else
            {
                buffer = new List<SignalSubscription>(sourceCount);
            }

            for (var i = 0; i < sourceCount; i++)
            {
                buffer.Add(source[i]);
            }

            return buffer;
        }

        protected void ReleaseBuffer(List<SignalSubscription> value)
        {
            value.Clear();
            poolBuffers.Push(value);
        }

        protected virtual object GetToken()
        {
            return configuration.TokenGenerator.Invoke();
        }

        protected void Register(Type signalType, Delegate sourceDelegate, SortOptions sortOptions = null, object token = null)
        {
            var subscription = new SignalSubscription(sourceDelegate, token, sortOptions);

            if (subscriptionsMap.TryGetValue(signalType, out var subscriptions))
            {
                subscriptions.Add(subscription);

                if (sortOptions == null)
                {
                    return;
                }

                SubscriptionTopologicalSorter.Sort(subscriptions, false);
            }
            else
            {
                subscriptionsMap.Add(signalType, new List<SignalSubscription>() { subscription });
            }
        }

        protected void Deregister(Type signalType, Delegate sourceDelegate)
        {
            if(!TryGetSubscriptions(signalType, out var subscriptions))
            {
                return;
            }

            var subscriptionToRemove = subscriptions.FindIndex(subscription => subscription.SourceDelegate == sourceDelegate);

            if (subscriptionToRemove == -1)
            {
                configuration.Logger?.Invoke(LogType.Log, $"Don't find Subscription. SignalType : {signalType}");
                return;
            }

            subscriptions.RemoveAt(subscriptionToRemove);
        }

        public int Deregister(object token)
        {
            var tokenHash = token.GetHashCode();
            var removeCount = 0;

            foreach (var subscriptions in subscriptionsMap.Values)
            {
                removeCount += subscriptions.RemoveAll(subscription => subscription.Token.GetHashCode() == tokenHash);
            }

            if (removeCount == 0)
            {
                configuration.Logger?.Invoke(LogType.Log, $"Don't find Subscription. Token : {token}");
            }

            return removeCount;
        }
        
        protected bool TryGetSubscriptions(Type signalType, out List<SignalSubscription> subscriptions)
        {
            if (subscriptionsMap.TryGetValue(signalType, out subscriptions))
            {
                return true;
            }
            
            configuration.Logger?.Invoke(LogType.Log, $"Don't find Subscription. Signal Type : {signalType}");
            return false;

        }
        
        public void ClearSubscriptions()
        {
            subscriptionsMap.Clear();
        }
    }
}