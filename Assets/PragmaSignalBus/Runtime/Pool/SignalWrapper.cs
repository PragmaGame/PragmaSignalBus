namespace Pragma.SignalBus
{
    public readonly struct SignalWrapper<T> where T : class
    {
        private readonly ISignalSender _sender;
        private readonly T _signal;
        
        public T Signal => _signal;

        internal SignalWrapper(ISignalSender sender, T signal)
        {
            _sender = sender;
            _signal = signal;
        }

        public void Send()
        {
            _sender.Send(_signal);
            SignalPool.Release<T>(_signal);
        }
    }
}