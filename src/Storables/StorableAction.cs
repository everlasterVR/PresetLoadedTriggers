using System.Diagnostics.CodeAnalysis;

namespace everlaster
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "UnusedType.Global")]
    sealed class StorableAction : JSONStorableAction
    {
        public StorableAction(string name, ActionCallback callback) : base(name, callback)
        {
        }

        public void Callback() => actionCallback();

        public void RegisterTo(MVRScript script) => script.RegisterAction(this);
    }
}
