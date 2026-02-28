using System;
using UnityEngine;
using UnityEngine.Scripting;

namespace PragmaSignalBus
{
    [Serializable, Preserve]
    public class SignalBusConfiguration
    {
        public Action<LogType, string> Logger { get; private set; }
        public Func<object> TokenGenerator { get; private set; }

        [RequiredMember]
        public SignalBusConfiguration(Action<LogType, string> logger = null, Func<object> tokenGenerator = null)
        {
            Logger = logger;

            TokenGenerator = tokenGenerator ?? (() => Guid.NewGuid());
        }
    }
}
