using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace penta
{
    public class RankHubUI : MonoBehaviour
    {
        // 랭크허브는 내부에 있는 UI 요소를 관리하며 데이터를 가져와 표시해줘야함
        // 1. 내부에 있는 컴포넌트 필요    => Ok
        // 2. 데이터를 가져오는것을 구현해야함 (이전에 랭크를 먼저 생성해야하고)
        // 3. 스테이지 별로 출력을 해주어야하며 갱신 기간이 지나기 이전의 데이터라면 그것을 디스크저장후 재사용

        public MainMenuRankStageUI snowStage = null;
        public MainMenuRankStageUI fireStage = null;
        public MainMenuRankStageUI stoneStage = null;

        private static CachedRankings cachedRankings = null;

        private async void OnEnable()
        {
            // 💡 OnEnable마다 최신 랭킹 데이터 가져오기 (forceRefresh=true)
            await RankingDataInit(forceRefresh: true);

            await UniTask.Yield();      

            if(cachedRankings == null || cachedRankings.StageRankings == null || cachedRankings.StageRankings.Count == 0)
            {
                $"[MainMenuRankHubUI] : cachedRankin Is NULL or Empty\ncachedRankin is null? : {cachedRankings == null}\ncacheRanking Count : {cachedRankings.StageRankings?.Count}".DWarning();
                return;
            }
            cachedRankings.StageRankings.TryGetValue(PentaConst.StageSnowAgeName, out List<RankData> snowRanks);
            _ = snowStage.UpdateView(snowRanks);

            cachedRankings.StageRankings.TryGetValue(PentaConst.StageFireWorldName, out List<RankData> fireRanks);
            _ = fireStage.UpdateView(fireRanks);

            cachedRankings.StageRankings.TryGetValue(PentaConst.StageStoneAgeName, out List<RankData> stoneRanks);
            _ = stoneStage.UpdateView(stoneRanks);
        }

        private async UniTask RankingDataInit(bool forceRefresh = false)
        {
            "[MainMenuRankHubUI] Fetching latest ranking data...".Log();
            cachedRankings = await PentaFirebase.Shared.PfireStore.GetStageRankingsAsync(forceRefresh);

            if (cachedRankings != null)
                $"[MainMenuRankHubUI] ✅ Ranking data loaded (LastUpdated: {cachedRankings.LastUpdated})".Log();
        }
}