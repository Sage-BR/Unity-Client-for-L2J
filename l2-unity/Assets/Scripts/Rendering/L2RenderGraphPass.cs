using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

/// <summary>Registers the L2 multi-target command sequences with URP Render Graph.</summary>
public abstract class L2RenderGraphPass : ScriptableRenderPass
{
    readonly List<Material> materialSnapshots = new();
    int snapshotIndex;

    protected L2RenderGraphPass()
    {
        requiresIntermediateTexture = true;
    }

    // Command buffers execute later: each draw must retain its own material values.
    protected Material SnapshotMaterial(Material source)
    {
        if (snapshotIndex == materialSnapshots.Count)
            materialSnapshots.Add(new Material(source) { hideFlags = HideFlags.HideAndDontSave });
        var snapshot = materialSnapshots[snapshotIndex++];
        snapshot.shader = source.shader;
        snapshot.CopyPropertiesFromMaterial(source);
        return snapshot;
    }

    protected void ReleaseMaterialSnapshots()
    {
        foreach (var material in materialSnapshots)
            CoreUtils.Destroy(material);
        materialSnapshots.Clear();
    }

    protected virtual L2FxCompositorSettings EffectSettings => null;
    protected virtual IEnumerable<RTHandle> PersistentTargets { get { yield break; } }
    protected virtual void Prepare(L2RenderGraphFrame frame) { }
    protected virtual void RegisterRendererLists(L2RenderGraphFrame frame) { }
    protected abstract void Render(CommandBuffer cmd, ref L2RenderGraphFrame frame);

    sealed class PassData
    {
        public L2RenderGraphPass pass;
        public L2RenderGraphFrame frame;
        public TextureHandle color;
        public TextureHandle depth;
    }

    public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData)
    {
        var resources = frameData.Get<UniversalResourceData>();
        if (!resources.activeColorTexture.IsValid())
            return;

        var frame = new L2RenderGraphFrame(frameData, graph);
        Prepare(frame);
        RegisterRendererLists(frame);

        using (var builder = graph.AddUnsafePass<PassData>(GetType().Name, out var data))
        {
            data.pass = this;
            data.frame = frame;
            data.color = resources.activeColorTexture;
            data.depth = resources.activeDepthTexture;
            builder.UseTexture(data.color, AccessFlags.ReadWrite);
            if (data.depth.IsValid())
                builder.UseTexture(data.depth, AccessFlags.ReadWrite);
            if (resources.cameraDepthTexture.IsValid() && !resources.cameraDepthTexture.Equals(data.depth))
                builder.UseTexture(resources.cameraDepthTexture, AccessFlags.Read);
            if (resources.cameraOpaqueTexture.IsValid() && !resources.cameraOpaqueTexture.Equals(data.color))
                builder.UseTexture(resources.cameraOpaqueTexture, AccessFlags.Read);
            var imported = new HashSet<RTHandle>();
            foreach (var target in PersistentTargets)
                if (target != null && imported.Add(target))
                    builder.UseTexture(graph.ImportTexture(target), AccessFlags.ReadWrite);
            foreach (var list in frame.RendererLists)
                builder.UseRendererList(list);
            builder.AllowPassCulling(false);
            builder.AllowGlobalStateModification(true);
            builder.SetRenderFunc((PassData payload, UnsafeGraphContext context) =>
            {
                payload.frame.cameraColor = payload.color;
                payload.frame.cameraDepth = payload.depth;
                var cmd = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);
                payload.pass.snapshotIndex = 0;
                payload.pass.Render(cmd, ref payload.frame);
            });
        }
    }
}

public sealed class L2RenderGraphFrame
{
    public readonly UniversalCameraData cameraData;
    readonly UniversalRenderingData renderingData;
    readonly UniversalLightData lightData;
    readonly RenderGraph graph;
    readonly Dictionary<(int, int, int, SortingCriteria), RendererListHandle> lists = new();
    public RTHandle cameraColor;
    public RTHandle cameraDepth;
    public IEnumerable<RendererListHandle> RendererLists => lists.Values;

    public L2RenderGraphFrame(ContextContainer frameData, RenderGraph graph)
    {
        cameraData = frameData.Get<UniversalCameraData>();
        renderingData = frameData.Get<UniversalRenderingData>();
        lightData = frameData.Get<UniversalLightData>();
        this.graph = graph;
    }

    public void Register(int mask, RenderQueueRange range, SortingCriteria sorting)
    {
        if (mask == 0) return;
        var key = (mask, range.lowerBound, range.upperBound, sorting);
        if (lists.ContainsKey(key)) return;
        var drawing = RenderingUtils.CreateDrawingSettings(new ShaderTagId("UniversalForwardOnly"), renderingData, cameraData, lightData, sorting);
        drawing.perObjectData = PerObjectData.None;
        var filtering = new FilteringSettings(range, mask);
        lists.Add(key, graph.CreateRendererList(new RendererListParams(renderingData.cullResults, drawing, filtering)));
    }

    public void Draw(CommandBuffer cmd, int mask, RenderQueueRange range, SortingCriteria sorting)
    {
        if (mask != 0)
            cmd.DrawRendererList(lists[(mask, range.lowerBound, range.upperBound, sorting)]);
    }
}
