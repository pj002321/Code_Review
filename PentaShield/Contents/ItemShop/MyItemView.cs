using Cysharp.Threading.Tasks;
using penta;
using System;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace penta
{
    /// <summary>
    /// 아이템 뷰 UI (주요 로직)
    /// - 아이템 개수 표시
    /// - 아이템 획득 비행 이펙트
    /// </summary>
    public class MyItemView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI hasteCount;
        [SerializeField] private TextMeshProUGUI godCount;
        [SerializeField] private TextMeshProUGUI randomboxCount;
        [SerializeField] private TextMeshProUGUI randomGoldenboxCount;
        [SerializeField] private TextMeshProUGUI randomCacheboxCount;
        [SerializeField] private TextMeshProUGUI randomCardCount;
        [SerializeField] private TextMeshProUGUI healCount;
        [SerializeField] private TextMeshProUGUI fiverCount;
        [SerializeField] private TextMeshProUGUI eliCount;
        [SerializeField] private TextMeshProUGUI stoneCount;

        private const float FLY_EFFECT_DURATION = 1.0f;
        private const float FLY_EFFECT_ARC_HEIGHT = 5f;
        private const float FLY_EFFECT_FADE_START = 0.7f;

        private CancellationTokenSource flyEffectCts;
        private readonly SemaphoreSlim flyEffectSemaphore = new(1, 1);

        private Dictionary<ItemType, TextMeshProUGUI> itemCountTextMap;
        private Dictionary<ItemType, string> itemSpriteAssetKeyMap;

        private void Awake()
        {
            InitializeItemMappings();
        }

        private void OnDestroy()
        {
            if (UserDataManager.Shared?.ItemData != null)
            {
                UserDataManager.Shared.ItemData.OnItemCountChanged -= OnUpdateItemCount;
            }

            flyEffectCts?.Cancel();
            flyEffectCts?.Dispose();
            flyEffectSemaphore?.Dispose();
        }

        /// <summary> 아이템 매핑 초기화 </summary>
        private void InitializeItemMappings()
        {
            itemCountTextMap = new Dictionary<ItemType, TextMeshProUGUI>
            {
                { ItemType.Haste, hasteCount },
                { ItemType.God, godCount },
                { ItemType.Potion, healCount },
                { ItemType.Fiver, fiverCount },
                { ItemType.RandomBox, randomboxCount },
                { ItemType.RandomCard, randomCardCount },
                { ItemType.RandomCacheBox, randomCacheboxCount },
                { ItemType.GoldenBox, randomGoldenboxCount },
                { ItemType.Eli, eliCount },
                { ItemType.Stone, stoneCount }
            };

            itemSpriteAssetKeyMap = new Dictionary<ItemType, string>
            {
                { ItemType.Haste, PentaConst.KGIconHaste },
                { ItemType.God, PentaConst.KGIconGod },
                { ItemType.Potion, PentaConst.KGIconHeal },
                { ItemType.Fiver, PentaConst.KGIconFever },
                { ItemType.RandomCard, PentaConst.KGIconRandomCard },
                { ItemType.RandomCacheBox, PentaConst.KGIconCacheBox },
                { ItemType.RandomBox, PentaConst.KGIconRandomBox },
                { ItemType.Eli, PentaConst.kIconEli },
                { ItemType.Stone, PentaConst.kIconStone }
            };
        }

        /// <summary> 아이템 뷰 초기화 </summary>
        public async UniTask InitializeAsync()
        {
            await UniTask.WaitUntil(() => UserDataManager.Shared.IsInitialized == true);
            UserDataManager.Shared.ItemData.OnItemCountChanged += OnUpdateItemCount;
            OnUpdateItemCount(ItemType.Other, 0);
        }

        /// <summary> 아이템 개수 업데이트 </summary>
        private void OnUpdateItemCount(ItemType type, int curCount)
        {
            ItemData itemData = UserDataManager.Shared.ItemData;
            UserData userData = UserDataManager.Shared.Data;

            if (itemData == null || userData == null) return;

            UpdateAllItemCounts(itemData, userData);
        }

        /// <summary> 모든 아이템 개수 업데이트 </summary>
        private void UpdateAllItemCounts(ItemData itemData, UserData userData)
        {
            foreach (var kvp in itemCountTextMap)
            {
                if (kvp.Value == null) continue;

                int count = kvp.Key switch
                {
                    ItemType.Eli => userData.Eli,
                    ItemType.Stone => userData.Stone,
                    _ => itemData.GetItemCount(kvp.Key)
                };

                kvp.Value.text = $"{count}";
            }
        }

        /// <summary> 아이템 획득 비행 이펙트 재생 </summary>
        public async UniTask PlayItemFlyEffect(ItemType itemType, Vector3 startWorldPos, Image flyingImage)
        {
            await flyEffectSemaphore.WaitAsync();

            flyEffectCts?.Cancel();
            flyEffectCts?.Dispose();
            flyEffectCts = new CancellationTokenSource();

            try
            {
                RectTransform targetSlot = GetTargetSlot(itemType);
                Sprite itemSprite = await GetItemSprite(itemType);

                if (targetSlot == null || itemSprite == null || flyingImage == null)
                {
                    return;
                }

                InitializeFlyingImage(flyingImage, itemSprite, startWorldPos);
                await PlayFlyAnimation(flyingImage, startWorldPos, targetSlot.position, flyEffectCts.Token);
                flyingImage.gameObject.SetActive(false);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                flyEffectCts?.Dispose();
                flyEffectCts = null;
                flyEffectSemaphore.Release();
            }
        }

        private void InitializeFlyingImage(Image flyingImage, Sprite itemSprite, Vector3 startPosition)
        {
            flyingImage.sprite = itemSprite;
            flyingImage.color = Color.white;
            flyingImage.gameObject.SetActive(true);
            flyingImage.transform.position = startPosition;
        }

        private async UniTask PlayFlyAnimation(Image flyingImage, Vector3 startPos, Vector3 targetPos, CancellationToken token)
        {
            float elapsed = 0f;

            while (elapsed < FLY_EFFECT_DURATION)
            {
                if (token.IsCancellationRequested) break;

                elapsed += Time.deltaTime;
                float t = elapsed / FLY_EFFECT_DURATION;

                Vector3 currentPos = CalculateBezierPosition(startPos, targetPos, t);
                flyingImage.transform.position = currentPos;

                if (t > FLY_EFFECT_FADE_START)
                {
                    float alpha = CalculateFadeAlpha(t);
                    flyingImage.color = new Color(1, 1, 1, alpha);
                }

                await UniTask.Yield(PlayerLoopTiming.Update, flyEffectCts.Token);
            }
        }

        private Vector3 CalculateBezierPosition(Vector3 start, Vector3 end, float t)
        {
            Vector3 midPoint = (start + end) / 2f;
            midPoint.y += FLY_EFFECT_ARC_HEIGHT;

            Vector3 m1 = Vector3.Lerp(start, midPoint, t);
            Vector3 m2 = Vector3.Lerp(midPoint, end, t);
            return Vector3.Lerp(m1, m2, t);
        }

        private float CalculateFadeAlpha(float t)
        {
            float fadeDuration = 1f - FLY_EFFECT_FADE_START;
            float fadeProgress = (t - FLY_EFFECT_FADE_START) / fadeDuration;
            return 1f - fadeProgress;
        }

        private RectTransform GetTargetSlot(ItemType itemType)
        {
            if (itemCountTextMap.TryGetValue(itemType, out TextMeshProUGUI textComponent))
            {
                return textComponent?.rectTransform;
            }
            return null;
        }

        private async UniTask<Sprite> GetItemSprite(ItemType itemType)
        {
            if (!itemSpriteAssetKeyMap.TryGetValue(itemType, out string assetKey))
            {
                return null;
            }

            if (string.IsNullOrEmpty(assetKey))
            {
                return null;
            }

            return await AbHelper.Shared.LoadAssetAsync<Sprite>(assetKey);
        }
    }
}
