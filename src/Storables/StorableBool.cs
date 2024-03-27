using System;
using System.Diagnostics.CodeAnalysis;

namespace everlaster
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "UnusedType.Global")]
    sealed class StorableBool : JSONStorableBool
    {
        public StorableBool(string paramName, bool startingValue) : base(paramName, startingValue)
        {
            storeType = StoreType.Full;
        }

        public void SetCallback(Action<StorableBool> callback) => setCallbackFunction = _ => callback(this);
        public void SetCallback(SetBoolCallback callback) => setCallbackFunction = callback;
        public void SetCallback(Action callback) => setCallbackFunction = _ => callback();
        public void Callback() => setCallbackFunction?.Invoke(val);

        public void RegisterTo(MVRScript script) => script.RegisterBool(this);
    }
}
