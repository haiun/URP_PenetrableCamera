# URP_PenetrableCamera

이 프로젝트는 URP RenderFeature와 Shader를 활용해서 투시카메라 효과를 구현하였습니다.<br>
투시 효과의 범위와 투명도 설정이 가능합니다.<br>
Unity 2022.3.56f1버전에서 작업되었습니다.<br>
URP의 구조변경으로 다른버전에서 정상적동 하지 않을 가능성이 있습니다.<br>
[링크 - 웹에서 실행](https://haiun.github.io/URP_PenetrableCamera_TEST/, "웹에서 실행") <br>
<br>
<img src="https://raw.githubusercontent.com/haiun/URP_PenetrableCamera/refs/heads/main/ReadMeImage/main.png?row=true"/><br>
<br>
<br>
## 연구 목표
"Cult of the Lamb - 컬트 오브 더 램"에서 활용되고 있는 투시카메라를 모방합니다.<br>
플레이중인 캐릭터를 가리는 오브젝트들을 일정 범위만큼 투명도를 설정하여 캐릭터를 표시해서 근경을 표현합니다.<br>
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
이 프로젝트는 카메라와 타겟이 모두 움직이고 있기 때문에 *SphereCast*를 통해 근경인지 여부를 결정합니다.<br>
*Penetrable, Penetrated* 레이어로 설정되어 있는 오브젝트를 *SphereCast*후 결과에 따라 근경이라면 *Penetrated*, 아니라면 *Penetrable*로 레이어를 변경합니다.<br>
표현목표에 따라 근경/중경/원경이 바뀌지 않는다면, *근경*만 *Penetrated*레이어로 설정해서 선별 로직의 비용을 줄일 수 있습니다.<br>
<br>
<br>
## 근경을 제외한 투시 이미지 렌더링<br>
<img src="https://github.com/haiun/URP_PenetrableCamera/blob/main/ReadMeImage/renderer1.png?raw=true"/><br>

*UniversalRenderData*에서 기본 렌더링 시 *Penetrated*레이어를 제외하고 렌더링 합니다.<br>

<img src="https://github.com/haiun/URP_PenetrableCamera/blob/main/ReadMeImage/K-001.png?raw=true"/><br>
<br>
<br>
## 근경을 제외한 투시 이미지 저장<br>

화면에 표시되고 있는 이미지를 저장하는 *GrabRenderPass*를 ScriptableRenderPass를 상속받아 구현합니다.<br>
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
화면 크기와 동일한 저장용 텍스쳐버퍼를 생성 후 다른 셰이더에서 사용할 이름을 부여합니다.<br>
기본값을 *_GrabRenderPass0*를 이름으로 사용하지만, 다른곳에서도 재활용 가능 하도록 설정했습니다.<br>
<br>
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
*GrabRenderPass*가 실행될 때까지 그려진 이미지를 *_GrabRenderPass0*로 이름이 붙혀진 버퍼에 즉시 저장합니다.<br>
<br>
*GrabRenderPass*를 사용하기 위해서는 아래와 같은 작업이 추가로 필요합니다.<br>
1. *GrabRenderPass*를 추가하는 *GrabRendererFeature*를 정의합니다.<br>
2. 활성화된 *UniversalRenderData*에 *GrabRendererFeature*를 등록합니다.<br>
   <img src="https://github.com/haiun/URP_PenetrableCamera/blob/main/ReadMeImage/renderer2.png?raw=true"/>
   부가적으로 여기서 저장용 텍스쳐버퍼의 이름 변경이 가능합니다.
3. 씬에서 전역 Volume을 적용하고 *GrabRendererFeature*를 등록 후 제어합니다.<br>
   <img src="https://github.com/haiun/URP_PenetrableCamera/blob/main/ReadMeImage/volume.png?raw=true"/>
   게임 로직에서 RenderFeature에 대한 조작이 필요하다면, Volume을 통해 조작하면 됩니다.
<br>
<br>

## 근경을 포함한 투시 전 이미지 생성

<img src="https://github.com/haiun/URP_PenetrableCamera/blob/main/ReadMeImage/renderer3.png?raw=true"/><br>
*GrabRenderPass*이후 제외했던 *Penetrated*레이어 오브젝트를 렌더링해서 투시 전 정상적인 화면를 완성합니다.<br>
Z버퍼나 스텐실등 다른 버퍼들을 초기화하지 않았기 떄문에 정상적인 렌더링이 완성됩니다.<br>
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

일반적인 오브젝트 렌더링 시 정점의Position에 TransformObjectToHClip, ComputeScreenPos함수를 차례대로 계산하면, 카메라 이미지 버퍼와 같은 좌표계가 됩니다.<br>
계산 결과값을 fragment shader로 전달하면 아래와 같이 저장해둔 버퍼의 자연스러운 참조가 가능합니다.<br>

```hlsl
half4 frag(Varyings IN) : SV_Target
{
    float alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).a * _Alpha;
    half4 color = half4(tex2Dproj(_GrabRenderPass0, IN.screenPos).rgb, alpha);
    return color;
}
```

<img src="https://github.com/haiun/URP_PenetrableCamera/blob/main/ReadMeImage/renderer4.png?raw=true"/><br>
*PenetratingMask*레이어를 추가로 렌더링 하도록 지정 한 후<br>
해당 셰이더를 *PenetratingMask*레이어의 반투명 빌보드에 마스킹텍스쳐와 혼합하여 아래와 같은 이미지를 생성합니다.<br>
<img src="https://github.com/haiun/URP_PenetrableCamera/blob/main/ReadMeImage/K-003.png?raw=true"/>

<br>
<br>

## 프로그렘 설명과 결과

<img src="https://github.com/haiun/URP_PenetrableCamera/blob/main/ReadMeImage/Result1.gif?raw=true"/><br>

좌상단 버튼을 통해 기능의 on/off와 투시되는 영역에대한 크기와 알파조작이 가능합니다.
<br>

[링크 - 웹에서 실행](https://haiun.github.io/URP_PenetrableCamera_TEST/, "웹에서 실행")<br>
<br>
이 프로젝트로 구현한 투시카메라기능은 아래와 같은 강점을 가집니다.<br>

1. 일반 오브젝트를 렌더링 하는데에 쓴 셰이더를 수정하지 않았습니다.<br>
2. 혼합 마스크 연출을 직관적으로 구현 가능합니다.<br>
3. Z버퍼를 활용하지 않는 포스트이펙트에 대응이 가능합니다.<br>
4. 다른 오브젝트들을 추가로 렌더링 하지 않습니다.<br>

<br>

반면 개선이 필요한 점은 아래와 같습니다.<br>

1. 복수 타겟에 대한 투시에 대응이 필요합니다.<br>
   여러개의 투시 시점이 생긴다면, 저장해야할 버퍼와 근경 선별 횟수도 비례해서 늘어날 것입니다.<br>
2. Z버퍼활용 포스트이펙트 대응이 필요합니다.<br>
   알파로 중첩된 영역에 대한 Z값이 모호해지기 때문에, 디더링 필터를 통해 선택적으로 Z값을 선택 혼합한다면 해결 가능할 것으로 보입니다.<br>
3. 스크린과 같은 크기의 저장 버퍼에 대한 고민이 필요합니다.<br>
   해상도와 플랫폼에 따라 업스케일링도 고려할 수 있습니다.<br>

<br>
