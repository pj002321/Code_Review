using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using UnityEditor.Rendering;

namespace Hunt
{

    [System.Serializable]
    public class SlotUI
    {
        public Image iconImage;
        public TextMeshProUGUI countText = null;
        public Slider useOverlaySlider;
    }

    public class QuickSlotView : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private List<SlotUI> skillQuickList;
        [SerializeField] private List<SlotUI> itemQuickList;

        public void Init()
        {
            foreach (var slot in skillQuickList)
                slot.useOverlaySlider.value = 0f;
            foreach (var slot in itemQuickList)
                slot.useOverlaySlider.value = 0f;
        }

        public void UpdateSkillSlot(int index, QuickSlotEntry data)
        {
            if (index < 0 || index >= skillQuickList.Count) return;

            var ui = skillQuickList[index];

            if (data.cooldownMax > 0f)
            {
                ui.useOverlaySlider.value = data.cooldownRemain / data.cooldownMax;
            }
            else
            {
                ui.useOverlaySlider.value = 0f;
            }
        }

        public void UpdateItemSlot(int index, QuickSlotEntry data)
        {
            if (index < 0 || index >= itemQuickList.Count) return;

            var ui = itemQuickList[index];

            ui.countText.text = data.count > 1 ? data.count.ToString() : "0";

            if (data.cooldownMax > 0f)
            {
                ui.useOverlaySlider.value = data.cooldownRemain / data.cooldownMax;
            }
            else
            {
                ui.useOverlaySlider.value = 0f;
            }

            if (ui.iconImage != null)
            {
                ui.iconImage.enabled = data.type != QuickSlotType.None;
            }

        }

        public void SetSkillIcon(int index, Sprite sprite)
        {
            if (index < 0 || index >= skillQuickList.Count) return;

            var ui = skillQuickList[index];

            if (ui.iconImage != null)
            {
                ui.iconImage.sprite = sprite;
                ui.iconImage.enabled = (sprite != null);
            }
        }


        public void SetItemIcon(int index, Sprite sprite)
        {
            if (index < 0 || index >= itemQuickList.Count) return;

            var ui = itemQuickList[index];

            if (ui.iconImage != null)
            {
                ui.iconImage.sprite = sprite;
                ui.iconImage.enabled = (sprite != null);
            }
        }
    }
}
