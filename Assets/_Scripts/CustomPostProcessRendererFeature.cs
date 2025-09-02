using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CustomPostProcessRendererFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class CustomPostProcessSettings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        public Material postProcessMaterial;
    }

    public CustomPostProcessSettings settings = new CustomPostProcessSettings();
    private CustomPostProcessPass customPass;

    public override void Create()
    {
        customPass = new CustomPostProcessPass(settings.renderPassEvent, settings.postProcessMaterial);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.postProcessMaterial == null) return;
        
        // ▼▼▼ 変更点1: ここでソースを渡す処理を削除 ▼▼▼
        // 計画段階ではパスをキューに入れるだけにする
        renderer.EnqueuePass(customPass);
    }

    // 内部クラス
    class CustomPostProcessPass : ScriptableRenderPass
    {
        private Material postProcessMaterial;
        
        // ▼▼▼ 変更点2: 不要になった変数を削除 ▼▼▼
        // private RTHandle source;
        private RTHandle temporaryTexture; // 一時テクスチャは引き続き必要

        public CustomPostProcessPass(RenderPassEvent passEvent, Material material)
        {
            this.renderPassEvent = passEvent;
            this.postProcessMaterial = material;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (postProcessMaterial == null) return;
            if (renderingData.cameraData.camera.cameraType != CameraType.Game) return;

            CommandBuffer cmd = CommandBufferPool.Get("Custom Post-Process");

            // ▼▼▼ 変更点3: 描画ソースを「実行」段階で取得する ▼▼▼
            var source = renderingData.cameraData.renderer.cameraColorTargetHandle;

            // 一時的なレンダーテクスチャを取得
            var cameraTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            cameraTargetDescriptor.depthBufferBits = 0; // ポストエフェクトに深度は不要
            RenderingUtils.ReAllocateIfNeeded(ref temporaryTexture, cameraTargetDescriptor);

            // ソースから一時テクスチャへ、マテリアルを適用してBlit（転写）
            Blit(cmd, source, temporaryTexture, postProcessMaterial, 0);
            // 一時テクスチャからソースへ、結果を書き戻す
            Blit(cmd, temporaryTexture, source);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        // ▼▼▼ 変更点4: OnCameraCleanupを追加して一時テクスチャを解放 ▼▼▼
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            if (temporaryTexture != null)
            {
                temporaryTexture.Release();
            }
        }
    }
}