# Shadow Rendering Documentation

## Overview
HuntVerse의 렌더링 최적화 및 그림자 시스템에 대한 문서입니다.

---

## 1. Shadow Rendering System

### 1.1 Shadow Map Generation (Depth Pass)
**Role**: `CDepthRenderShader` (C++)가 담당하며, 조명 시점에서 깊이 정보를 기록합니다.

<details>
<summary>CDepthRenderShader::PrepareShadowMap</summary>

```cpp
void CDepthRenderShader::PrepareShadowMap(ID3D12GraphicsCommandList* pd3dCommandList)
{
	for (int j = 0; j < MAX_LIGHTS; j++)
	{
		if (m_pLights[j].m_bEnable)
		{
			XMFLOAT3 xmf3Position = m_pLights[j].m_xmf3Position;
			XMFLOAT3 xmf3Look = m_pLights[j].m_xmf3Direction;
			XMFLOAT3 xmf3Up = XMFLOAT3(0.0f, +1.0f, 0.0f);

			XMMATRIX xmmtxView = XMMatrixLookToLH(XMLoadFloat3(&xmf3Position), XMLoadFloat3(&xmf3Look), XMLoadFloat3(&xmf3Up));

			float fNearPlaneDistance = 10.0f, fFarPlaneDistance = m_pLights[j].m_fRange;

			XMMATRIX xmmtxProjection{};
			if (m_pLights[j].m_nType == DIRECTIONAL_LIGHT)
			{
				float fWidth = 2000, fHeight = 2000;
				xmmtxProjection = XMMatrixOrthographicLH(fWidth, fHeight, fNearPlaneDistance, fFarPlaneDistance);
			}
			else if (m_pLights[j].m_nType == SPOT_LIGHT)
			{
				float fWidth = _PLANE_WIDTH, fHeight = _PLANE_HEIGHT;
				m_pLights[j].m_fPhi = cos(60.0f);
				xmmtxProjection = XMMatrixOrthographicLH(fWidth, fHeight, fNearPlaneDistance, fFarPlaneDistance);
			}

			m_ppDepthRenderCameras[j]->SetPosition(xmf3Position);
			XMStoreFloat4x4(&m_ppDepthRenderCameras[j]->m_xmf4x4View, xmmtxView);
			XMStoreFloat4x4(&m_ppDepthRenderCameras[j]->m_xmf4x4Projection, xmmtxProjection);

			// ... (GPU Resource Transition & Clear Render Target) ...

			pd3dCommandList->OMSetRenderTargets(1, &m_pd3dRtvCPUDescriptorHandles[j], TRUE, &m_d3dDsvDescriptorCPUHandle);

			Render(pd3dCommandList, m_ppDepthRenderCameras[j], 0);

			// ... (Restore Resource State) ...
		}
	}
}
```
</details>

<details>
<summary>VSLighting & PSDepthWriteShader (HLSL)</summary>

```hlsl
VS_LIGHTING_OUTPUT VSLighting(VS_LIGHTING_INPUT input)
{
	VS_LIGHTING_OUTPUT output;

	if (!gbBoneShader)
	{
		output.normalW = mul(input.normal, (float3x3) gmtxGameObject);
		output.positionW = (float3) mul(float4(input.position, 1.0f), gmtxGameObject);
		output.position = mul(mul(float4(output.positionW, 1.0f), gmtxView), gmtxProjection);
		// ...
	}
	else if (gbBoneShader)
	{
        // ... (Bone Animation Calculation) ...
		output.position = mul(mul(float4(output.positionW, 1.0f), gmtxView), gmtxProjection);
	}
	return(output);
}

PS_DEPTH_OUTPUT PSDepthWriteShader(VS_LIGHTING_OUTPUT input)
{
	PS_DEPTH_OUTPUT output;

	output.fzPosition = input.position.z;
	output.fDepth = input.position.z;

	return(output);
}
```
</details>

### 1.3 Dynamic Shadow for Skinned Meshes
**Role**: 캐릭터와 같은 스킨 메쉬 객체가 움직일 때, 그림자도 이에 맞춰 동적으로 생성되어야 합니다.
이를 위해 Depth Map 생성 단계(`VSLighting`)에서 Bone Animation 변환을 수행하여 현재 프레임의 정확한 정점 위치를 깊이 버퍼에 기록합니다.

<details>
<summary>Dynamic Shadow Logic in VSLighting</summary>

```hlsl
if (gbBoneShader)
{
	float4x4 mtxVertexToBoneWorld = (float4x4) 0.0f;
	for (int i = 0; i < MAX_VERTEX_INFLUENCES; i++)
	{
		// 정점 가중치를 고려하여 Bone 변환 행렬 계산
		mtxVertexToBoneWorld += input.weights[i] * mul(gpmtxBoneOffsets[input.indices[i]], gpmtxBoneTransforms[input.indices[i]]);
	}
	
	// 애니메이션이 적용된 월드 좌표 계산
	float4 positionW = mul(float4(input.position, 1.0f), mtxVertexToBoneWorld);
	output.positionW = positionW.xyz;
	
	// Light Space로 투영하여 깊이 값 계산
	output.position = mul(mul(float4(output.positionW, 1.0f), gmtxView), gmtxProjection);
}
```
</details>

### 1.2 Shadow Application (Lighting Pass)
**Role**: 렌더링 시 Shadow Map을 PCF 필터링으로 샘플링하여 그림자를 적용합니다.

<details>
<summary>Shadow Calculation (HLSL)</summary>

```hlsl
// VSStandard (Transform Logic)
for (int i = 0; i < MAX_LIGHTS; i++)
{
	if (gcbToLightSpaces[i].f4Position.w != 0.0f)
		output.uvs[i] = mul(positionW, gcbToLightSpaces[i].mtxToTexture);
}

// Compute3x3ShadowFactor (PCF Logic)
float Compute3x3ShadowFactor(float2 uv, float fDepth, uint nIndex)
{
	float fPercentLit = gtxtDepthTextures[nIndex].SampleCmpLevelZero(gssComparisonPCFShadow, uv, fDepth).r;
	// ... (Sampling neighbors) ...
	fPercentLit += gtxtDepthTextures[nIndex].SampleCmpLevelZero(gssComparisonPCFShadow, uv + float2(+DELTA_X, +DELTA_Y), fDepth).r;

	return(fPercentLit / 9.0f);
}

// Lighting Function
float4 Lighting(float3 vPosition, float3 vNormal, bool bShadow, float4 uvs[MAX_LIGHTS])
{
    // ...
	for (int i = 0; i < MAX_LIGHTS; i++)
	{
		if (gLights[i].m_bEnable)
		{
			float fShadowFactor = 1.0f;
#ifdef _WITH_PCF_FILTERING
			if (bShadow) fShadowFactor = Compute3x3ShadowFactor(uvs[i].xy / uvs[i].ww, uvs[i].z / uvs[i].w, i);
#else
			if (bShadow) fShadowFactor = gtxtDepthTextures[i].SampleCmpLevelZero(gssComparisonPCFShadow, uvs[i].xy / uvs[i].ww, uvs[i].z / uvs[i].w).r;
#endif
            // Apply Shadow Factor
			// ...
		}
	}
    // ...
	return cColor;
}
```
</details>

---


