using Sirenix.OdinInspector;
using Cysharp.Threading.Tasks;


namespace penta
{

    public class CacheCharge : MonoBehaviour
    {
        public enum ChargeType { ElI, STONE };

        public ChargeType type;

        [ShowIf("type", ChargeType.ElI)]
        [SerializeField] private float cacheAmount;

        [ShowIf("type", ChargeType.ElI)]
        [SerializeField] private TextMeshProUGUI cachemountCountText;

        [ShowIf("type", ChargeType.STONE)]
        [SerializeField] private int stoneAmount;

        [ShowIf("type", ChargeType.STONE)]
        [SerializeField] private TextMeshProUGUI stoneamountCountText;

        [SerializeField] private int eliSellAmount;

        [SerializeField] private TextMeshProUGUI eliamountText;

        [SerializeField] private Button sellButton;

        [Header("Fly Effect")]
        [Tooltip("포물선 이펙트를 위한 이미지 (MyItemView 스타일)")]
        [SerializeField] private Image flyingImage;

        [Tooltip("Eli가 날아갈 목표 RectTransform (ShopUserEliUI 등)")]
        [SerializeField] private RectTransform targetEliSlot;

        private void Awake()
        {
            if (sellButton == null) sellButton = GetComponentInChildren<Button>();
            sellButton.onClick.AddListener(() => OnSellEli());
        }
        private void OnEnable()
        {
            if (type == ChargeType.ElI)
            {
                cachemountCountText.text = $"{cacheAmount} $";
                eliamountText.text = $"{eliSellAmount}";
            }
            else
            {
                stoneamountCountText.text = $"{stoneAmount}";
                eliamountText.text = $"{eliSellAmount}";
            }
        }

        private void OnSellEli()
        {
            var userStone = UserDataManager.Shared.Data.Stone;
            var userEli = UserDataManager.Shared.Data.Eli;
            if (userStone > stoneAmount)
            {
                UserDataManager.Shared.Data.Stone -= stoneAmount;
                UserDataManager.Shared.Data.Eli += eliSellAmount;
                PlayEliFlyEffect().Forget();

                UserDataManager.Shared.NotifyDataUpdated();
            }
            else
            {
                $"💲 [Charge] Not enough Stone! Required: {stoneAmount}, Current: {userStone}".DWarning();
            }

            $"💲 [Charge] userStone :{userStone} , userEli : {userEli}".DLog();
        }


        private async UniTaskVoid PlayEliFlyEffect()
        {
            if (flyingImage == null || targetEliSlot == null)
            {
                "📜 [CacheCharge] FlyingImage or TargetEliSlot is not assigned. Skipping fly effect.".DWarning();
                return;
            }

            Sprite eliSprite = await AbHelper.Shared.LoadAssetAsync<Sprite>(PentaConst.kIconEli);
            if (eliSprite == null)
            {
                "📜 [CacheCharge] Failed to load Eli sprite".DError();
                return;
            }

            Image clonedImage = Instantiate(flyingImage, flyingImage.transform.parent);
            clonedImage.gameObject.SetActive(true);
            clonedImage.sprite = eliSprite;
            clonedImage.color = Color.white;


            Vector3 startWorldPos = sellButton.transform.position;
            clonedImage.transform.position = startWorldPos;

            float duration = 1.0f;
            float elapsed = 0f;
            Vector3 startPosition = startWorldPos;
            Vector3 targetPos = targetEliSlot.position;

 
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                Vector3 midPoint = (startPosition + targetPos) / 2f;
                midPoint.y += 5f; 

                Vector3 m1 = Vector3.Lerp(startPosition, midPoint, t);
                Vector3 m2 = Vector3.Lerp(midPoint, targetPos, t);
                clonedImage.transform.position = Vector3.Lerp(m1, m2, t);

                if (t > 0.7f)
                {
                    float alpha = 1f - ((t - 0.7f) / 0.3f);
                    clonedImage.color = new Color(1, 1, 1, alpha);
                }

                await UniTask.Yield();
            }

            Destroy(clonedImage.gameObject);
        }
    }
}