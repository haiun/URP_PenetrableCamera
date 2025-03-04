# URP_PenetrableCamera

이 프로젝트는 URP RenderFeature와 셰이더를 활용하여 투시카메라 효과를 구현한 것입니다.<br>
투시 효과의 범위와 투명도 설정이 가능하며, Unity 2022.3.56f1 버전에서 작업되었습니다.<br>
URP의 구조 변경으로 다른 버전에서는 정상 동작하지 않을 수 있습니다.<br>
[링크 - 웹에서 실행](https://haiun.github.io/URP_PenetrableCamera_TEST/ "웹에서 실행") <br>
<br>
<img src="https://github.com/haiun/URP_PenetrableCamera/blob/main/ReadMeImage/Result1.gif?raw=true"/><br>
<br>
<br>
## 연구 목표
이 프로젝트는 "Cult of the Lamb - 컬트 오브 더 램"에서 사용된 스타일의 투시카메라를 모방한 것입니다.<br>
<img src="https://github.com/haiun/URP_PenetrableCamera/blob/main/ReadMeImage/target.gif?raw=true"/><br>
<img src="https://github.com/haiun/URP_PenetrableCamera/blob/main/ReadMeImage/target_ex.png?raw=true"/><br>

플레이 중인 캐릭터를 가리는 오브젝트들의 투명도를 설정하여 캐릭터를 표시하고, 이를 통해 근경을 표현합니다.<br>
카메라와 타겟 사이의 오브젝트를 선별하고, 장애물이 없는 이미지와 혼합하여 투시카메라 효과를 구현합니다.<br>

<img src="https://github.com/haiun/URP_PenetrableCamera/blob/main/ReadMeImage/milestone.png?raw=true"/><br>
<br>
<br>

## 장애물의 선별

<img src="https://github.com/haiun/URP_PenetrableCamera/blob/main/ReadMeImage/layer.png?raw=true"/><br>
레이어를 추가합니다.
* Penetrable: 장애물이 될 가능성이 있는 오브젝트가 포함된 레이어입니다. 투시 효과가 적용될 오브젝트는 이 레이어에서 Penetrated로 변경될 수 있습니다.
* Penetrated: 현재 투시 중인 오브젝트가 포함된 레이어입니다. 이 레이어는 투시된 후, 다시 Penetrable로 되돌릴 수 있습니다.
* PenetratingMask: 투시되지 않은 Penetrable 레이어를 제외한 이미지와 투시된 이미지를 혼합하는 마스킹 레이어입니다.

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
PenetrableLayerMask는 Physics로 선별할 레이어들을 정의합니다.
그리고 장애물이라면 PenetratedLayer를 장애물이 아니면 PenetrableLayer로 각 오브젝트의 레이어를 변경합니다.
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
이 프로젝트는 카메라와 타겟이 모두 움직이기 때문에, SphereCast를 통해 장애물 여부를 판단합니다.<br>
장애물이 바뀌지 않는다면, 선별 로직의 비용을 줄일 수 있습니다.<br>
<br>
<br>
## 장애물을 제외한 이미지 렌더링<br>
<img src="https://github.com/haiun/URP_PenetrableCamera/blob/main/ReadMeImage/renderer1.png?raw=true"/><br>

UniversalRenderData에서 기본 렌더링 시 장애물인 Penetrated와, 이미지 혼합 시 사용할 PenetratingMask 레이어를 제외하고 렌더링합니다.<br>

<img src="https://github.com/haiun/URP_PenetrableCamera/blob/main/ReadMeImage/K-001.png?raw=true"/><br>

장애물이 제외된 이미지를 얻을 수 있습니다.<br>

<br>
<br>

## 장애물을 제외한 이미지 저장<br>

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
화면 크기와 동일한 저장용 텍스처 버퍼를 생성하고, 다른 셰이더에서 사용할 이름을 부여합니다. 이름은 임의로 _GrabRenderPass0로 정했습니다.<br>
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
1. ScriptableRendererFeature를 상속받아 GrabRenderPass를 추가하는 GrabRendererFeature를 정의합니다.<br>
    ```csharp
    public class GrabRendererFeature : ScriptableRendererFeature
    {
        [SerializeField]
        private GrabSettings _settings;
        private GrabRenderPass _grabRenderPass;
    
        public override void Create()
        {
            _grabRenderPass = new GrabRenderPass(_settings);
            _grabRenderPass.renderPassEvent = RenderPassEvent.AfterRenderingSkybox;
        }
    
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(_grabRenderPass);
        }
    
        protected override void Dispose(bool disposing)
        {
            _grabRenderPass.Dispose();
        }
    }
    ```
    * GrabRendererFeature가 컴파일되면, RendererFeature관련 매뉴에서 사용 가능해집니다.
2. 활성화된 UniversalRenderData에 GrabRendererFeature를 등록합니다.<br>
   <img src="https://github.com/haiun/URP_PenetrableCamera/blob/main/ReadMeImage/renderer2.png?raw=true"/>
   * 부가적으로 텍스처 버퍼 이름을 변경할 수 있습니다.<br>
3. 씬에서 전역 Volume을 적용하고, GrabRendererFeature를 등록하여 제어합니다.<br>
   <img src="https://github.com/haiun/URP_PenetrableCamera/blob/main/ReadMeImage/volume.png?raw=true"/>
   * 게임 로직에서 RenderFeature 조작이 필요하면, Volume을 통해 조작할 수 있습니다.<br>
<br>
<br>

## 장애물을 포함한 투시 전 이미지 생성

UniversalRenderData에 기본적으로 추가할 수 있는 Render Objects는 특정 레이어에 포함된 오브젝트를 선택적으로 렌더링합니다.<br>

<img src="https://github.com/haiun/URP_PenetrableCamera/blob/main/ReadMeImage/renderer3.png?raw=true"/>

GrabRenderPass 이후, 제외한 Penetrated 레이어 오브젝트를 렌더링하여 투시 전 완성된 화면을 완성합니다.<br>
깊이 버퍼를 포함한 다른 버퍼를 초기화하지 않기 때문에 자연스럽게 렌더링됩니다.<br>

<img src="https://github.com/haiun/URP_PenetrableCamera/blob/main/ReadMeImage/K-002.png?raw=true"/><br>

<br>
<br>

## 두 이미지의 혼합

<img src="https://github.com/haiun/URP_PenetrableCamera/blob/main/ReadMeImage/billboard.png?raw=true"/>

마지막으로 두 이미지를 혼합하기 위해 타겟에 적당한 크기의 빌보드를 배치합니다.<br>

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

일반적인 오브젝트 렌더링 시, TransformObjectToHClip, ComputeScreenPos 함수로 스크린 버퍼와 같은 좌표계를 얻습니다.<br>

```hlsl
sampler2D _GrabRenderPass0;
//...
half4 frag(Varyings IN) : SV_Target
{
    float alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).a * _Alpha;
    half4 color = half4(tex2Dproj(_GrabRenderPass0, IN.screenPos).rgb, alpha);
    return color;
}
```
그 결과값을 fragment 셰이더로 전달하면 _GrabRenderPass0를 참조할 수 있습니다.<br>

<img src="https://github.com/haiun/URP_PenetrableCamera/blob/main/ReadMeImage/renderer4.png?raw=true"/><br>

PenetratingMask 레이어를 추가로 렌더링하고, 해당 셰이더를 PenetratingMask 레이어의 반투명 빌보드에 마스킹 텍스처와 혼합하여 최종 이미지를 생성합니다.<br>
이때 Depth Test를 Always를 설정해서 빌보드의 모든 영역이 항상 렌더링되도록 지정합니다.<br>

<img src="https://github.com/haiun/URP_PenetrableCamera/blob/main/ReadMeImage/K-003.png?raw=true"/>

<br>
<br>

## 프로그램 설명과 결과

<img src="https://github.com/haiun/URP_PenetrableCamera/blob/main/ReadMeImage/Result2.gif?raw=true"/><br>
<img src="https://github.com/haiun/URP_PenetrableCamera/blob/main/ReadMeImage/Result3.gif?raw=true"/><br>
<img src="https://github.com/haiun/URP_PenetrableCamera/blob/main/ReadMeImage/Result4.gif?raw=true"/><br>

좌상단 버튼을 통해 기능의 ON/OFF와 슬라이더UI를 통해 투시되는 영역의 크기 및 알파를 조작할 수 있습니다.<br>

[링크 - 웹에서 실행](https://haiun.github.io/URP_PenetrableCamera_TEST/ "웹에서 실행")

<br>

이 프로젝트로 구현한 투시카메라기능은 아래와 같은 강점을 가집니다.<br>

* 일반 오브젝트를 렌더링 하는데에 쓴 셰이더를 수정하지 않았습니다.<br>
* 혼합 마스크 연출을 직관적으로 구현 가능합니다.<br>
* 깊이 버퍼를 활용하지 않는 포스트이펙트에 대응이 가능합니다.<br>
* 다른 오브젝트들을 추가로 렌더링 하지 않습니다.<br>

<br>

반면 개선이 필요한 점은 아래와 같습니다.<br>

* 복수 타겟에 대한 투시에 대응이 필요합니다.<br>
   * 여러개의 투시 시점이 생긴다면, 저장해야할 버퍼와 장애물의 선별 횟수도 비례해서 늘어날 것입니다.<br>
* 깊이 버퍼를 활용하는 포스트이펙트 대응이 필요합니다.<br>
   * 알파로 중첩된 영역에 대한 깊이값이 모호해지기 때문에, 디더링 필터를 통해 선택적으로 깊이값을 선택 혼합한다면 개선될 것으로 보입니다.<br>
* 스크린과 같은 크기의 저장 버퍼에 대한 고민이 필요합니다.<br>
   * 해상도와 플랫폼에 따라 업스케일링도 고려할 수 있습니다.<br>

<br>
