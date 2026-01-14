using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

namespace Hunt
{
    enum ChatType
    {
        Normal = 0,
        Party = 1,
    }
    public class HudChatPanel : MonoBehaviour
    {
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private Transform content;
        private VerticalLayoutGroup layoutGroup;
        [SerializeField] private GameObject messageItemPrefab;
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private int maxMessages = 50;

        [SerializeField] private Button normalChatButton;
        [SerializeField] private Button partyChatButton;
        private Color textColor = Color.white;
        private List<GameObject> messageItems = new List<GameObject>();

        void Awake()
        {
            if (layoutGroup == null) layoutGroup = content.GetComponent<VerticalLayoutGroup>();
            if (layoutGroup == null) layoutGroup = content.gameObject.AddComponent<VerticalLayoutGroup>();

            layoutGroup.childAlignment = TextAnchor.UpperLeft;
            layoutGroup.childControlHeight = false;
            layoutGroup.childControlWidth = true;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.spacing = 5f;

            RectTransform contentRect = content as RectTransform;
            if (contentRect != null)
            {
                ContentSizeFitter contentSizeFitter = content.GetComponent<ContentSizeFitter>();
                if (contentSizeFitter == null)
                {
                    contentSizeFitter = content.gameObject.AddComponent<ContentSizeFitter>();
                }
                contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            }

            if (inputField != null)
            {
                inputField.onEndEdit.AddListener(OnInputEndEdit);
            }
            normalChatButton.onClick.AddListener(() => SwitchingType(ChatType.Normal));
            partyChatButton.onClick.AddListener(() => SwitchingType(ChatType.Party));

            SwitchingType(ChatType.Normal);
        }
        void OnDestroy()
        {
            if (inputField != null)
            {
                inputField.onEndEdit.RemoveListener(OnInputEndEdit);
            }

            normalChatButton.onClick.RemoveListener(() => SwitchingType(ChatType.Normal));
            partyChatButton.onClick.RemoveListener(() => SwitchingType(ChatType.Party));
        }

        private void OnInputEndEdit(string text)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                if (!string.IsNullOrEmpty(text))
                {
                    AddMessage(text);
                    inputField.text = "";
                    inputField.ActivateInputField();
                }
            }
        }

        private void SwitchingType(ChatType t)
        {
            if (t == ChatType.Party)
            {
                textColor = Color.green;
                normalChatButton.GetComponent<Image>().color = Color.black;
                partyChatButton.GetComponent<Image>().color = Color.white;
            }
            else
            {
                textColor = Color.white;
                partyChatButton.GetComponent<Image>().color = Color.black;
                normalChatButton.GetComponent<Image>().color = Color.white;
            }

            inputField.textComponent.color = textColor;

        }
        public void AddMessage(string message)
        {
            GameObject item = Instantiate(messageItemPrefab, content);
            RectTransform itemRect = item.GetComponent<RectTransform>();
            TextMeshProUGUI textComponent = item.GetComponentInChildren<TextMeshProUGUI>();
            RectTransform contentRect = content as RectTransform;

            if (textComponent != null && itemRect != null && contentRect != null)
            {
                RectTransform textRect = textComponent.GetComponent<RectTransform>();

                float contentWidth = contentRect.rect.width;

                textComponent.text = message;
                textComponent.color = textColor;
                if (textRect != null)
                {
                    textRect.sizeDelta = new Vector2(contentWidth, textRect.sizeDelta.y);
                }

                textComponent.ForceMeshUpdate();

                float preferredHeight = textComponent.GetPreferredValues(message, contentWidth, 0f).y;

                itemRect.anchorMin = new Vector2(0f, 1f);
                itemRect.anchorMax = new Vector2(1f, 1f);
                itemRect.pivot = new Vector2(0.5f, 1f);
                itemRect.sizeDelta = new Vector2(0f, preferredHeight);

                float totalHeight = 0f;
                for (int i = 0; i < messageItems.Count; i++)
                {
                    RectTransform prevRect = messageItems[i].GetComponent<RectTransform>();
                    if (prevRect != null)
                    {
                        totalHeight += prevRect.rect.height + layoutGroup.spacing;
                    }
                }

                itemRect.anchoredPosition = new Vector2(0f, -totalHeight);
            }
            else
            {
                this.DError("TextMeshProUGUI or RectTransform is null");
            }

            messageItems.Add(item);

            if (messageItems.Count > maxMessages)
            {
                GameObject oldItem = messageItems[0];
                messageItems.RemoveAt(0);
                Destroy(oldItem);

                UpdateAllPositions();
            }
            else
            {
                UpdateContentHeight();
            }

            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }

        private void UpdateAllPositions()
        {
            float totalHeight = 0f;
            for (int i = 0; i < messageItems.Count; i++)
            {
                RectTransform itemRect = messageItems[i].GetComponent<RectTransform>();
                if (itemRect != null)
                {
                    itemRect.anchoredPosition = new Vector2(0f, -totalHeight);
                    totalHeight += itemRect.rect.height + layoutGroup.spacing;
                }
            }
            UpdateContentHeight();
        }

        private void UpdateContentHeight()
        {
            RectTransform contentRect = content as RectTransform;
            if (contentRect == null) return;

            float totalHeight = 0f;
            for (int i = 0; i < messageItems.Count; i++)
            {
                RectTransform itemRect = messageItems[i].GetComponent<RectTransform>();
                if (itemRect != null)
                {
                    totalHeight += itemRect.rect.height;
                    if (i < messageItems.Count - 1)
                    {
                        totalHeight += layoutGroup.spacing;
                    }
                }
            }

            contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, totalHeight);
        }

        public void Clear()
        {
            foreach (GameObject item in messageItems)
            {
                Destroy(item);
            }
            messageItems.Clear();
        }
    }
}