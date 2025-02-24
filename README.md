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
## 근경을 제외한 투시 이미지 렌더링
<img src="https://github.com/haiun/URP_PenetrableCamera/blob/main/ReadMeImage/renderer1.png?raw=true"/><br>
<img src="https://github.com/haiun/URP_PenetrableCamera/blob/main/ReadMeImage/K-001.png?raw=true"/><br>
*UniversalRenderData*에서 기본 렌더링 시 *Penetrated*레이어를 제외하고 렌더링 합니다.<br>
<br>
<br>
## 근경을 제외한 투시 이미지 저장
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
3. 씬에서 전역 Volume을 적용하고 *GrabRendererFeature*를 등록 후 제어합니다.<br>
<br>
<br>
## 근경을 포함한 투시 전 이미지 생성
*GrabRenderPass*이후 제외해썬 *Penetrated*레이어 오브젝트를 렌더링해서 투시 전 정상적인 화면를 완성합니다.
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
<br>

```hlsl
half4 frag(Varyings IN) : SV_Target
{
    float alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).a * _Alpha;
    half4 color = half4(tex2Dproj(_GrabRenderPass0, IN.screenPos).rgb, alpha);
    return color;
}
```
<br>
<br>
## 프로그렘 설명과 결과
좌상단 버튼을 통해 기능의 on/off와 투시되는 영역에대한 크기와 알파조작이 가능합니다.<br>
<br>
이 프로젝트로 구현한 투시카메라기능은 아래와 같은 강점을 가집니다.<br>
1. 일반 오브젝트를 렌더링 하는데에 쓴 셰이더를 수정하지 않았습니다.<br>
2. 혼합 마스크 연출을 직관적으로 구현 가능합니다.<br>
3. Z-버퍼를 활용하지 않는 포스트이펙트에 대응이 가능합니다.<br>
<br>
반면 개선이 필요한 점은 아래와 같습니다.<br>
1. 서로 다른 깊이에 대한 투시<br>
2. z-버퍼활용 포스트이펙트<br>
3. 스크린과 같은 크기의 저장 버퍼<br>
<br>
