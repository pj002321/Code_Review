using penta;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 업그레이드 기본 클래스 (주요 로직)
/// - 업그레이드 데이터 관리
/// - 업그레이드 조건 검증
/// </summary>
public class UpgradeData
{
    public UpgradeType Type;
    public string Name;
    public int UnlockLevel;
    public int Cost;
    public bool IsUpgradeAble;
    public bool IsSpriteLoad;
    public Sprite UpgradeSprite;

    public void Clear()
    {
        Type = UpgradeType.None;
        Name = string.Empty;
        IsUpgradeAble = false;
        IsSpriteLoad = false;
        UpgradeSprite = null;
        Cost = -1;
        UnlockLevel = -1;
    }
}

/// <summary>
/// 업그레이드 기본 클래스 (주요 로직)
/// - 업그레이드 데이터 관리
/// - 업그레이드 조건 검증
/// </summary>
public class BaseUpgrade
{
    protected static GoogleSheetSO SheetData { get; private set; } = null;
    public UpgradeData UpgradeData { get; protected set; } = new UpgradeData();
    protected UpgradeTable owner = null;

    public BaseUpgrade(UpgradeTable owner)
    {
        EnsureSheetDataLoaded();
        this.owner = owner;
    }

    ~BaseUpgrade()
    {
        SheetData = null;
    }

    protected void EnsureSheetDataLoaded()
    {
        if (SheetData == null)
        {
            SheetData = SheetManager.GetSheetObject();
        }
    }

    /// <summary> 업그레이드 가능 여부 검증 </summary>
    protected bool ValidateUpgradeConditions(int cost, int unlockLevel)
    {
        var playerReward = PlayerReward.Shared;
        if (playerReward == null)
        {
            return false;
        }

        if (cost == -1 || cost > playerReward.Coin)
        {
            return false;
        }

        return unlockLevel <= playerReward.Level;
    }

    /// <summary> 업그레이드 캐시 데이터 초기화 </summary>
    public void ClearUpgradeCacheData()
    {
        UpgradeData.Clear();
    }

    /// <summary> 스프라이트 비동기 로드 </summary>
    protected async UniTask<Sprite> LoadSpriteAsync(string key)
    {
        if (AbHelper.Shared == null)
        {
            return null;
        }
        return await AbHelper.Shared.LoadAssetAsync<Sprite>(key);
    }

    /// <summary> 업그레이드 실행 </summary>
    public virtual async UniTask<bool> ExcuteLevelupUpgrade()
    {
        await UniTask.CompletedTask;
        return true;
    }

    /// <summary> 업그레이드 설정 </summary>
    public virtual void UpgradeSetting() { }

    /// <summary> 스프라이트 로드 </summary>
    protected virtual async UniTask<Sprite> LoadSprite()
    {
        await UniTask.CompletedTask;
        return null;
    }
}
