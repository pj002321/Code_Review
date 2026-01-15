using TMPro;
using UnityEngine;
using UnityEngine.TextCore.Text;

namespace Hunt
{

    public class HudStagePanel : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI stageNameText;

        private void Start()
        {
            UpdateStagePanel(0);
        }
        public void UpdateStagePanel(uint mapId)
        {
            stageNameText.text = BindKeyConst.GetMapNameByMapId(mapId);
        }
    }
}
