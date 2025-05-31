using System;

namespace Pragma.SignalBus
{
    public interface ISignalRegistrar
    {
        public object Register<TSignal>(Action<TSignal> action, Type owner = null, Type[] beforeOrder = null, Type[] afterOrder = null, bool isLazySorted = true)
            where TSignal : class;
        public object Register<TSignal>(Action action, Type owner = null, Type[] beforeOrder = null, Type[] afterOrder = null, bool isLazySorted = true)
            where TSignal : class;
        public void Register<TSignal>(Action<TSignal> action, object token, Type owner = null, Type[] beforeOrder = null, Type[] afterOrder = null, bool isLazySorted = true)
            where TSignal : class;
        public void Register<TSignal>(Action action, object token, Type owner = null, Type[] beforeOrder = null, Type[] afterOrder = null, bool isLazySorted = true)
            where TSignal : class;
        public bool Deregister<TSignal>(Action action) where TSignal : class;
        public bool Deregister<TSignal>(Action<TSignal> action) where TSignal : class;
        public int Deregister(object token);
    }
}