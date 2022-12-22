using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace OccaSoftware.LSPP.Runtime
{
    public class LightScatteringRenderFeature : ScriptableRendererFeature
    {
        class LightScatteringRenderPass : ScriptableRenderPass
        {
            private Material occluderMaterial;
            private Material mergeMaterial;

            private RenderTargetHandle occluderRT;
            private RenderTargetHandle lightScatteringRT;
            private RenderTargetHandle mergeRT;

            private const string occluderRtId = "_Occluders_LSPP";
            private const string lightScatterRtId = "_Scattering_LSPP";
            private const string mergeRtId = "_Merge_LSPP";

            private const string bufferPoolId = "LightScatteringPP";

            private LightScatteringPostProcess lspp;
            private Material lsppMaterial = null;

            public LightScatteringRenderPass()
            {
                occluderRT.Init(occluderRtId);
                lightScatteringRT.Init(lightScatterRtId);
                mergeRT.Init(mergeRtId);
            }

            internal void SetupMaterials()
            {
                Shader lsppShader = Shader.Find("Shader Graphs/LightScattering_LSPP");
                Shader occluderShader = Shader.Find("Shader Graphs/Occluders_LSPP");
                Shader mergeShader = Shader.Find("Shader Graphs/MergeLightScattering_LSPP");


                if (lsppShader != null && lsppMaterial == null)
                    lsppMaterial = CoreUtils.CreateEngineMaterial(lsppShader);

                if (occluderShader != null && occluderMaterial == null)
                    occluderMaterial = CoreUtils.CreateEngineMaterial(occluderShader);

                if (mergeShader != null && mergeMaterial == null)
                    mergeMaterial = CoreUtils.CreateEngineMaterial(mergeShader);
            }

            internal bool HasAllMaterials()
            {
                if (lsppMaterial == null)
                    return false;

                if (occluderMaterial == null)
                    return false;

                if (mergeMaterial == null)
                    return false;

                return true;
            }

            internal bool RegisterStackComponent()
            {
                lspp = VolumeManager.instance.stack.GetComponent<LightScatteringPostProcess>();

                if (lspp == null)
                    return false;

                return lspp.IsActive();
            }

            private void ClearTemporaryRT(CommandBuffer cmd, RenderTargetHandle rt)
			{
                cmd.SetRenderTarget(rt.id);
                cmd.ClearRenderTarget(true, true, Color.black);
			}

            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
                cmd.GetTemporaryRT(mergeRT.id, descriptor);

                descriptor.width /= 2;
                descriptor.height /= 2;
                cmd.GetTemporaryRT(lightScatteringRT.id, descriptor);
                cmd.GetTemporaryRT(occluderRT.id, descriptor);

                cmd.SetRenderTarget(occluderRT.id);
                cmd.ClearRenderTarget(true, true, Color.black);
                cmd.SetRenderTarget(lightScatteringRT.id);
                cmd.ClearRenderTarget(true, true, Color.black);
                cmd.SetRenderTarget(mergeRT.id);
                cmd.ClearRenderTarget(true, true, Color.black);
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                UnityEngine.Profiling.Profiler.BeginSample("LSPP");

                // Early exit
                if (!HasAllMaterials())
                    return;

                // Setup commandbuffer
                CommandBuffer cmd = CommandBufferPool.Get(bufferPoolId);

                // Disable Additional Decal Drawing.
                CoreUtils.SetKeyword(cmd, "_DBUFFER_MRT1", false);
                CoreUtils.SetKeyword(cmd, "_DBUFFER_MRT2", false);
                CoreUtils.SetKeyword(cmd, "_DBUFFER_MRT3", false);

                // Draw to occluder texture
                RenderTargetIdentifier source = renderingData.cameraData.renderer.cameraColorTarget;
                Blit(cmd, source, occluderRT.Identifier(), occluderMaterial);
                
                // Set up scattering data texture
                UpdateLSPPMaterial();
                Blit(cmd, occluderRT.Identifier(), lightScatteringRT.Identifier(), lsppMaterial);

                // Merge and blit to screen
                Blit(cmd, source, mergeRT.Identifier(), mergeMaterial);
                Blit(cmd, mergeRT.Identifier(), source);


                // Clean up command buffer
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                CommandBufferPool.Release(cmd);
                UnityEngine.Profiling.Profiler.EndSample();


                void UpdateLSPPMaterial()
                {
                    lsppMaterial.SetFloat(Params.Density.Id, lspp.fogDensity.value);
                    lsppMaterial.SetInt(Params.DoSoften.Id, BoolToInt(lspp.softenScreenEdges.value));
                    lsppMaterial.SetInt(Params.DoAnimate.Id, BoolToInt(lspp.animateSamplingOffset.value));
                    lsppMaterial.SetFloat(Params.MaxRayDistance.Id, lspp.maxRayDistance.value);
                    lsppMaterial.SetInt(Params.SampleCount.Id, lspp.numberOfSamples.value);
                    lsppMaterial.SetColor(Params.Tint.Id, lspp.tint.value);
                    lsppMaterial.SetInt(Params.LightOnScreenRequired.Id, BoolToInt(lspp.lightMustBeOnScreen.value));
                    lsppMaterial.SetInt(Params.FalloffDirective.Id, (int)lspp.falloffBasis.value);


                    static int BoolToInt(bool a)
                    {
                        return a == false ? 0 : 1;
                    }
                }
            }

            public override void OnCameraCleanup(CommandBuffer cmd)
            {
                cmd.ReleaseTemporaryRT(occluderRT.id);
                cmd.ReleaseTemporaryRT(lightScatteringRT.id);
                cmd.ReleaseTemporaryRT(mergeRT.id);
            }
        }

        LightScatteringRenderPass lightScatteringPass;


        private void OnEnable()
        {
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += Recreate;
        }

        private void OnDisable()
        {
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= Recreate;
        }

        private void Recreate(UnityEngine.SceneManagement.Scene current, UnityEngine.SceneManagement.Scene next)
        {
            Create();
        }

        private void LogWarningIfDepthTextureDisabled()
		{
            UniversalRenderPipelineAsset rpAsset = (UniversalRenderPipelineAsset)GraphicsSettings.renderPipelineAsset;
            if (rpAsset != null && !rpAsset.supportsCameraDepthTexture)
            {
                rpAsset.supportsCameraDepthTexture = true;
                Debug.LogWarning("LSPP: The currently active Universal Render Pipeline Asset did not have the Depth Texture option enabled. LSPP automatically enabled the Depth Texture setting on your Universal Render Pipeline Asset to ensure that LSPP can identify occluders.");
            }
        }

        public override void Create()
        {
            lightScatteringPass = new LightScatteringRenderPass();
            lightScatteringPass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

            LogWarningIfDepthTextureDisabled();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.camera.cameraType == CameraType.Reflection)
                return;

            if (!lightScatteringPass.RegisterStackComponent())
                return;

            lightScatteringPass.SetupMaterials();
            if (!lightScatteringPass.HasAllMaterials())
                return;

            renderer.EnqueuePass(lightScatteringPass);
        }


        private static class Params
        {
            public readonly struct Param
            {
                public Param(string property)
                {
                    Property = property;
                    Id = Shader.PropertyToID(property);
                }

                readonly public string Property;
                readonly public int Id;
            }

            public static Param Density = new Param("_Density");
            public static Param DoSoften = new Param("_DoSoften");
            public static Param DoAnimate = new Param("_DoAnimate");
            public static Param MaxRayDistance = new Param("_MaxRayDistance");
            public static Param SampleCount = new Param("_SampleCount");
            public static Param Tint = new Param("_Tint");
            public static Param LightOnScreenRequired = new Param("_LightOnScreenRequired");
            public static Param FalloffDirective = new Param("_FalloffDirective");

        }
    }
}