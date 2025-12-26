using UnityEngine;

namespace penta
{
    public interface IElemental
    {
        int Level { get; set; }
        int Stat { get; set; }
        void AroundTarget(Transform guardTarget, float orbitDistance, float orbitSpeed, float transitionSpeed, ref float orbitAngle, ref float currentOrbitRadius);
    }
}
