using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Hunt;

namespace Hunt.Ed
{

    public class HuntPresetEditor : OdinEditorWindow
    {
        [MenuItem("Tools/Hunt/Open HuntPreset")]
        public static void OpenWindow()
        {
            var window = GetWindow<HuntPresetEditor>("Hunt Preset");
            window.minSize = new Vector2(900, 600);
            window.Show();
        }

        #region Editor State

        private List<GameObject> _availableActorPrefabs = new List<GameObject>();

        [HideInInspector]
        private CharacterFxPreset _selectedPreset;
        
        [ValueDropdown("GetAllPresets")]
        [OnValueChanged("OnPresetSelected")]
        [LabelText("액터 프리셋 선택")]
        [ShowInInspector]
        private CharacterFxPreset SelectedPresetProperty
        {
            get => _selectedPreset;
            set
            {
                _selectedPreset = value;
                OnPresetSelected();
            }
        }

        [ShowInInspector, ReadOnly]
        [LabelText("프리셋 경로")]
        private string _currentPresetPath;

        [VerticalGroup("Main/Left")]
        [BoxGroup("Main/Left/Actor", CenterLabel = true)]
        [LabelText("액터 프리팹 선택")]
        [ValueDropdown("_availableActorPrefabs")] // 목록 드롭다운
        [ShowInInspector]
        private GameObject TargetActorPrefabProperty
        {
            get => _targetActorPrefab;
            set
            {
                if (_targetActorPrefab != value)
                {
                    _targetActorPrefab = value;
                    OnActorPrefabSelected(); // 값 변경 시 즉시 호출
                }
            }
        }
        
        [SerializeField, HideInInspector]
        private GameObject _targetActorPrefab;

        // 기존 프로퍼티 대체 (호환성 유지용)
        private GameObject SelectedActorPrefab => _targetActorPrefab;
        
        // _lastTargetPrefab 및 CheckPrefabChange 제거 (Setter로 대체)

        #endregion

        #region Animation Timeline Editor

        [Title("Animation Timeline", "Select a clip to edit VFX timings", TitleAlignments.Centered)]
        [ShowIf("_selectedPreset")]
        [ValueDropdown("GetClipNames")]
        [OnValueChanged("OnClipSelected")]
        [LabelText("편집할 클립 선택")]
        [ShowInInspector]
        private string _selectedClipName;

        private AnimationClip _currentClip;
        private GameObject _previewInstance;
        private float _previewTime;
        private bool _isPlaying;
        private double _lastTime;
        
        // Preview Rendering
        private UnityEditor.PreviewRenderUtility _previewRenderUtility;
        private static readonly Vector3 PREVIEW_DIR = new Vector3(-0.2f, -0.5f, 1f);
        private const float PREVIEW_ZOOM = 2.5f;

        [ShowIf("CanEditTimeline")]
        [OnInspectorGUI]
        private void DrawTimelineGUI()
        {
            if (_currentClip == null) return;

            EditorGUILayout.Space(10);
            GUILayout.BeginVertical("box");

            // Timestamp Slider
            GUILayout.BeginHorizontal();
            GUILayout.Label("Time", GUILayout.Width(40));
            float newTime = EditorGUILayout.Slider(_previewTime, 0f, _currentClip.length);
            GUILayout.Label($"{_previewTime:F2}s / {_currentClip.length:F2}s", GUILayout.Width(80));
            GUILayout.EndHorizontal();

            if (Mathf.Abs(newTime - _previewTime) > 0.001f)
            {
                _previewTime = newTime;
                SampleAnimation();
            }

            // Play/Pause & Add VFX Buttons
            GUILayout.BeginHorizontal();
            
            if (GUILayout.Button(_isPlaying ? "Pause" : "Play"))
            {
                _isPlaying = !_isPlaying;
                _lastTime = EditorApplication.timeSinceStartup;
            }

            if (GUILayout.Button("Add Event Here", GUILayout.Height(30)))
            {
                AddEventAtCurrentTime();
            }
            
            if (_previewInstance != null && GUILayout.Button("Destroy Preview"))
            {
                DestroyPreviewInstance();
            }

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        private void Update()
        {
            if (_isPlaying && _currentClip != null)
            {
                float deltaTime = (float)(EditorApplication.timeSinceStartup - _lastTime);
                _lastTime = EditorApplication.timeSinceStartup;
                
                _previewTime += deltaTime;
                if (_previewTime > _currentClip.length)
                {
                    _previewTime = 0;
                }
                
                SampleAnimation();
                Repaint();
            }
        }

        private void SampleAnimation()
        {
            if (_previewInstance == null && _selectedPreset != null && _selectedPreset.characterPrefab != null)
            {
                // Try to find existing instance in scene to avoid spamming
                // But simplified: User clicks Create
            }

            if (_previewInstance != null && _currentClip != null)
            {
                _currentClip.SampleAnimation(_previewInstance, _previewTime);
            }
        }

        private void CreatePreviewInstance()
        {
            if (_selectedPreset?.characterPrefab != null)
            {
                DestroyPreviewInstance();
                InitPreviewRenderer();
                
                _previewInstance = Instantiate(_selectedPreset.characterPrefab);
                _previewInstance.name = "[HuntPresetEditor_Preview] " + _selectedPreset.characterPrefab.name;
                _previewInstance.transform.position = Vector3.zero;
                _previewInstance.hideFlags = HideFlags.HideAndDontSave; // Hide from Scene and don't save
                
                SampleAnimation();
            }
        }

        private void DestroyPreviewInstance()
        {
            if (_previewInstance != null)
            {
                DestroyImmediate(_previewInstance);
                _previewInstance = null;
            }
        }

        private void AddEventAtCurrentTime()
        {
            if (string.IsNullOrEmpty(_selectedClipName)) return;

            var clipData = _selectedPreset.clipFxDataList.FirstOrDefault(c => c.clipName == _selectedClipName);
            if (clipData == null)
            {
                clipData = new ClipFxData { clipName = _selectedClipName };
                _selectedPreset.clipFxDataList.Add(clipData);
            }

            clipData.fxTimings.Add(new FxTiming
            {
                timeInSeconds = _previewTime,
                vfxType = VfxType.None,
                audioType = AudioType.SFX_HOVER
            });
            
            EditorUtility.SetDirty(_selectedPreset);
            // Focus on the newly added item logic could be added here
        }

        private List<string> GetClipNames()
        {
            if (_selectedPreset == null || _selectedPreset.characterPrefab == null) return new List<string>();
            
            // From Data List (Editable)
            var names = _selectedPreset.clipFxDataList.Select(c => c.clipName).ToList();
            
            // Also include from Animator if not yet added
             var animator = _selectedPreset.characterPrefab.GetComponent<Animator>();
             if (animator == null) animator = _selectedPreset.characterPrefab.GetComponentInChildren<Animator>();
             
             if (animator != null && animator.runtimeAnimatorController != null)
             {
                 foreach(var clip in animator.runtimeAnimatorController.animationClips)
                 {
                     if (!names.Contains(clip.name)) names.Add(clip.name);
                 }
             }
             
             return names;
        }

        private void OnClipSelected()
        {
            _previewTime = 0;
            if (_selectedPreset?.characterPrefab == null || string.IsNullOrEmpty(_selectedClipName))
            {
                _currentClip = null;
                return;
            }

            var animator = _selectedPreset.characterPrefab.GetComponent<Animator>();
             if (animator == null) animator = _selectedPreset.characterPrefab.GetComponentInChildren<Animator>();
             
             if (animator != null && animator.runtimeAnimatorController != null)
             {
                 _currentClip = animator.runtimeAnimatorController.animationClips.FirstOrDefault(c => c.name == _selectedClipName);
             }
             
             if (_currentClip != null)
             {
                 SampleAnimation();
             }
        }
        
        private bool CanEditTimeline => _selectedPreset != null && !string.IsNullOrEmpty(_selectedClipName);
        
        private void OnDestroy()
        {
            DestroyPreviewInstance();
            
            if (_previewRenderUtility != null)
            {
                _previewRenderUtility.Cleanup();
                _previewRenderUtility = null;
            }
        }
        
        private void InitPreviewRenderer()
        {
            if (_previewRenderUtility == null)
            {
                _previewRenderUtility = new UnityEditor.PreviewRenderUtility();
                
                // Camera setup
                _previewRenderUtility.camera.transform.position = new Vector3(0, 1, -3);
                _previewRenderUtility.camera.transform.rotation = Quaternion.identity;
                _previewRenderUtility.camera.fieldOfView = 60f;
                _previewRenderUtility.camera.nearClipPlane = 0.1f;
                _previewRenderUtility.camera.farClipPlane = 100f;
                _previewRenderUtility.camera.clearFlags = UnityEngine.CameraClearFlags.Skybox;
                
                // Lighting setup
                _previewRenderUtility.lights[0].intensity = 1.4f;
                _previewRenderUtility.lights[0].type = LightType.Directional;
                _previewRenderUtility.lights[0].transform.rotation = Quaternion.Euler(40f, 40f, 0);
                _previewRenderUtility.lights[1].intensity = 0.8f;
                _previewRenderUtility.lights[1].type = LightType.Directional;
                _previewRenderUtility.lights[1].transform.rotation = Quaternion.Euler(-10f, -30f, 0);
                
                Debug.Log("[HuntPresetEditor] PreviewRenderUtility initialized");
            }
        }
        
        private void RenderPreview(Rect previewRect)
        {
            if (_previewInstance == null || _previewRenderUtility == null) 
            {
                Debug.LogWarning("[HuntPresetEditor] Preview instance or render utility is null");
                return;
            }
            
            // Check if this is a 2D sprite character
            var spriteRenderers = _previewInstance.GetComponentsInChildren<SpriteRenderer>();
            if (spriteRenderers.Length > 0 && spriteRenderers[0].sprite != null)
            {
                // Render 2D sprite directly
                Render2DSprite(previewRect, spriteRenderers);
                return;
            }
            
            // Otherwise, render 3D mesh
            Render3DMesh(previewRect);
        }
        
        private void Render2DSprite(Rect previewRect, SpriteRenderer[] spriteRenderers)
        {
            // Draw a simple background
            EditorGUI.DrawRect(previewRect, new Color(0.2f, 0.2f, 0.2f));
            
            // Find the main sprite renderer (usually the first one)
            var mainSprite = spriteRenderers[0].sprite;
            if (mainSprite == null) return;
            
            // Calculate sprite rect to fit in preview while maintaining aspect ratio
            Texture2D spriteTexture = mainSprite.texture;
            Rect spriteRect = mainSprite.rect;
            
            float spriteAspect = spriteRect.width / spriteRect.height;
            float previewAspect = previewRect.width / previewRect.height;
            
            Rect drawRect = previewRect;
            if (spriteAspect > previewAspect)
            {
                // Sprite is wider
                float height = previewRect.width / spriteAspect;
                drawRect.y += (previewRect.height - height) / 2;
                drawRect.height = height;
            }
            else
            {
                // Sprite is taller
                float width = previewRect.height * spriteAspect;
                drawRect.x += (previewRect.width - width) / 2;
                drawRect.width = width;
            }
            
            // Calculate UV coordinates from sprite rect
            Rect uv = new Rect(
                spriteRect.x / spriteTexture.width,
                spriteRect.y / spriteTexture.height,
                spriteRect.width / spriteTexture.width,
                spriteRect.height / spriteTexture.height
            );
            
            // Check if sprite is flipped
            if (spriteRenderers[0].flipX)
            {
                uv.x += uv.width;
                uv.width = -uv.width;
            }
            
            // Draw the sprite
            GUI.DrawTextureWithTexCoords(drawRect, spriteTexture, uv);
        }
        
        private void Render3DMesh(Rect previewRect)
        {
            _previewRenderUtility.BeginPreview(previewRect, GUIStyle.none);
            
            // Position camera to frame character
            Bounds bounds = GetBounds(_previewInstance);
            float dist = bounds.size.magnitude * PREVIEW_ZOOM;
            Vector3 cameraPos = bounds.center - PREVIEW_DIR.normalized * dist;
            
            _previewRenderUtility.camera.transform.position = cameraPos;
            _previewRenderUtility.camera.transform.LookAt(bounds.center);
            
            // Manually draw all renderers
            var renderers = _previewInstance.GetComponentsInChildren<Renderer>();
            int drawnMeshCount = 0;
            
            foreach (var renderer in renderers)
            {
                if (renderer != null && renderer.enabled)
                {
                    Mesh mesh = GetMeshFromRenderer(renderer);
                    if (mesh != null)
                    {
                        for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                        {
                            if (renderer.sharedMaterials[i] != null)
                            {
                                _previewRenderUtility.DrawMesh(
                                    mesh,
                                    renderer.transform.localToWorldMatrix,
                                    renderer.sharedMaterials[i],
                                    i
                                );
                                drawnMeshCount++;
                            }
                        }
                    }
                }
            }
            
            if (drawnMeshCount == 0)
            {
                Debug.LogWarning($"[HuntPresetEditor] No meshes drawn! Renderer count: {renderers.Length}");
            }
            
            // Render
            _previewRenderUtility.camera.Render();
            
            Texture resultTexture = _previewRenderUtility.EndPreview();
            GUI.DrawTexture(previewRect, resultTexture, ScaleMode.ScaleToFit, false);
        }
        
        private Mesh GetMeshFromRenderer(Renderer renderer)
        {
            // Skip SpriteRenderer (2D sprites can't be rendered with DrawMesh)
            if (renderer is SpriteRenderer)
            {
                return null; // Silently skip, not an error
            }
            
            if (renderer is MeshRenderer meshRenderer)
            {
                var meshFilter = meshRenderer.GetComponent<MeshFilter>();
                var mesh = meshFilter != null ? meshFilter.sharedMesh : null;
                if (mesh == null)
                {
                    Debug.LogWarning($"[HuntPresetEditor] MeshRenderer has no mesh: {renderer.name}");
                }
                return mesh;
            }
            else if (renderer is SkinnedMeshRenderer skinnedMeshRenderer)
            {
                if (skinnedMeshRenderer.sharedMesh == null)
                {
                    Debug.LogWarning($"[HuntPresetEditor] SkinnedMeshRenderer has no sharedMesh: {renderer.name}");
                    return null;
                }
                
                // For SkinnedMeshRenderer, we need to bake the current pose
                Mesh bakedMesh = new Mesh();
                skinnedMeshRenderer.BakeMesh(bakedMesh);
                Debug.Log($"[HuntPresetEditor] Baked mesh for {renderer.name}, vertices: {bakedMesh.vertexCount}");
                return bakedMesh;
            }
            
            Debug.LogWarning($"[HuntPresetEditor] Unsupported renderer type: {renderer.GetType().Name} on {renderer.name}");
            return null;
        }
        
        private Bounds GetBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                    bounds.Encapsulate(renderers[i].bounds);
                return bounds;
            }
            return new Bounds(go.transform.position, Vector3.one);
        }

        #endregion

        #region LayOut

        [VerticalGroup("Main/Left")]
        [Button(ButtonSizes.Large), GUIColor(0.2f, 0.5f, 0.8f)]
        private void CreateNewPreset()
        {
            // 사용 예: 프리팹 선택 확인 (SelectedActorPrefab을 활용)
            if (SelectedActorPrefab == null)
            {
                // 프리팹이 선택되지 않았을 경우, 일단 빈 프리셋 생성 후 나중에 설정할 수도 있지만
                // 자동 네이밍을 위해 프리팹 선택을 유도하거나 기본 이름 사용
            }

            string defaultName = SelectedActorPrefab != null ? $"Preset_{SelectedActorPrefab.name}" : "NewCharacterFxPreset";
            string path = EditorUtility.SaveFilePanelInProject("새 프리셋 생성", defaultName, "asset", "프리셋을 저장할 위치를 선택하세요.");
            
            if (!string.IsNullOrEmpty(path))
            {
                // 이름 강제 규칙 적용 (선택 사항) - 사용자가 SavePanel에서 이름을 바꿀 수도 있음
                // 하지만 사용자가 규칙을 따르길 원하므로, 파일명을 파싱해서 Addressable 등록 시 활용

                var newPreset = ScriptableObject.CreateInstance<CharacterFxPreset>();
                if (SelectedActorPrefab != null)
                {
                    newPreset.characterPrefab = SelectedActorPrefab;
                    
                    // 자동 추출 실행
                    var animator = SelectedActorPrefab.GetComponent<Animator>();
                    if (animator == null) animator = SelectedActorPrefab.GetComponentInChildren<Animator>();
                    if (animator != null && animator.runtimeAnimatorController != null)
                    {
                         foreach (var clip in animator.runtimeAnimatorController.animationClips)
                        {
                            if (clip == null) continue;
                            newPreset.clipFxDataList.Add(new ClipFxData { clipName = clip.name });
                        }
                    }
                }

                AssetDatabase.CreateAsset(newPreset, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                _selectedPreset = newPreset;
                OnPresetSelected(); // 경로 업데이트

                // Addressable 자동 등록
                RegisterToAddressable(path);

                EditorUtility.DisplayDialog("생성 완료", $"새 프리셋이 생성되고 Addressable에 등록되었습니다:\n{path}", "확인");
            }
        }

        [HorizontalGroup("Main", Width = 300)]
        [VerticalGroup("Main/Left")]
        [OnInspectorGUI]
        private void DrawLeftPanelPreview()
        {
            // Auto-create preview instance if clip is selected but instance doesn't exist
            if (_currentClip != null && _previewInstance == null)
            {
                CreatePreviewInstance();
            }
            
            if (_previewInstance != null && _previewRenderUtility != null)
            {
                Rect previewRect = GUILayoutUtility.GetRect(300, 300);
                RenderPreview(previewRect);
            }
            else if (_selectedPreset != null)
            {
                EditorGUILayout.HelpBox("클립을 선택하세요.", MessageType.Info);
                GUILayoutUtility.GetRect(300, 60); // Reserve space
            }
        }
        
        [VerticalGroup("Main/Right")]
        [ShowIf("_selectedPreset")]
        [InlineEditor(InlineEditorObjectFieldModes.Hidden)]
        [ShowInInspector]
        private CharacterFxPreset SelectedPresetEditor
        {
            get => _selectedPreset;
            set => _selectedPreset = value;
        }

        [VerticalGroup("Main/Right")]
        [ShowIf("_selectedPreset")]
        [Button(ButtonSizes.Large), GUIColor(0.3f, 1f, 0.3f)]
        private void SavePreset()
        {
            if (_selectedPreset == null) return;

            EditorUtility.SetDirty(_selectedPreset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            // 저장 시에도 Addressable 등록 확인 (이름이 바뀌었거나 이동했을 수 있으므로)
            string path = AssetDatabase.GetAssetPath(_selectedPreset);
            RegisterToAddressable(path);
            
            EditorUtility.DisplayDialog("저장 완료", $"{_selectedPreset.name} 프리셋이 저장되었습니다.", "확인");
        }

        private void RegisterToAddressable(string assetPath)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) 
            {
                // 혹시 null이면 에셋 직접 로드 시도
                var guids = AssetDatabase.FindAssets("t:AddressableAssetSettings");
                if (guids.Length > 0)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    settings = AssetDatabase.LoadAssetAtPath<AddressableAssetSettings>(path);
                }

                if (settings == null)
                {
                    Debug.LogWarning("[HuntPresetEditor] Addressable Settings not found. Cannot register asset automatically.");
                    return;
                }
            }

            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid)) return;

            // 이미 등록되어 있는지 확인
            var existingEntry = settings.FindAssetEntry(guid);
            if (existingEntry != null)
            {
                // 이미 등록됨 -> 그룹 유지 (이동 금지)
                string fileName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
                
                // 이름 동기화 필요 시
                if (existingEntry.address != fileName)
                {
                    existingEntry.SetAddress(fileName);
                    EditorUtility.SetDirty(settings);
                    AssetDatabase.SaveAssets();
                    Debug.Log($"[HuntPresetEditor] Address updated: {fileName} (Group: {existingEntry.parentGroup.Name})");
                }
                return;
            }

            var group = settings.DefaultGroup; // 기본 그룹에 추가
            var entry = settings.CreateOrMoveEntry(guid, group);
            
            // 주소 설정: 파일명 (확장자 제외)
            string address = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            entry.SetAddress(address);
            
            // 라벨 추가 (선택 사항)
            // entry.SetLabel("CharacterFxPreset", true);
            
            // 변경 사항 저장 중요!
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();


        }

        [Button("Animator에서 클립 목록 자동 추출"), GUIColor(0.8f, 0.8f, 1f)]
        private void AutoExtractClipsFromAnimator()
        {
            if (_selectedPreset == null || _selectedPreset.characterPrefab == null)
            {
                EditorUtility.DisplayDialog("추출 실패", "캐릭터 프리팹을 먼저 설정하세요.", "확인");
                return;
            }

            var animator = _selectedPreset.characterPrefab.GetComponent<Animator>();
            if (animator == null)
            {
                animator = _selectedPreset.characterPrefab.GetComponentInChildren<Animator>();
            }

            if (animator == null || animator.runtimeAnimatorController == null)
            {
                EditorUtility.DisplayDialog("추출 실패", "Animator 사용 불가", "Animator 컴포년트 혹은 Controller 가 없습니다.");
                return;
            }

            var clips = animator.runtimeAnimatorController.animationClips;
            if (clips == null || clips.Length == 0)
            {
                return;
            }

            int added = 0;
            foreach (var clip in clips)
            {
                if (clip == null) continue;
                bool exists = _selectedPreset.clipFxDataList.Any(c => c.clipName == clip.name);
                if (!exists)
                {
                    _selectedPreset.clipFxDataList.Add(new ClipFxData
                    {
                        clipName = clip.name,
                        fxTimings = new List<FxTiming>()
                    });
                    added++;
                }
            }

            if (added > 0)
            {
                EditorUtility.SetDirty(_selectedPreset);
                EditorUtility.DisplayDialog("추출 완료", $"{added}개의 새로운 클립을 추가했습니다.", "확인");
            }
            else
            {
                // 이미 다 있는 경우 조용히 넘어가되, 수동 클릭일 땐 알려주는 게 좋음
               // EditorUtility.DisplayDialog("알림", "새로 추가할 클립이 없습니다.", "확인");
            }
        }

        #endregion

        #region Helper

        private IEnumerable<CharacterFxPreset> GetAllPresets()
        {
            var guids = AssetDatabase.FindAssets("t:CharacterFxPreset");
            var presets = new List<CharacterFxPreset>();

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var preset = AssetDatabase.LoadAssetAtPath<CharacterFxPreset>(path);
                if (preset != null)
                {
                    presets.Add(preset);
                }
            }

            return presets;
        }

        private void OnPresetSelected()
        {
            if (_selectedPreset != null)
            {
                _currentPresetPath = AssetDatabase.GetAssetPath(_selectedPreset);
                _targetActorPrefab = _selectedPreset.characterPrefab; // 프리셋 로드 시 프리팹 연동
            }
            else
            {
                _currentPresetPath = string.Empty;
                _targetActorPrefab = null;
            }
            _selectedClipName = string.Empty;
            _currentClip = null;
            DestroyPreviewInstance();
        }

        private void OnActorPrefabSelected()
        {
            if (_targetActorPrefab == null) return;

            // 이미 선택된 프리셋의 프리팹과 같다면 패스
            if (_selectedPreset != null && _selectedPreset.characterPrefab == _targetActorPrefab) return;

            Debug.Log($"[HuntPresetEditor] Searching preset for target: {_targetActorPrefab.name} (InstanceID: {_targetActorPrefab.GetInstanceID()})");

            // 해당 프리팹을 사용하는 프리셋이 있는지 검색
            string[] guids = AssetDatabase.FindAssets("t:CharacterFxPreset");
            Debug.Log($"[HuntPresetEditor] Found {guids.Length} total presets in project.");
            
            bool found = false;

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var preset = AssetDatabase.LoadAssetAtPath<CharacterFxPreset>(path);
                
                if (preset != null)
                {
                    // 비교 디버깅
                     string pName = preset.characterPrefab != null ? preset.characterPrefab.name : "null";
                     // Debug.Log($"Checking preset '{preset.name}' -> Linked Prefab: {pName}");

                    if (preset.characterPrefab == _targetActorPrefab)
                    {
                        _selectedPreset = preset;
                        OnPresetSelected(); // UI 갱신
                        Debug.Log($"[HuntPresetEditor] SUCCESS: Found matching preset '{preset.name}' at {path}");
                        found = true;
                        break;
                    }
                }
            }

            if (!found)
            {
                // 없으면 새 프리셋 생성을 위한 상태로 (기존 선택 해제)
                // 만약 사용자가 '기존 프리셋의 프리팹을 교체'하려는 목적이라면 이 동작이 방해가 될 수 있음.
                // 하지만 보통 1:1 관계이므로 로드해주는 게 편함.
                if (_selectedPreset != null && _selectedPreset.characterPrefab != _targetActorPrefab)
                {
                     // 기존 선택된 프리셋이 있는데 다른 프리팹을 골랐다 -> 새거 만들려는 의도? 아니면 잘못 눌렀나?
                     // 여기서는 '프리셋 찾기' 기능에 집중하여, 못 찾으면 null로 두거나 유지
                     // 유저 요청: "선택하면 불러와야 할 거 아냐" -> 없으면 안 불러오면 됨.
                     // 하지만 '기존게 남아있으면' 헷갈림.
                     _selectedPreset = null;
                     OnPresetSelected(); // 초기화
                     Debug.Log($"[HuntPresetEditor] No existing preset found for {_targetActorPrefab.name}. Ready to create new one.");
                }
            }
            
            // 자동 추출 (로드된 프리셋이 있거나, 없어서 새로 만들 예정이어도 프리팹은 선택된 상태)
            // _selectedPreset이 null이면 자동추출 불가 (데이터 담을 곳이 없음)
            // 따라서 Found일 때만 의미 있음.
            if (_selectedPreset != null)
            {
                AutoExtractClipsFromAnimator();
            }
        }

        [VerticalGroup("Main/Left")]
        [PropertyOrder(-1)] // 최상단 노출
        [Button("Refresh Actor List", ButtonSizes.Medium), GUIColor(0.9f, 0.9f, 0.4f)]
        private void LoadAvailableActors()
        {
            Debug.Log("[HuntPresetEditor] Refreshing Actor List...");
            _availableActorPrefabs.Clear();

            // 1. Settings 가져오기
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                // 혹시 null이면 에셋 직접 로드 시도
                var guids = AssetDatabase.FindAssets("t:AddressableAssetSettings");
                if (guids.Length > 0)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    settings = AssetDatabase.LoadAssetAtPath<AddressableAssetSettings>(path);
                }
            }

            if (settings == null) 
            {
                Debug.LogError("[HuntPresetEditor] Addressable Asset Settings NOT found!");
                return;
            }

            var targetLabels = new HashSet<string> { "character", "monster", "npc" };
            int entryCount = 0;
            
            foreach (var group in settings.groups)
            {
                if (group == null) continue;
                foreach (var entry in group.entries)
                {
                    if (entry == null) continue;

                    bool hasLabel = false;
                    foreach (var label in entry.labels)
                    {
                        foreach (var target in targetLabels)
                        {
                            if (string.Equals(label, target, System.StringComparison.OrdinalIgnoreCase))
                            {
                                hasLabel = true;
                                break;
                            }
                        }
                        if (hasLabel) break;
                    }

                    if (hasLabel)
                    {
                        var guid = entry.guid;
                        var path = AssetDatabase.GUIDToAssetPath(guid);
                        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        if (prefab != null && !_availableActorPrefabs.Contains(prefab))
                        {
                            _availableActorPrefabs.Add(prefab);
                            entryCount++;
                        }
                    }
                }
            }
            
            Debug.Log($"[HuntPresetEditor] Loaded {entryCount} actors from Addressables.");

            // Fallback: Addressable에서 못 찾았으면 AssetDatabase 라벨 검색
            if (entryCount == 0)
            {
                Debug.LogWarning("[HuntPresetEditor] Addressable에서 라벨을 찾지 못했습니다. AssetDatabase l:label 검색을 시도합니다.");
                foreach (var label in targetLabels)
                {
                    var guids = AssetDatabase.FindAssets($"l:{label} t:Prefab");
                    foreach (var guid in guids)
                    {
                        var path = AssetDatabase.GUIDToAssetPath(guid);
                        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        if (prefab != null && !_availableActorPrefabs.Contains(prefab))
                        {
                            _availableActorPrefabs.Add(prefab);
                        }
                    }
                }
                if (_availableActorPrefabs.Count > 0)
                {
                     Debug.Log($"[HuntPresetEditor] AssetDatabase 검색으로 {_availableActorPrefabs.Count}개의 프리팹을 찾았습니다. (주의: 이들은 Addressable 라벨 설정이 안 되어 있을 수 있습니다)");
                }
            }

            if (_availableActorPrefabs.Count == 0)
            {
                 Debug.LogWarning("[HuntPresetEditor] 목록이 비어있습니다. 프리팹에 'character', 'monster', 'npc' 라벨(Addressable 또는 Unity Label)을 붙여주세요.");
            }
        }

        #endregion
    }
}
