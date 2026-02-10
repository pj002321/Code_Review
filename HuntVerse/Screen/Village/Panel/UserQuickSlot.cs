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

    public class UserQuickSlot : MonoBehaviour
    {
        [Header("Data")]

        [SerializeField] private List<QuickSlotEntry> skillSlots;      
        [SerializeField] private List<QuickSlotEntry> itemSlots;

        [Header("View Connection")]
        [SerializeField] private QuickSlotView view;

        private void Update()
        {
            HandleInput();
            UpdateCooldowns(Time.deltaTime);
        }
        
        private void HandleInput()
        {
            // 아이템 슬롯 (1~4)
            if (Input.GetKeyDown(KeyCode.Alpha1)) UseQuickItem(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) UseQuickItem(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) UseQuickItem(2);
            if (Input.GetKeyDown(KeyCode.Alpha4)) UseQuickItem(3);

            // 스킬 슬롯 (QERT)
            if (Input.GetKeyDown(KeyCode.Q)) UseQuickSkill(0);
            if (Input.GetKeyDown(KeyCode.E)) UseQuickSkill(1);
            if (Input.GetKeyDown(KeyCode.R)) UseQuickSkill(2);
            if (Input.GetKeyDown(KeyCode.T)) UseQuickSkill(3);
        }
        private void Start()
        {
            
            RefreshAllSlots();
        }
        public void RefreshAllSlots() 
        {
            if (view == null) return;
   
            for (int i = 0; i < skillSlots.Count; i++)
            {
                view.UpdateSkillSlot(i, skillSlots[i]);
            }

            for (int i = 0; i < itemSlots.Count; i++)
            {
                view.UpdateItemSlot(i, itemSlots[i]);
            }
        }
        private void UseQuickSkill(int index)
        {
            if (index < 0 || index >= skillSlots.Count) return;

            var slot = skillSlots[index];
            if (slot.type != QuickSlotType.Skill || slot.cooldownRemain > 0f) return;

            $"스킬 퀵슬롯 사용 {index}".DLog();

            // TODO: 실제 스킬 사용 로직은 SkillManager 등과 연동

            if (slot.cooldownMax > 0f)
            {
                slot.cooldownRemain = slot.cooldownMax;
            }

            view?.UpdateSkillSlot(index, skillSlots[index]);
        }

        private void UseQuickItem(int index)
        {
            if (index < 0 || index >= itemSlots.Count) return;

            var slot = itemSlots[index];
            if (slot.type != QuickSlotType.Item || slot.count <= 0 || slot.cooldownRemain > 0f) return;

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
            }
            
            view?.UpdateItemSlot(index, slot);
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
                    
                    view?.UpdateSkillSlot(i, slot);
                }
            }

            for (int i = 0; i < itemSlots.Count; i++)
            {
                var slot = itemSlots[i];
                if (slot.cooldownRemain > 0f)
                {
                    slot.cooldownRemain -= deltaTime;
                    if (slot.cooldownRemain < 0f) slot.cooldownRemain = 0f;

                    view?.UpdateItemSlot(i, slot);
                }
            }
        }

        public void SetSkillSlotIcon(int index, Sprite icon)
        {
            if (view != null) view.SetSkillIcon(index, icon);
        }
        public void SetItemSlotIcon(int index, Sprite icon)
        {
            if (view != null) view.SetItemIcon(index, icon);
        }


    }
}
