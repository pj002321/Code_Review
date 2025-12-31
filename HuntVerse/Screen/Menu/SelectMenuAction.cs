using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Hunt.Net;

namespace Hunt
{
    
    public class SelectMenuAction : MonoBehaviour
    {
        [Header("Menu Items")]
        [SerializeField] private List<GameObject> menuFields = new List<GameObject>();
        private string selectedBoolName = ResourceKeyConst.Ka_isActive;
        private int initialIndex = 0;

        private int currentIndex = -1;
        private readonly List<Animator> animators = new();
        private readonly List<Button> buttons = new();

        private void Awake()
        {
            CacheComponents();
            for (int i = 0; i < menuFields.Count; i++)
            {
                var field = menuFields[i];
                if (!field) continue;

                var helper = field.GetComponent<SelectMenuField>();
                if (!helper) helper = field.AddComponent<SelectMenuField>();
                helper.Bind(this, i);
            }
        }

        private void OnEnable()
        {
           
            SetAllSelected(false);
            if (menuFields.Count > 0)
            {
                int init = Mathf.Clamp(initialIndex, 0, menuFields.Count - 1);
                SetIndex(init, playSound: false);
            }
        }

        private void Update()
        {
            if (menuFields.Count == 0) return;

            // Ű���� Ž�� (��/W, ��/S)
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            {
                Move(-1);
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            {
                Move(+1);
            }

            // Enter �Ǵ� KeypadEnter�� ���� ����
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                SubmitCurrent();
            }
        }

        private void CacheComponents()
        {
            animators.Clear();
            buttons.Clear();

            foreach (var go in menuFields)
            {
                if (!go)
                {
                    animators.Add(null);
                    buttons.Add(null);
                    continue;
                }

                animators.Add(go.GetComponentInChildren<Animator>(true));
                buttons.Add(go.GetComponentInChildren<Button>(true));
            }
        }

        private void SetAllSelected(bool value)
        {
            for (int i = 0; i < animators.Count; i++)
            {
                if (animators[i] && !string.IsNullOrEmpty(selectedBoolName))
                    animators[i].SetBool(selectedBoolName, value);
            }
        }

        private void Move(int delta)
        {
            int next = currentIndex < 0 ? 0 : (currentIndex + delta + menuFields.Count) % menuFields.Count;
            SetIndex(next);
        }

        public void SetIndex(int index, bool playSound = true)
        {
            if (menuFields.Count == 0) return;
            index = Mathf.Clamp(index, 0, menuFields.Count - 1);
            if (currentIndex == index) return;

            if (currentIndex >= 0 && currentIndex < animators.Count)
            {
                if (animators[currentIndex] && !string.IsNullOrEmpty(selectedBoolName))
                    animators[currentIndex].SetBool(selectedBoolName, false);
            }

            currentIndex = index;

            if (animators[currentIndex] && !string.IsNullOrEmpty(selectedBoolName))
                animators[currentIndex].SetBool(selectedBoolName, true);
        }

        public void SubmitCurrent()
        {
            if (currentIndex < 0 || currentIndex >= buttons.Count) return;
            var btn = buttons[currentIndex];
            if (btn && btn.interactable)
            {
                btn.onClick?.Invoke();
            }
        }

        public void OnHovered(int index)
        {
            SetIndex(index);
        }

        public void OnClicked(int index)
        {
            SetIndex(index);
            SubmitCurrent();
        }


        public async void ReturnLogin()
        {
            // 1. MainMenu에서 생성된 DontDestroyOnLoad 객체들 정리
            CleanupMainMenuObjects();
            
            // 2. Boot 씬으로 이동
            if (SceneLoadHelper.Shared != null)
            {
                await SceneLoadHelper.Shared.LoadToLogOut();
            }
        }

        private void CleanupMainMenuObjects()
        {
            // MainMenu에서 생성된 DontDestroyOnLoad 객체들 찾아서 제거
            var allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            
            foreach (var obj in allObjects)
            {
                if (obj == null || obj.transform.parent != null)
                    continue;
                
                if (obj.scene.name == "DontDestroyOnLoad")
                {
                    // SceneLoadHelper, SystemBoot, NetworkManager, GameSession, ContentsDownloader, SteamManager는 유지
                    if (obj.GetComponent<SceneLoadHelper>() != null ||
                        obj.GetComponent<SystemBoot>() != null ||
                        obj.GetComponent<NetworkManager>() != null ||
                        obj.GetComponent<GameSession>() != null ||
                        obj.GetComponent<ContentsDownloader>() != null ||
                        obj.name.Contains("SteamManager"))
                    {
                        continue;
                    }
                    
                    // MainMenu에서 생성된 나머지 객체들 제거
                    Debug.Log($"[SelectMenuAction] MainMenu 객체 제거: {obj.name}");
                    Destroy(obj);
                }
            }
        }
    }
}
