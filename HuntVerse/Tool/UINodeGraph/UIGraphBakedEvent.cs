using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using UnityEngine.Events;

namespace Hunt
{
    /// <summary>
    /// Bake 시점에 Button의 onClick에 등록되는 컴포넌트입니다.
    /// ButtonClickNode에서 연결된 노드들만 실행합니다.
    /// </summary>
    public class UIGraphBakedEvent : MonoBehaviour
    {
        [SerializeField] private UINodeGraph _graph;
        [SerializeField] private string _startNodeGuid; // ButtonClickNode의 GUID
        
        public UINodeGraph graph => _graph;
        public string startNodeGuid => _startNodeGuid;
        
        private Button _button;
        private UnityAction _onClickAction;
        private bool _isRegistered = false;
        
#if UNITY_EDITOR
        public void SetGraph(UINodeGraph g) => _graph = g;
        public void SetStartNodeGuid(string guid) => _startNodeGuid = guid;
#endif
        
        private void Awake()
        {
            _button = GetComponent<Button>();
        }
        
        public void RegisterToButton()
        {
            if (_button == null)
            {
                _button = GetComponent<Button>();
            }
            
            if (_button != null && !_isRegistered)
            {
                _onClickAction = OnButtonClick;
                _button.onClick.AddListener(_onClickAction);
                _isRegistered = true;
            }
        }
        
        public void UnregisterFromButton()
        {
            if (_button != null && _onClickAction != null && _isRegistered)
            {
                _button.onClick.RemoveListener(_onClickAction);
                _isRegistered = false;
            }
        }
        
        private void OnEnable()
        {
            RegisterToButton();
        }
        
        private void OnDisable()
        {
            UnregisterFromButton();
        }
        
        public void OnButtonClick()
        {
            if (_graph == null)
            {
                $"UIGraphBakedEvent: graph가 null입니다.".DWarnning();
                return;
            }
            
            if (UIManager.Shared == null)
            {
                $"UIGraphBakedEvent: UIManager.Shared가 null입니다.".DWarnning();
                return;
            }
            
            if (string.IsNullOrEmpty(_startNodeGuid))
            {
                $"UIGraphBakedEvent: startNodeGuid가 비어있습니다.".DWarnning();
                return;
            }
            
            UIManager.Shared.ExecuteGraphFromNode(_graph, _startNodeGuid).Forget();
        }
    }
}

