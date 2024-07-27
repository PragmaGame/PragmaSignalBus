using System;

namespace Pragma.SignalBus
{
    public interface ISignalRegistrar
    {
        public void Deregister<TSignal>(Action action) where TSignal : class;
        public void Deregister<TSignal>(Action<TSignal> action) where TSignal : class;
        public void Register<TSignal>(Action<TSignal> action, int order = int.MaxValue, object extraToken = null)
            where TSignal : class;
        public void Register<TSignal>(Action action, int order = int.MaxValue, object extraToken = null)
            where TSignal : class;
        public void ClearSubscriptions();
    }
}