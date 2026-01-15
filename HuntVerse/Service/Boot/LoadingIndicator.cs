using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
namespace Hunt
{
    public class LoadingIndicator : MonoBehaviour
    {
        [SerializeField] private Slider progressBar;
        [SerializeField] private TextMeshProUGUI scriptText;
        [Header("Loading Script")]
        [SerializeField] private List<string> texts;
        [SerializeField] private float fillSpeed = 0.5f;
        [SerializeField] private float finishDelay = 0.8f;
        private float textTimer = 0f;
        private const float TextRotateInterval = 2f;
        private float displayedProgress = 0f;
        private float targetProgress = 0f;
        private bool finishRequested = false;
        private float finishDelayTimer = 0f;

        void Start()
        {
            if (texts.Count > 0)
            {
                scriptText.text = texts[0];
            }
            if (progressBar != null)
            {
                progressBar.normalizedValue = 0f;
            }
        }

        public void UpdateProgress(float normalizedValue)
        {
            if (progressBar == null)
                return;

            float clamped = Mathf.Clamp01(normalizedValue);

            if (!finishRequested)
            {
                float capped = Mathf.Min(clamped, 0.98f);
                targetProgress = Mathf.Max(targetProgress, capped);

                if (clamped >= 0.999f)
                {
                    finishRequested = true;
                    finishDelayTimer = finishDelay;
                }
            }
        }

        public void Update()
        {
            if (progressBar != null)
            {
                if (finishRequested && finishDelayTimer > 0f)
                {
                    finishDelayTimer -= Time.deltaTime;
                    if (finishDelayTimer <= 0f)
                    {
                        targetProgress = 1f;
                    }
                }

                displayedProgress = Mathf.MoveTowards(displayedProgress, targetProgress, fillSpeed * Time.deltaTime);
                progressBar.normalizedValue = displayedProgress;
            }

            if (texts == null || texts.Count == 0 || scriptText == null)
                return;

            textTimer += Time.deltaTime;
            if (textTimer < TextRotateInterval)
                return;

            textTimer -= TextRotateInterval;
            if (texts.Count > 1)
            {
                var first = texts[0];
                texts.RemoveAt(0);
                texts.Add(first);
            }
            scriptText.text = texts[0];
        }

    }
}
