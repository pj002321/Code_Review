
using Hunt.Game;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Hunt
{
    public class CreateCharDocuPanel : MonoBehaviour
    {
        [SerializeField] private PentagonBalanceUI pentagonBalanceUI;
        [SerializeField] private TextMeshProUGUI charStoryText;
        [SerializeField] private TextMeshProUGUI charNameText;

        private List<StatInfo> cachedStats = null;
        private string cachedStory = string.Empty;
        private bool hasCachedData = false;

        private void OnEnable()
        {
            if (hasCachedData && cachedStats != null && cachedStats.Count >= 5)
            {
                float[] floatStats = ConvertStatInfoToFloatArray(cachedStats);
                ApplyStatsAnimation(floatStats[0], floatStats[1], floatStats[2], floatStats[3], floatStats[4]);
            }
        }

        private void ApplyStatsAnimation(float attack, float defense, float movespeed, float hp, float agility)
        {
            if (pentagonBalanceUI == null)
            {
                this.DError($"pentagonBalanceUI가 null입니다");
                return;
            }

            if (attack <= 0 || defense <= 0 || movespeed <= 0 || hp <= 0 || agility <= 0)
            {
                this.DWarnning($"0 이하, 기본값 사용");
                pentagonBalanceUI.AnimateStatsFromZero(0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 1f);
                return;
            }

            this.DLog($"애니메이션 실행 - ATT:{attack}, DEF:{defense}, SPD:{movespeed}, HP:{hp}, AGI:{agility}");
            pentagonBalanceUI.AnimateStatsFromZero(attack, defense, movespeed, hp, agility, 1f);
        }

        /// <summary>
        /// StatInfo 리스트를 받아서 UI에 표시합니다.
        /// </summary>
        public void SetStats(List<StatInfo> statInfos)
        {
            if (statInfos == null || statInfos.Count < 5)
            {
                this.DError($"StatInfo가 null이거나 5개 미만입니다. 기본값 사용");
                statInfos = CreateDefaultStatInfos();
            }

            cachedStats = new List<StatInfo>(statInfos);
            hasCachedData = true;

            float[] floatStats = ConvertStatInfoToFloatArray(statInfos);

            if (gameObject.activeInHierarchy && pentagonBalanceUI != null && pentagonBalanceUI.gameObject.activeInHierarchy)
            {
                ApplyStatsAnimation(floatStats[0], floatStats[1], floatStats[2], floatStats[3], floatStats[4]);
            }
        }

        /// <summary>
        /// StatInfo를 float 배열로 변환합니다. (PentagonBalanceUI는 float를 받습니다)
        /// </summary>
        private float[] ConvertStatInfoToFloatArray(List<StatInfo> statInfos)
        {
            if (statInfos == null || statInfos.Count < 5)
            {
                return new float[] { 0.5f, 0.5f, 0.5f, 0.5f, 0.5f };
            }

            // StatInfo의 Type을 기반으로 올바른 순서로 변환
            // 순서: 0=ATT, 1=DEF, 2=SPD, 3=LUK, 4=AGI
            float[] result = new float[5];
            for (int i = 0; i < statInfos.Count && i < 5; i++)
            {
                // Point를 float로 변환 (0.0 ~ 1.0 범위로 정규화)
                // Point는 ulong이므로 100으로 나누어 float로 변환
                result[i] = Mathf.Clamp01(statInfos[i].Point / 100f);
            }

            return result;
        }

        /// <summary>
        /// 기본 StatInfo 리스트를 생성합니다.
        /// </summary>
        private List<StatInfo> CreateDefaultStatInfos()
        {
            return new List<StatInfo>
            {
                new StatInfo { Type = 0, Point = 50 },  // ATT
                new StatInfo { Type = 1, Point = 50 },  // DEF
                new StatInfo { Type = 2, Point = 50 },  // SPD
                new StatInfo { Type = 3, Point = 50 },  // LUK
                new StatInfo { Type = 4, Point = 50 }   // AGI
            };
        }
        public string GetStoryText(string s)
        {
            return s;
        }
        public void SetStroyText(string s)
        {
            if (charStoryText != null)
            {
                charStoryText.text = s;
            }
        }

        public void SetCharNameText(ClassType profession)
        {
            charNameText.text = BindKeyConst.GetProfessionMatchName(profession,true);
        }
        /// <summary>
        /// 필드의 스토리와 스탯 값을 설정합니다.
        /// </summary>
        /// <param name="charProfile">캐릭터 정보 필드</param>
        /// <returns>설정된 스토리와 StatInfo 리스트</returns>
        public (string, List<StatInfo>) OnSetFieldValue(CreateCharProfile charProfile)
        {
            cachedStory = charProfile.charStory ?? string.Empty;
            SetStroyText(GetStoryText(cachedStory));
            SetCharNameText(charProfile.ProfessionType);
            // 전달받은 StatInfo 리스트 사용
            if (charProfile.stats != null && charProfile.stats.Count >= 5)
            {
                SetStats(charProfile.stats);
            }
            else
            {
                // 기본값 사용
                List<StatInfo> defaultStats = CreateDefaultStatInfos();
                SetStats(defaultStats);
            }
            
            return (GetStoryText(cachedStory), cachedStats ?? CreateDefaultStatInfos());
        }
    }
}