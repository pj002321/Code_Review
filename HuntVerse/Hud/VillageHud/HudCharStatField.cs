using TMPro;
using UnityEngine;

namespace Hunt
{
    public enum CharStatType
    {
        HP,MP,      // 체력,마력
        STR,        // 힘
        INT,        // 지능
        PATK,MATK,  // 물리 및 마법 공격력
        CRIT,       // 크리티컬
        ASPD,MSPD,  // 공격 속도, 이동 속도
        LUK,        // 운
        DEF,        // 방어력
        EVA,        // 회피
    }

    public class HudCharStatField : MonoBehaviour
    {
        [SerializeField] public CharStatType charStatType;
        [SerializeField] private TextMeshProUGUI statNameText;
        [SerializeField] private TextMeshProUGUI statValueText;

        private void Awake()
        {
            SetFieldText();
        }
        private void UpdateFieldValue()
        {

        }

        private void SetFieldText()
        {
            statNameText.text = BindKeyConst.GetStatStringByType(charStatType);
        }
    }
}
