using System;
using System.Collections.Generic;
using System.Linq;

namespace Pragma.SignalBus
{
    internal static class SubscriptionTopologicalSorter
    {
        private static readonly List<Subscription> _sortedSubscriptionsCache = new();
        private static readonly List<Subscription> _unsortedSubscriptionsCache = new();
        private static readonly List<Subscription> _sortedResultCache = new();
        private static readonly Queue<Subscription> _queueCache = new();

        private static readonly Stack<HashSet<Subscription>> _hashSetPool = new();
        private static readonly Dictionary<Subscription, HashSet<Subscription>> _dependenciesCache = new();
        private static readonly Dictionary<Subscription, HashSet<Subscription>> _reverseDependenciesCache = new();

        public static void Sort(List<Subscription> subscriptions)
        {
            if (subscriptions.All(s => s.owner == null))
            {
                return;
            }

            foreach (var subscription in subscriptions)
            {
                if (subscription.owner == null)
                {
                    _unsortedSubscriptionsCache.Add(subscription);
                }
                else
                {
                    _sortedSubscriptionsCache.Add(subscription);
                }
            }

            foreach (var sub in _sortedSubscriptionsCache)
            {
                _dependenciesCache[sub] = GetHashSetFromPool();
                _reverseDependenciesCache[sub] = GetHashSetFromPool();
            }

            foreach (var current in _sortedSubscriptionsCache)
            {
                ProcessAfterOrder(current);
                ProcessBeforeOrder(current);
            }

            TopologicalSort();

            if (_sortedResultCache.Count != _sortedSubscriptionsCache.Count)
            {
                ClearCollections();
                throw new InvalidOperationException("Outer loop in dependent subscriptions. Cannot be ordered.");
            }

            subscriptions.Clear();
            subscriptions.AddRange(_sortedResultCache);
            subscriptions.AddRange(_unsortedSubscriptionsCache);

            ClearCollections();
        }

        private static void ClearCollections()
        {
            foreach (var set in _dependenciesCache.Values)
            {
                set.Clear();
                _hashSetPool.Push(set);
            }

            foreach (var set in _reverseDependenciesCache.Values)
            {
                set.Clear();
                _hashSetPool.Push(set);
            }

            _sortedSubscriptionsCache.Clear();
            _unsortedSubscriptionsCache.Clear();
            _sortedResultCache.Clear();
            _queueCache.Clear();
            _dependenciesCache.Clear();
            _reverseDependenciesCache.Clear();
        }

        private static void ProcessAfterOrder(Subscription current)
        {
            if (current.afterOrder == null)
            {
                return;
            }

            foreach (var afterType in current.afterOrder)
            {
                if (afterType == null)
                {
                    continue;
                }

                foreach (var pred in _sortedSubscriptionsCache)
                {
                    if (pred.owner == afterType && pred != current)
                    {
                        _dependenciesCache[current].Add(pred);
                        _reverseDependenciesCache[pred].Add(current);
                    }
                }
            }
        }

        private static void ProcessBeforeOrder(Subscription current)
        {
            if (current.beforeOrder == null)
            {
                return;
            }

            foreach (var beforeType in current.beforeOrder)
            {
                if (beforeType == null)
                {
                    continue;
                }

                foreach (var succ in _sortedSubscriptionsCache)
                {
                    if (succ.owner == beforeType && succ != current)
                    {
                        _dependenciesCache[succ].Add(current);
                        _reverseDependenciesCache[current].Add(succ);
                    }
                }
            }
        }

        private static void TopologicalSort()
        {
            foreach (var sub in _sortedSubscriptionsCache)
            {
                if (_dependenciesCache[sub].Count == 0)
                {
                    _queueCache.Enqueue(sub);
                }
            }

            while (_queueCache.Count > 0)
            {
                var current = _queueCache.Dequeue();
                _sortedResultCache.Add(current);

                foreach (var dependent in _reverseDependenciesCache[current])
                {
                    _dependenciesCache[dependent].Remove(current);
                    if (_dependenciesCache[dependent].Count == 0)
                    {
                        _queueCache.Enqueue(dependent);
                    }
                }
            }
        }

        private static HashSet<Subscription> GetHashSetFromPool()
        {
            return _hashSetPool.Count > 0 ? _hashSetPool.Pop() : new HashSet<Subscription>();
        }
    }
}