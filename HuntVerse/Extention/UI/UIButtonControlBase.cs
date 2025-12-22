using Hunt;
using UnityEngine;
using UnityEngine.UI;

namespace Hunt
{
    [RequireComponent(typeof(Button))]
    public abstract class UIButtonControlBase : UIControlBase
    {
        private Button button;
        protected Button Button => button;

        protected virtual void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(OnClickEvent);
        }

        protected virtual void OnDestroy()
        {
            button.onClick.RemoveListener(OnClickEvent);
        }

        protected abstract void OnClickEvent();

    }
}
