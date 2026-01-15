using Cysharp.Threading.Tasks;
using System.Collections;
using System.Threading;
using TMPro;
using UnityEngine;

namespace Hunt
{
    public static class UIEffect
    {
        public static IEnumerator CO_FadeText(TextMeshProUGUI textUI, string message, Color color)
        {
            textUI.text = message;
            textUI.color = color;
            textUI.gameObject.SetActive(true);

            // Fade In
            float a = 0f;
            while (a < 1f)
            {
                a += Time.deltaTime * 3f;
                textUI.color = new Color(color.r, color.g, color.b, a);
                yield return null;
            }

            yield return new WaitForSeconds(2f);

            while (a > 0f)
            {
                a -= Time.deltaTime * 3f;
                textUI.color = new Color(color.r, color.g, color.b, a);
                yield return null;
            }

            textUI.text = "";
            textUI.gameObject.SetActive(false);
        }
        public static async UniTask FadeIn(CanvasGroup canvasgroup, CancellationToken token,float duration = 0.5f)
        {
            if (canvasgroup == null) return;

            float elapsed = 0f;
            canvasgroup.alpha = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasgroup.alpha = Mathf.Clamp01(elapsed / duration);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            canvasgroup.alpha = 1f;
        }
        public static async UniTask FadeOut(CanvasGroup canvasgroup, CancellationToken token, float duration = 0.5f)
        {
            if (canvasgroup == null) return;

            float elapsed = 0f;
            canvasgroup.alpha = 1f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasgroup.alpha = Mathf.Clamp01(1f - (elapsed / duration));
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            canvasgroup.alpha = 0f;
        }
    }
}

