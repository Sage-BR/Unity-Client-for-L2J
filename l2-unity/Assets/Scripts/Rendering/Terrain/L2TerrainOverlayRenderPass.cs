using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Draws SrcAlpha terrain overlays after opaque dirt, before sky-transparents/FX.
/// Graphics.RenderMesh + UniversalForwardOnly is skipped by URP Deferred's
/// transparent list, so this pass CommandBuffer.DrawMesh's pass 0 explicitly.
/// </summary>
public sealed class L2TerrainOverlayRenderPass : L2RenderGraphPass
{
    static readonly ProfilingSampler Sampler = new ProfilingSampler("L2 Terrain Overlays");
    static bool _loggedSkip;

    public L2TerrainOverlayRenderPass()
    {
        renderPassEvent = RenderPassEvent.AfterRenderingOpaques + 1;
        ConfigureInput(ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Depth);
    }

    protected override void Render(CommandBuffer cmd, ref L2RenderGraphFrame renderingData)
    {
        CameraType type = renderingData.cameraData.camera.cameraType;
        if (type != CameraType.Game && type != CameraType.SceneView && type != CameraType.VR)
            return;

        L2TerrainLayerStack.EnsureAttached();

        RTHandle cameraColor = renderingData.cameraColor;
        RTHandle cameraDepth = renderingData.cameraDepth;
        if (cameraColor == null)
        {
            if (!_loggedSkip)
            {
                _loggedSkip = true;
                Debug.LogWarning("[L2TerrainLayerStack] overlay pass: camera color target is null");
            }
            return;
        }
        using (new ProfilingScope(cmd, Sampler))
        {
            if (cameraDepth != null)
                CoreUtils.SetRenderTarget(cmd, cameraColor, cameraDepth, ClearFlag.None, Color.clear);
            else
                CoreUtils.SetRenderTarget(cmd, cameraColor, ClearFlag.None, Color.clear);
            L2TerrainLayerStack.DrawAll(cmd);
        }
    }
}
