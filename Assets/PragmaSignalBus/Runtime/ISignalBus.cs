namespace Pragma.SignalBus
{
    public interface ISignalBus : ISignalRegistrar, ISignalSender
    {
        public void ClearSubscriptions();
        public void SortSubscriptions();
    }
}