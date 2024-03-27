using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace everlaster
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "UnusedType.Global")]
    sealed class StorableStringChooser : JSONStorableStringChooser
    {
        public StorableStringChooser(
            string paramName,
            List<string> options,
            string startingValue,
            string displayName = null
        ) : base(paramName, options, startingValue, displayName ?? paramName)
        {
            storeType = StoreType.Full;
        }

        public StorableStringChooser(
            string paramName,
            List<string> options,
            List<string> displayOptions,
            string startingValue,
            string displayName = null
        ) : base(paramName, options, displayOptions, startingValue, displayName ?? paramName)
        {
            storeType = StoreType.Full;
        }

        public void SetCallback(Action<StorableStringChooser> callback) => setCallbackFunction = _ => callback(this);
        public void SetCallback(Action callback) => setCallbackFunction = _ => callback();
        public void SetCallback(SetStringCallback callback) => setCallbackFunction = callback;
        public void Callback() => setCallbackFunction?.Invoke(val);

        public void RegisterTo(MVRScript script, bool isStorable = true)
        {
            script.RegisterStringChooser(this);
            this.isStorable = isStorable;
        }
    }
}
