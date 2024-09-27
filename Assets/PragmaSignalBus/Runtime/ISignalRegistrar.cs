using System;

namespace Pragma.SignalBus
{
    public interface ISignalRegistrar
    {
        public object Register<TSignal>(Action<TSignal> action, int order = int.MaxValue)
            where TSignal : class;
        public object Register<TSignal>(Action action, int order = int.MaxValue)
            where TSignal : class;
        public void Register<TSignal>(Action<TSignal> action, object token, int order = int.MaxValue)
            where TSignal : class;
        public void Register<TSignal>(Action action, object token, int order = int.MaxValue)
            where TSignal : class;
        public void Deregister<TSignal>(Action action) where TSignal : class;
        public void Deregister<TSignal>(Action<TSignal> action) where TSignal : class;
        public void Deregister(object token);
    }
}