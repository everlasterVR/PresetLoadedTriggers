using System;
using System.Diagnostics.CodeAnalysis;

namespace everlaster
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "UnusedType.Global")]
    sealed class StorableString : JSONStorableString
    {
        public StorableString(string paramName, string startingValue) : base(paramName, startingValue)
        {
            storeType = StoreType.Full;
        }

        public void SetCallback(Action<StorableString> callback) => setCallbackFunction = _ => callback(this);
        public void SetCallback(SetStringCallback callback) => setCallbackFunction = callback;
        public void Callback() => setCallbackFunction?.Invoke(val);

        public void RegisterTo(MVRScript script) => script.RegisterString(this);
    }
}
