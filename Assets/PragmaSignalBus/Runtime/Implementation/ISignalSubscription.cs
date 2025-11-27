namespace PragmaSignalBus
{
    public interface ISignalSubscription
    {
        public object Token { get; }
        public object ExtraToken { get; }
        public SortOptions SortOptions { get; }
    }
}