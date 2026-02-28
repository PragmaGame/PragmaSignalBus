using System;
using System.Collections.Generic;
using System.Linq;

namespace PragmaSignalBus
{
    internal static class SubscriptionTopologicalSorter
    {
        private static readonly List<SignalSubscription> _sortedSubscriptionsCache = new();
        private static readonly List<SignalSubscription> _unsortedSubscriptionsCache = new();
        private static readonly List<SignalSubscription> _sortedResultCache = new();
        private static readonly Queue<SignalSubscription> _queueCache = new();

        private static readonly Stack<HashSet<SignalSubscription>> _hashSetPool = new();
        private static readonly Dictionary<SignalSubscription, HashSet<SignalSubscription>> _dependenciesCache = new();
        private static readonly Dictionary<SignalSubscription, HashSet<SignalSubscription>> _reverseDependenciesCache = new();

        public static void Sort(List<SignalSubscription> subscriptions, bool checkSortOptions = true)
        {
            try
            {
                if (checkSortOptions && subscriptions.All(s => s.SortOptions == null))
                {
                    return;
                }

                foreach (var subscription in subscriptions)
                {
                    if (subscription.SortOptions == null)
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
                    throw new InvalidOperationException("Outer loop in dependent subscriptions. Cannot be ordered.");
                }

                subscriptions.Clear();
                subscriptions.AddRange(_sortedResultCache);
                subscriptions.AddRange(_unsortedSubscriptionsCache);
            }
            finally
            {
                ClearCollections();
            }
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

        private static void ProcessAfterOrder(SignalSubscription current)
        {
            if (current.SortOptions.AfterOrder == null)
            {
                return;
            }

            foreach (var afterType in current.SortOptions.AfterOrder)
            {
                if (afterType == null)
                {
                    continue;
                }

                foreach (var pred in _sortedSubscriptionsCache)
                {
                    if (pred.SortOptions.SortedKey == afterType && pred != current)
                    {
                        _dependenciesCache[current].Add(pred);
                        _reverseDependenciesCache[pred].Add(current);
                    }
                }
            }
        }

        private static void ProcessBeforeOrder(SignalSubscription current)
        {
            if (current.SortOptions.BeforeOrder == null)
            {
                return;
            }

            foreach (var beforeType in current.SortOptions.BeforeOrder)
            {
                if (beforeType == null)
                {
                    continue;
                }

                foreach (var subscription in _sortedSubscriptionsCache)
                {
                    if (subscription.SortOptions.SortedKey == beforeType && subscription != current)
                    {
                        _dependenciesCache[subscription].Add(current);
                        _reverseDependenciesCache[current].Add(subscription);
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

        private static HashSet<SignalSubscription> GetHashSetFromPool()
        {
            return _hashSetPool.Count > 0 ? _hashSetPool.Pop() : new HashSet<SignalSubscription>();
        }
    }
}