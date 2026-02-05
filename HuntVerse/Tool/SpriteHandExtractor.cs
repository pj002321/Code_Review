using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Hunt.Tool;

namespace Hades.Tool
{
    public class SpriteHandExtractor : EditorWindow
    {
        private Texture2D sourceTexture;
        private Sprite[] sourceSprites;
        private Rect handRect = new Rect(0.3f, 0.6f, 0.4f, 0.3f);
        private string outputPath = "Assets/Art/Character/Seible/Sprite/Hand";
        private Vector2 scrollPos;
        private SpriteHandPositionData positionData;
        private Dictionary<int, Vector2> handPositions = new Dictionary<int, Vector2>();
        private Dictionary<int, float> handRotations = new Dictionary<int, float>();
        private Dictionary<int, int> handSortingOrders = new Dictionary<int, int>();
        private int selectedFrameIndex = 0;
        private float previewSize = 200f;
        private bool isClicking = false;
        
        private Vector2 copiedPosition;
        private float copiedRotation;
        private int copiedSortingOrder;
        private bool hasCopiedData = false;

        [MenuItem("Tools/Sprite Hand Extractor")]
        public static void ShowWindow()
        {
            GetWindow<SpriteHandExtractor>("손 위치 추출기");
        }

        void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            
            EditorGUILayout.LabelField("스프라이트 손 위치 추출 도구", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            sourceTexture = (Texture2D)EditorGUILayout.ObjectField("소스 텍스처", sourceTexture, typeof(Texture2D), false);
            
            if (sourceTexture != null)
            {
                if (GUILayout.Button("스프라이트 로드"))
                {
                    LoadSprites();
                }

                if (sourceSprites != null && sourceSprites.Length > 0)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField($"로드된 스프라이트: {sourceSprites.Length}개", EditorStyles.helpBox);
                    EditorGUILayout.LabelField($"손 위치 지정: {handPositions.Count}/{sourceSprites.Length}개", EditorStyles.helpBox);
                    
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("손 위치 지정 방법", EditorStyles.boldLabel);
                    EditorGUILayout.HelpBox("각 프레임을 클릭하여 손 위치를 지정하세요. 손은 활을 든 왼쪽 손입니다.", MessageType.Info);
                    
                    EditorGUILayout.Space();
                    previewSize = EditorGUILayout.Slider("프레임 크기", previewSize, 100f, 400f);
                    
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("스프라이트 프레임 (클릭하여 손 위치 지정)", EditorStyles.boldLabel);
                    
                    int cols = Mathf.FloorToInt((position.width - 20) / (previewSize + 10));
                    if (cols < 1) cols = 1;
                    
                    for (int i = 0; i < sourceSprites.Length; i++)
                    {
                        if (i % cols == 0)
                        {
                            EditorGUILayout.BeginHorizontal();
                        }
                        
                        DrawSpriteFrame(i);
                        
                        if ((i + 1) % cols == 0 || i == sourceSprites.Length - 1)
                        {
                            EditorGUILayout.EndHorizontal();
                        }
                    }

                    if (selectedFrameIndex >= 0 && selectedFrameIndex < sourceSprites.Length)
                    {
                        EditorGUILayout.Space();
                        EditorGUILayout.LabelField($"선택된 프레임: {sourceSprites[selectedFrameIndex].name}", EditorStyles.boldLabel);
                        
                        if (handPositions.ContainsKey(selectedFrameIndex))
                        {
                            Vector2 currentPos = handPositions[selectedFrameIndex];
                            
                            EditorGUILayout.LabelField("위치 (정규화 좌표 0-1)", EditorStyles.boldLabel);
                            
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField("X:", GUILayout.Width(20));
                            float newX = EditorGUILayout.Slider(currentPos.x, 0f, 1f);
                            float directX = EditorGUILayout.FloatField(newX, GUILayout.Width(60));
                            if (directX != newX)
                            {
                                newX = Mathf.Clamp01(directX);
                            }
                            EditorGUILayout.EndHorizontal();
                            
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField("Y:", GUILayout.Width(20));
                            float newY = EditorGUILayout.Slider(currentPos.y, 0f, 1f);
                            float directY = EditorGUILayout.FloatField(newY, GUILayout.Width(60));
                            if (directY != newY)
                            {
                                newY = Mathf.Clamp01(directY);
                            }
                            EditorGUILayout.EndHorizontal();
                            
                            if (newX != currentPos.x || newY != currentPos.y)
                            {
                                handPositions[selectedFrameIndex] = new Vector2(newX, newY);
                            }
                            
                            if (!handRotations.ContainsKey(selectedFrameIndex))
                            {
                                handRotations[selectedFrameIndex] = 0f;
                            }
                            if (!handSortingOrders.ContainsKey(selectedFrameIndex))
                            {
                                handSortingOrders[selectedFrameIndex] = 1;
                            }
                            
                            float rot = handRotations[selectedFrameIndex];
                            rot = EditorGUILayout.Slider("회전 (도)", rot, 0f, 360f);
                            handRotations[selectedFrameIndex] = rot;
                            handSortingOrders[selectedFrameIndex] = EditorGUILayout.IntField("Sorting Order", handSortingOrders[selectedFrameIndex]);
                            
                            EditorGUILayout.Space();
                            EditorGUILayout.BeginHorizontal();
                            if (GUILayout.Button("복사"))
                            {
                                copiedPosition = currentPos;
                                copiedRotation = handRotations[selectedFrameIndex];
                                copiedSortingOrder = handSortingOrders[selectedFrameIndex];
                                hasCopiedData = true;
                            }
                            
                            GUI.enabled = hasCopiedData;
                            if (GUILayout.Button("붙여넣기"))
                            {
                                handPositions[selectedFrameIndex] = copiedPosition;
                                handRotations[selectedFrameIndex] = copiedRotation;
                                handSortingOrders[selectedFrameIndex] = copiedSortingOrder;
                            }
                            GUI.enabled = true;
                            EditorGUILayout.EndHorizontal();
                            
                            if (hasCopiedData)
                            {
                                EditorGUILayout.HelpBox($"복사된 값: 위치({copiedPosition.x:F3}, {copiedPosition.y:F3}), 회전({copiedRotation:F0}°), Sorting({copiedSortingOrder})", MessageType.Info);
                            }
                            
                            EditorGUILayout.HelpBox("Sorting Order: 캐릭터보다 크면 앞에, 작으면 뒤에 표시됩니다.", MessageType.Info);
                        }
                        else
                        {
                            EditorGUILayout.HelpBox("프레임을 클릭하여 손 위치를 먼저 지정하세요.", MessageType.Info);
                            
                            if (hasCopiedData)
                            {
                                EditorGUILayout.Space();
                                EditorGUILayout.BeginHorizontal();
                                if (GUILayout.Button("붙여넣기 (위치만)"))
                                {
                                    handPositions[selectedFrameIndex] = copiedPosition;
                                    if (!handRotations.ContainsKey(selectedFrameIndex))
                                    {
                                        handRotations[selectedFrameIndex] = 0f;
                                    }
                                    if (!handSortingOrders.ContainsKey(selectedFrameIndex))
                                    {
                                        handSortingOrders[selectedFrameIndex] = 1;
                                    }
                                }
                                EditorGUILayout.EndHorizontal();
                            }
                        }
                    }
                    
                    EditorGUILayout.Space();
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("모두 지우기"))
                    {
                        handPositions.Clear();
                        handRotations.Clear();
                        handSortingOrders.Clear();
                    }
                    if (GUILayout.Button("손 위치 데이터 저장", GUILayout.Height(30)))
                    {
                        ExtractHandPositions();
                    }
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUILayout.Space();
                    positionData = (SpriteHandPositionData)EditorGUILayout.ObjectField("위치 데이터", positionData, typeof(SpriteHandPositionData), false);
                    if (positionData != null && positionData.handPositions.Count > 0)
                    {
                        EditorGUILayout.LabelField($"저장된 위치 데이터: {positionData.handPositions.Count}개", EditorStyles.helpBox);
                    }
                }
            }

            EditorGUILayout.EndScrollView();
        }

        void LoadSprites()
        {
            string path = AssetDatabase.GetAssetPath(sourceTexture);
            
            Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(path);
            sourceSprites = allAssets.OfType<Sprite>().ToArray();
            
            if (sourceSprites == null || sourceSprites.Length == 0)
            {
                Object[] subAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);
                sourceSprites = subAssets.OfType<Sprite>().ToArray();
            }
            
            if (sourceSprites == null || sourceSprites.Length == 0)
            {
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                string importerInfo = importer != null 
                    ? $"Import Mode: {importer.spriteImportMode}, Sprites: {importer.spritesheet?.Length ?? 0}" 
                    : "Importer 없음";
                    
                EditorUtility.DisplayDialog("오류", 
                    $"스프라이트를 찾을 수 없습니다.\n경로: {path}\n{importerInfo}\n\n텍스처를 선택하고 Inspector에서 'Apply'를 눌러보세요.", 
                    "확인");
            }
            else
            {
                System.Array.Sort(sourceSprites, (a, b) => string.Compare(a.name, b.name));
            }
        }

        void DrawSpriteFrame(int index)
        {
            Sprite sprite = sourceSprites[index];
            if (sprite == null) return;

            Rect frameRect = GUILayoutUtility.GetRect(previewSize, previewSize, GUILayout.Width(previewSize), GUILayout.Height(previewSize));
            
            EditorGUI.DrawRect(frameRect, Color.black);
            
            Rect uvRect = new Rect(
                sprite.rect.x / sprite.texture.width,
                sprite.rect.y / sprite.texture.height,
                sprite.rect.width / sprite.texture.width,
                sprite.rect.height / sprite.texture.height
            );
            
            GUI.DrawTextureWithTexCoords(frameRect, sprite.texture, uvRect, true);

            if (handPositions.ContainsKey(index))
            {
                Vector2 handPos = handPositions[index];
                Vector2 screenPos = new Vector2(
                    frameRect.x + handPos.x * frameRect.width,
                    frameRect.y + (1f - handPos.y) * frameRect.height
                );
                
                float rotation = handRotations.ContainsKey(index) ? handRotations[index] : 0f;
                Color markerColor = (index == selectedFrameIndex) ? Color.yellow : Color.red;
                
                DrawRotationArrow(screenPos, rotation, markerColor, 20f);
                
                string info = $"({handPos.x:F2}, {handPos.y:F2})";
                if (handRotations.ContainsKey(index))
                {
                    info += $"\nR:{handRotations[index]:F0}°";
                }
                if (handSortingOrders.ContainsKey(index))
                {
                    info += $"\nS:{handSortingOrders[index]}";
                }
                GUI.Label(new Rect(screenPos.x + 12, screenPos.y - 8, 100, 40), info, EditorStyles.miniLabel);
            }

            Event evt = Event.current;
            if (evt.type == EventType.MouseDown && frameRect.Contains(evt.mousePosition))
            {
                if (evt.button == 0)
                {
                    Vector2 localPos = evt.mousePosition - new Vector2(frameRect.x, frameRect.y);
                    Vector2 normalizedPos = new Vector2(
                        localPos.x / frameRect.width,
                        1f - (localPos.y / frameRect.height)
                    );
                    
                    handPositions[index] = normalizedPos;
                    selectedFrameIndex = index;
                    
                    if (!handRotations.ContainsKey(index))
                    {
                        handRotations[index] = 0f;
                    }
                    if (!handSortingOrders.ContainsKey(index))
                    {
                        handSortingOrders[index] = 1;
                    }
                }
                else if (evt.button == 1 && hasCopiedData)
                {
                    handPositions[index] = copiedPosition;
                    if (!handRotations.ContainsKey(index))
                    {
                        handRotations[index] = 0f;
                    }
                    if (!handSortingOrders.ContainsKey(index))
                    {
                        handSortingOrders[index] = 1;
                    }
                    handRotations[index] = copiedRotation;
                    handSortingOrders[index] = copiedSortingOrder;
                    selectedFrameIndex = index;
                }
                
                Repaint();
                evt.Use();
            }
        }

        void ExtractHandPositions()
        {
            if (sourceSprites == null || sourceSprites.Length == 0)
            {
                EditorUtility.DisplayDialog("오류", "스프라이트를 먼저 로드하세요.", "확인");
                return;
            }

            if (handPositions.Count == 0)
            {
                EditorUtility.DisplayDialog("경고", "손 위치를 먼저 지정하세요. 각 프레임을 클릭하여 손 위치를 지정할 수 있습니다.", "확인");
                return;
            }

            if (positionData == null)
            {
                string path = EditorUtility.SaveFilePanelInProject("손 위치 데이터 저장", "HandPositionData", "asset", "저장할 위치를 선택하세요");
                if (string.IsNullOrEmpty(path))
                {
                    return;
                }

                positionData = ScriptableObject.CreateInstance<SpriteHandPositionData>();
                AssetDatabase.CreateAsset(positionData, path);
            }

            string animationName = ExtractAnimationNameFromTexture(sourceTexture.name);
            
            AnimationHandData animData = positionData.animationDataList.FirstOrDefault(a => a.animationName == animationName);
            if (animData == null)
            {
                animData = new AnimationHandData
                {
                    animationName = animationName
                };
                positionData.animationDataList.Add(animData);
            }
            
            animData.framePositions.Clear();

            for (int i = 0; i < sourceSprites.Length; i++)
            {
                Sprite sprite = sourceSprites[i];
                if (sprite == null) continue;

                Rect spriteRect = sprite.rect;
                
                if (handPositions.ContainsKey(i))
                {
                    Vector2 normalizedPos = handPositions[i];
                    Vector2 normalizedSize = new Vector2(0.1f, 0.1f);
                    
                    Vector2 pixelPos = new Vector2(
                        spriteRect.x + normalizedPos.x * spriteRect.width,
                        spriteRect.y + normalizedPos.y * spriteRect.height
                    );
                    Vector2 pixelSize = new Vector2(
                        normalizedSize.x * spriteRect.width,
                        normalizedSize.y * spriteRect.height
                    );

                    float rotation = handRotations.ContainsKey(i) ? handRotations[i] : 0f;
                    int sortingOrder = handSortingOrders.ContainsKey(i) ? handSortingOrders[i] : 1;

                    HandPositionData handData = new HandPositionData
                    {
                        spriteName = sprite.name,
                        normalizedPosition = normalizedPos,
                        normalizedSize = normalizedSize,
                        pixelPosition = pixelPos,
                        pixelSize = pixelSize,
                        rotation = rotation,
                        sortingOrder = sortingOrder
                    };

                    animData.framePositions.Add(handData);
                }
            }
            
            System.Array.Sort(sourceSprites, (a, b) => string.Compare(a.name, b.name));
            animData.framePositions = animData.framePositions
                .OrderBy(x => ExtractFrameIndex(x.spriteName))
                .ToList();

            EditorUtility.SetDirty(positionData);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("완료", 
                $"{animData.framePositions.Count}개의 손 위치 데이터가 저장되었습니다.\n애니메이션: {animationName}", 
                "확인");
        }
        
        private string ExtractAnimationNameFromTexture(string textureName)
        {
            int atIndex = textureName.IndexOf('@');
            if (atIndex >= 0)
            {
                return textureName.Substring(0, atIndex);
            }
            
            int lastUnderscore = textureName.LastIndexOf('_');
            if (lastUnderscore >= 0)
            {
                return textureName.Substring(0, lastUnderscore);
            }
            
            return textureName;
        }
        
        private void DrawRotationArrow(Vector2 center, float rotation, Color color, float length)
        {
            float angleRad = rotation * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(angleRad), -Mathf.Sin(angleRad));
            Vector2 arrowEnd = center + direction * length;
            
            Handles.BeginGUI();
            Handles.color = color;
            Handles.DrawLine(center, arrowEnd);
            
            Vector2 arrowHead1 = arrowEnd - direction * 6f + new Vector2(-direction.y, direction.x) * 4f;
            Vector2 arrowHead2 = arrowEnd - direction * 6f - new Vector2(-direction.y, direction.x) * 4f;
            
            Handles.DrawLine(arrowEnd, arrowHead1);
            Handles.DrawLine(arrowEnd, arrowHead2);
            
            Handles.color = Color.white;
            Handles.EndGUI();
            
            EditorGUI.DrawRect(new Rect(center.x - 3, center.y - 3, 6, 6), color);
        }
        
        private int ExtractFrameIndex(string spriteName)
        {
            int atIndex = spriteName.IndexOf('@');
            string nameToCheck = spriteName;
            
            if (atIndex >= 0)
            {
                nameToCheck = spriteName.Substring(0, atIndex);
            }
            
            int lastUnderscore = nameToCheck.LastIndexOf('_');
            if (lastUnderscore >= 0 && int.TryParse(nameToCheck.Substring(lastUnderscore + 1), out int frameIndex))
            {
                return frameIndex;
            }
            return 0;
        }
    }
}
