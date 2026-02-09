# Shadow Rendering System Documentation

## Overview
HuntVerse의 그림자 렌더링 시스템은 **Shadow Mapping** 기법을 사용하며, Directional, Point, Spot Light에 대한 그림자를 지원합니다.
구현은 크게 두 단계로 나뉩니다:
1. **Shadow Map Generation (Depth Pass)**: 빛의 시점에서 깊이(Depth) 정보를 텍스처에 기록합니다.
2. **Shadow Application (Lighting Pass)**: 렌더링 시 Shadow Map을 샘플링하여 그림자 여부를 판단하고 적용합니다.

## 1. Shadow Map Generation (Depth Pass)

### 1.1 C++ Implementation (`ShadowShader.cpp`)
*   **Class**: `CDepthRenderShader`
*   **Role**: 그림자 맵(Depth Map) 생성을 담당합니다.
*   **Key Process (`PrepareShadowMap`)**:
    1.  활성화된 모든 조명(`MAX_LIGHTS`)을 순회합니다.
    2.  각 조명의 View, Projection Matrix를 계산합니다.
        *   **Directional Light**: `XMMatrixOrthographicLH` (직교 투영) 사용.
        *   **Spot Light**: 코드상으로는 `XMMatrixOrthographicLH`를 사용 중 (Perspective 투영 주석 처리됨).
    3.  **Render Target 설정**:
        *   `m_pDepthTexture` (Texture Array)의 해당 인덱스를 Render Target으로 설정합니다.
        *   Format: `DXGI_FORMAT_R32_FLOAT` (Color), `DXGI_FORMAT_D32_FLOAT` (Depth).
    4.  **객체 렌더링**:
        *   `Render` 함수를 호출하여 씬의 객체들을 그립니다.
        *   이때 셰이더는 `VSlighting`과 `PSDepthWriteShader`를 사용합니다.

<details>
<summary>Click to view code: CDepthRenderShader::PrepareShadowMap</summary>

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

### 1.2 Shader Implementation (`Shadow.hlsl`)
*   **Vertex Shader (`VSLighting`)**:
    *   기본적인 World-View-Projection 변환을 수행합니다.
    *   Bone Animation이 있는 경우(`gbBoneShader`) 스킨 변환을 적용합니다.
*   **Pixel Shader (`PSDepthWriteShader`)**:
    *   `SV_Target` (Red Channel)과 `SV_Depth`에 깊이 값을 기록합니다.
    *   `output.fzPosition = input.position.z;`

<details>
<summary>Click to view code: VSLighting & PSDepthWriteShader</summary>

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

## 2. Shadow Application (Lighting Pass)

### 2.1 Vertex Shader (`Shaders.hlsl`)
*   씬 렌더링 시(`VSStandard` 등), 월드 좌표(`positionW`)를 각 조명의 텍스처 공간(Shadow Map UV)으로 변환합니다.

<details>
<summary>Click to view code: VSStandard (Shadow Loop)</summary>

```hlsl
	for (int i = 0; i < MAX_LIGHTS; i++)
	{
		if (gcbToLightSpaces[i].f4Position.w != 0.0f)
			output.uvs[i] = mul(positionW, gcbToLightSpaces[i].mtxToTexture);
	}
```
</details>

### 2.2 Pixel Shader Lighting (`Light.hlsl`)
*   **PCF (Percentage Closer Filtering)**: 부드러운 그림자를 위해 3x3 PCF 필터링을 사용합니다.
*   **`Compute3x3ShadowFactor`**:
    *   현재 픽셀의 깊이(`fDepth`)와 Shadow Map의 깊이 값을 비교합니다.
    *   `SampleCmpLevelZero`를 사용하여 하드웨어 비교 샘플링을 수행합니다.
    *   주변 9개 샘플(3x3)을 평균내어 `fShadowFactor`(그림자 계수 0.0~1.0)를 반환합니다.

<details>
<summary>Click to view code: Compute3x3ShadowFactor</summary>

```hlsl
float Compute3x3ShadowFactor(float2 uv, float fDepth, uint nIndex)
{
	float fPercentLit = gtxtDepthTextures[nIndex].SampleCmpLevelZero(gssComparisonPCFShadow, uv, fDepth).r;
	fPercentLit += gtxtDepthTextures[nIndex].SampleCmpLevelZero(gssComparisonPCFShadow, uv + float2(-DELTA_X, 0.0f), fDepth).r;
	fPercentLit += gtxtDepthTextures[nIndex].SampleCmpLevelZero(gssComparisonPCFShadow, uv + float2(+DELTA_X, 0.0f), fDepth).r;
	fPercentLit += gtxtDepthTextures[nIndex].SampleCmpLevelZero(gssComparisonPCFShadow, uv + float2(0.0f, -DELTA_Y), fDepth).r;
	fPercentLit += gtxtDepthTextures[nIndex].SampleCmpLevelZero(gssComparisonPCFShadow, uv + float2(0.0f, +DELTA_Y), fDepth).r;
    // ... (More samples) ...
	fPercentLit += gtxtDepthTextures[nIndex].SampleCmpLevelZero(gssComparisonPCFShadow, uv + float2(+DELTA_X, +DELTA_Y), fDepth).r;

	return(fPercentLit / 9.0f);
}
```
</details>

### 2.3 Lighting Calculation (`Lighting` Function)
*   계산된 `fShadowFactor`를 Diffuse 및 Specular 조명 계산에 곱해줍니다.
*   그림자가 있는 곳(`fShadowFactor < 1.0`)은 빛의 영향이 줄어들어 어둡게 렌더링됩니다.

<details>
<summary>Click to view code: Lighting Function</summary>

```hlsl
float4 Lighting(float3 vPosition, float3 vNormal, bool bShadow, float4 uvs[MAX_LIGHTS])
{
    // ...
	[unroll]
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

			if (gLights[i].m_nType == DIRECTIONAL_LIGHT)
			{
				cColor += DirectionalLight(i, vNormal, vToCamera) * fShadowFactor;
			}
			else if (gLights[i].m_nType == POINT_LIGHT)
			{
				cColor += PointLight(i, vPosition, vNormal, vToCamera) * fShadowFactor;
			}
			else if (gLights[i].m_nType == SPOT_LIGHT)
			{
				cColor += SpotLight(i, vPosition, vNormal, vToCamera) * fShadowFactor;
			}

			cColor += gLights[i].m_cAmbient * gMaterial.m_cAmbient;
		}
	}
    // ...
	return cColor;
}
```
</details>

## 3. Key Structures & Variables

### C++
*   **`TOLIGHTSPACES`**: 조명 공간 변환 행렬을 담는 Constant Buffer 구조체.
*   **`MAX_DEPTH_TEXTURES`**: 생성할 그림자 맵의 최대 개수.

### HLSL
*   **`gcbToLightSpaces`**: 조명 별 텍스처 변환 행렬 배열.
*   **`gtxtDepthTextures`**: 그림자 맵 텍스처 배열.
*   **`gssComparisonPCFShadow`**: 그림자 비교를 위한 Sampler State (Comparison Filter).

## 4. Notes
*   **Spot Light Projection**: 현재 Spot Light도 Orthographic 투영을 사용하도록 설정되어 있습니다. 원근감 있는 그림자를 원한다면 Perspective 투영으로 변경이 필요할 수 있습니다.
*   **PCF Quality**: 3x3 필터링을 사용하고 있어 비교적 부드러운 그림자를 생성하지만, 성능 비용이 발생합니다.
