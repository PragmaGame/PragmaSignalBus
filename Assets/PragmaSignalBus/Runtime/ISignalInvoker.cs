using System;

namespace Pragma.SignalBus
{
    public interface ISignalInvoker
    {
        public void Invoke<TSignal>(TSignal signal) where TSignal : class;
        public void Invoke<TSignal>() where TSignal : class;
        public TSignal InvokeWithCreateInstance<TSignal>() where TSignal : class;
        public void InvokeWithBroadcast<TSignal>(TSignal signal) where TSignal : class;
        public void InvokeWithBroadcast<TSignal>() where TSignal : class;
        public void InvokeWithBroadcast(Type signalType, object signal);
    }
}