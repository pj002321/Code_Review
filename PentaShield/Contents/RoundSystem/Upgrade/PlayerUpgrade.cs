using penta;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 무기 업그레이드 시스템 (주요 로직)
/// - 무기 테이블 기반 레벨업 처리
/// - 랜덤 업그레이드 옵션 제공 (데미지, 발사체 수, 유도탄, 힐)
/// </summary>
public class PlayerUpgrade : BaseUpgrade
{
    public PlayerBehaviour Player => PlayerBehaviour.Shared;
    private PlayerWeaponTable cacheTable = null;
    private string selectedUpgradeKey = null;

    public PlayerUpgrade(UpgradeTable owner)
        : base(owner)
    {
    }

    /// <summary> 업그레이드 설정 </summary>
    public override void UpgradeSetting()
    {
        EnsureSheetDataLoaded();

        cacheTable = GetNextLevelTable();
        if (cacheTable == null || Player?.playerTable == null || cacheTable.Name == Player.playerTable.Name)
        {
            cacheTable = null;
            UpgradeData.Clear();
            return;
        }

        UpgradeData.Type = UpgradeType.Upgrade_Player;
        UpgradeData.Name = cacheTable.Name;
        UpgradeData.Cost = cacheTable.Cost;
        UpgradeData.UnlockLevel = cacheTable.UnlockLevel;
        UpgradeData.IsUpgradeAble = ValidateUpgradeConditions(UpgradeData.Cost, UpgradeData.UnlockLevel);

        _ = LoadSprite();
    }

    /// <summary> 플레이어 업그레이드 실행 </summary>
    public override async UniTask<bool> ExcuteLevelupUpgrade()
    {
        await UniTask.CompletedTask;

        if (cacheTable == null || Player == null)
        {
            return false;
        }

        if (!ValidateUpgradeConditions(UpgradeData.Cost, UpgradeData.UnlockLevel))
        {
            return false;
        }

        PlayerReward.Shared?.UseCoin(UpgradeData.Cost);
        Player.playerTable = cacheTable;
        ApplyRandomUpgrade();

        cacheTable = null;
        UpgradeData.Clear();
        return true;
    }

    /// <summary> 랜덤 업그레이드 적용 </summary>
    private void ApplyRandomUpgrade()
    {
        if (string.IsNullOrEmpty(selectedUpgradeKey) || Player == null)
        {
            return;
        }

        if (selectedUpgradeKey == PentaConst.kUpgradeImgPlayerDamage)
        {
            int newDamage = Player.playerTable.Damage;
            Player.Enhance(PlayerEnhancementType.Damage, newDamage, 0);
            Player.playerTable.Damage = newDamage;
        }
        else if (selectedUpgradeKey == PentaConst.kUpgradeImgPlayerProjCount)
        {
            int newProjCount = Player.playerTable.ProjCount;
            Player.Enhance(PlayerEnhancementType.ProjCount, newProjCount, 0);
            Player.playerTable.ProjCount = newProjCount;
        }
        else if (selectedUpgradeKey == PentaConst.kUpgradeImgPlayerRate)
        {
            Player.AddHomingMissile();
        }
        else if (selectedUpgradeKey == PentaConst.kUpgradeImgPlayerHeal)
        {
            Player.Enhance(PlayerEnhancementType.Heal, 30, 0);
        }
    }

    /// <summary> 스프라이트 로드 </summary>
    protected override async UniTask<Sprite> LoadSprite()
    {
        List<string> availableUpgrades = GetAvailableUpgrades();
        selectedUpgradeKey = availableUpgrades[UnityEngine.Random.Range(0, availableUpgrades.Count)];

        Sprite sprite = await LoadSpriteAsync(selectedUpgradeKey);
        if (sprite == null && availableUpgrades.Count > 0)
        {
            selectedUpgradeKey = availableUpgrades[0];
            sprite = await LoadSpriteAsync(selectedUpgradeKey);
        }

        if (sprite != null)
        {
            UpgradeData.UpgradeSprite = sprite;
            UpgradeData.IsSpriteLoad = true;
        }
        return sprite;
    }

    /// <summary> 사용 가능한 업그레이드 목록 조회 </summary>
    private List<string> GetAvailableUpgrades()
    {
        var upgrades = new List<string>
        {
            PentaConst.kUpgradeImgPlayerDamage,
            PentaConst.kUpgradeImgPlayerProjCount,
            PentaConst.kUpgradeImgPlayerHeal
        };

        if (Player?.playerTable != null)
        {
            int currentProjCount = Player.playerTable.ProjCount;
            int currentHomingCount = Player.GetHomingMissileCount();
            int maxHomingCount = UnityEngine.Mathf.Max(0, currentProjCount - 1);

            if (currentProjCount >= 2 && currentHomingCount < maxHomingCount)
            {
                upgrades.Add(PentaConst.kUpgradeImgPlayerRate);
            }
        }

        return upgrades.Count > 0 ? upgrades : new List<string> { PentaConst.kUpgradeImgPlayerDamage };
    }

    /// <summary> 다음 레벨 테이블 조회 </summary>
    private PlayerWeaponTable GetNextLevelTable()
    {
        if (Player?.playerTable == null || string.IsNullOrEmpty(Player.playerTable.Name))
        {
            return null;
        }

        string currentLvName = Player.playerTable.Name;
        int idx = currentLvName.LastIndexOf('_');
        if (idx < 0 || !int.TryParse(currentLvName.Substring(idx + 1), out int currentLevel))
        {
            return Player.playerTable;
        }

        if (SheetData == null)
        {
            return null;
        }

        var weaponTables = SheetData.GetList<PlayerWeaponTable>();
        if (weaponTables == null)
        {
            return null;
        }

        string prefix = currentLvName.Substring(0, idx);
        string nextLvName = $"{prefix}_{currentLevel + 1}";
        return weaponTables.Find(x => x.Name == nextLvName) ?? Player.playerTable;
    }
}
