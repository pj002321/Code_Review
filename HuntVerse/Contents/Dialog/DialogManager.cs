using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hunt
{
    public class DialogManager : MonoBehaviourSingleton<DialogManager>
    {
        #region Field
        [SerializeField] private DialogPanel dialogPanel;
        [SerializeField] private float typingSpeed = 0.05f;

        private DialogData currentDialog;
        private int currentNodeIndex;
        private Coroutine typingCoroutine;
        private bool isTyping;
        private Action<int, string> onChoiceSelected;
        private Action onDialogEnd;
        private InputManager inputKey;

        private DialogState currentState = DialogState.None;
        private Stack<int> nodeHistory = new Stack<int>();

        #endregion
        protected override bool DontDestroy => false;
        protected override void Awake()
        {
            base.Awake();
            UniTask.WaitUntil(() => !InputManager.Shared);
            inputKey = InputManager.Shared;
            if (dialogPanel == null)
            {
                "DialogPanel이 없습니다.".DError();
            }
            else
            {
                dialogPanel.Hide();
            }

            inputKey.Player.Skip.performed += OnSkipPerformed;

        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            inputKey.Player.Skip.performed -= OnSkipPerformed;
        }

        private void OnSkipPerformed(InputAction.CallbackContext context)
        {
            if (currentDialog == null) return;

            switch (currentState)
            {
                case DialogState.Typing:
                    CompleteTyping();
                    break;
                case DialogState.WaitingForIput:    // 입력 대기
                    ShowNextNode();
                    break;
                default:
                    break;
            }
        }

        public void StartDialog(DialogData data, Action<int, string> onChoiceSelected = null, Action onComplete = null)
        {
            if (data == null || data.nodes == null || data.nodes.Count == 0)
            {
                $"대사 데이터가 없습니다.".DError();
                onComplete?.Invoke();
                return;
            }

            currentDialog = data;
            currentNodeIndex = 0;
            nodeHistory.Clear();

            this.onChoiceSelected = onChoiceSelected;
            this.onDialogEnd = onComplete;

            LoadSpeakerIcon(data.speakerIconkey);
            dialogPanel?.Show();

            "DialogPanel Show".DLog();

            ShowCurrentNode();

        }

        public void EndDialog()
        {
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
                typingCoroutine = null;
            }

            isTyping = false;

            var callback = onDialogEnd;
            onDialogEnd = null;
            onChoiceSelected = null;

            dialogPanel.Hide();
            callback?.Invoke();

            currentDialog = null;
            nodeHistory.Clear();
            ChangeState(DialogState.None);
        }
        private void ChangeState(DialogState newState)
        {
            if (currentState == newState) return;
            $"[DialogManager] 상태 변경 : {currentState}->{newState}".DLog();

            currentState = newState;
        }
        private void ShowCurrentNode()
        {
            if (currentDialog == null || currentNodeIndex >= currentDialog.nodes.Count)
            {
                ChangeState(DialogState.Completed);
                EndDialog();
                return;
            }

            DialogNode node = currentDialog.nodes[currentNodeIndex];
            $"[DialogManager] 대사 표시: nodeId={node.nodeId}, text={node.dialogText}".DLog();

            var allowPreviouse = node.allowPrev && nodeHistory.Count > 0;
            dialogPanel.ShowNode(node, allowPreviouse, ShowPreviousNode);

            if (node.choices != null && node.choices.Count > 0)
            {
                ChangeState(DialogState.ShowingChoices);
                dialogPanel.ShowChoices(node.choices, OnChoiceSelected);

                if (typingCoroutine != null)
                {
                    StopCoroutine(typingCoroutine);
                }

                typingCoroutine = StartCoroutine(TypeText(node.dialogText));
            }
            else
            {
                if (typingCoroutine != null)
                {
                    StopCoroutine(typingCoroutine);
                }
                typingCoroutine = StartCoroutine(TypeText(node.dialogText));
            }


        }

        private void ShowNextNode()
        {
            if (currentNodeIndex >= 0 && currentNodeIndex < currentDialog.nodes.Count)
            {
                nodeHistory.Push(currentNodeIndex);
            }
            currentNodeIndex++;
            ShowCurrentNode();
        }

        public void ShowPreviousNode()
        {
            if (nodeHistory.Count == 0)
            {
                this.DWarnning("이전 노드가 없습니다.");
                return;
            }

            currentNodeIndex = nodeHistory.Pop();
            ShowCurrentNode();
        }

        private IEnumerator TypeText(string text)
        {
            ChangeState(DialogState.Typing);
            isTyping = true;

            dialogPanel.SetDialogText("");

            foreach (char c in text)
            {
                dialogPanel.AppenDialogText(c);
                yield return new WaitForSeconds(typingSpeed);
            }

            isTyping = false;
            typingCoroutine = null;

            if (currentDialog != null && currentNodeIndex < currentDialog.nodes.Count)
            {
                DialogNode node = currentDialog.nodes[currentNodeIndex];
                if (node.choices == null || node.choices.Count == 0)
                {
                    ChangeState(DialogState.WaitingForIput);
                }
                else
                {
                    ChangeState(DialogState.ShowingChoices);
                }
            }
        }

        private void CompleteTyping()
        {
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
                typingCoroutine = null;
            }

            if (currentDialog != null && currentNodeIndex < currentDialog.nodes.Count)
            {
                dialogPanel.SetDialogText(currentDialog.nodes[currentNodeIndex].dialogText);
            }

            isTyping = false;

            if (currentDialog != null && currentNodeIndex < currentDialog.nodes.Count)
            {
                var node = currentDialog.nodes[currentNodeIndex];
                if (node.choices == null || node.choices.Count == 0)
                {
                    ChangeState(DialogState.WaitingForIput);
                }
                else
                {
                    ChangeState(DialogState.ShowingChoices);
                }
            }

        }

        private void OnChoiceSelected(int choiceIndex)
        {
            if (currentState != DialogState.ShowingChoices)
            {
                this.DError($"선택 상태가 아닙니다 : {currentState}");
                return;
            }

            ChangeState(DialogState.ProcessingChoice);
            
            if (currentDialog == null || currentNodeIndex >= currentDialog.nodes.Count)
            {
                this.DError("OnChoiceSelected - currentDialog가 없습니다.");
                return;
            }

            DialogNode node = currentDialog.nodes[currentNodeIndex];

            if (choiceIndex < 0 || choiceIndex >= node.choices.Count)
            {
                this.DError("선택 인덱스가 범위를 벗어납니다.");
                ChangeState(DialogState.ShowingChoices);
                return;
            }

            DialogChoice choice = node.choices[choiceIndex];

            string choiceId = string.IsNullOrEmpty(choice.choiceId) ? choice.choiceText : choice.choiceId;
            onChoiceSelected?.Invoke(choiceIndex, choiceId);
            nodeHistory.Push(currentNodeIndex);

            if (choice.nextNodeId < 0)
            {
                ChangeState(DialogState.Completed);
                EndDialog();
                return;
            }

            currentNodeIndex = choice.nextNodeId;
            ShowCurrentNode();
        }

        private async UniTask LoadSpeakerIcon(string iconKey)
        {
            if (string.IsNullOrEmpty(iconKey))
            {
                dialogPanel.SetSpeakerIcon(null);
                return;
            }

            var sprite = await AbLoader.Shared.LoadAssetAsync<Sprite>(iconKey);
            dialogPanel.SetSpeakerIcon(sprite);
        }
    }
}
