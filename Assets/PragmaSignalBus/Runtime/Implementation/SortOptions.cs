using System;

namespace PragmaSignalBus
{
    public sealed class SortOptions
    {
        public Type SortedKey { get; }
        public Type[] BeforeOrder { get; }
        public Type[] AfterOrder { get; }

        public SortOptions(Type sortedKey, Type[] beforeOrder = null, Type[] afterOrder = null)
        {
            SortedKey = sortedKey;
            BeforeOrder = beforeOrder;
            AfterOrder = afterOrder;
        }
    }
}