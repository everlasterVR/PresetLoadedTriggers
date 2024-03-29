using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace everlaster
{
    sealed class PassThroughScroll : ScrollRect
    {
        public override void OnScroll(PointerEventData data)
        {
            if(!vertical && !horizontal)
            {
                ExecuteEvents.ExecuteHierarchy(transform.parent.gameObject, data, ExecuteEvents.scrollHandler);
            }
            else
            {
                base.OnScroll(data);
            }
        }
    }
}
