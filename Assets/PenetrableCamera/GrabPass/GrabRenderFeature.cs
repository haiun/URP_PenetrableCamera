using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

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

[Serializable]
public class GrabSettings
{
    public string RTName = "_GrabRenderPass0";
}