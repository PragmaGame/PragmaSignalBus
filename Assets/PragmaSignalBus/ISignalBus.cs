using System;

namespace Import.PragmaSignalBus
{
    public interface ISignalBus : ISignalRegistrar, ISignalInvoker
    {
        public void AddChildren(ISignalBus signalBus);
        public void RemoveChildren(ISignalBus signalBus);
    }
}