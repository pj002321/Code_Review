using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Hunt
{
    public class CharacterCreateController : MonoBehaviourSingleton<CharacterCreateController>
    {
        #region Serialized Fields
        [Header("Character Slots")]
        [SerializeField] private List<CharacterInfoField> characterInfoFields;
        [SerializeField] private List<GenerateCharInfoField> newCharacterInfoFields;

        [Header("UI Panels")]
        [SerializeField] private GenerateCharPanel generationcharacterPanel;
        [SerializeField] private UserCharacterPanel userCharacterPanel;

        [Header("Navigation Buttons")]
        [SerializeField] private Button nextButton;
        [SerializeField] private Button prevButton;
        #endregion

        #region Private Fields
        private string currentChannelName;
        private int currentCharacterCount;
        private int currentGenerationCharacterIndex = 0;
        private List<CharacterModel> cachedCharacters = new List<CharacterModel>();
        private readonly Dictionary<string, List<CharacterModel>> channelCharacterCache = new Dictionary<string, List<CharacterModel>>();
        #endregion

        #region Properties
        protected override bool DontDestroy => false;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            InitializeButtons();
            
        }

        private async void OnEnable()
        {
            await UniTask.Yield(PlayerLoopTiming.PostLateUpdate);
            userCharacterPanel.gameObject.SetActive(false);
            ResetSelectionState();
        }

        private void OnDisable()
        {
            ResetSelectionState();
        }

        protected override void OnDestroy()
        {
            CleanupButtons();
            base.OnDestroy();
        }
        #endregion

        #region Initialization
        private void InitializeButtons()
        {
            nextButton.onClick.AddListener(() => OnShowChracterInfo(1));
            prevButton.onClick.AddListener(() => OnShowChracterInfo(-1));
        }

        private void CleanupButtons()
        {
            nextButton.onClick.RemoveAllListeners();
            prevButton.onClick.RemoveAllListeners();
        }
        #endregion

        /// <summary>
        /// 채널 필드를 클릭할 때 업데이트 되는 캐릭터 슬롯 필드들의 정보입니다.
        /// </summary>
        /// <param name="channelName">채널 이름</param>
        /// <param name="characterCount">해당 채널에서 보유한 캐릭터 수</param>
        public void UpdateCharacterSlots(string channelName, int characterCount)
        {
            UpdateChannelContext(channelName, characterCount);

            if (!ValidateCharacterFields()) return;

            var charactersToBind = LoadCharactersFromCache(channelName, characterCount);
            UpdateCachedCharacters(charactersToBind);

            int bindCount = charactersToBind?.Count ?? characterCount;
            BindCharactersToFields(bindCount);
        }

        /// <summary>
        /// 서버에서 받은 채널별 캐릭터 리스트를 캐시에 저장합니다.
        /// UI 바인딩은 채널 클릭 시 UpdateCharacterSlots에서 처리됩니다.
        /// </summary>
        /// <param name="channelName">채널 이름</param>
        /// <param name="characters">캐릭터 모델 리스트</param>
        public void OnRecvCharacterList(string channelName, List<CharacterModel> characters)
        {
            if (characters == null)
            {
                "[Character] OnRecvCharacterList - characters is null".DLog();
                return;
            }

            if (string.IsNullOrEmpty(channelName))
            {
                "[Character] OnRecvCharacterList - Invalid channel name".DLog();
                return;
            }

            channelCharacterCache[channelName] = characters;
            $"[Character] Cached characters - Channel: {channelName}, Count: {characters.Count}".DLog();
        }

        /// <summary>
        /// 선택한 캐릭터 필드 영역에 대한 처리입니다.
        /// 캐릭터가 이미 존재한다면 하이라이트 효과 처리와 키 값을 통해 필요한 정보를 로드합니다.
        /// 공용으로 표시되는 패널에 선택된 캐릭터의 정보를 업데이트하게 됩니다.
        /// </summary>
        /// <param name="selected">선택된 캐릭터 필드 영역</param>
        public async void OnSelectCharacterField(CharacterInfoField selected)
        {
            if (selected == null) return;

            DeselectAllFields();
            selected.HightlightField(true);

            if (selected.HasCharacterData)
            {
                await UpdateUserCharacterPanel(selected.CurrentModel);
            }
        }
        
        #region Character Generation
        /// <summary>
        /// 생성할 캐릭터 정보를 prev/next 버튼으로 전환합니다.
        /// </summary>
        /// <param name="indexOffset">이동할 인덱스 오프셋 (-1: 이전, 1: 다음)</param>
        public void OnShowChracterInfo(int indexOffset)
        {
            if (!ValidateGenerationFields()) return;

            UpdateCurrentGenerationIndex(indexOffset);
            UpdateGenerationFieldsVisibility();
            UpdateGenerationPanelContent();
            UpdateGenerationPanelButtons();
        }

        /// <summary>
        /// 새로운 캐릭터를 생성합니다.
        /// </summary>
        /// <param name="profession">생성할 캐릭터의 직업</param>
        public void OnCreateNewCharacter(ClassType profession)
        {
            $"[Create-Character] 캐릭터 생성 시작 - 직업: {profession}".DLog();

            if (!ValidateGenerationFieldIndex())
            {
                "❌ [Create-Character] 유효하지 않은 캐릭터 Index".DError();
                return;
            }

            var generationField = newCharacterInfoFields[currentGenerationCharacterIndex];

            int emptySlotIndex = FindEmptyCharacterSlot();
            if (emptySlotIndex == -1)
            {
                "❌ [Create-Character] 사용 가능한 슬롯이 없습니다".DLog();
                return;
            }

            $"[Create-Character] 빈 슬롯 찾음: {emptySlotIndex}".DLog();

            // TODO: 서버 통신으로 교체
            // var response = await NetworkManager.Shared.CreateCharacterAsync(...)
            
            CharacterModel newModel = CreateCharacterModel(generationField);
            characterInfoFields[emptySlotIndex].Bind(newModel);
            UpdateCharacterCache(newModel);
            OnSelectCharacterField(characterInfoFields[emptySlotIndex]);

            $"✅ [Create-Character] 캐릭터 생성 완료: {newModel.name}".DLog();
        }
        #endregion

        #region Public API - Query Methods
        /// <summary>
        /// 특정 인덱스의 CharacterInfoField가 캐릭터 데이터를 가지고 있는지 확인합니다.
        /// </summary>
        public bool HasCharacterDataAt(int index)
        {
            if (index < 0 || index >= characterInfoFields.Count) return false;
            if (characterInfoFields[index] == null) return false;
            return characterInfoFields[index].HasCharacterData;
        }

        /// <summary>
        /// 특정 인덱스의 CharacterInfoField에서 CharacterModel을 가져옵니다.
        /// </summary>
        public CharacterModel GetCharacterModelAt(int index)
        {
            if (index < 0 || index >= characterInfoFields.Count) return null;
            if (characterInfoFields[index] == null) return null;
            return characterInfoFields[index].CurrentModel;
        }
        #endregion

        #region Character Slots
        private void UpdateChannelContext(string channelName, int characterCount)
        {
            currentChannelName = channelName;
            currentCharacterCount = characterCount;
            $"[Character] UpdateCharacterSlots - Channel: {channelName}, Count: {characterCount}".DLog();
        }

        private bool ValidateCharacterFields()
        {
            if (characterInfoFields == null || characterInfoFields.Count == 0)
            {
                "[Character] characterInfoField is null or empty".DLog();
                return false;
            }
            return true;
        }

        private List<CharacterModel> LoadCharactersFromCache(string channelName, int expectedCount)
        {
            if (string.IsNullOrEmpty(channelName)) return null;

            if (channelCharacterCache.TryGetValue(channelName, out var cached))
            {
                if (cached.Count != expectedCount)
                {
                    $"⚠️ [Character] Count mismatch - Server: {expectedCount}, Cache: {cached.Count}".DLog();
                }
                else
                {
                    $"✅ [Character] Loaded from cache: {channelName}, Count: {cached.Count}".DLog();
                }
                return cached;
            }

            $"⚠️ [Character] No cache for '{channelName}'. Using server count: {expectedCount}".DLog();
            return null;
        }

        private void UpdateCachedCharacters(List<CharacterModel> characters)
        {
            cachedCharacters.Clear();
            if (characters != null)
            {
                cachedCharacters.AddRange(characters);
            }
        }

        private void BindCharactersToFields(int bindCount)
        {
            for (int i = 0; i < characterInfoFields.Count; i++)
            {
                var field = characterInfoFields[i];
                if (field == null) continue;

                if (i < bindCount && i < cachedCharacters.Count && cachedCharacters[i] != null)
                {
                    field.Bind(cachedCharacters[i]);
                }
                else if (i < bindCount)
                {
                    if (!field.HasCharacterData)
                    {
                        field.InitField(true);
                    }
                }
                else
                {
                    ResetEmptySlot(field);
                }
            }
        }

        private void ResetEmptySlot(CharacterInfoField field)
        {
            field.InitField(false);
            field.SetLevelFieldValue(0);
            field.SetNameFieldValue(string.Empty);
            field.SetSavePointFieldValie(string.Empty);
            userCharacterPanel.gameObject.SetActive(false);
        }
        #endregion

        #region Character Selection
        private void DeselectAllFields()
        {
            if (characterInfoFields == null) return;

            foreach (var field in characterInfoFields)
            {
                if (field == null) continue;
                field.HightlightField(false);
            }
        }

        private async UniTask UpdateUserCharacterPanel(CharacterModel model)
        {
            if (model == null || userCharacterPanel == null) return;

            SaveCharacterToGameSession(model);

            string illustKey = BindKeyConst.GetIllustKeyByProfession(model.classtype);

            await userCharacterPanel.HandleUpdateConfig(
                level: model.level,
                name: model.name,
                stats: model.stats,
                illustKey: illustKey,
                mapId: model.mapId,
                characterProfession: model.classtype
            );

            $"✅ [Character] Updated panel: {model.name} (Level: {model.level})".DLog();
        }

        private void SaveCharacterToGameSession(CharacterModel model)
        {
            var simpleChar = GameSession.Shared.CharacterInfos?.Find(c => c.Name == model.name);

            if (simpleChar != null)
            {
                GameSession.Shared.SelectCharacter(simpleChar);
                $"[Character] Saved to GameSession: {simpleChar.Name} (CharId: {simpleChar.CharId})".DLog();
            }
            else
            {
                GameSession.Shared.SelectCharacterModel(model);
                $"⚠ [Character] SimpleCharacterInfo not found: {model.name}".DError();
            }
        }
        #endregion

        #region Character Generation UI
        private bool ValidateGenerationFields()
        {
            return newCharacterInfoFields != null && newCharacterInfoFields.Count > 0;
        }

        private void UpdateCurrentGenerationIndex(int offset)
        {
            currentGenerationCharacterIndex += offset;
            currentGenerationCharacterIndex = Mathf.Clamp(
                currentGenerationCharacterIndex,
                0,
                newCharacterInfoFields.Count - 1
            );
        }

        private void UpdateGenerationFieldsVisibility()
        {
            for (int i = 0; i < newCharacterInfoFields.Count; i++)
            {
                var field = newCharacterInfoFields[i];
                if (field == null) continue;

                bool active = (i == currentGenerationCharacterIndex);
                if (field.gameObject.activeSelf != active)
                {
                    field.gameObject.SetActive(active);
                }
            }
        }

        private void UpdateGenerationPanelContent()
        {
            var currentField = newCharacterInfoFields[currentGenerationCharacterIndex];
            if (currentField == null || generationcharacterPanel == null) return;

            float[] stats = currentField.stats != null && currentField.stats.Count >= 5
                ? currentField.stats.ToArray()
                : new float[] { 0.5f, 0.5f, 0.5f, 0.5f, 0.5f };

            generationcharacterPanel.OnSetFieldValue(currentField.storyString, stats);

            $"[Character] Updated panel - Index: {currentGenerationCharacterIndex}, Name: {currentField.characterName}".DLog();
        }

        private void UpdateGenerationPanelButtons()
        {
            if (prevButton != null)
            {
                prevButton.interactable = currentGenerationCharacterIndex > 0;
            }

            if (nextButton != null)
            {
                nextButton.interactable = currentGenerationCharacterIndex < newCharacterInfoFields.Count - 1;
            }
        }
        #endregion

        #region Character Creation
        private bool ValidateGenerationFieldIndex()
        {
            return newCharacterInfoFields != null &&
                   currentGenerationCharacterIndex >= 0 &&
                   currentGenerationCharacterIndex < newCharacterInfoFields.Count;
        }

        private int FindEmptyCharacterSlot()
        {
            if (characterInfoFields == null || characterInfoFields.Count == 0)
            {
                "❌ [Create-Character] characterInfoFields가 null이거나 비어있음".DError();
                return -1;
            }

            for (int i = 0; i < characterInfoFields.Count; i++)
            {
                if (characterInfoFields[i] != null && !characterInfoFields[i].HasCharacterData)
                {
                    return i;
                }
            }

            return -1;
        }

        private CharacterModel CreateCharacterModel(GenerateCharInfoField generationField)
        {
            List<Hunt.Game.StatInfo> statInfoList = ConvertFloatStatsToStatInfo(generationField.stats);

            return new CharacterModel
            {
                name = generationField.characterName,
                classtype = generationField.professionType,
                level = 1,
                mapId = 1,
                stats = statInfoList,
                charId = 0,
                worldId = 0
            };
        }

        private List<Hunt.Game.StatInfo> ConvertFloatStatsToStatInfo(List<float> floatStats)
        {
            List<Hunt.Game.StatInfo> statInfoList = new List<Hunt.Game.StatInfo>();

            if (floatStats == null || floatStats.Count < 5)
            {
                "⚠️ [Character] 스탯이 5개 미만입니다. 기본값 사용".DLog();
                floatStats = new List<float> { 0.5f, 0.5f, 0.5f, 0.5f, 0.5f };
            }

            // 스탯 타입 순서: 0=ATT, 1=DEF, 2=SDP, 3=LUK, 4=AGI
            for (int i = 0; i < 5 && i < floatStats.Count; i++)
            {
                var statInfo = new Hunt.Game.StatInfo
                {
                    Type = (uint)i,
                    Point = (ulong)(floatStats[i] * 100) // float를 정수로 변환
                };
                statInfoList.Add(statInfo);
            }

            return statInfoList;
        }
        #endregion

        private void UpdateCharacterCache(CharacterModel newModel)
        {
            if (newModel == null)
            {
                "[Create-Character] newModel이 null".DError();
                return;
            }

            if (string.IsNullOrEmpty(currentChannelName))
            {
                "[Create-Character] 채널명이 없음".DError();
                return;
            }

            if (!channelCharacterCache.ContainsKey(currentChannelName))
            {
                channelCharacterCache[currentChannelName] = new List<CharacterModel>();
            }

            channelCharacterCache[currentChannelName].Add(newModel);
            cachedCharacters.Add(newModel);

            $"[Create-Character] 캐시 업데이트 완료 - Channel: {currentChannelName}, Total: {channelCharacterCache[currentChannelName].Count}".DLog();
        }

        private void ResetSelectionState()
        {
            if (characterInfoFields == null) return;

            foreach (var field in characterInfoFields)
            {
                if (field == null) continue;
                field.HightlightField(false);
            }
        }
    }
}
