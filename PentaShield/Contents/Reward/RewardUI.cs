using TMPro;
using UnityEngine;
namespace penta
{
    public class RewardUI : MonoBehaviourSingleton<RewardUI>, IDestroyOnThisScene
    {
        private int levelAmount = 0;
        private int expAmount = 0;
        private int coinAmount = 0;
        private int scoreAmount = 0;

        public int Score => scoreAmount;

        [SerializeField] private TextMeshProUGUI expText;
        [SerializeField] private TextMeshProUGUI coinText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI scoreText;

        [SerializeField] private Animator expanicon;
        [SerializeField] private Animator coinanicon;
        [SerializeField] private Animator levelanicon;

        public void Start()
        {
            levelAmount = PlayerReward.Shared?.Level ?? 0;
            scoreText?.SetText(scoreAmount.ToString());
            expText?.SetText(expAmount.ToString());
            levelText?.SetText(levelAmount.ToString());
            coinText?.SetText(coinAmount.ToString());
        }

        public void SetExperienceAmountText(int currentValue)
        {
            expAmount = currentValue;
            expanicon?.SetTrigger(PentaConst.kScale);
            expText?.SetText(currentValue.ToString());
        }

        public void SetLevelAmountToText(int amount)
        {
            levelAmount = amount;
            levelanicon?.SetTrigger(PentaConst.kScale);
            levelText?.SetText(amount.ToString());
        }

        public void SetCoinAmountToText(int currentCoin)
        {
            coinAmount = currentCoin;
            coinanicon?.SetTrigger(PentaConst.kScale);
            coinText?.SetText(currentCoin.ToString());
        }

        public void SetScoreAmountToText(int amount)
        {
            scoreAmount += amount;
            scoreText?.SetText(scoreAmount.ToString());
            $"Get Score {amount}".DLog();
        }

        

    }
}