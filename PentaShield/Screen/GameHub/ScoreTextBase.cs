using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


namespace penta
{


    public class ScoreTextBase : MonoBehaviour, IMainMenuScoreText
    {
        public TextMeshProUGUI ScoreText { get; set; }
        public string TargetStageName { get; set; }
        public string StageScoreText => ScoreText?.text;

        protected virtual void Awake()
        {
            ScoreText = GetComponent<TextMeshProUGUI>();
        }
        protected virtual void OnEnable()
        {  
            // Base Use Interface Method Reference
            IMainMenuScoreText view = this;
            view.UpdateScoreText().Forget();
        }


    }      
}