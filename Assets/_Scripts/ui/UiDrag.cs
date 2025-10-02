using core.Managers;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI_
{
    public class UiDrag : MonoBehaviour, IDragHandler,IEndDragHandler, IBeginDragHandler
    {
        public  Vector2 Delta;
        private Vector2 _lastDelta = new Vector2(21, 21);

       

        public void OnEndDrag(PointerEventData eventData)
        {
            Delta = Vector2.zero;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            
        }

        public void OnDrag(PointerEventData eventData)
        {
             Delta = eventData.delta;
            //
            // if (UiManager.Instance != null) {
            //     if (UiManager.Instance.rccCamera.isActiveAndEnabled) {
            //         UiManager.Instance.rccCamera.OnDrag(eventData);
            //     }
            // }
        }
    }
}