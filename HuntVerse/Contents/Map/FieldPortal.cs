using UnityEngine;

namespace Hunt
{
    public enum PortalDirection
    {
        Left,
        Right,
        Up,
        Down
    }

    /// <summary> 필드 전환 정보 </summary>
    public struct FieldTransitionInfo
    {
        public uint targetMapId;
        public PortalDirection entryDirection;
        public PortalDirection spawnDirection;
    }

    /// <summary> 필드 이동 포털 (진입/출구) </summary>
    public class FieldPortal : MonoBehaviour
    {
        [Header("포털 설정")]
        [SerializeField] private uint targetMapId;
        [SerializeField] private PortalDirection direction;
             
        private int playerLayer;

        public PortalDirection Direction => direction;

        private void Awake()
        {
            playerLayer = LayerMask.NameToLayer("Player");
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.gameObject.layer == playerLayer)
            {
                var userChar = collision.GetComponent<UserCharacter>();
                if (userChar != null && userChar.IsLocalPlayer())
                {
                    this.DLog($"UserChar Collision : {userChar}");
                    FieldTransitionInfo transitionInfo = new FieldTransitionInfo
                    {
                        targetMapId = targetMapId,
                        entryDirection = direction,
                        spawnDirection = GetOppositeDirection(direction)
                    };
                    
                    GameSession.Shared?.InGameService?.ReqMapChange(targetMapId);
                    WorldMapManager.Shared?.SetTransitionInfo(transitionInfo);
                }
            }
        }

        /// <summary> 반대 방향 반환 </summary>
        private PortalDirection GetOppositeDirection(PortalDirection dir)
        {
            return dir switch
            {
                PortalDirection.Left => PortalDirection.Right,
                PortalDirection.Right => PortalDirection.Left,
                PortalDirection.Up => PortalDirection.Down,
                PortalDirection.Down => PortalDirection.Up,
                _ => PortalDirection.Right
            };
        }

        /// <summary> 플레이어를 이 포털 위치에 스폰 </summary>
        public void SpawnPlayer(UserCharacter player)
        {
            if (player != null)
            {
                player.transform.position = Vector3.zero; 
                $"[FieldPortal] 플레이어 스폰: {direction} 포털".DLog();
            }
        }
    }
}

