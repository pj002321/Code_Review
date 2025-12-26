using penta;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ElementalUpgrade : BaseUpgrade
{
    private List<ElementTable> elementTables = null;
    private List<Elemental> ElementalList => owner.GetElementalsList();

    public ElementalUpgrade(UpgradeTable owner)
        : base(owner)
    {
        if (SheetData != null)
        {
            elementTables = SheetData.GetList<ElementTable>();
        }
    }

    public override void UpgradeSetting()
    {
        EnsureSheetDataLoaded();
        UpgradeData.Clear();

        var potentialUpgrades = new List<string>();

        foreach (var elemental in ElementalList)
        {
            var nextLevelTable = GetNextLevelTable(elemental.Name);
            if (nextLevelTable != null)
            {
                potentialUpgrades.Add(nextLevelTable.Name);
            }
        }

        if (ElementalList.Count < Elemental.MAX_ELEMENTAL_COUNT)
        {
            HashSet<string> ownedElementalBaseNames = new HashSet<string>();
            foreach (var elemental in ElementalList)
            {
                if (!string.IsNullOrEmpty(elemental.Name))
                {
                    ownedElementalBaseNames.Add(elemental.Name.Split('_')[0]);
                }
            }

            var unownedElementals = elementTables
                .Where(table => table.Level == 1 && !ownedElementalBaseNames.Contains(table.Name.Split('_')[0]))
                .Select(table => table.Name)
                .ToList();
            potentialUpgrades.AddRange(unownedElementals);
        }

        if (potentialUpgrades.Count == 0)
        {
            return;
        }

        string targetName = potentialUpgrades[UnityEngine.Random.Range(0, potentialUpgrades.Count)];
        var targetTable = elementTables.Find(x => x.Name == targetName);
        if (targetTable == null)
        {
            return;
        }

        UpgradeData.Name = targetTable.Name;
        UpgradeData.Cost = targetTable.Cost;
        UpgradeData.UnlockLevel = targetTable.UnlockLevel;

        string[] nameParts = targetName.Split('_');
        int level = 1;
        if (nameParts.Length >= 2 && int.TryParse(nameParts[1], out int parsedLevel))
        {
            level = parsedLevel;
            UpgradeData.Type = level == 1 ? UpgradeType.New_Elemental : UpgradeType.Upgrade_Elemental;
        }
        else
        {
            UpgradeData.Type = UpgradeType.New_Elemental;
        }

        _ = LoadSprite();
        UpgradeData.IsUpgradeAble = ValidateUpgradeConditions(targetTable.Cost, targetTable.UnlockLevel);

        if (level >= 2)
        {
            UpgradeViewer.Shared?.SetElementalUpgradeFlag(true);
        }
        else
        {
            UpgradeViewer.Shared?.SetElementalUpgradeFlag(false);
        }
    }

    public override async UniTask<bool> ExcuteLevelupUpgrade()
    {
        if (UpgradeData.IsUpgradeAble == false) { return false; }

        UpgradeType upgradeType = UpgradeData.Type;
        if (upgradeType == UpgradeType.New_Elemental)
        {
            await owner.OnSpawnElemental(UpgradeData.Name);
        }
        else if (upgradeType == UpgradeType.Upgrade_Elemental)
        {
            owner.OnUpgradeElemental();
        }
        PlayerReward.Shared.UseCoin(UpgradeData.Cost);
        UpgradeData.Clear();

        return true;
    }

    protected async override UniTask<Sprite> LoadSprite()
    {
        if (UpgradeData.Name.IsNullOrEmpty())
        {
            Sprite defaultSprite = await LoadSpriteAsync("fire@sprite");
            UpgradeData.UpgradeSprite = defaultSprite;
            UpgradeData.IsSpriteLoad = true;
            return defaultSprite;
        }

        string elementalName = UpgradeData.Name.Split('_').First();
        string loadKey = $"{elementalName}@enhance";
        Sprite loadSprite = await LoadSpriteAsync(loadKey);

        if (loadSprite == null)
        {
            loadSprite = await LoadSpriteAsync("fire@sprite");
        }

        UpgradeData.UpgradeSprite = loadSprite;
        UpgradeData.IsSpriteLoad = true;
        return loadSprite;
    }

    public async UniTask<Elemental> CreateElemental(string name)
    {
        string createTargetName = name;
        ElementTable targetTable = elementTables.Find(x => x.Name == createTargetName);
        if (targetTable == null)
        {
            return null;
        }

        if (createTargetName.Contains("@") == false)
        {
            createTargetName = createTargetName.Split('_').First() + "@helper";
        }

        GameObject prefab = await AbHelper.Shared.LoadAssetAsync<GameObject>(createTargetName);
        GameObject createdElemental = GameObject.Instantiate(prefab, Vector3.zero, Quaternion.identity);

        var activeScene = SceneManager.GetActiveScene();
        if (createdElemental.scene != activeScene)
        {
            SceneManager.MoveGameObjectToScene(createdElemental, activeScene);
        }

        if (penta.RoundSystem.Shared != null)
        {
            createdElemental.transform.SetParent(penta.RoundSystem.Shared.transform, true);
        }
        Elemental targetElementar = createdElemental.GetComponent<Elemental>();
        ElementalDataCopyInit(targetElementar, targetTable);
        return targetElementar;
    }

    public void UpgradeElemental()
    {
        Dictionary<ElementalType, Elemental> elementals = owner.GetElementalsRef();
        Elemental upgradeTarget = null;

        string targetBaseName = UpgradeData.Name.Split('_')[0];

        foreach (var kvp in elementals)
        {
            Elemental elemental = kvp.Value;
            if (!string.IsNullOrEmpty(elemental.Name))
            {
                string elementalBaseName = elemental.Name.Split('_')[0];
                if (elementalBaseName == targetBaseName)
                {
                    upgradeTarget = elemental;
                    break;
                }
            }
        }

        if (upgradeTarget == null)
        {
            return;
        }

        ElementTable targetTable = elementTables.Find(x => x.Name == UpgradeData.Name);
        if (targetTable == null)
        {
            return;
        }

        ElementalDataCopyInit(upgradeTarget, targetTable);
    }

    private void ElementalDataCopyInit(Elemental paste, ElementTable copy)
    {
        paste.Name = copy.Name;
        
        string[] nameParts = copy.Name.Split('_');
        if (nameParts.Length >= 2 && int.TryParse(nameParts[1], out int parsedLevel))
        {
            paste.Level = parsedLevel;
        }
        else
        {
            paste.Level = 1;
        }
        
        paste.Enhance(ElementalEnhancementType.Damage, copy.Damage);
        paste.Enhance(ElementalEnhancementType.AttackRate, copy.AttackRate);
        paste.Enhance(ElementalEnhancementType.CurseTime, copy.CurseTime);
        paste.Enhance(ElementalEnhancementType.DotDamage, copy.DotDamage);
        paste.Enhance(ElementalEnhancementType.NuckBack, copy.NuckBackValue);
        paste.Enhance(ElementalEnhancementType.AreaDamage, copy.AreaDamage);
    }

    public string FindNotExistRandomElemental()
    {
        List<Elemental> elementals = ElementalList;
        HashSet<string> ownedElementalBaseNames = new HashSet<string>();

        foreach (var elemental in elementals)
        {
            if (!string.IsNullOrEmpty(elemental.Name))
            {
                ownedElementalBaseNames.Add(elemental.Name.Split('_')[0]);
            }
        }

        List<ElementTable> unownedElementals = elementTables.Where(table =>
        {
            string tableBaseName = table.Name.Split('_')[0];
            return !ownedElementalBaseNames.Contains(tableBaseName);
        }).ToList();

        if (unownedElementals.Count == 0) { return ""; }

        int randomIndex = UnityEngine.Random.Range(0, unownedElementals.Count);
        string selectedName = unownedElementals[randomIndex].Name;
        return $"{selectedName.Split('_')[0]}_1";
    }

    private ElementTable GetNextLevelTable(string curName)
    {
        if (string.IsNullOrEmpty(curName))
        {
            return null;
        }

        string[] names = curName.Split('_');
        if (names.Length < 2)
        {
            return null;
        }

        string baseName = names[0];
        if (!int.TryParse(names[1], out int result))
        {
            return null;
        }

        string nextName = $"{baseName}_{result + 1}";

        if (elementTables == null)
        {
            return null;
        }

        return elementTables.Find(x => x.Name == nextName);
    }
}
