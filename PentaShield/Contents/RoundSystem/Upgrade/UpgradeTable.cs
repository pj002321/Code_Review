using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

namespace penta
{
    /// <summary>
    /// 업그레이드 테이블 관리 (주요 로직)
    /// - Player, Guard, Elemental 업그레이드 총괄
    /// - Elemental 생성 및 관리
    /// </summary>
    public class UpgradeTable : MonoBehaviourSingleton<UpgradeTable>
    {
        private Dictionary<ElementalType, Elemental> elementals = new Dictionary<ElementalType, Elemental>();

        public ElementalUpgrade ElementalUpgrade { get; private set; }
        public PlayerUpgrade PlayerUpgrade { get; private set; }
        public GuardUpgrade GuardUpgrade { get; private set; }

        public bool Initalize { get; private set; } = false;
        protected override bool DontDestroy => false;

        protected override void Awake()
        {
            base.Awake();
        }

        /// <summary> 업그레이드 시스템 초기화 </summary>
        public void Initialize()
        {
            ElementalUpgrade = new ElementalUpgrade(this);
            PlayerUpgrade = new PlayerUpgrade(this);
            GuardUpgrade = new GuardUpgrade(this);

            InitializeElementals();
            Initalize = true;
        }

        /// <summary> 씬에 존재하는 모든 Elemental 초기화 </summary>
        private void InitializeElementals()
        {
            Elemental[] allElementals = FindObjectsOfType<Elemental>();
            foreach (var elemental in allElementals)
            {
                ElementalType type = elemental.GetType().Name switch
                {
                    "Thunder" => ElementalType.Thunder,
                    "Flame" => ElementalType.Flame,
                    "Water" => ElementalType.Water,
                    "Stone" => ElementalType.Stone,
                    "Curse" => ElementalType.Curse,
                    _ => ElementalType.Unknown
                };

                if (type != ElementalType.Unknown)
                {
                    elementals[type] = elemental;
                }
            }
        }

        /// <summary> 모든 업그레이드 캐시 데이터 초기화 </summary>
        public void UpgradeSettingAllClear()
        {
            ElementalUpgrade.ClearUpgradeCacheData();
            PlayerUpgrade.ClearUpgradeCacheData();
            GuardUpgrade.ClearUpgradeCacheData();
        }

        /// <summary> Player 테이블 업그레이드 </summary>
        public void UpgradePlayerTable(PlayerWeaponTable playerTable)
        {
            if (playerTable == null)
            {
                return;
            }
        }

        /// <summary> Player 강화 </summary>
        public void UpgradePlayer(PlayerEnhancementType enhancementType, float value, int coinValue)
        {
            PlayerBehaviour.Shared?.Enhance(enhancementType, value, coinValue);
        }

        /// <summary> Guard 강화 </summary>
        public void UpgradeGuard(GuardEnhancementType enhancementType, float value, int coinValue)
        {
            Guard.Shared?.Enhance(enhancementType, value, coinValue);
        }

        /// <summary> Elemental 생성 </summary>
        public async UniTask OnSpawnElemental(string name, bool isRandom = false)
        {
            if (isRandom)
            {
                name = ElementalUpgrade.FindNotExistRandomElemental();
            }

            if (name.IsNullOrEmpty())
            {
                return;
            }

            await ElementalUpgrade.CreateElemental(name);
            InitializeElementals();
        }

        /// <summary> Elemental 업그레이드 </summary>
        public void OnUpgradeElemental()
        {
            ElementalUpgrade.UpgradeElemental();
        }

        /// <summary> Elemental 강화 </summary>
        public void UpgradeElemental(ElementalType type, ElementalEnhancementType enhancementType, int value)
        {
            if (elementals.TryGetValue(type, out Elemental elemental))
            {
                elemental.Enhance(enhancementType, value);
            }
        }

        /// <summary> Elemental 조회 </summary>
        public Elemental GetElemental(ElementalType type)
        {
            elementals.TryGetValue(type, out Elemental elemental);
            return elemental;
        }

        /// <summary> Elemental Dictionary 참조 반환 </summary>
        public Dictionary<ElementalType, Elemental> GetElementalsRef()
        {
            return elementals;
        }

        /// <summary> Elemental 리스트 반환 </summary>
        public List<Elemental> GetElementalsList()
        {
            return new List<Elemental>(elementals.Values);
        }
    }
}
