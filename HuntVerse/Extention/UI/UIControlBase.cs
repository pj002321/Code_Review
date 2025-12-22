using UnityEngine;

namespace Hunt
{
    public abstract class UIControlBase : MonoBehaviour
    {
        protected bool IsActive => isActiveAndEnabled;
    }
}
