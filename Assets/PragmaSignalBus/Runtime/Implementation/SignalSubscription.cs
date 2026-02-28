using System;
using UnityEngine.Scripting;

namespace PragmaSignalBus
{
    [Preserve]
    public class SignalSubscription<TSignalHandler> : ISignalSubscription
    {
        public TSignalHandler Handler { get; }
        public Delegate SourceDelegate { get; }
        public object Token { get; }
        public SortOptions SortOptions { get; }

        [RequiredMember]
        public SignalSubscription(TSignalHandler handler,
                                  Delegate sourceDelegate,
                                  object token = null,
                                  SortOptions sortOptions = null)
        {
            Handler = handler;
            SourceDelegate = sourceDelegate;
            Token = token;
            SortOptions = sortOptions;
        }
    }
}
