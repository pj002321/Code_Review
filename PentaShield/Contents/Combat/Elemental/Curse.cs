using Cysharp.Threading.Tasks;
using UnityEngine;

namespace penta
{
    /// <summary>
    /// Curse 엘리멘탈
    /// </summary>
    public partial class Curse : Elemental
    {
        private ElementalType elementalType => ElementalType.Curse;
        public float Damage => damage;

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


