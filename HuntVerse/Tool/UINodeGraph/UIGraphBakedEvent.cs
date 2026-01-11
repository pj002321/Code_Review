using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

namespace Hunt
{
    /// <summary>
    /// Bake 시점에 Button의 onClick에 등록되는 컴포넌트입니다.
    /// ButtonClickNode에서 연결된 노드들만 실행합니다.
    /// </summary>
    public class UIGraphBakedEvent : MonoBehaviour
    {
        [SerializeField] private UINodeGraph graph;
        [SerializeField] private string startNodeGuid; // ButtonClickNode의 GUID
        
#if UNITY_EDITOR
        public void SetGraph(UINodeGraph g) => graph = g;
        public void SetStartNodeGuid(string guid) => startNodeGuid = guid;
#endif
        
        public void OnButtonClick()
        {
            Debug.Log($"[UIGraphBakedEvent] OnButtonClick 호출됨 - graph: {graph?.name}, startNodeGuid: {startNodeGuid}, UIManager: {UIManager.Shared != null}");
            
            if (graph == null)
            {
                Debug.LogError("[UIGraphBakedEvent] graph가 null입니다.");
                return;
            }
            
            if (UIManager.Shared == null)
            {
                Debug.LogError("[UIGraphBakedEvent] UIManager.Shared가 null입니다.");
                return;
            }
            
            if (string.IsNullOrEmpty(startNodeGuid))
            {
                Debug.LogError("[UIGraphBakedEvent] startNodeGuid가 비어있습니다.");
                return;
            }
            
            UIManager.Shared.ExecuteGraphFromNode(graph, startNodeGuid).Forget();
        }
    }
}

