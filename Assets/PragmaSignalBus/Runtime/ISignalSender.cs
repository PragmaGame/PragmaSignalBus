using System;

namespace Pragma.SignalBus
{
    public interface ISignalSender
    {
        public void Send<TSignal>(TSignal signal) where TSignal : class;
        public void Send<TSignal>() where TSignal : class;
        public TSignal SendWithCreateInstance<TSignal>() where TSignal : class;
        public void SendWithBroadcast<TSignal>(TSignal signal) where TSignal : class;
        public void SendWithBroadcast<TSignal>() where TSignal : class;
        public void SendWithBroadcast(Type signalType, object signal);
    }
}