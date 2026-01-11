using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace penta
{
    public interface IMainMenuScoreText
    {
        public TextMeshProUGUI ScoreText { get; set; }
        public string TargetStageName { get; set; }


        public async UniTask<bool> UpdateScoreText()
        {       // default method
            await UniTask.WaitUntil(() => UserDataManager.Shared.IsInitialized == true);
            if (ScoreText == null)
            {
                $"[IMainMenuScoreView] : ScoreText Is NULL!".DError();
            }
            if(UserDataManager.Shared.Data.StageDatas == null || UserDataManager.Shared.Data.StageDatas.Count == 0)
            {
                return false; 
            }

            foreach (StageData stageData in UserDataManager.Shared.Data.StageDatas)
            {
                if (stageData.StageName != TargetStageName || stageData == null) { continue; }
                ScoreText.text = stageData.Score.ToString();
                break;
            }
            return true;
        }

    }
}