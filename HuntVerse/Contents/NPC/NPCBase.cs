using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Hunt
{
    [RequireComponent(typeof(CapsuleCollider2D))]
    public class NPCBase : InteractionBase
    {
        [Header("NPC DATA")]
        [SerializeField] protected NPCData npcData;
        [SerializeField] protected NPCNotiType currentNotification = NPCNotiType.None;

        public float detectionRange => interactionRange;

        [Header("VISUAL")]
        [SerializeField] protected SpriteRenderer notificationIcon;

        private CapsuleCollider2D detectionTrigger;
        private Transform localPlayer;
        private bool isLocalPlayerInRange;

        protected bool isInteracting;

        public NPCData Data => npcData;
        public NPCNotiType NotificationType => currentNotification;
        public bool IsInteracting => isInteracting;
        public bool HasPlayerNearby => isLocalPlayerInRange;

        protected virtual void Awake()
        {
            InitializeTrigger();
        }
        protected virtual void Start()
        {
            InitializeNPC();
        }

        protected virtual void OnDestroy()
        {
            localPlayer = null;
        }

        private void InitializeTrigger()
        {
            detectionTrigger = GetComponent<CapsuleCollider2D>();
            detectionTrigger ??= gameObject.AddComponent<CapsuleCollider2D>();

            detectionTrigger.isTrigger = true;

            $"[NPC] {npcData?.npcName} 감지 트리거 초기화 (반경 : {detectionRange} m)".DLog();
        }

        protected virtual void InitializeNPC()
        {
            if (npcData == null)
            {
                this.DError("Npc Data is NULL");
                return;
            }

            FindLocalPlayer();
            SetNotification(NPCNotiType.Exclamation).Forget();
            $"[NPC] {npcData.npcName} 초기화 완료 (타입: {npcData.npcType})".DLog();
        }

        private void InitializeNotificationState()
        {
            // NPC 타입에 따른 기본 알림
            switch (npcData.npcType)
            {
                case NPCType.Trade:
                    SetNotification(NPCNotiType.Exclamation);
                    break;

                case NPCType.QuestGiver:
                    // TODO: QuestManager가 나중에 설정
                    // 일단 기본값 유지
                    break;

                default:
                    // 일반 대화 NPC는 알림 없음
                    SetNotification(NPCNotiType.None);
                    break;
            }
        }

        public virtual async UniTask SetNotification(NPCNotiType type)
        {
            //if (currentNotification == type) return;
            currentNotification = type;

            if (AbLoader.Shared != null)
            {
                notificationIcon.sprite = await AbLoader.Shared.LoadAssetAsync<Sprite>(NotiInteractionConst.GetIconKeyNpcNotiType(type));
            }

            $"[NPC] {npcData?.npcName} 알림 상태 변경:{type}".DLog();
        }

        private void FindLocalPlayer()
        {
            // 서버에서 할당 받은 Local ID로 바꿔야함
            var userChar = FindAnyObjectByType<UserCharacter>();
            if (userChar != null)
            {
                localPlayer = userChar.transform;
                $"[NPC] 로컬 플레이어 발견: {localPlayer.name}".DLog();
            }
        }



        private void OnTriggerEnter2D(Collider2D other)
        {
            $"[NPC] {npcData?.npcName} - Trigger Enter: {other.name}".DLog();
            if (IsLocalPlayer(other))
            {
                isLocalPlayerInRange = true;
                OnPlayerEnterRange();
            }
        }
        private void OnTriggerExit2D(Collider2D other)
        {
            if (IsLocalPlayer(other))
            {
                isLocalPlayerInRange = false;
                OnPlayerExitRange();
            }
        }

        private bool IsLocalPlayer(Collider2D collider)
        {
            var userChar = collider.GetComponent<UserCharacter>();
            return userChar != null && collider.transform == localPlayer;
        }

        protected virtual void OnPlayerEnterRange()
        {
            $"[NPC] {npcData?.npcName} - 플레이어 범위 진입".DLog();
            var userCharLoco = localPlayer?.GetComponent<UserCharLoco>();
            if (userCharLoco != null)
            {
                userCharLoco.RegisterInteractable(this);
            }
        }

        protected virtual void OnPlayerExitRange()
        {
            $"[NPC] {npcData?.npcName} - 플레이어 범위 이탈".DLog();

            var userCharLoco = localPlayer?.GetComponent<UserCharLoco>();
            if (userCharLoco != null)
            {
                userCharLoco.UnregisterInteractable(this);
            }
        }
        public override bool CanInteract()
        {
            var result = npcData != null && !isInteracting && isLocalPlayerInRange;
            $"[NPC] {npcData?.npcName} - CanInteract 체크: npcData={npcData != null}, isInteracting={isInteracting}, isLocalPlayerInRange={isLocalPlayerInRange}, 결과={result}".DLog();
            return result;
        }

        public override string GetInteractionText()
        {
            if (npcData == null) return "";

            return npcData.npcType switch
            {
                NPCType.Trade => $"[E] {npcData.npcName}과 거래하기",
                NPCType.Healer => $"[E] {npcData.npcName}과 치료받기",
                NPCType.Blacksmith => $"[E] {npcData.npcName}과 제작 의뢰",
                NPCType.TalkOnly => $"[E] {npcData.npcName}과 대화하기",
                NPCType.QuestGiver => $"[E] {npcData.npcName}과 대화하기",
                _ => $"[E] 상호작용"
            };

        }

        protected override void OnInteractLocal(InteractionEventArgs args)
        {
            if (!CanInteract()) return;

            isInteracting = true;
            args.Interactor?.GetComponent<UserCharLoco>()?.SetJumpEnabled(false);
            $"[NPC] {npcData.npcName} - 상호작용 시작".DLog();

            if (DialogManager.Shared == null)
            {
                $"[NPC] DialogManager 없음 - 상호작용 리셋".DWarnning();
                EndInteraction();
                return;
            }

            StartDialog();
        }


        protected virtual void OpenTradeMenu(InteractionEventArgs args)
        {
            $"[NPC] {npcData.npcName} 상점 열기".DLog();
            EndInteraction();
        }

        protected virtual void OpenQuestMenu(InteractionEventArgs args)
        {
            $"[NPC] {npcData.npcName} 퀘스트 메뉴".DLog();
            EndInteraction();
        }

        protected virtual void OpenHealMenu(InteractionEventArgs args)
        {
            $"[NPC] {npcData.npcName} 치료 메뉴".DLog();
            EndInteraction();
        }

        protected virtual void OpenBlacksmithMenu(InteractionEventArgs args)
        {
            $"[NPC] {npcData.npcName} 대장간 메뉴".DLog();
            EndInteraction();
        }

        protected virtual void OpenBankMenu(InteractionEventArgs args)
        {
            $"[NPC] {npcData.npcName} 은행 메뉴".DLog();
            EndInteraction();
        }

        protected virtual void StartDialog()
        {
            if (DialogManager.Shared == null)
            {
                "[NPC] DialogManager가 없습니다".DError();
                EndInteraction();
                return;
            }

            $"[NPC] {npcData.npcName} 대화 시작 (타입: {npcData.npcType})".DLog();

            // ✅ DialogData 로드
            var dialogData = LoadDialogData();
            if (dialogData == null)
            {
                $"[NPC] {npcData.npcName}의 DialogData가 없습니다".DWarnning();
                EndInteraction();
                return;
            }

            DialogManager.Shared.StartDialog(dialogData, OnChoiceSelected,OnDialogComplete);
        }
        private void OnDialogComplete()
        {
            $"[NPC] {npcData.npcName} 대화 완료".DLog();
            currentInteractor?.GetComponent<UserCharLoco>()?.SetJumpEnabled(true);
            EndInteraction();
        }
        private void OnChoiceSelected(int choiceIndex, string choiceId)
        {
            HandleChoiceAction(choiceId);
        }
        private void HandleChoiceAction(string choiceId)
        {
            this.DLog($"선택 행동 : {choiceId}");
        }
        /// <summary>
        /// NPC ID와 타입에 따른 DialogData 로드
        /// </summary>
        private DialogData LoadDialogData()
        {
            // 임시: 기본 대화 생성
            return new DialogData
            {
                npcId = npcData.npcId,
                npcName = npcData.npcName,
                speakerIconkey = npcData.portaitSpriteKey,
                nodes = new List<DialogNode>
                {
                    new DialogNode
                    {
                        nodeId = 0,
                        dialogText = GetDefaultDialogText(),
                        choices = GetDefaultChoices() // ✅ NPC 타입별 선택지
                    }
                }
            };

        }

        private string GetDefaultDialogText()
        {
            return npcData.npcType switch
            {
                NPCType.Trade => $"{npcData.npcName} 물건들은 잠경촌에서 최고라고!",
                NPCType.Healer => $"언제든 쉬다 가십쇼.. 건강이 우선입니다.",
                NPCType.Blacksmith => $"어떤 물건이든 만든 사람을 생각하면서 쓰라고!",
                NPCType.Banker => $"{npcData.npcName} 은행입니다. 무엇을 도와드릴까요?",
                NPCType.QuestGiver => $"헌터님! 부탁이 있습니다.",
                NPCType.Obstacle => "오래된 비석처럼 보인다. '\n' 누가 지은걸까.",
                _ => $"안녕하세요, {npcData.npcName}입니다."
            };
        }


        private List<DialogChoice> GetDefaultChoices()
        {
            var choices = new List<DialogChoice>();

            switch (npcData.npcType)
            {
                case NPCType.Trade:
                    choices.Add(new DialogChoice
                    {
                        choiceText = "거래하기",
                        nextNodeId = -1,
                        choiceId = "trade"
                    });
                    choices.Add(new DialogChoice
                    {
                        choiceText = "대화 종료",
                        nextNodeId = -1,
                        choiceId = "exit" 
                    });
                    break;

                case NPCType.Blacksmith:
                    choices.Add(new DialogChoice
                    {
                        choiceText = "장비 수리 좀 할게요",
                        nextNodeId = -1,
                        choiceId = "craft" 
                    });
                    choices.Add(new DialogChoice
                    {
                        choiceText = "다음에 올게요",
                        nextNodeId = -1,
                        choiceId = "exit" 
                    });
                    break;

                case NPCType.Healer:
                    choices.Add(new DialogChoice
                    {
                        choiceText = "치료받기",
                        nextNodeId = -1,
                        choiceId = "heal" 
                    });
                    choices.Add(new DialogChoice
                    {
                        choiceText = "대화 종료",
                        nextNodeId = -1,
                        choiceId = "exit" 
                    });
                    break;

                case NPCType.Banker:
                    choices.Add(new DialogChoice
                    {
                        choiceText = "보관하기",
                        nextNodeId = -1,
                        choiceId = "bank"
                    });
                    choices.Add(new DialogChoice
                    {
                        choiceText = "대화 종료",
                        nextNodeId = -1,
                        choiceId = "exit" 
                    });
                    break;

                case NPCType.QuestGiver:
                    choices.Add(new DialogChoice
                    {
                        choiceText = "퀘스트 보기",
                        nextNodeId = -1,
                        choiceId = "quest" 
                    });
                    choices.Add(new DialogChoice
                    {
                        choiceText = "대화 종료",
                        nextNodeId = -1,
                        choiceId = "exit" 
                    });
                    break;

                default:
                    choices.Add(new DialogChoice
                    {
                        choiceText = "확인",
                        nextNodeId = -1,
                        choiceId = "confirm"
                    });
                    break;
            }

            return choices;
        }
        public virtual void EndInteraction()
        {
            isInteracting = false;
            ClearInteractor();

            $"[NPC] {npcData.npcName} 상호작용 종료".DLog();
        }


        protected virtual void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interactionRange);

            if (Application.isPlaying && localPlayer != null && isLocalPlayerInRange)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position + Vector3.up * 2, localPlayer.position + Vector3.up * 2);
            }
        }
    }
}