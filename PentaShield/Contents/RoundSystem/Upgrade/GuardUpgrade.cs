using penta;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Guard 업그레이드 관리 (주요 로직)
/// - Guard 테이블 기반 레벨업 처리
/// - 체력 및 탄약 업그레이드
/// </summary>
public class GuardUpgrade : BaseUpgrade
{
    private GuardTable cacheGuardTable = null;

    public GuardUpgrade(UpgradeTable owner) : base(owner)
    {
    }

    /// <summary> Guard 업그레이드 실행 </summary>
    public override async UniTask<bool> ExcuteLevelupUpgrade()
    {
        await UniTask.CompletedTask;

        if (!ValidateUpgrade())
        {
            return false;
        }

        Guard guard = Guard.Shared;
        if (guard == null)
        {
            return false;
        }

        ApplyUpgrade(guard);
        return true;
    }

    private bool ValidateUpgrade()
    {
        if (cacheGuardTable == null || string.IsNullOrEmpty(UpgradeData.Name))
        {
            return false;
        }

        if (!UpgradeData.IsUpgradeAble)
        {
            return false;
        }

        return ValidateUpgradeConditions(cacheGuardTable.Cost, cacheGuardTable.UnlockLevel);
    }

    private void ApplyUpgrade(Guard guard)
    {
        float healthDiff = cacheGuardTable.MaxHp - guard.MaxHealth;
        float healAmount = healthDiff + (cacheGuardTable.Ammo * 10f);

        guard.CurLevel = cacheGuardTable.Name;
        guard.MaxHealth = cacheGuardTable.MaxHp;
        guard.UpdateMaxHealthSlider();
        guard.SetGuardAmmo(cacheGuardTable.Ammo * 10f);
        guard.Enhance(GuardEnhancementType.Healing, healAmount, 0);

        PlayerReward.Shared.UseCoin(cacheGuardTable.Cost);
        cacheGuardTable = null;
        UpgradeData.Clear();
    }

    /// <summary> 업그레이드 설정 </summary>
    public override void UpgradeSetting()
    {
        EnsureSheetDataLoaded();

        cacheGuardTable = GetNextLevelTable();

        if (cacheGuardTable == null)
        {
            UpgradeData.Clear();
            return;
        }

        UpgradeData.Type = UpgradeType.Upgrade_Guard;
        UpgradeData.Name = cacheGuardTable.Name;
        UpgradeData.Cost = cacheGuardTable.Cost;
        UpgradeData.UnlockLevel = cacheGuardTable.UnlockLevel;
        UpgradeData.IsUpgradeAble = ValidateUpgradeConditions(cacheGuardTable.Cost, cacheGuardTable.UnlockLevel);

        _ = LoadSprite();
    }

    /// <summary> 스프라이트 로드 </summary>
    protected override async UniTask<Sprite> LoadSprite()
    {
        Sprite loadSprite = cacheGuardTable == null
            ? await LoadSpriteAsync("guard@enhance")
            : await LoadSpriteAsync(PentaConst.kUpgradeImgGuardHeal) ?? await LoadSpriteAsync("guard@enhance");

        UpgradeData.UpgradeSprite = loadSprite;
        UpgradeData.IsSpriteLoad = true;
        return loadSprite;
    }

    /// <summary> 다음 레벨 테이블 조회 </summary>
    private GuardTable GetNextLevelTable()
    {
        if (Guard.Shared == null || string.IsNullOrEmpty(Guard.Shared.CurLevel))
        {
            return null;
        }

        string[] splitName = Guard.Shared.CurLevel.Split("_");
        if (splitName.Length < 2 || !int.TryParse(splitName[1], out int currentLevel))
        {
            return null;
        }

        if (SheetData == null)
        {
            return null;
        }

        List<GuardTable> guardTable = SheetData.GetList<GuardTable>();
        if (guardTable == null)
        {
            return null;
        }

        string nextLvName = $"{splitName[0]}_{currentLevel + 1}";
        return guardTable.Find(x => x.Name == nextLvName);
    }
}
