namespace PragmaSignalBus
{
    public interface ISignalBus : ISignalSender, ISignalRegistrar
    {
        void ClearSubscriptions();
    }
}