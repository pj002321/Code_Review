using UnityEngine;

namespace Hunt
{
    public interface IPlayer
    {
        public void HandleInput();

        public void HandleMovement();

        public void HandleJump();
        public void HandleAttack();
    }
}
