using System;
using System.Diagnostics.CodeAnalysis;

namespace everlaster
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "UnusedType.Global")]
    sealed class StorableFloat : JSONStorableFloat
    {
        public StorableFloat(
            string paramName,
            float startingValue,
            float minValue,
            float maxValue,
            bool constrain = true
        ) : base(paramName, startingValue, minValue, maxValue, constrain)
        {
            storeType = StoreType.Full;
        }

        // ReSharper disable UnusedMember.Global
        public void SetCallback(Action<StorableFloat> callback) => setCallbackFunction = _ => callback(this);
        public void SetCallback(SetFloatCallback callback) => setCallbackFunction = callback;
        public void SetCallback(Action callback) => setCallbackFunction = _ => callback();
        public void Callback() => setCallbackFunction?.Invoke(val);
        // ReSharper restore UnusedMember.Global

        public void RegisterTo(MVRScript script) => script.RegisterFloat(this);
    }
}
