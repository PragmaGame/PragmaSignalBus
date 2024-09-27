using System;

namespace Pragma.SignalBus
{
    public interface ISignalSender
    {
        public void Send<TSignal>(TSignal signal) where TSignal : class;
        public void Send<TSignal>() where TSignal : class;
        public void Send(Type signalType, object signal);
        public void Broadcast<TSignal>(TSignal signal) where TSignal : class;
        public void Broadcast<TSignal>() where TSignal : class;
        public void Broadcast(Type signalType, object signal);
    }
}