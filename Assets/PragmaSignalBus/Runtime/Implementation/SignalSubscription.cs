using System;
using UnityEngine.Scripting;

namespace PragmaSignalBus
{
    [Preserve]
    public class SignalSubscription : ISignalSubscription
    {
        public Delegate SourceDelegate { get; }
        public object Token { get; }
        public SortOptions SortOptions { get; }

        [RequiredMember]
        public SignalSubscription(Delegate sourceDelegate,
                                  object token = null,
                                  SortOptions sortOptions = null)
        {
            SourceDelegate = sourceDelegate;
            Token = token;
            SortOptions = sortOptions;
        }
    }
}
