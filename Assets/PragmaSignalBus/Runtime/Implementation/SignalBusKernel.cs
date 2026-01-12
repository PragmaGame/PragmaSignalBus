using System;
using System.Collections.Generic;
using UnityEngine.Scripting;

namespace PragmaSignalBus
{
    [Preserve]
    internal abstract class SignalBusKernel<TSignalHandler>
    {
        protected readonly Dictionary<Type, List<SignalSubscription<TSignalHandler>>> subscriptions;
        protected readonly SignalBusConfiguration configuration;
        protected readonly Stack<List<SignalSubscription<TSignalHandler>>> poolBuffers;

        protected SignalBusKernel(SignalBusConfiguration configuration)
        {
            this.configuration = configuration ?? new SignalBusConfiguration();

            subscriptions = new Dictionary<Type, List<SignalSubscription<TSignalHandler>>>();
            poolBuffers = new Stack<List<SignalSubscription<TSignalHandler>>>();
        }
        
        protected List<SignalSubscription<TSignalHandler>> RentBuffer(List<SignalSubscription<TSignalHandler>> source)
        {
            List<SignalSubscription<TSignalHandler>> buffer;

            var sourceCount = source.Count;
            
            if (poolBuffers.Count > 0)
            {
                buffer = poolBuffers.Pop();
            }
            else
            {
                buffer = new List<SignalSubscription<TSignalHandler>>(sourceCount);
            }

            for (var i = 0; i < sourceCount; i++)
            {
                buffer.Add(source[i]);
            }

            return buffer;
        }

        protected void ReleaseBuffer(List<SignalSubscription<TSignalHandler>> value)
        {
            value.Clear();
            poolBuffers.Push(value);
        }

        protected virtual object GetToken()
        {
            return Guid.NewGuid();
        }

        protected void Register(Type signalType, TSignalHandler signal, object token, SortOptions sortOptions = null, object extraToken = null)
        {
            var subscription = new SignalSubscription<TSignalHandler>(signal, token, extraToken, sortOptions);

            if (this.subscriptions.TryGetValue(signalType, out var subscriptions))
            {
                subscriptions.Add(subscription);

                if (sortOptions == null)
                {
                    return;
                }

                SubscriptionTopologicalSorter<TSignalHandler>.Sort(subscriptions, false);
            }
            else
            {
                this.subscriptions.Add(signalType, new List<SignalSubscription<TSignalHandler>>() { subscription });
            }
        }
        
        protected void Deregister(Type signalType, object handler)
        {
            if (!this.subscriptions.TryGetValue(signalType, out var subscriptions))
            {
                TryThrowException($"Don't find SignalType. SignalType : {signalType}");

                return;
            }

            var subscriptionToRemove = subscriptions.FindIndex(subscription => subscription.Token.GetHashCode() == handler.GetHashCode());

            if (subscriptionToRemove == -1)
            {
                TryThrowException($"Don't find Subscription. SignalType : {signalType}");
                return;
            }

            subscriptions.RemoveAt(subscriptionToRemove);
        }

        public void Deregister(object token)
        {
            var tokenHash = token.GetHashCode();
            var removeCount = 0;

            foreach (var subscriptions in subscriptions.Values)
            {
                removeCount += subscriptions.RemoveAll(subscription => subscription.ExtraToken.GetHashCode() == tokenHash);
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