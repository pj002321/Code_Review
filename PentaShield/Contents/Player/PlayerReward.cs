using Cysharp.Threading.Tasks;
using UnityEngine;

namespace penta
{
    /// <summary>
    /// 플레이어 보상 및 통계 관리
    /// - 경험치 및 레벨 관리
    /// - 코인 관리 (획득/사용)
    /// - 레벨업 시 체력 증가 및 보상 스폰
    /// </summary>
    public class PlayerReward : MonoBehaviourSingleton<PlayerReward>
    {
        #region Constants
        private const int INITIAL_LEVEL = 1;
        private const int MAX_LEVEL = 100;
        private const int REQUIRED_EXP_PER_LEVEL = 100;
        private const int HP_INCREASE_ON_LEVELUP = 10;
        private const float VFX_Y_POSITION = 3.5f;
        private const float VFX_ROTATION_X = -90f;
        #endregion

        #region Properties
        public int Experience { get; set; }
        public int Coin { get; set; }
        public int Level { get; set; } = INITIAL_LEVEL;
        public int MaxLevel { get; set; } = MAX_LEVEL;
        #endregion

        private protected override bool DontDestroy => false;
        protected override void Awake()
        {
            base.Awake();
            Level = INITIAL_LEVEL;
            Experience = 0;
            Coin = 0;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        public void GainExperience(int amount)
        {
            Experience += amount;

            RewardUI.Shared?.SetExperienceAmountText(Experience);

            while (Experience >= REQUIRED_EXP_PER_LEVEL && Level < MaxLevel)
            {
                LevelUp();
            }
        }

        private void LevelUp()
        {
            if (Level >= MaxLevel) return;

            Level++;
            Debug.Log($"LevelUp : {Level} - {Experience}");

            Experience = 0;
            RewardUI.Shared?.SetLevelAmountToText(Level);
            AudioHelper.PlaySFX(AudioConst.LEVEL_UP, 1f);
            SpawnLevelUpVFX().Forget();

            var playerController = PlayerController.Shared;
            if (playerController != null)
            {
                playerController.CurMaxHeath += HP_INCREASE_ON_LEVELUP;
                playerController.healthSlider?.SetMaxHealth(playerController.CurMaxHeath);
            }

            LevelUpItemSpawner.Shared?.SpawnLevelUpRewards().Forget();
        }


        private async UniTaskVoid SpawnLevelUpVFX()
        {
            var playerController = PlayerController.Shared;
            if (playerController == null || VFXManager.Shared == null) return;

            Vector3 spawnPosition = GetVFXSpawnPosition(playerController.transform.position);
            await VFXManager.Shared.SpawnVFX(PentaConst.KVfxPlayerLevelUp, spawnPosition, Quaternion.Euler(VFX_ROTATION_X, 0, 0));
        }

        private Vector3 GetVFXSpawnPosition(Vector3 playerPosition)
        {
            return new Vector3(playerPosition.x, VFX_Y_POSITION, playerPosition.z);
        }


        public void GainCoin(int amount)
        {
            Coin += amount;
            RewardUI.Shared?.SetCoinAmountToText(Coin);
        }

        public void UseCoin(int amount)
        {
            Coin -= amount;
            RewardUI.Shared?.SetCoinAmountToText(Coin);
        }
    }
}