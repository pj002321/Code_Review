using UnityEngine;

namespace Hunt
{
    public interface IVfxMover
    {
        void Tick(float deltaTime);
        bool IsFinished { get; }
    }
}
