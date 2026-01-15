using UnityEngine;
using UnityEngine.EventSystems;

namespace Hunt
{ 
    public class SelectMenuField : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
    {
        private SelectMenuAction controller;
        private int myIndex;

        public void Bind(SelectMenuAction controller, int index)
        {
            this.controller = controller;
            this.myIndex = index;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            controller?.OnHovered(myIndex);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            controller?.OnClicked(myIndex);
        }
    }
}
