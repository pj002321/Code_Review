using System;
using UnityEngine;

namespace Hunt
{

    public class InGameHud : MonoBehaviourSingleton<InGameHud>
    {
        private HudSettingPanel settingPanel;
        private HudChatPanel chatPanel;
        private HudUserPanel userPanel;
        private HudCharStatPanel charStatPanel;
        private HudCharInventoryPanel charInventoryPanel;
        private HudStagePanel stagePanel;
        public HudSettingPanel SettingPanel => settingPanel;
        public HudChatPanel ChatPanel => chatPanel;
        public HudUserPanel UserPanel => userPanel;
        public HudCharStatPanel CharStatPanel => charStatPanel;
        public HudCharInventoryPanel CharInventoryPanel => charInventoryPanel;
        public HudStagePanel StagePanel => stagePanel;

        [Header("Quick Slot")]
        [SerializeField] private UserQuickSlot quickSlotController;
        public UserQuickSlot QuickSlot => quickSlotController;

        protected override bool DontDestroy => true;

        protected override void Awake()
        {
            base.Awake();
            InitialzePanels();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        private void InitialzePanels()
        {
            try
            {
                settingPanel = GetComponentInChildren<HudSettingPanel>(true).ValidInit("SettingPanel");
                chatPanel = GetComponentInChildren<HudChatPanel>(true).ValidInit("ChatPanel");
                charStatPanel = GetComponentInChildren<HudCharStatPanel>(true).ValidInit("CharStatPanel");
                userPanel = GetComponentInChildren<HudUserPanel>(true).ValidInit("UserPanel");
                charInventoryPanel = GetComponentInChildren<HudCharInventoryPanel>(true).ValidInit("CharInventoryPanel");
                stagePanel = GetComponentInChildren<HudStagePanel>(true).ValidInit("StagePanel");
                quickSlotController = GetComponentInChildren<UserQuickSlot>(true).ValidInit("QuickSlot");

                this.DLog("Panel �ʱ�ȭ �Ϸ�");
            }
            catch(Exception e)
            {
                this.DError($"Panel �ʱ�ȭ ���� : {e.Message}");
            }

        }
    }
}
