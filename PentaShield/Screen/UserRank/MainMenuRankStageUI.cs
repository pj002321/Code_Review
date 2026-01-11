using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using penta;

namespace penta
{
    public class MainMenuRankStageUI : MonoBehaviour
    {
        private List<UserRankBoardUI> rankBoardUIs = new List<UserRankBoardUI>();
        private UserRankListUI rankListUI;

        private void Awake()
        {
            rankListUI = GetComponent<UserRankListUI>();
            if (rankListUI == null)
            {
                rankListUI = GetComponentInChildren<UserRankListUI>();
            }
            
            Transform boardListRoot = transform.GetChild(0).GetChild(0);
            
            for (int i = 0; i < boardListRoot.childCount; i++)
            {
                Transform child = boardListRoot.GetChild(i);                
                UserRankBoardUI boardUI = child.GetComponent<UserRankBoardUI>();
                if (boardUI != null)
                {
                    rankBoardUIs.Add(boardUI);
                }
            }
        }

        public async UniTask UpdateView(List<RankData> datas)
        {            
            if (datas == null)
            {                
                $"Data is null".EWarning();
              
                return;
            }
            
            if (rankListUI != null)
            {
                await rankListUI.UpdateRanking(datas);
                return;
            }        
        }
     



    }    
}