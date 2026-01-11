using UnityEngine;

namespace Hunt
{
    /// <summary>
    /// 노드 그래프에서 참조할 GameObject에 붙이는 컴포넌트입니다.
    /// Bake 시점에 자동으로 추가됩니다.
    /// </summary>
    public class UIGraphTarget : MonoBehaviour
    {
        [SerializeField] private string targetId;
        
        public string TargetId => targetId;
        
        private void Awake()
        {
            RegisterToManager();
        }
        
        private void Start()
        {
            RegisterToManager();
        }
        
        private void RegisterToManager()
        {
            if (string.IsNullOrEmpty(targetId))
            {
                this.DError($"{gameObject.name}의 targetId가 비어있습니다.");
                return;
            }
            
            if (UIManager.Shared == null)
            {
                this.DError($"{gameObject.name}: UIManager.Shared가 null입니다. 나중에 다시 시도합니다.");
                return;
            }
            
          
            var existing = UIManager.Shared.FindGameObjectById(targetId);
            if (existing == gameObject)
            {
                return;
            }
            
            this.DLog($"{gameObject.name} (ID: {targetId}) 등록 중...");
            UIManager.Shared.RegisterGraphTarget(this);
            this.DLog($"{gameObject.name} (ID: {targetId}) 등록 완료");
        }
        
        private void OnDestroy()
        {
            if (!string.IsNullOrEmpty(targetId) && UIManager.Shared != null)
            {
                UIManager.Shared.UnregisterGraphTarget(targetId);
            }
        }
        
        public void SetTargetId(string id)
        {
            if (targetId == id) return;
            
            if (!string.IsNullOrEmpty(targetId) && UIManager.Shared != null)
            {
                UIManager.Shared.UnregisterGraphTarget(targetId);
            }
            
            targetId = id;
            
            if (!string.IsNullOrEmpty(targetId) && UIManager.Shared != null)
            {
                UIManager.Shared.RegisterGraphTarget(this);
            }
            
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
    }
}

