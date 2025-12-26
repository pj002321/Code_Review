using Cysharp.Threading.Tasks;
using UnityEngine;

namespace penta
{
    /// <summary>
    /// Stone 엘리멘탈
    /// </summary>
    public partial class Stone : Elemental
    {
        private ElementalType elementalType => ElementalType.Stone;
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


