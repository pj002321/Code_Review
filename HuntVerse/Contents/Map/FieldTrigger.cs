using UnityEngine;

namespace Hunt
{
    public class MapTransitionTrigger : MonoBehaviour
    {
        [SerializeField] private uint targetMapId;

        private void OnTriggerEnter2D(Collider2D collision)
        {
            var userChar = collision.GetComponent<UserCharacter>();
            this.DLog($"collision userChar : {userChar}");
            if (userChar != null && GameSession.Shared.LocalPlayer)
            {
     
                GameSession.Shared?.InGameService?.ReqMapChange(targetMapId);
            }
        }
    }

}