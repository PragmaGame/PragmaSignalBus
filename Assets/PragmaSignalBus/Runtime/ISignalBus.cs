namespace Pragma.SignalBus
{
    public interface ISignalBus : ISignalRegistrar, ISignalSender
    {
        public void AddChildren(ISignalBus signalBus);
        public void RemoveChildren(ISignalBus signalBus);
        public void ClearSubscriptions();
    }
}