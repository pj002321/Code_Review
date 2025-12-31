using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hunt
{
    public class DialogChoiceButton : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI choiceText;
        private Button button;
        private Action onClickCallback;

        private void Awake()
        {
            button = GetComponent<Button>();
            button?.onClick.AddListener(OnButtonClick);
        }

        private void OnDestroy()
        {
            button?.onClick.RemoveListener(OnButtonClick);
        }

        public void SetUp(string text, Action onClick)
        {
            if (choiceText != null) choiceText.text = text;
            onClickCallback = onClick;
        }
        private void OnButtonClick()
        {
            onClickCallback?.Invoke();
        }
    }
}
