using System;
using System.Collections.Generic;
using UnityEngine;
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
            return configuration.TokenGenerator.Invoke();
        }

        protected void Register(Type signalType, TSignalHandler handler, Delegate sourceDelegate, SortOptions sortOptions = null, object token = null)
        {
            var subscription = new SignalSubscription<TSignalHandler>(handler, sourceDelegate, token, sortOptions);

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
        
        protected void Deregister(Type signalType, Delegate sourceDelegate)
        {
            if (!this.subscriptions.TryGetValue(signalType, out var invocationList))
            {
                configuration.Logger?.Invoke(LogType.Log, $"Don't find SignalType. SignalType : {signalType}");
                return;
            }

            var subscriptionToRemove = invocationList.FindIndex(subscription => subscription.SourceDelegate == sourceDelegate);

            if (subscriptionToRemove == -1)
            {
                configuration.Logger?.Invoke(LogType.Log, $"Don't find Subscription. SignalType : {signalType}");
                return;
            }

            invocationList.RemoveAt(subscriptionToRemove);
        }

        public int Deregister(object token)
        {
            var tokenHash = token.GetHashCode();
            var removeCount = 0;

            foreach (var invocationList in subscriptions.Values)
            {
                removeCount += invocationList.RemoveAll(subscription => subscription.Token.GetHashCode() == tokenHash);
            }

            if (removeCount == 0)
            {
                configuration.Logger?.Invoke(LogType.Log, $"Don't find Subscription. Token : {token}");
            }
            
            return removeCount;
        }
        
        public void ClearSubscriptions()
        {
            subscriptions.Clear();
        }
    }
}