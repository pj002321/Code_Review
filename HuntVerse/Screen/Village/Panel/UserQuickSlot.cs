using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using Hunt.Login;
using TMPro;

namespace Hunt
{
    public enum QuickSlotType
    {
        None = 0,
        Skill = 1,
        Item = 2
    }

    [System.Serializable]
    public class QuickSlotEntry
    {
        public QuickSlotType type = QuickSlotType.None;
 
        /*[HideInInspector] */public int count;
        /*[HideInInspector] */public float cooldownMax;
        [HideInInspector] public float cooldownRemain;
    }

    [System.Serializable]
    public class SlotUI
    {
        public Image iconImage;
        public TextMeshProUGUI countText = null;
        public Slider useOverlaySlider;
    }

    public class UserQuickSlot : MonoBehaviourSingleton<UserQuickSlot>
    {
        [Header("UI - Skill Slots")]
        [SerializeField] public List<SlotUI> skillQuickList;

        [Header("UI - Item Slots")]
        [SerializeField] public List<SlotUI> itemQuickList;

        [Header("Data - Skill Slots")]
        [SerializeField] private List<QuickSlotEntry> skillSlots = new List<QuickSlotEntry>();

        [Header("Data - Item Slots")]
        [SerializeField] private List<QuickSlotEntry> itemSlots = new List<QuickSlotEntry>();

        protected override bool DontDestroy => false;

        protected override void Awake()
        {
            base.Awake();
            Init();
        }

        private void Update()
        {
            HandleHotKey();
            UpdateCooldowns(Time.deltaTime);
        }
        private void Init()
        {
            foreach (var slot in skillQuickList)
            {
                slot.useOverlaySlider.value = 0f;
            }
            foreach (var slot in itemQuickList)
            {
                slot.useOverlaySlider.value = 0f;
            }
        }
        private void HandleHotKey()
        {
            // 아이템 슬롯 (1~4)
            if (Input.GetKeyDown(KeyCode.Alpha1)) UseItemSlot(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) UseItemSlot(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) UseItemSlot(2);
            if (Input.GetKeyDown(KeyCode.Alpha4)) UseItemSlot(3);

            // 스킬 슬롯 (QERT)
            if (Input.GetKeyDown(KeyCode.Q)) UseSkillSlot(0);
            if (Input.GetKeyDown(KeyCode.E)) UseSkillSlot(1);
            if (Input.GetKeyDown(KeyCode.R)) UseSkillSlot(2);
            if (Input.GetKeyDown(KeyCode.T)) UseSkillSlot(3);
        }

        private void UseSkillSlot(int index)
        {
            if (index < 0 || index >= skillSlots.Count) return;

            var slot = skillSlots[index];
            if (slot.type != QuickSlotType.Skill) return;
            if (slot.cooldownRemain > 0f) return;

            $"스킬 퀵슬롯 사용 {index}".DLog();

            // TODO: 실제 스킬 사용 로직은 SkillManager 등과 연동

            if (slot.cooldownMax > 0f)
            {
                slot.cooldownRemain = slot.cooldownMax;
            }

            RefreshSkillSlotUI(index);
        }

        private void UseItemSlot(int index)
        {
            if (index < 0 || index >= itemSlots.Count) return;

            var slot = itemSlots[index];
            if (slot.type != QuickSlotType.Item || slot.count <= 0) return;
            if (slot.cooldownRemain > 0f) return;

            $"아이템 퀵슬롯 사용 {index}".DLog();

            // TODO: 실제 아이템 사용 로직은 Inventory / ItemManager 등과 연동
            slot.count--;

            if (slot.cooldownMax > 0f)
            {
                slot.cooldownRemain = slot.cooldownMax;
            }

            if (slot.count <= 0)
            {
                slot.type = QuickSlotType.None;
                itemQuickList[index].iconImage.sprite = null;
                ClearItemSlotUI(index);
            }
            else
            {
                RefreshItemSlotUI(index);
            }
        }

        /// <summary>
        /// 쿨타임 감소 및 오버레이 갱신
        /// </summary>
        private void UpdateCooldowns(float deltaTime)
        {
            // 스킬 슬롯 쿨타임
            for (int i = 0; i < skillSlots.Count; i++)
            {
                var slot = skillSlots[i];
                if (slot.cooldownRemain > 0f)
                {
                    slot.cooldownRemain -= deltaTime;
                    if (slot.cooldownRemain < 0f) slot.cooldownRemain = 0f;
                    RefreshSkillSlotUI(i);
                }
            }

            for (int i = 0; i < itemSlots.Count; i++)
            {
                var slot = itemSlots[i];
                if (slot.cooldownRemain > 0f)
                {
                    slot.cooldownRemain -= deltaTime;
                    if (slot.cooldownRemain < 0f) slot.cooldownRemain = 0f;
                    RefreshItemSlotUI(i);
                }
            }
        }

        /// <summary>
        /// 아이템 모두 소진 시 슬롯 UI 비움.
        /// </summary>
        private void ClearItemSlotUI(int index)
        {
            if (index < 0 || index >= itemQuickList.Count) return;

            var ui = itemQuickList[index];

            if (ui.iconImage != null)
            {
                ui.iconImage.sprite = null;
                ui.iconImage.enabled = false;
            }

            if (ui.countText != null)
            {
                ui.countText.text = string.Empty;
            }

            if (ui.useOverlaySlider != null)
            {
                ui.useOverlaySlider.value = 0f;
            }
        }

        /// <summary>
        /// 아이템 슬롯 UI 갱신 (수량 텍스트, 쿨타임 오버레이 포함)
        /// </summary>
        private void RefreshItemSlotUI(int index)
        {
            if (index < 0 || index >= itemQuickList.Count) return;

            var ui = itemQuickList[index];
            var slot = itemSlots[index];

            if (ui.iconImage != null)
            {
                if (slot.type == QuickSlotType.None)
                {
                    ui.iconImage.sprite = null;
                    ui.iconImage.enabled = false;
                }
                else
                {
                    ui.iconImage.enabled = true;
                }
            }

            if (ui.countText != null)
            {
                if (slot.type == QuickSlotType.Item && slot.count > 1)
                {
                    ui.countText.text = slot.count.ToString();
                }
                else
                {
                    ui.countText.text = string.Empty;
                }
            }

            // 쿨타임 오버레이 (0~1)
            if (ui.useOverlaySlider != null)
            {
                if (slot.cooldownMax > 0f)
                {
                    ui.useOverlaySlider.value = slot.cooldownRemain / slot.cooldownMax;
                }
                else
                {
                    ui.useOverlaySlider.value = 0f;
                }
            }
        }

        /// <summary>
        /// 스킬 슬롯 UI 갱신 (아이콘, 쿨타임 오버레이)
        /// </summary>
        private void RefreshSkillSlotUI(int index)
        {
            if (index < 0 || index >= skillQuickList.Count) return;

            var ui = skillQuickList[index];
            var slot = skillSlots[index];

            if (ui.countText != null)
            {
                ui.countText.text = string.Empty;
            }

            if (ui.useOverlaySlider != null)
            {
                if (slot.cooldownMax > 0f)
                {
                    ui.useOverlaySlider.value = slot.cooldownRemain / slot.cooldownMax;
                }
                else
                {
                    ui.useOverlaySlider.value = 0f;
                }
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }
    }
}
