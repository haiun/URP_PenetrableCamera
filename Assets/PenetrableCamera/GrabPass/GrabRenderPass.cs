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

        RenderingUtils.ReAllocateIfNeeded(ref _grabTextureHandle, _grabTextureDescriptor);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        var volumeComponent = VolumeManager.instance.stack.GetComponent<GrabVolumeComponent>();
        if (!volumeComponent.isActive.value)
            return;
        
        var cmd = CommandBufferPool.Get();

        var cameraTargetHandle = renderingData.cameraData.renderer.cameraColorTargetHandle;

        Blit(cmd, cameraTargetHandle, _grabTextureHandle);
        cmd.SetGlobalTexture(_defaultSettings.rtName, _grabTextureHandle);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public void Dispose()
    {
        _grabTextureHandle?.Release();
    }
}