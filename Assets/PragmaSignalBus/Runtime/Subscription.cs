using System;

namespace Pragma.SignalBus
{
    public class Subscription
    {
        public readonly Action<object> action;
        public readonly int order;
        public readonly object token;
        public readonly object extraToken;

        public Subscription(Action<object> action, object token, int order = int.MaxValue, object extraToken = null)
        {
            this.action = action;
            this.token = token;
            this.order = order;
            this.extraToken = extraToken;
        }
    }
}