using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace penta
{
    /// <summary>
    /// 부팅 화면 가이드 이미지 자동 전환 (주요 로직)
    /// - 가이드 이미지 순환 표시
    /// </summary>
    public class BootingGuide : MonoBehaviour
    {
        [Header("IMAGE GUIDE")]
        [SerializeField] private Image targetImage;
        [SerializeField] private Sprite[] images;
        [SerializeField] private float swapInterval = 2f;

        private int currentIndex = 0;
        private Coroutine swapCoroutine;

        private void Awake()
        {
            if (images != null && images.Length > 0)
            {
                targetImage.sprite = images[0];

                if (images.Length > 1)
                {
                    swapCoroutine = StartCoroutine(SwapImagesCoroutine());
                }
            }
        }

        private void OnDestroy()
        {
            if (swapCoroutine != null)
            {
                StopCoroutine(swapCoroutine);
                swapCoroutine = null;
            }
        }

        /// <summary> 이미지 순환 코루틴 </summary>
        private IEnumerator SwapImagesCoroutine()
        {
            while (gameObject != null && targetImage != null)
            {
                yield return new WaitForSeconds(swapInterval);

                if (gameObject == null || targetImage == null || images == null)
                {
                    yield break;
                }

                currentIndex = (currentIndex + 1) % images.Length;

                if (images[currentIndex] != null)
                {
                    targetImage.sprite = images[currentIndex];
                }
            }
        }
    }
}
