using TMPro;
using UnityEngine;

namespace penta
{
    /// <summary>
    /// 부팅 진행 상태 UI 컴포넌트 
    /// - 로딩 진행률 및 가이드 텍스트 표시
    /// </summary>
    public class BootProgress : MonoBehaviour
    {
        [SerializeField] public TextMeshProUGUI loadingGuideText;
        [SerializeField] public TextMeshProUGUI loadingPercentText;
    }
}
