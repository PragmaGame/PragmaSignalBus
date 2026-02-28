using System;

namespace PragmaSignalBus
{
    public interface ISignalSubscription
    {
        public Delegate SourceDelegate { get; }
        public object Token { get; }
        public SortOptions SortOptions { get; }
    }
}