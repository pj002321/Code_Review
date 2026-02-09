# Frustum Culling System Documentation

## Overview
Frustum Culling(절두체 컬링)은 카메라 시야(Frustum) 밖에 있는 객체를 렌더링에서 제외하여 성능을 최적화하는 기법입니다.
현재 구현은 **Bounding Box(AABB/OBB)** 와 **Camera Frustum** 간의 교차 검사(`Intersects`)를 기반으로 합니다.

## 1. Frustum Generation (`Camera.cpp`)
*   **GenerateFrustum**: 투영 행렬(`Projection Matrix`)을 기반으로 Frustum을 생성하고, 뷰 행렬(`View Matrix`)의 역행렬을 사용하여 월드 공간으로 변환합니다.

<details open>
<summary>Click to view code: GenerateFrustum</summary>

```cpp
void CCamera::GenerateFrustum()
{
	m_xmFrustum.CreateFromMatrix(m_xmFrustum, XMLoadFloat4x4(&m_xmf4x4Projection));
	XMMATRIX xmmtxInverseView = XMMatrixInverse(NULL, XMLoadFloat4x4(&m_xmf4x4View));
	m_xmFrustum.Transform(m_xmFrustum, xmmtxInverseView);
}
```
</details>

## 2. Intersection Check (`Camera.cpp`)
*   **IsInFrustum**: DirectXMath의 `BoundingFrustum::Intersects` 함수를 사용하여 Bounding Box가 Frustum 내부에 있는지 판별합니다.

<details open>
<summary>Click to view code: IsInFrustum</summary>

```cpp
bool CCamera::IsInFrustum(BoundingBox& xmBoundingBox)
{
	return(m_xmFrustum.Intersects(xmBoundingBox));
}
```
</details>

## 3. Object Visibility Check (`Object.cpp`)
*   **IsVisible**: 객체의 Bounding Box를 월드 공간으로 변환한 후, 카메라의 `IsInFrustum`을 호출하여 가시성을 확인합니다.
*   *Note*: 현재 코드베이스에서 `IsVisible` 함수는 구현되어 있으나, 메인 렌더링 루프에서 직접적으로 호출되어 사용되고 있지는 않은 것으로 파악됩니다.

<details open>
<summary>Click to view code: IsVisible</summary>

```cpp
bool GameObjectMgr::IsVisible(CCamera* pCamera)
{
	OnPrepareRender();
	bool bIsVisible = false;
	BoundingBox xmBoundingBox = m_pMesh->GetBoundingBox();
	
	// Transform BoundingBox to World Space
	xmBoundingBox.Transform(xmBoundingBox, XMLoadFloat4x4(&m_xmf4x4World));
	
	if (pCamera) bIsVisible = pCamera->IsInFrustum(xmBoundingBox);
	return(bIsVisible);
}
```
</details>
