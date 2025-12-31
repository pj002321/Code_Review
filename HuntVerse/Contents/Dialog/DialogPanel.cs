using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.UI;

namespace Hunt
{
    public class DialogPanel : UIControlBase
    {
        [Header("UI")]
        [SerializeField] private TextMeshProUGUI dialogText;
        [SerializeField] private Image npcIcon;
        [SerializeField] private Image playerIcon;
        [SerializeField] private Transform choiceContainer;
        [SerializeField] private GameObject choiceButtonPrefab;
        [SerializeField] private Button previousButton;

        private List<DialogChoiceButton> activeButtons = new List<DialogChoiceButton>();
        private StringBuilder dialogBuilder = new StringBuilder();
        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            ClearChoices();
            dialogText.text = "";
            dialogBuilder.Clear();
            gameObject.SetActive(false);
        }


        public void SetDialogText(string text)
        {
            dialogBuilder.Clear();
            if (dialogText != null)
            {
                dialogText.text = text ?? "";
            }
        }

        public void AppenDialogText(char c)
        {
            if (dialogText != null)
            {
                dialogBuilder.Append(c);
                dialogText.text = dialogBuilder.ToString();
            }
        }
        public void ShowNode(DialogNode node, bool allowPrevious, Action onPreviouClick)
        {
            if (previousButton != null)
            {
                previousButton.gameObject.SetActive(allowPrevious);
                previousButton.onClick.RemoveAllListeners();
                if (allowPrevious)
                {
                    previousButton.onClick.AddListener(() => onPreviouClick?.Invoke());
                }
            }
        }
        public void SetSpeakerIcon(Sprite sprite)
        {
            if (npcIcon != null)
            {
                npcIcon.sprite = sprite;
                npcIcon.gameObject.SetActive(sprite != null);
            }
        }

        public void ShowChoices(List<DialogChoice> choices, Action<int> onChoiceClick)
        {
            ClearChoices();

            if (choices == null || choices.Count == 0) return;

            for (int i = 0; i < choices.Count; i++)
            {
                GameObject go = Instantiate(choiceButtonPrefab, choiceContainer);
                DialogChoiceButton btn = go.GetComponent<DialogChoiceButton>();

                if (btn != null)
                {
                    int index = i;
                    btn.SetUp(choices[i].choiceText, () => onChoiceClick?.Invoke(index));
                    activeButtons.Add(btn);
                }
            }
        }

        private void ClearChoices()
        {
            if (activeButtons.Count > 0)
            {
                foreach (var btn in activeButtons)
                {
                    if (btn != null)
                    {
                        Destroy(btn.gameObject);
                    }
                }
                activeButtons.Clear(); 
            }
        }
    }
}
