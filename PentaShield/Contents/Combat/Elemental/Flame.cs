using Cysharp.Threading.Tasks;
using UnityEngine;

namespace penta
{
    /// <summary>
    /// Flame 엘리멘탈
    /// </summary>
    public partial class Flame : Elemental
    {
        private ElementalType elementalType => ElementalType.Flame;
        [SerializeField] private int Damage => damage;

        protected override void OnAttack()
        {
            OnUpgradedAttack();
        }

        protected override float GetCurrentDamage()
        {
            return Damage + damageEnhancement;
        }

        protected override ElementalType GetElementalType()
        {
            return elementalType;
        }

        protected override async UniTask OnLevelUp()
        {
            await base.OnLevelUp();
        }
    }
}


