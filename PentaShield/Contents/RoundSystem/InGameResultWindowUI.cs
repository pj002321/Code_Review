namespace penta
{
    public class InGameResultWindowUI : MonoBehaviour
    {
        [Header("RESULT UI")]
        [SerializeField] private Button nextBtn = null;
        [SerializeField] private TextMeshProUGUI scoreText = null;
        [SerializeField] private TextMeshProUGUI waveText = null;
        private int rewardAmount = 0;
        [SerializeField] private TextMeshProUGUI rewardAmountText = null;

        private void Awake()
        {
            Initalize();
        }

        private void OnEnable()
        {
            UIUpdateAsync().Forget();
        }

        private void Initalize()
        {
            nextBtn ??= GetComponentInChildren<Button>();
            scoreText ??= transform.FindDeepChild("scoreText")?.GetComponent<TextMeshProUGUI>();
        }

        private async UniTaskVoid UIUpdateAsync()
        {
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);

            var score = RewardUI.Shared?.Score ?? 0;
            var wave = RoundSystem.Shared?.CurrentRound ?? 0;
            
            scoreText?.SetText(score.ToString());
            waveText?.SetText($"Wave {wave}");
            
            if (RoundSystem.Shared?.OngameClear == true)
            {
                GameClearReward(AccClearReward(score, wave));
                var starList = GetComponentInChildren<StarList>();
                if (starList != null)
                {
                    await starList.ActiveStarList(score);
                }
                else
                {
                    "[InGameResultWindowUI] StarList component not found!".DWarning();
                }
            }
        }

        public void GameClearReward(int amount)
        {
            rewardAmountText?.SetText($"x {amount}");
            
            var userData = UserDataManager.Shared?.Data;
            if (userData != null)
            {
                userData.Stone += amount;
                UserDataManager.Shared.NotifyDataUpdated();
            }
        }


    }       
}