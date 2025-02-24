using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GrabRenderPass : ScriptableRenderPass
{
    private RenderTextureDescriptor _grabTextureDescriptor;
    private RTHandle _grabTextureHandle;
    private readonly GrabSettings _defaultSettings;
    
    public GrabRenderPass(GrabSettings defaultSettings)
    {
        _defaultSettings = defaultSettings;
        _grabTextureDescriptor = new RenderTextureDescriptor(Screen.width, Screen.height, RenderTextureFormat.Default, 0);
    }

    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        _grabTextureDescriptor.width = cameraTextureDescriptor.width;
        _grabTextureDescriptor.height = cameraTextureDescriptor.height;

        // 저장용 버퍼의 크기 변경이 필요하다면 재할당합니다.
        RenderingUtils.ReAllocateIfNeeded(ref _grabTextureHandle, _grabTextureDescriptor);
        
        // 저장용 버퍼의 이름을 지정합니다.
        cmd.SetGlobalTexture(_defaultSettings.RTName, _grabTextureHandle);
    }

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

    public void Dispose()
    {
        _grabTextureHandle?.Release();
    }
}