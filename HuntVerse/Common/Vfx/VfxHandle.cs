using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Hunt
{

    public class VfxHandle
    {
        public VfxObject vfxObject { get; private set; }
        public bool IsVaild => vfxObject != null && vfxObject.gameObject.activeSelf;

        public VfxHandle(VfxObject vfxObject)
        {
            this.vfxObject = vfxObject;
        }

        public void Stop()
        {
            if (IsVaild) vfxObject.ReturnToPool();
        }
    }
}
