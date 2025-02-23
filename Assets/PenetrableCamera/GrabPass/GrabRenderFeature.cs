using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class GrabRendererFeature : ScriptableRendererFeature
{
    [SerializeField] private GrabSettings settings;
    private GrabRenderPass _grabRenderPass;

    public override void Create()
    {
        _grabRenderPass = new GrabRenderPass(settings);
        
        _grabRenderPass.renderPassEvent = RenderPassEvent.AfterRenderingSkybox;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType == CameraType.Game)
        {
            renderer.EnqueuePass(_grabRenderPass);
        }
    }

    protected override void Dispose(bool disposing)
    {
        _grabRenderPass.Dispose();
    }
}

[Serializable]
public class GrabSettings
{
    public string rtName = "_GrabRenderPass0";
}