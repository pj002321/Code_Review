using Cysharp.Threading.Tasks;
using Hunt.Game;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
namespace Hunt
{
    public class UserCharProfilePanel : MonoBehaviour
    {
        public CharacterPanelConfig config;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private PentagonBalanceUI balanceData;
        [SerializeField] private Image illustImg;
        [SerializeField] private TextMeshProUGUI savePointText;
        [SerializeField] private TextMeshProUGUI professionText;

        public async UniTask HandleUpdateConfig(
            ulong level,
            string name,
            List<StatInfo> stats,
            string illustKey,
            ulong mapId,
            ClassType characterProfession)
        {
            gameObject.SetActive(false);
            gameObject.SetActive(true);

            // Level
            config.Level = level;
            levelText.text = $"레벨 : {config.Level.ToString()}";

            // Name
            config.UserName = name;
            nameText.text = config.UserName;

            config.Stats = stats;
            if (balanceData != null && stats != null && stats.Count >= 5)
            {
                // ulong → float 변환 + 정규화 (0~100 → 0~1)
                float stat0 = stats[0].Point / 100f;
                float stat1 = stats[1].Point / 100f;
                float stat2 = stats[2].Point / 100f;
                float stat3 = stats[3].Point / 100f;
                float stat4 = stats[4].Point / 100f;

                balanceData.AnimateStatsFromZero(stat0, stat1, stat2, stat3, stat4, 1f);
            }

            config.Illust = illustKey;
            var illust = await AbLoader.Shared.LoadAssetAsync<Sprite>(illustKey);
            illustImg.sprite = illust;

            config.mapId = mapId;
            string mapName = BindKeyConst.GetMapNameByMapId(mapId);
            savePointText.text = mapName;

            config.Profession = characterProfession;
            string professionName = BindKeyConst.GetProfessionMatchName(config.Profession);
            professionText.text = $"직업 : {professionName}";

        }

        private void OnDisable()
        {
            this.gameObject.SetActive(false);
        }

    }

    public struct CharacterPanelConfig
    {
        public ulong Level;
        public string UserName;
        public List<StatInfo> Stats;
        public string Illust;
        public ulong mapId;
        public ClassType Profession;
    }

}