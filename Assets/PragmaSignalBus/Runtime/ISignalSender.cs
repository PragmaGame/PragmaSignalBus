using System.Threading;
using Cysharp.Threading.Tasks;

namespace PragmaSignalBus
{
    public interface ISignalSender
    {
        void Send<TSignal>(TSignal signal);
        void Send<TSignal>();
        void SendUnsafe<TSignal>();
        void SendAbstract(object signal);
        UniTask SendAsync<TSignal>(TSignal signal, CancellationToken token = default, AsyncSendInvocationType asyncSendInvocationType = AsyncSendInvocationType.Sequence);
        UniTask SendAsync<TSignal>(CancellationToken token = default, AsyncSendInvocationType asyncSendInvocationType = AsyncSendInvocationType.Sequence);
        UniTask SendAbstractAsync(object signal, CancellationToken token = default, AsyncSendInvocationType asyncSendInvocationType = AsyncSendInvocationType.Sequence);
    }
}