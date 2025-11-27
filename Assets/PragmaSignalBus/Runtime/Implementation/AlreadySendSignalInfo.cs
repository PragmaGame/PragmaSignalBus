using System.Collections.Generic;

namespace PragmaSignalBus
{
    internal class AlreadySendSignalInfo<TSignal>
    {
        public List<SignalSubscription<TSignal>> SubscriptionsToRegister { get; }
        public List<SignalSubscription<TSignal>> SubscriptionsToDeregister { get; }

        public bool IsDirtySubscriptions => SubscriptionsToRegister.Count > 0 || SubscriptionsToDeregister.Count > 0;
        
        public AlreadySendSignalInfo()
        {
            SubscriptionsToRegister = new List<SignalSubscription<TSignal>>();
            SubscriptionsToDeregister = new List<SignalSubscription<TSignal>>();
        }
    }
}