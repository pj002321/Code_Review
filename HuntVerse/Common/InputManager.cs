using UnityEngine;

namespace Hunt
{

    public class InputManager : MonoBehaviourSingleton<InputManager>
    {
        public HuntKeyAction Action;
        public HuntKeyAction.PlayerActions Player;
        protected override void Awake()
        {
            Action = new HuntKeyAction();
            Player = Action.Player;
            base.Awake();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }



    }

}