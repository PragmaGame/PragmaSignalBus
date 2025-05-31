using System;

namespace Pragma.SignalBus
{
    public class Subscription
    {
        public readonly Action<object> action;
        public readonly object token;
        public readonly object extraToken;
        public readonly Type owner;
        public readonly Type[] beforeOrder;
        public readonly Type[] afterOrder;

        public Subscription(Action<object> action, object token, object extraToken = null, Type owner = null, Type[] beforeOrder = null, Type[] afterOrder = null)
        {
            this.action = action;
            this.token = token;
            this.extraToken = extraToken;
            this.owner = owner;
            this.beforeOrder = beforeOrder;
            this.afterOrder = afterOrder;
        }
    }
}