using UnityEngine.Scripting;

namespace PragmaSignalBus
{
    [Preserve]
    public class SignalSubscription<TAction> : ISignalSubscription
    {
        public TAction Action { get; }
        public object Token { get; }
        public object ExtraToken { get; }
        public SortOptions SortOptions { get; }

        [RequiredMember]
        public SignalSubscription(TAction action,
                                 object token,
                                 object extraToken = null,
                                 SortOptions sortOptions = null)
        {
            Action = action;
            Token = token;
            ExtraToken = extraToken;
            SortOptions = sortOptions;
        }
    }
}
