using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace everlaster
{
    sealed class PassThroughScroll : ScrollRect
    {
        public Transform target { get; set; }

        public override void OnScroll(PointerEventData data)
        {
            if(!vertical && !horizontal && target != null)
            {
                ExecuteEvents.ExecuteHierarchy(target.gameObject, data, ExecuteEvents.scrollHandler);
            }
            else
            {
                base.OnScroll(data);
            }
        }
    }
}
