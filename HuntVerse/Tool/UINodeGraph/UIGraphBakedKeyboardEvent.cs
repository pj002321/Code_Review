using UnityEngine;
using Cysharp.Threading.Tasks;

namespace Hunt
{
    ///<summary>
    /// Bake 시점에 키보드 입력을 감지하는 컴포넌트입니다.
    /// KeyboardInputNode에서 연결된 노드들을 실행합니다.
    ///</summary>
    public class UIGraphBakedKeyboardEvent : MonoBehaviour
    {
        [SerializeField] private UINodeGraph _graph;
        [SerializeField] private string _startNodeGuid;
        [SerializeField] private KeyCode _targetKeyCode = KeyCode.None;
        
        public UINodeGraph graph => _graph;
        public string startNodeGuid => _startNodeGuid;
        public KeyCode targetKeyCode => _targetKeyCode;
        
#if UNITY_EDITOR
        public void SetGraph(UINodeGraph g) => _graph = g;
        public void SetStartNodeGuid(string guid) => _startNodeGuid = guid;
        public void SetKeyCode(KeyCode keyCode) => _targetKeyCode = keyCode;
#endif
        
        private bool _isRegistered = false;
        
        public void RegisterToManager()
        {
            if (UIManager.Shared != null && _targetKeyCode != KeyCode.None && !_isRegistered)
            {
                UIManager.Shared.RegisterKeyboardEvent(this);
                _isRegistered = true;
            }
        }
        
        public void UnregisterFromManager()
        {
            if (UIManager.Shared != null && _isRegistered)
            {
                UIManager.Shared.UnregisterKeyboardEvent(this);
                _isRegistered = false;
            }
        }
        
        private void OnEnable()
        {
            RegisterToManager();
        }
        
        private void OnDisable()
        {
            UnregisterFromManager();
        }
    }
}

