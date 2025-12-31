
using TMPro;
using UnityEngine;

namespace Hunt
{
    public class GenerateCharPanel : MonoBehaviour
    {
        [SerializeField] private PentagonBalanceUI pentagonBalanceUI;
        [SerializeField] private TextMeshProUGUI characterStoryText;
 
        public void SetStats(float attack, float defense, float movespeed, float hp, float agility)
        {
            if (attack <= 0 || defense <= 0 || movespeed <= 0 || hp <= 0 || agility <= 0)
            {
                pentagonBalanceUI.AnimateStatsFromZero(0.5f, 0.5f, 0.5f, 0.5f, 0.5f,1f);
            }
            pentagonBalanceUI.AnimateStatsFromZero(attack, defense, movespeed, hp, agility, 1f);
        }
        public float[] GetStats(float attack, float defense, float movespeed, float hp, float agility)
        {
            return new float[] { attack, defense, movespeed, hp, agility };
        }
        public string GetStoryText(string s)
        {
            return s;
        }
        public void SetStroyText(string s)
        {
            characterStoryText.text = s;
        }

        /// <summary>
        /// 필드의 스토리와 스탯 값을 설정합니다.
        /// </summary>
        /// <param name="story">캐릭터 스토리 텍스트</param>
        /// <param name="stats">스탯 배열 (5개: ATT, DEF, SDP, LUK, AGI)</param>
        /// <returns>설정된 스토리와 스탯 배열</returns>
        public (string, float[] f) OnSetFieldValue(string story, float[] stats)
        {
            SetStroyText(GetStoryText(story));
            
            // 전달받은 스탯 배열 사용 (5개 값이 있어야 함)
            if (stats != null && stats.Length >= 5)
            {
                SetStats(stats[0], stats[1], stats[2], stats[3], stats[4]);
            }
            else
            {
                // 기본값 사용
                SetStats(0.5f, 0.5f, 0.5f, 0.5f, 0.5f);
            }
            
            return (GetStoryText(story), stats ?? new float[] { 0.5f, 0.5f, 0.5f, 0.5f, 0.5f });
        }
    }
}