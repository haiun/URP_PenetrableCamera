# URP_PenetrableCamera

이 프로젝트는 URP RenderFeature와 Shader를 활용하여 투시카메라 효과를 구현한 것입니다.<br>
투시 효과의 범위와 투명도 설정이 가능하며, Unity 2022.3.56f1 버전에서 작업되었습니다.<br>
URP의 구조 변경으로 다른 버전에서는 정상 동작하지 않을 수 있습니다.<br>
[링크 - 웹에서 실행](https://haiun.github.io/URP_PenetrableCamera_TEST/, "웹에서 실행") <br>
<br>
<img src="https://raw.githubusercontent.com/haiun/URP_PenetrableCamera/refs/heads/main/ReadMeImage/main.png?row=true"/><br>
<br>
<br>
## 연구 목표
이 프로젝트는 "Cult of the Lamb - 컬트 오브 더 램"에서 사용된 투시카메라를 모방한 것입니다.<br>
플레이 중인 캐릭터를 가리는 오브젝트들의 투명도를 설정하여 캐릭터를 표시하고, 이를 통해 근경을 표현합니다.<br>
<img src="https://github.com/haiun/URP_PenetrableCamera/blob/main/ReadMeImage/target.gif?raw=true"/>
<img src="https://github.com/haiun/URP_PenetrableCamera/blob/main/ReadMeImage/target_ex.png?raw=true"/>
<br>
<br>
## 투시할 오브젝트의 선별
<img src="https://github.com/haiun/URP_PenetrableCamera/blob/main/ReadMeImage/layer.png?raw=true"/><br>

```csharp
public static class LayerMaskDefine
{
    // 카메라와 타겟 사이에 있을 가능성이 있는 모든 오브젝트
    public static readonly int PenetrableLayerMask = LayerMask.GetMask("Penetrable", "Penetrated");
    
    // 카메라와 타겟 사이에 배치되어 타겟을 가릴 가능성이 있는 오브젝트
    public static readonly int PenetrableLayer = LayerMask.NameToLayer("Penetrable");
    
    // 카메라와 타겟 사이에 배치되어 타겟을 가린 오브젝트
    public static readonly int PenetratedLayer = LayerMask.NameToLayer("Penetrated");
}
```
Physics와 Render Feature에서 오브젝트의 분류를 위해 레이어를 설정합니다.<br>
<br>

```csharp
private void FilteringNearbyObject()
{
    var camPos = transform.position;
    var sphereCastDirection = (_target.position - camPos);
    sphereCastDirection.Normalize();
    var sphereCastDistance = (_target.position - camPos).magnitude - _sphareCastRadius;
    var hitCount = Physics.SphereCastNonAlloc(camPos, _sphareCastRadius, sphereCastDirection, _hitBuffer, sphereCastDistance,
        LayerMaskDefine.PenetrableLayerMask, QueryTriggerInteraction.Ignore);

    _hitOnFrame.Clear();
    for (var i = 0; i < hitCount; i++)
    {
        var hit = _hitBuffer[i];
        var penetrableGameObject = hit.transform.gameObject.GetComponent<PenetrableGameObject>();
        if (penetrableGameObject == null)
            continue;

        _hitOnFrame.Add(penetrableGameObject);
    }

    foreach (var penetrableGameObject in _penetrableGameObjects)
    {
        penetrableGameObject.SetPenetrated(_hitOnFrame.Contains(penetrableGameObject));
    }
}
```
이 프로젝트는 카메라와 타겟이 모두 움직이기 때문에, SphereCast를 통해 근경인지 여부를 판단합니다.<br>
Penetrable과 Penetrated 레이어로 설정된 오브젝트를 SphereCast로 확인하고, 근경이라면 Penetrated로, 아니면 Penetrable로 레이어를 변경합니다.<br>
근경/중경/원경이 바뀌지 않는다면, 근경만 Penetrated 레이어로 설정하여 선별 로직의 비용을 줄일 수 있습니다.<br>
<br>
<br>
## 근경을 제외한 투시 이미지 렌더링<br>
<img src="https://github.com/haiun/URP_PenetrableCamera/blob/main/ReadMeImage/renderer1.png?raw=true"/><br>

UniversalRenderData에서 기본 렌더링 시 Penetrated 레이어를 제외하고 렌더링합니다.<br>

<img src="https://github.com/haiun/URP_PenetrableCamera/blob/main/ReadMeImage/K-001.png?raw=true"/><br>
<br>
<br>
## 근경을 제외한 투시 이미지 저장<br>

화면에 표시된 이미지를 저장하기 위해 GrabRenderPass를 ScriptableRenderPass를 상속받아 구현합니다.<br>
```csharp
// GrabRenderPass.cs
public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
{
    _grabTextureDescriptor.width = cameraTextureDescriptor.width;
    _grabTextureDescriptor.height = cameraTextureDescriptor.height;

    // 저장용 버퍼의 크기 변경이 필요하다면 재할당합니다.
    RenderingUtils.ReAllocateIfNeeded(ref _grabTextureHandle, _grabTextureDescriptor);
    
    // 저장용 버퍼의 이름을 지정합니다.
    cmd.SetGlobalTexture(_defaultSettings.RTName, _grabTextureHandle);
}
```
화면 크기와 동일한 저장용 텍스처 버퍼를 생성하고, 다른 셰이더에서 사용할 이름을 부여합니다. 기본값은 _GrabRenderPass0을 사용합니다.<br>
```csharp
// GrabRenderPass.cs
public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
{
    var volumeComponent = VolumeManager.instance.stack.GetComponent<GrabVolumeComponent>();
    if (!volumeComponent.isActive.value)
        return;
    
    // 카메라의 타겟버퍼의 핸들을 획득합니다.
    var cameraTargetHandle = renderingData.cameraData.renderer.cameraColorTargetHandle;
    var cmd = CommandBufferPool.Get();
    
    // 지금까지 구성된 화면을 _grabTextureHandle에 복사합니다.
    Blit(cmd, cameraTargetHandle, _grabTextureHandle);
    context.ExecuteCommandBuffer(cmd);
    CommandBufferPool.Release(cmd);
}
```
GrabRenderPass가 실행될 때 그려진 이미지를 _GrabRenderPass0 버퍼에 즉시 저장합니다.<br>
<br>
GrabRenderPass를 사용하기 위해서는 아래와 같은 작업이 추가로 필요합니다.<br>
1. GrabRenderPass를 추가하는 GrabRendererFeature를 정의합니다.<br>
2. 활성화된 UniversalRenderData에 GrabRendererFeature를 등록합니다.<br>
   <img src="https://github.com/haiun/URP_PenetrableCamera/blob/main/ReadMeImage/renderer2.png?raw=true"/>
   * 부가적으로 텍스처 버퍼 이름을 변경할 수 있습니다.<br>
3. 씬에서 전역 Volume을 적용하고, GrabRendererFeature를 등록하여 제어합니다.<br>
   <img src="https://github.com/haiun/URP_PenetrableCamera/blob/main/ReadMeImage/volume.png?raw=true"/>
   * 게임 로직에서 RenderFeature 조작이 필요하면, Volume을 통해 조작할 수 있습니다.<br>
<br>
<br>

## 근경을 포함한 투시 전 이미지 생성

<img src="https://github.com/haiun/URP_PenetrableCamera/blob/main/ReadMeImage/renderer3.png?raw=true"/>

GrabRenderPass 이후, 제외한 Penetrated 레이어 오브젝트를 렌더링하여 투시 전 정상적인 화면을 완성합니다.<br>
Z 버퍼나 스텐실 버퍼를 초기화하지 않기 때문에 정상적으로 렌더링됩니다.<br>

<img src="https://github.com/haiun/URP_PenetrableCamera/blob/main/ReadMeImage/K-002.png?raw=true"/><br>
<br>
<br>

## 두 이미지의 혼합

```hlsl
Varyings vert(Attributes IN)
{
    Varyings OUT;
    OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
    OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
    OUT.screenPos = ComputeScreenPos(OUT.positionHCS);
    return OUT;
}
```

일반적인 오브젝트 렌더링 시, TransformObjectToHClip, ComputeScreenPos 함수로 카메라 이미지 버퍼와 같은 좌표계를 얻습니다.<br>
그 결과값을 fragment shader로 전달하면 아래와 같이 저장된 버퍼를 자연스럽게 참조할 수 있습니다.<br>

```hlsl
half4 frag(Varyings IN) : SV_Target
{
    float alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).a * _Alpha;
    half4 color = half4(tex2Dproj(_GrabRenderPass0, IN.screenPos).rgb, alpha);
    return color;
}
```

<img src="https://github.com/haiun/URP_PenetrableCamera/blob/main/ReadMeImage/renderer4.png?raw=true"/><br>

PenetratingMask 레이어를 추가로 렌더링하고, 해당 셰이더를 PenetratingMask 레이어의 반투명 빌보드에 마스킹 텍스처와 혼합하여 최종 이미지를 생성합니다.<br>

<img src="https://github.com/haiun/URP_PenetrableCamera/blob/main/ReadMeImage/K-003.png?raw=true"/>

<br>
<br>

## 프로그렘 설명과 결과

<img src="https://github.com/haiun/URP_PenetrableCamera/blob/main/ReadMeImage/Result1.gif?raw=true"/><br>

좌상단 버튼을 통해 기능의 ON/OFF와 투시되는 영역의 크기 및 알파를 조작할 수 있습니다.<br>

[링크 - 웹에서 실행](https://haiun.github.io/URP_PenetrableCamera_TEST/, "웹에서 실행")

<br>
이 프로젝트로 구현한 투시카메라기능은 아래와 같은 강점을 가집니다.<br>

* 일반 오브젝트를 렌더링 하는데에 쓴 셰이더를 수정하지 않았습니다.<br>
* 혼합 마스크 연출을 직관적으로 구현 가능합니다.<br>
* Z버퍼를 활용하지 않는 포스트이펙트에 대응이 가능합니다.<br>
* 다른 오브젝트들을 추가로 렌더링 하지 않습니다.<br>

<br>

반면 개선이 필요한 점은 아래와 같습니다.<br>

* 복수 타겟에 대한 투시에 대응이 필요합니다.<br>
   * 여러개의 투시 시점이 생긴다면, 저장해야할 버퍼와 근경 선별 횟수도 비례해서 늘어날 것입니다.<br>
* Z버퍼활용 포스트이펙트 대응이 필요합니다.<br>
   * 알파로 중첩된 영역에 대한 Z값이 모호해지기 때문에, 디더링 필터를 통해 선택적으로 Z값을 선택 혼합한다면 개선 가능할 것으로 보입니다.<br>
* 스크린과 같은 크기의 저장 버퍼에 대한 고민이 필요합니다.<br>
   * 해상도와 플랫폼에 따라 업스케일링도 고려할 수 있습니다.<br>

<br>
