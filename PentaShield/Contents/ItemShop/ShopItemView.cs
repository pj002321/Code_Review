using penta;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace penta
{
    /// <summary>
    /// 상점 아이템 목록 뷰 관리 (주요 로직)
    /// - 판매 가능한 아이템 목록 제공
    /// </summary>
    public class ShopItemView : MonoBehaviour
    {
        [SerializeField] private List<SellableItemInfo> sellableItemInfos = new List<SellableItemInfo>();

        public List<SellableItemInfo> GetSellableItems() => sellableItemInfos;
    }
}
