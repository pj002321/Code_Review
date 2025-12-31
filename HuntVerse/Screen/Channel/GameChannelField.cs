using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hunt
{
    public class GameChannelField : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI channelNameText;
        [SerializeField] private TextMeshProUGUI congestionText;
        [SerializeField] private TextMeshProUGUI myCharCountText;
        [SerializeField] private Button channelButton;

        private ChannelModel channelModel;

        private void Awake()
        {
            if (channelButton != null)
            {
                channelButton.onClick.AddListener(OnChannelClicked);
            }
        }

        private void OnDestroy()
        {
            if (channelButton != null)
            {
                channelButton.onClick.RemoveListener(OnChannelClicked);
            }
        }

        public void Bind(ChannelModel model)
        {
            channelModel = model;
            channelNameText.text = model.channelName;
            congestionText.text = model.GetCongestionString();
            myCharCountText.text = model.myCharacterCount.ToString();
        }

        private void OnChannelClicked()
        {
            if (channelModel == null) return;
            
            // 채널 클릭 시 해당 채널의 캐릭터 리스트를 요청하는 로직이 여기 들어가야 함
            // 현재는 캐시된 데이터를 사용하여 UI 업데이트
            CharacterCreateController.Shared?.UpdateCharacterSlots(channelModel.channelName, channelModel.myCharacterCount);
        }
    }
}
