using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace PragmaSignalBus
{
    public interface ISignalRegistrar
    {
        object Register<TSignal>(Action<TSignal> signal, SortOptions sortOptions = null);
        object Register<TSignal>(Action signal, SortOptions sortOptions = null);
        void Register<TSignal>(Action<TSignal> signal, object token, SortOptions sortOptions = null);
        void Register<TSignal>(Action signal, object token, SortOptions sortOptions = null);
        void Deregister<TSignal>(Action<TSignal> signal);
        void Deregister<TSignal>(Action signal);
        object Register<TSignal>(Func<TSignal, CancellationToken, UniTask> signal, SortOptions sortOptions = null);
        object Register<TSignal>(Func<CancellationToken, UniTask> signal, SortOptions sortOptions = null);
        void Register<TSignal>(Func<TSignal, CancellationToken, UniTask> signal, object token, SortOptions sortOptions = null);
        void Register<TSignal>(Func<CancellationToken, UniTask> signal, object token, SortOptions sortOptions = null);
        void Deregister<TSignal>(Func<TSignal, CancellationToken, UniTask> signal);
        void Deregister<TSignal>(Func<CancellationToken, UniTask> signal);
        int Deregister(object token);
    }
}