using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace PragmaSignalBus
{
    public class SignalBus : ISignalBus
    {
        private SyncSignalBusKernel _syncKernel;
        private AsyncSignalBusKernel _asyncKernel;
        
        public SignalBus(SignalBusConfiguration configuration = null)
        {
            _syncKernel = new SyncSignalBusKernel(configuration);
            _asyncKernel = new AsyncSignalBusKernel(configuration);
        }
        
        public void Send<TSignal>(TSignal signal)
        {
            _syncKernel.Send<TSignal>(signal);
        }

        public void Send<TSignal>()
        {
            _syncKernel.Send<TSignal>();
        }
        
        public void SendUnsafe<TSignal>()
        {
            _syncKernel.SendUnsafe<TSignal>(typeof(TSignal), default);
        }

        public UniTask SendAsync<TSignal>(TSignal signal, CancellationToken token = default,
            AsyncSendInvocationType asyncSendInvocationType = AsyncSendInvocationType.Sequence)
        {
            return _asyncKernel.Send<TSignal>(signal, token, asyncSendInvocationType);
        }

        public UniTask SendAsync<TSignal>(CancellationToken token = default,
            AsyncSendInvocationType asyncSendInvocationType = AsyncSendInvocationType.Sequence)
        {
            return _asyncKernel.Send<TSignal>(token, asyncSendInvocationType);
        }

        public object Register<TSignal>(Action<TSignal> signal, SortOptions sortOptions = null)
        {
            return _syncKernel.Register<TSignal>(signal, sortOptions);
        }

        public object Register<TSignal>(Action signal, SortOptions sortOptions = null)
        {
            return _syncKernel.Register<TSignal>(signal, sortOptions);
        }

        public void Register<TSignal>(Action<TSignal> signal, object token, SortOptions sortOptions = null)
        {
            _syncKernel.Register<TSignal>(signal, token, sortOptions);
        }

        public void Register<TSignal>(Action signal, object token, SortOptions sortOptions = null)
        {
            _syncKernel.Register<TSignal>(signal, token, sortOptions);
        }

        public void Deregister<TSignal>(Action<TSignal> signal)
        {
            _syncKernel.Deregister<TSignal>(signal);
        }

        public void Deregister<TSignal>(Action signal)
        {
            _syncKernel.Deregister<TSignal>(signal);
        }

        public object Register<TSignal>(Func<TSignal, CancellationToken, UniTask> signal, SortOptions sortOptions = null)
        {
            return _asyncKernel.Register<TSignal>(signal, sortOptions);
        }

        public object Register<TSignal>(Func<CancellationToken, UniTask> signal, SortOptions sortOptions = null)
        {
            return _asyncKernel.Register<TSignal>(signal, sortOptions);
        }

        public void Register<TSignal>(Func<TSignal, CancellationToken, UniTask> signal, object token, SortOptions sortOptions = null)
        {
            _asyncKernel.Register<TSignal>(signal, token, sortOptions);
        }

        public void Register<TSignal>(Func<CancellationToken, UniTask> signal, object token, SortOptions sortOptions = null)
        {
            _asyncKernel.Register<TSignal>(signal, token, sortOptions);
        }

        public void Deregister<TSignal>(Func<TSignal, CancellationToken, UniTask> signal)
        {
            _asyncKernel.Deregister<TSignal>(signal);
        }

        public void Deregister<TSignal>(Func<CancellationToken, UniTask> signal)
        {
            _asyncKernel.Deregister<TSignal>(signal);
        }

        public void Deregister(object token)
        {
            _syncKernel.Deregister(token);
            _asyncKernel.Deregister(token);
        }

        public void ClearSubscriptions()
        {
            _syncKernel.ClearSubscriptions();
            _asyncKernel.ClearSubscriptions();
        }
    }
}