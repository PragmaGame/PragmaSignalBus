using System;

namespace Pragma.SignalBus
{
    public interface ISignalBusBroadcaster : ISignalBus
    {
        public void AddChildren(ISignalBusBroadcaster signalBus);

        public void RemoveChildren(ISignalBusBroadcaster signalBus);
        public void Broadcast<TSignal>(TSignal signal) where TSignal : class;
        public void Broadcast<TSignal>() where TSignal : class;
        public void Broadcast(Type signalType, object signal);
    }
}