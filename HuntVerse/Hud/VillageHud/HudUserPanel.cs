using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hunt
{

    public class HudUserPanel : MonoBehaviour
    {
        public UserPanelInfo UserPanelInfo;
        public UserQuickSlot UserQuickSlot;
        [Header("USER INFO UI")]
        [SerializeField] private TextMeshProUGUI userLevelText;
        [SerializeField] private TextMeshProUGUI userNameText;
        [SerializeField] private Slider userHpSlider;
        [SerializeField] private Slider userMpSlider;
        [SerializeField] private Slider userExpSlider;


        private async UniTask UpdatePanelUI()
        {
            //UserPanelInfo.SetUserPanelValue();
            userNameText.text = UserPanelInfo.Name;
            userLevelText.text = UserPanelInfo.Level.ToString();
            userHpSlider.value = UserPanelInfo.Hp;
            userMpSlider.value = UserPanelInfo.Mp;

            if (AbLoader.Shared != null)
            {

                for (int i = 0; i < UserPanelInfo.SlotSkills.Count; i++)
                {
                    if (UserPanelInfo.SlotSkills != null && UserPanelInfo.SlotSkills.TryGetValue(i, out var skillKey))
                    {
                        UserQuickSlot.skillQuickList[i].iconImage.sprite = await AbLoader.Shared.LoadAssetAsync<Sprite>(skillKey);
                    }
                }

                for (int i = 0; i < UserPanelInfo.SlotItems.Count; i++)
                {
                    if (UserPanelInfo.SlotItems != null && UserPanelInfo.SlotItems.TryGetValue(i, out var itemKey))
                    {
                        UserQuickSlot.itemQuickList[i].iconImage.sprite = await AbLoader.Shared.LoadAssetAsync<Sprite>(itemKey);
                    }

                }
            }

        }
    }

    public class UserPanelInfo
    {
        public string Name;
        public uint Level;
        public Dictionary<int, string> SlotSkills;
        public Dictionary<int, string> SlotItems;
        public float Hp;
        public float Mp;
        public float Exp;

        public void SetUserPanelValue(
            string name,
            uint level,
            Dictionary<int, string> slotskills,
            Dictionary<int, string> slotitems,
            float hp,
            float mp,
            float exp)
        {
            Name = name;
            Level = level;
            SlotSkills = slotskills;
            SlotItems = slotitems;
            Hp = hp;
            Mp = mp;
            Exp = exp;
        }


    }

}