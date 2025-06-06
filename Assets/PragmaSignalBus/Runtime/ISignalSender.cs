using System;

namespace Pragma.SignalBus
{
    public interface ISignalSender
    {
        public void Send<TSignal>(TSignal signal) where TSignal : class;
        public void Send<TSignal>() where TSignal : class;
        public void Send(Type signalType, object signal);
        public void SendFromPool<TSignal>(Action<TSignal> setter) where TSignal : class;
    }
}