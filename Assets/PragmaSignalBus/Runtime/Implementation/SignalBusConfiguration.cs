using System;
using UnityEngine.Scripting;

namespace PragmaSignalBus
{
    [Serializable, Preserve]
    public class SignalBusConfiguration
    {
        public bool IsThrowException { get; }

        [RequiredMember]
        public SignalBusConfiguration()
        {
            IsThrowException = false;
        }

        [RequiredMember]
        public SignalBusConfiguration(bool isThrowException)
        {
            IsThrowException = isThrowException;
        }
    }
}
