using Cysharp.Threading.Tasks;
using UnityEngine;

namespace penta
{
    /// <summary>
    /// Thunder 엘리멘탈
    /// </summary>
    public partial class Thunder : Elemental
    {
        public ElementalType elementalType => ElementalType.Thunder;
        private int Damage => damage;

        protected override void OnAttack()
        {
            if (level == 1)
            {
                base.OnAttack();
            }
            else if (level >= 2)
            {
                OnUpgradedAttack();
            }
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


