using UnityEngine.Scripting;

namespace PragmaSignalBus
{
    [Preserve]
    public class SignalSubscription<TSignalHandler> : ISignalSubscription
    {
        public TSignalHandler Handler { get; }
        public object Token { get; }
        public object ExtraToken { get; }
        public SortOptions SortOptions { get; }

        [RequiredMember]
        public SignalSubscription(TSignalHandler handler,
                                 object token,
                                 object extraToken = null,
                                 SortOptions sortOptions = null)
        {
            Handler = handler;
            Token = token;
            ExtraToken = extraToken;
            SortOptions = sortOptions;
        }
    }
}
