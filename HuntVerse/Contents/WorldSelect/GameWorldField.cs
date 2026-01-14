using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hunt
{
    public class GameWorldField : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI channelNameText;
        [SerializeField] private TextMeshProUGUI congestionText;
        [SerializeField] private TextMeshProUGUI myCharCountText;
        [SerializeField] private Button channelButton;

        private WorldModel channelModel;

        private void Awake()
        {
            if (channelButton != null)
            {
                channelButton.onClick.AddListener(OnChannelClicked);
                $"[GameWorldField] Button 리스너 등록 완료".DLog();
            }
            else
            {
                $"[GameWorldField] ❌ channelButton이 null입니다! Inspector에서 할당하세요.".DError();
            }
        }

        private void OnDestroy()
        {
            if (channelButton != null)
            {
                channelButton.onClick.RemoveListener(OnChannelClicked);
            }
        }

        public void Bind(WorldModel model)
        {
            if (model == null)
            {
                $"[GameWorldField] ❌ Bind에 전달된 model이 null입니다!".DError();
                return;
            }
            
            channelModel = model;
            channelNameText.text = model.worldName;
            congestionText.text = model.GetCongestionString();
            myCharCountText.text = model.myCharCount.ToString();
            $"[GameWorldField] ✅ Bind 완료: {model.worldName} (this: {this.gameObject.name})".DLog();
        }

        private void OnChannelClicked()
        {
            $"[GameWorldField] 🖱️ OnChannelClicked 호출됨! (GameObject: {this.gameObject.name})".DLog();
            
            if (channelModel == null)
            {
                $"[GameWorldField] ❌ channelModel이 null입니다! (GameObject: {this.gameObject.name})".DError();
                $"[GameWorldField] Bind()가 호출되지 않았거나, null로 초기화되었습니다.".DError();
                return;
            }

            uint worldId = BindKeyConst.GetWorldIdByWorldName(channelModel.worldName);
            GameSession.Shared?.SetSelectedWorld(worldId);
            $"[GameWorldField] ✅ 월드 선택: {channelModel.worldName} (ID: {worldId})".DLog();

            CharacterSetupController.Shared?.UpdateCharacterSlots(channelModel.worldName, channelModel.myCharCount);
        }
    }
}
