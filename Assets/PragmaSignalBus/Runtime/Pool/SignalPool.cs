using System;
using System.Collections.Generic;

namespace Pragma.SignalBus
{
    internal static class SignalPool
    {
        private static readonly Dictionary<Type, Stack<object>> _pools = new();

        public static TSignal Rent<TSignal>() where TSignal : class
        {
            var type = typeof(TSignal);
            
            if (_pools.TryGetValue(type, out var pool))
            {
                if (pool.TryPop(out var item))
                {
                    return item as TSignal;
                }
            }
            else
            {
                _pools[type] = new Stack<object>();
            }

            return Activator.CreateInstance(type) as TSignal;
        }

        public static void Release<TSignal>(object value) where TSignal : class
        {
            _pools[typeof(TSignal)].Push(value);
        }
    }
}