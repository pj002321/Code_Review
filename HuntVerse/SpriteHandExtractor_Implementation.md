# SpriteHandExtractor 구현 및 동작 방식 정리

`SpriteHandExtractor`는 스프라이트 애니메이션 프레임마다 손의 위치, 회전, 정렬 순서(Sorting Order)를 지정하고 데이터를 추출하는 유니티 에디터 도구입니다. 
이 도구는 캐릭터가 무기를 쥐거나 특정 지점에서 이펙트를 발생시켜야 할 때, 정확한 위치 데이터를 제공하기 위해 사용됩니다.

---

## 1. 핵심 기능 및 워크플로우

1.  **스프라이트 로드**: 소스 텍스처를 선택하고 로드하면, 해당 텍스처에 포함된 모든 스프라이트를 프레임 단위로 표시합니다.
2.  **시각적 편집**:
    *   각 프레임 이미지를 그리드 형태로 보여줍니다.
    *   사용자는 프레임을 클릭하여 직관적으로 손 위치를 지정할 수 있습니다.
    *   선택된 프레임에 대해 상세 조정(위치 미세 조정, 회전 각도, Sorting Order)이 가능합니다.
3.  **데이터 시각화**: 지정된 손 위치와 회전 정보는 이미지 위에 마커와 화살표로 실시간 렌더링되어 즉각적인 피드백을 제공합니다.
4.  **데이터 저장**: 작업한 내용을 `SpriteHandPositionData` (ScriptableObject) 에셋으로 추출하여 런타임에 사용할 수 있게 합니다.

---

## 2. 주요 구현 상세

### 2.1 데이터 관리 (`OnGUI`)
EditorWindow의 `OnGUI` 메서드에서 모든 UI 및 입력 처리가 이루어집니다.

```csharp
// SpriteHandExtractor.cs
private Dictionary<int, Vector2> handPositions = new Dictionary<int, Vector2>();
private Dictionary<int, float> handRotations = new Dictionary<int, float>();
private Dictionary<int, int> handSortingOrders = new Dictionary<int, int>();

void OnGUI()
{
    // ... 상단 툴바 및 정보 표시
    
    // 그리드 형태로 스프라이트 프레임 표시
    int cols = Mathf.FloorToInt((position.width - 20) / (previewSize + 10));
    for (int i = 0; i < sourceSprites.Length; i++)
    {
        // ... Layout 계산
        DrawSpriteFrame(i); // 각 프레임 그리기 및 입력 처리
    }
    
    // ... 선택된 프레임 상세 속성 편집 UI (Slider, IntField 등)
}
```

### 2.2 프레임 렌더링 및 입력 처리 (`DrawSpriteFrame`)
가장 핵심적인 메서드로, 스프라이트를 그리고 마우스 입력을 받아 좌표를 변환합니다.

```csharp
void DrawSpriteFrame(int index)
{
    // 1. 스프라이트 렌더링 (텍스처 좌표 계산)
    Rect uvRect = new Rect(
        sprite.rect.x / sprite.texture.width,
        sprite.rect.y / sprite.texture.height,
        sprite.rect.width / sprite.texture.width,
        sprite.rect.height / sprite.texture.height
    );
    GUI.DrawTextureWithTexCoords(frameRect, sprite.texture, uvRect, true);

    // 2. 마우스 입력 처리 (좌표 정규화)
    Event evt = Event.current;
    if (evt.type == EventType.MouseDown && frameRect.Contains(evt.mousePosition))
    {
        if (evt.button == 0) // 좌클릭: 위치 지정
        {
            Vector2 localPos = evt.mousePosition - new Vector2(frameRect.x, frameRect.y);
            
            // 0~1 사이의 정규화된 좌표로 변환하여 해상도 독립성 확보
            Vector2 normalizedPos = new Vector2(
                localPos.x / frameRect.width,
                1f - (localPos.y / frameRect.height) // Y축 반전 주의
            );
            
            handPositions[index] = normalizedPos;
            // ... 초기값 설정 및 선택 상태 업데이트
        }
        // ... 우클릭: 데이터 붙여넣기 로직
    }
}
```

### 2.3 데이터 추출 및 저장 (`ExtractHandPositions`)
편집된 데이터를 실제 게임에서 사용할 수 있는 형태로 변환합니다.

```csharp
void ExtractHandPositions()
{
    // ... 유효성 검사 및 저장 경로 설정
    
    // 애니메이션 이름 추출 (텍스처 이름 기반 파싱)
    string animationName = ExtractAnimationNameFromTexture(sourceTexture.name);
    
    // 데이터 생성 및 리스트 채우기
    for (int i = 0; i < sourceSprites.Length; i++)
    {
        // ... 좌표 변환: Normalized(0~1) -> Pixel Coordinates
        Vector2 pixelPos = new Vector2(
            spriteRect.x + normalizedPos.x * spriteRect.width,
            spriteRect.y + normalizedPos.y * spriteRect.height
        );
        
        HandPositionData handData = new HandPositionData
        {
            spriteName = sprite.name,
            normalizedPosition = normalizedPos, // UI 표시용
            pixelPosition = pixelPos,           // 실제 픽셀 좌표
            rotation = rotation,
            sortingOrder = sortingOrder
        };
        animData.framePositions.Add(handData);
    }
    
    // 프레임 순서 정렬 (파일 이름의 숫자 접미사 기준)
    animData.framePositions = animData.framePositions
        .OrderBy(x => ExtractFrameIndex(x.spriteName))
        .ToList();
        
    // 에셋 저장
    EditorUtility.SetDirty(positionData);
    AssetDatabase.SaveAssets();
}
```

---

## 3. 편의 기능

*   **복사/붙여넣기**: 특정 프레임의 위치/회전/오더 값을 복사하여 다른 프레임에 빠르게 적용할 수 있습니다. (우클릭 단축 기능 지원)
*   **시각적 피드백**: `DrawRotationArrow` 메서드를 통해 회전 각도를 화살표로 시각화하여, 무기가 쥐어질 방향을 미리 가늠할 수 있습니다.
*   **자동 정렬**: 파일 이름(예: `Attack_0`, `Attack_1`...)을 파싱하여 프레임 번호 순서대로 데이터를 정렬하여 저장합니다.
