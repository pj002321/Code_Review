using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Hunt
{
    public class UIButtonClickCount : UIButtonControlBase
    {
        [SerializeField] private float doubleClickTime = 0.5f;
        [SerializeField] private UnityEvent onDoubleClick;
        [SerializeField] private UnityEvent onOneClick;

        private float lastClickTime = 0f;
        public static bool SelectedOnce = false;
        protected override void OnClickEvent()
        {
            if (!IsActive) return;
            float t = Time.time;

            if (t - lastClickTime < doubleClickTime)
            {
                SelectedOnce = true;
                onDoubleClick?.Invoke();
            }
            else
            {
                SelectedOnce = true;
                onOneClick?.Invoke();
            }

            lastClickTime = t;
        }
    }
}
