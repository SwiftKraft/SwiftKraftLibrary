using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
#if UNITY_2023_3_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif
using UnityEngine.Rendering.Universal;
#if ENABLE_VR_MODULE && ENABLE_VR
using XRSettings = UnityEngine.XR.XRSettings;
#endif

namespace HorizonBasedAmbientOcclusion.Universal
{
    public class HBAORendererFeature : ScriptableRendererFeature
    {
        private class HBAORenderPass : ScriptableRenderPass
        {
            public HBAO hbao;

#if UNITY_2023_3_OR_NEWER
            // Holds the data needed for the render pass.
            private class PassData
            {
                public Material Material { get; set; }
                public RenderTextureDescriptor TargetDescriptor { get; set; }
                public RenderTextureDescriptor AOTextureDescriptor { get; set; }
                public TextureHandle CameraDepthTexture { get; set; }
                public TextureHandle SourceTexture { get; set; }
                public TextureHandle AOTexture { get; set; }
                public TextureHandle TempTexture { get; set; }
                public TextureHandle DestinationTexture { get; set; }
                public CameraHistoryBuffers HistoryBuffers { get; set; }
                public RenderTargetIdentifier[] TemporalFilterRenderTargets { get; set; }
                public Mesh FullscreenTriangle { get; set; }
                public MaterialPropertyBlock MaterialProperties { get; set; }
                public bool UseLitAO { get; set; }
                public bool UseColorBleeding { get; set; }
                public bool UseBlur { get; set; }
                public bool UseTemporalFilter { get; set; }
                public float DirectLightingStrength { get; set; }
                public bool ShowDebug { get; set; }
                public bool ShowViewNormals { get; set; }
                public bool RenderingInSceneView { get; set; }
            }
#endif

            private static class Pass
            {
                public const int AO = 0;
                public const int AO_Deinterleaved = 1;

                public const int Deinterleave_Depth = 2;
                public const int Deinterleave_Normals = 3;
                public const int Atlas_AO_Deinterleaved = 4;
                public const int Reinterleave_AO = 5;

                public const int Blur = 6;

                public const int Temporal_Filter = 7;

                public const int Copy = 8;

                public const int Composite = 9;

                public const int Debug_ViewNormals = 10;
            }

            private static class ShaderProperties
            {
                public static int mainTex;
                public static int inputTex;
                public static int hbaoTex;
                public static int tempTex;
                public static int tempTex2;
                public static int noiseTex;
                public static int depthTex;
                public static int normalsTex;
                public static int ssaoTex;
                public static int[] depthSliceTex;
                public static int[] normalsSliceTex;
                public static int[] aoSliceTex;
                public static int[] deinterleaveOffset;
                public static int atlasOffset;
                public static int jitter;
                public static int uvTransform;
                public static int inputTexelSize;
                public static int aoTexelSize;
                public static int deinterleavedAOTexelSize;
                public static int reinterleavedAOTexelSize;
                public static int uvToView;
                //public static int worldToCameraMatrix;
                public static int targetScale;
                public static int radius;
                public static int maxRadiusPixels;
                public static int negInvRadius2;
                public static int angleBias;
                public static int aoMultiplier;
                public static int intensity;
                public static int multiBounceInfluence;
                public static int offscreenSamplesContrib;
                public static int maxDistance;
                public static int distanceFalloff;
                public static int baseColor;
                public static int colorBleedSaturation;
                public static int albedoMultiplier;
                public static int colorBleedBrightnessMask;
                public static int colorBleedBrightnessMaskRange;
                public static int blurDeltaUV;
                public static int blurSharpness;
                public static int temporalParams;
                public static int historyBufferRTHandleScale;
                public static int cameraDepthTexture;
                public static int screenSpaceOcclusionTexture;
                public static int screenSpaceOcclusionParam;
#if UNITY_2023_3_OR_NEWER
                public static GlobalKeyword screenSpaceOcclusionKeyword;
#endif

                static ShaderProperties()
                {
                    mainTex = Shader.PropertyToID("_MainTex");
                    inputTex = Shader.PropertyToID("_InputTex");
                    hbaoTex = Shader.PropertyToID("_HBAOTex");
                    tempTex = Shader.PropertyToID("_TempTex");
                    tempTex2 = Shader.PropertyToID("_TempTex2");
                    noiseTex = Shader.PropertyToID("_NoiseTex");
                    depthTex = Shader.PropertyToID("_DepthTex");
                    normalsTex = Shader.PropertyToID("_NormalsTex");
                    ssaoTex = Shader.PropertyToID("_SSAOTex");
                    depthSliceTex = new int[4 * 4];
                    normalsSliceTex = new int[4 * 4];
                    aoSliceTex = new int[4 * 4];
                    for (int i = 0; i < 4 * 4; i++)
                    {
                        depthSliceTex[i] = Shader.PropertyToID("_DepthSliceTex" + i);
                        normalsSliceTex[i] = Shader.PropertyToID("_NormalsSliceTex" + i);
                        aoSliceTex[i] = Shader.PropertyToID("_AOSliceTex" + i);
                    }
                    deinterleaveOffset = new int[] {
                        Shader.PropertyToID("_Deinterleave_Offset00"),
                        Shader.PropertyToID("_Deinterleave_Offset10"),
                        Shader.PropertyToID("_Deinterleave_Offset01"),
                        Shader.PropertyToID("_Deinterleave_Offset11")
                    };
                    atlasOffset = Shader.PropertyToID("_AtlasOffset");
                    jitter = Shader.PropertyToID("_Jitter");
                    uvTransform = Shader.PropertyToID("_UVTransform");
                    inputTexelSize = Shader.PropertyToID("_Input_TexelSize");
                    aoTexelSize = Shader.PropertyToID("_AO_TexelSize");
                    deinterleavedAOTexelSize = Shader.PropertyToID("_DeinterleavedAO_TexelSize");
                    reinterleavedAOTexelSize = Shader.PropertyToID("_ReinterleavedAO_TexelSize");
                    uvToView = Shader.PropertyToID("_UVToView");
                    //worldToCameraMatrix = Shader.PropertyToID("_WorldToCameraMatrix");
                    targetScale = Shader.PropertyToID("_TargetScale");
                    radius = Shader.PropertyToID("_Radius");
                    maxRadiusPixels = Shader.PropertyToID("_MaxRadiusPixels");
                    negInvRadius2 = Shader.PropertyToID("_NegInvRadius2");
                    angleBias = Shader.PropertyToID("_AngleBias");
                    aoMultiplier = Shader.PropertyToID("_AOmultiplier");
                    intensity = Shader.PropertyToID("_Intensity");
                    multiBounceInfluence = Shader.PropertyToID("_MultiBounceInfluence");
                    offscreenSamplesContrib = Shader.PropertyToID("_OffscreenSamplesContrib");
                    maxDistance = Shader.PropertyToID("_MaxDistance");
                    distanceFalloff = Shader.PropertyToID("_DistanceFalloff");
                    baseColor = Shader.PropertyToID("_BaseColor");
                    colorBleedSaturation = Shader.PropertyToID("_ColorBleedSaturation");
                    albedoMultiplier = Shader.PropertyToID("_AlbedoMultiplier");
                    colorBleedBrightnessMask = Shader.PropertyToID("_ColorBleedBrightnessMask");
                    colorBleedBrightnessMaskRange = Shader.PropertyToID("_ColorBleedBrightnessMaskRange");
                    blurDeltaUV = Shader.PropertyToID("_BlurDeltaUV");
                    blurSharpness = Shader.PropertyToID("_BlurSharpness");
                    temporalParams = Shader.PropertyToID("_TemporalParams");
                    historyBufferRTHandleScale = Shader.PropertyToID("_HistoryBuffer_RTHandleScale");
                    cameraDepthTexture = Shader.PropertyToID("_CameraDepthTexture");
                    screenSpaceOcclusionTexture = Shader.PropertyToID("_ScreenSpaceOcclusionTexture");
                    screenSpaceOcclusionParam = Shader.PropertyToID("_AmbientOcclusionParam");
#if UNITY_2023_3_OR_NEWER
                    screenSpaceOcclusionKeyword = GlobalKeyword.Create(ShaderKeywordStrings.ScreenSpaceOcclusion);
#endif
                }

                public static string GetOrthographicProjectionKeyword(bool orthographic)
                {
                    return orthographic ? "ORTHOGRAPHIC_PROJECTION" : "__";
                }

                public static string GetQualityKeyword(HBAO.Quality quality)
                {
                    switch (quality)
                    {
                        case HBAO.Quality.Lowest:
                            return "QUALITY_LOWEST";
                        case HBAO.Quality.Low:
                            return "QUALITY_LOW";
                        case HBAO.Quality.Medium:
                            return "QUALITY_MEDIUM";
                        case HBAO.Quality.High:
                            return "QUALITY_HIGH";
                        case HBAO.Quality.Highest:
                            return "QUALITY_HIGHEST";
                        default:
                            return "QUALITY_MEDIUM";
                    }
                }

                public static string GetNoiseKeyword(HBAO.NoiseType noiseType)
                {
                    switch (noiseType)
                    {
                        case HBAO.NoiseType.InterleavedGradientNoise:
                            return "INTERLEAVED_GRADIENT_NOISE";
                        case HBAO.NoiseType.Dither:
                        case HBAO.NoiseType.SpatialDistribution:
                        default:
                            return "__";
                    }
                }

                public static string GetDeinterleavingKeyword(HBAO.Deinterleaving deinterleaving)
                {
                    switch (deinterleaving)
                    {
                        case HBAO.Deinterleaving.x4:
                            return "DEINTERLEAVED";
                        case HBAO.Deinterleaving.Disabled:
                        default:
                            return "__";
                    }
                }

                public static string GetDebugKeyword(HBAO.DebugMode debugMode)
                {
                    switch (debugMode)
                    {
                        case HBAO.DebugMode.AOOnly:
                            return "DEBUG_AO";
                        case HBAO.DebugMode.ColorBleedingOnly:
                            return "DEBUG_COLORBLEEDING";
                        case HBAO.DebugMode.SplitWithoutAOAndWithAO:
                            return "DEBUG_NOAO_AO";
                        case HBAO.DebugMode.SplitWithAOAndAOOnly:
                            return "DEBUG_AO_AOONLY";
                        case HBAO.DebugMode.SplitWithoutAOAndAOOnly:
                            return "DEBUG_NOAO_AOONLY";
                        case HBAO.DebugMode.Disabled:
                        default:
                            return "__";
                    }
                }

                public static string GetMultibounceKeyword(bool useMultiBounce, bool litAoModeEnabled)
                {
                    return useMultiBounce && !litAoModeEnabled ? "MULTIBOUNCE" : "__";
                }

                public static string GetOffscreenSamplesContributionKeyword(float offscreenSamplesContribution)
                {
                    return offscreenSamplesContribution > 0 ? "OFFSCREEN_SAMPLES_CONTRIBUTION" : "__";
                }

                public static string GetPerPixelNormalsKeyword(HBAO.PerPixelNormals perPixelNormals)
                {
                    switch (perPixelNormals)
                    {
                        case HBAO.PerPixelNormals.Reconstruct4Samples:
                            return "NORMALS_RECONSTRUCT4";
                        case HBAO.PerPixelNormals.Reconstruct2Samples:
                            return "NORMALS_RECONSTRUCT2";
                        case HBAO.PerPixelNormals.Camera:
                        default:
                            return "__";
                    }
                }

                public static string GetBlurRadiusKeyword(HBAO.BlurType blurType)
                {
                    switch (blurType)
                    {
                        case HBAO.BlurType.Narrow:
                            return "BLUR_RADIUS_2";
                        case HBAO.BlurType.Medium:
                            return "BLUR_RADIUS_3";
                        case HBAO.BlurType.Wide:
                            return "BLUR_RADIUS_4";
                        case HBAO.BlurType.ExtraWide:
                            return "BLUR_RADIUS_5";
                        case HBAO.BlurType.None:
                        default:
                            return "BLUR_RADIUS_3";
                    }
                }

                public static string GetVarianceClippingKeyword(HBAO.VarianceClipping varianceClipping)
                {
                    switch (varianceClipping)
                    {
                        case HBAO.VarianceClipping._4Tap:
                            return "VARIANCE_CLIPPING_4TAP";
                        case HBAO.VarianceClipping._8Tap:
                            return "VARIANCE_CLIPPING_8TAP";
                        case HBAO.VarianceClipping.Disabled:
                        default:
                            return "__";
                    }
                }

                public static string GetColorBleedingKeyword(bool colorBleedingEnabled, bool litAoModeEnabled)
                {
                    return colorBleedingEnabled && !litAoModeEnabled ? "COLOR_BLEEDING" : "__";
                }

                public static string GetModeKeyword(HBAO.Mode mode)
                {
                    return mode == HBAO.Mode.LitAO ? "LIT_AO" : "__";
                }
            }

            private static class MersenneTwister
            {
                // Mersenne-Twister random numbers in [0,1).
                public static float[] Numbers = new float[] {
                    //0.463937f,0.340042f,0.223035f,0.468465f,0.322224f,0.979269f,0.031798f,0.973392f,0.778313f,0.456168f,0.258593f,0.330083f,0.387332f,0.380117f,0.179842f,0.910755f,
                    //0.511623f,0.092933f,0.180794f,0.620153f,0.101348f,0.556342f,0.642479f,0.442008f,0.215115f,0.475218f,0.157357f,0.568868f,0.501241f,0.629229f,0.699218f,0.707733f
                    0.556725f,0.005520f,0.708315f,0.583199f,0.236644f,0.992380f,0.981091f,0.119804f,0.510866f,0.560499f,0.961497f,0.557862f,0.539955f,0.332871f,0.417807f,0.920779f,
                    0.730747f,0.076690f,0.008562f,0.660104f,0.428921f,0.511342f,0.587871f,0.906406f,0.437980f,0.620309f,0.062196f,0.119485f,0.235646f,0.795892f,0.044437f,0.617311f
                };
            }

            private class CameraHistoryBuffers
            {
                public Camera camera { get; set; }
                public BufferedRTHandleSystem historyRTSystem { get; set; }
                public int frameCount { get; set; }
                public int lastRenderedFrame { get; set; }
            }

            private enum HistoryBufferType
            {
                AmbientOcclusion,
                ColorBleeding
            }

            private static readonly Vector2[] s_jitter = new Vector2[4 * 4];
            private static readonly float[] s_temporalRotations = { 60.0f, 300.0f, 180.0f, 240.0f, 120.0f, 0.0f };
            private static readonly float[] s_temporalOffsets = { 0.0f, 0.5f, 0.25f, 0.75f };

            private Material material { get; set; }
            private RenderTargetIdentifier source { get; set; }
            private CameraData cameraData { get; set; }
            private RenderTextureDescriptor sourceDesc { get; set; }
            private RenderTextureDescriptor aoDesc { get; set; }
            private RenderTextureDescriptor deinterleavedDepthDesc { get; set; }
            private RenderTextureDescriptor deinterleavedNormalsDesc { get; set; }
            private RenderTextureDescriptor deinterleavedAoDesc { get; set; }
            private RenderTextureDescriptor reinterleavedAoDesc { get; set; }
            private RenderTextureDescriptor ssaoDesc { get; set; }
            private RenderTextureFormat colorFormat { get; set; }
            private RenderTextureFormat ssaoFormat { get; set; }
            private GraphicsFormat graphicsColorFormat { get; set; }
            private GraphicsFormat graphicsDepthFormat { get; set; }
            private GraphicsFormat graphicsNormalsFormat { get; set; }
            private RenderTextureFormat depthFormat { get; set; }
            private RenderTextureFormat normalsFormat { get; set; }
            private bool motionVectorsSupported { get; set; }
            private Texture2D noiseTex { get; set; }
            private static bool isLinearColorSpace { get { return QualitySettings.activeColorSpace == ColorSpace.Linear; } }
            private bool renderingInSceneView { get { return cameraData.camera.cameraType == CameraType.SceneView; } }

            private Mesh fullscreenTriangle
            {
                get
                {
                    if (m_FullscreenTriangle != null)
                        return m_FullscreenTriangle;

                    m_FullscreenTriangle = new Mesh { name = "Fullscreen Triangle" };

                    // Because we have to support older platforms (GLES2/3, DX9 etc) we can't do all of
                    // this directly in the vertex shader using vertex ids :(
                    m_FullscreenTriangle.SetVertices(new List<Vector3>
                    {
                        new Vector3(-1f, -1f, 0f),
                        new Vector3(-1f,  3f, 0f),
                        new Vector3( 3f, -1f, 0f)
                    });
                    m_FullscreenTriangle.SetIndices(new[] { 0, 1, 2 }, MeshTopology.Triangles, 0, false);
                    m_FullscreenTriangle.UploadMeshData(false);

                    return m_FullscreenTriangle;
                }
            }

            private MaterialPropertyBlock materialPropertyBlock
            {
                get
                {
                    if (m_MaterialPropertyBlock != null)
                        return m_MaterialPropertyBlock;

                    m_MaterialPropertyBlock = new MaterialPropertyBlock();

                    return m_MaterialPropertyBlock;
                }
            }

            private Mesh m_FullscreenTriangle;
            private MaterialPropertyBlock m_MaterialPropertyBlock;
            private HBAO.Resolution? m_PreviousResolution;
            private HBAO.NoiseType? m_PreviousNoiseType;
            private bool m_PreviousColorBleedingEnabled;
#if ENABLE_VR_MODULE && ENABLE_VR
            private XRSettings.StereoRenderingMode m_PrevStereoRenderingMode;
#endif
            private string[] m_ShaderKeywords;
            private RenderTargetIdentifier[] m_RtsDepth = new RenderTargetIdentifier[4];
            private RenderTargetIdentifier[] m_RtsNormals = new RenderTargetIdentifier[4];
            private RenderTargetIdentifier[] m_RtsTemporalFilter = new RenderTargetIdentifier[2];
            private List<CameraHistoryBuffers> m_CameraHistoryBuffers = new List<CameraHistoryBuffers>();
            private Vector4[] m_UVToViewPerEye = new Vector4[2];
            private float[] m_RadiusPerEye = new float[2];
            private ProfilingSampler m_ProfilingSampler = new ProfilingSampler("HBAO");


            public void FillSupportedRenderTextureFormats()
            {
                colorFormat = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf) ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.Default;
                ssaoFormat = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.R8) ? RenderTextureFormat.R8 : RenderTextureFormat.ARGB32;
                graphicsColorFormat = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf) ? GraphicsFormat.R16G16B16A16_SFloat : GraphicsFormat.R8G8B8A8_SRGB;
                graphicsDepthFormat = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RFloat) ? GraphicsFormat.R32_SFloat : GraphicsFormat.R16_SFloat;
                graphicsNormalsFormat = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGB2101010) ? GraphicsFormat.A2R10G10B10_UNormPack32 : GraphicsFormat.R8G8B8A8_SRGB;
                depthFormat = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RFloat) ? RenderTextureFormat.RFloat : RenderTextureFormat.RHalf;
                normalsFormat = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGB2101010) ? RenderTextureFormat.ARGB2101010 : RenderTextureFormat.Default;
                motionVectorsSupported = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RGHalf);
            }

            public void Setup(Shader shader, ScriptableRenderer renderer, RenderingData renderingData)
            {
                if (material == null) material = CoreUtils.CreateEngineMaterial(shader);

                //---
                FetchVolumeComponent();

                var passInput = ScriptableRenderPassInput.Depth;
                if (hbao.perPixelNormals.value == HBAO.PerPixelNormals.Camera)
                    passInput |= ScriptableRenderPassInput.Normal;
#if UNITY_2021_2_OR_NEWER
                if (hbao.temporalFilterEnabled.value)
                    passInput |= ScriptableRenderPassInput.Motion;
#endif
                ConfigureInput(passInput);

#if UNITY_2021_2_OR_NEWER
#if !UNITY_2023_3_OR_NEWER
                ConfigureColorStoreAction(RenderBufferStoreAction.DontCare);
#endif

                // Configures where the render pass should be injected.
                // Rendering after PrePasses is usually correct except when depth priming is in play:
                // then we rely on a depth resolve taking place after the PrePasses in order to have it ready for SSAO.
                // Hence we set the event to RenderPassEvent.AfterRenderingPrePasses + 1 at the earliest.
                renderPassEvent = hbao.debugMode.value == HBAO.DebugMode.Disabled ?
                    hbao.mode.value == HBAO.Mode.LitAO ?
                    hbao.renderingPath.value == HBAO.RenderingPath.Deferred ? RenderPassEvent.AfterRenderingGbuffer : RenderPassEvent.AfterRenderingPrePasses + 1 :
                    RenderPassEvent.BeforeRenderingTransparents : RenderPassEvent.AfterRenderingTransparents;
#else
                // Configures where the render pass should be injected.
                // Rendering after PrePasses is usually correct except when depth priming is in play:
                // then we rely on a depth resolve taking place after the PrePasses in order to have it ready for SSAO.
                // Hence we set the event to RenderPassEvent.AfterRenderingPrePasses + 1 at the earliest.
                renderPassEvent = hbao.debugMode.value == HBAO.DebugMode.Disabled ?
                    hbao.mode.value == HBAO.Mode.LitAO ? RenderPassEvent.AfterRenderingPrePasses + 1 : RenderPassEvent.BeforeRenderingTransparents :
                    RenderPassEvent.AfterRenderingTransparents;
#endif
            }

#if UNITY_2023_3_OR_NEWER
            [System.Obsolete]
#endif
            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                //source = new RenderTargetIdentifier("_CameraColorTexture");
#if UNITY_2022_1_OR_NEWER
                source = renderingData.cameraData.renderer.cameraColorTargetHandle;
#else
                source = renderingData.cameraData.renderer.cameraColorTarget;
#endif
                cameraData = renderingData.cameraData;

                /*
                FetchVolumeComponent();

                var passInput = ScriptableRenderPassInput.Depth;
                if (hbao.perPixelNormals.value == HBAO.PerPixelNormals.Camera)
                    passInput |= ScriptableRenderPassInput.Normal;
#if UNITY_2021_2_OR_NEWER
                if (hbao.temporalFilterEnabled.value)
                    passInput |= ScriptableRenderPassInput.Motion;
#endif
                ConfigureInput(passInput);

#if UNITY_2021_2_OR_NEWER
                ConfigureColorStoreAction(RenderBufferStoreAction.DontCare);

                // Configures where the render pass should be injected.
                // Rendering after PrePasses is usually correct except when depth priming is in play:
                // then we rely on a depth resolve taking place after the PrePasses in order to have it ready for SSAO.
                // Hence we set the event to RenderPassEvent.AfterRenderingPrePasses + 1 at the earliest.
                renderPassEvent = hbao.debugMode.value == HBAO.DebugMode.Disabled ?
                    hbao.mode.value == HBAO.Mode.LitAO ?
                    hbao.renderingPath.value == HBAO.RenderingPath.Deferred ? RenderPassEvent.AfterRenderingGbuffer : RenderPassEvent.AfterRenderingPrePasses + 1 :
                    RenderPassEvent.BeforeRenderingTransparents : RenderPassEvent.AfterRenderingTransparents;
#else
                // Configures where the render pass should be injected.
                // Rendering after PrePasses is usually correct except when depth priming is in play:
                // then we rely on a depth resolve taking place after the PrePasses in order to have it ready for SSAO.
                // Hence we set the event to RenderPassEvent.AfterRenderingPrePasses + 1 at the earliest.
                renderPassEvent = hbao.debugMode.value == HBAO.DebugMode.Disabled ?
                    hbao.mode.value == HBAO.Mode.LitAO ? RenderPassEvent.AfterRenderingPrePasses + 1 : RenderPassEvent.BeforeRenderingTransparents :
                    RenderPassEvent.AfterRenderingTransparents;
#endif
                */
            }

            // This method is called before executing the render pass.
            // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
            // When empty this render pass will render to the active camera render target.
            // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
            // The render pipeline will ensure target setup and clearing happens in an performance manner.
#if UNITY_2023_3_OR_NEWER
            [System.Obsolete]
#endif
            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
                if (material == null) return;

                FetchVolumeComponent();

                if (!hbao.IsActive()) return;

                FetchRenderParameters(cameraTextureDescriptor);
                CheckParameters();
                UpdateMaterialProperties();
                UpdateShaderKeywords();
            }

            // Here you can implement the rendering logic.
            // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
            // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
            // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
#if UNITY_2023_3_OR_NEWER
            [System.Obsolete]
#endif
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (material == null)
                {
                    Debug.LogError("HBAO material has not been correctly initialized...");
                    return;
                }

                if (!hbao.IsActive()) return;

#if UNITY_2021_2_OR_NEWER
                var historyBuffers = GetCurrentCameraHistoryBuffers();
                historyBuffers?.historyRTSystem.SwapAndSetReferenceSize(aoDesc.width, aoDesc.height);
#else
                var historyBuffers = null as CameraHistoryBuffers;
#endif

                var cmd = CommandBufferPool.Get("HBAO");

                if (hbao.mode.value == HBAO.Mode.LitAO)
                {
                    CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.ScreenSpaceOcclusion, true);

                    cmd.GetTemporaryRT(ShaderProperties.ssaoTex, aoDesc, FilterMode.Bilinear);
                }
                else
                {
                    cmd.GetTemporaryRT(ShaderProperties.inputTex, sourceDesc, FilterMode.Point);

                    // Source copy
                    CopySource(cmd);
                }

                // AO
                cmd.SetGlobalVector(ShaderProperties.temporalParams, historyBuffers != null ? new Vector2(s_temporalRotations[historyBuffers.frameCount % 6] / 360.0f, s_temporalOffsets[historyBuffers.frameCount % 4]) : Vector2.zero);
                if (hbao.deinterleaving.value == HBAO.Deinterleaving.Disabled)
                {
                    cmd.GetTemporaryRT(ShaderProperties.hbaoTex, aoDesc, FilterMode.Bilinear);
                    AO(cmd);
                }
                else
                {
                    cmd.GetTemporaryRT(ShaderProperties.hbaoTex, reinterleavedAoDesc, FilterMode.Bilinear);
                    DeinterleavedAO(cmd);
                }

                // Blur
                Blur(cmd);

                // Temporal Filter
                TemporalFilter(cmd, historyBuffers);

                // Composite
                Composite(cmd);

                cmd.ReleaseTemporaryRT(ShaderProperties.hbaoTex);
                if (hbao.mode.value != HBAO.Mode.LitAO) cmd.ReleaseTemporaryRT(ShaderProperties.inputTex);

                context.ExecuteCommandBuffer(cmd);

                CommandBufferPool.Release(cmd);
            }

#if UNITY_2023_3_OR_NEWER
            // The RecordRenderGraph function will define the Setup and Rendering functions for our render pass.
            // In the Setup we will configure resources such as which textures it reads from and what render textures
            // it writes to. The Rendering delegate will contain the rendering code that will execute in the render
            // graph execution step.
            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if (material == null || !hbao.IsActive()) return;


                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                UniversalCameraData universalCameraData = frameData.Get<UniversalCameraData>();


                FetchRenderParameters(universalCameraData.cameraTargetDescriptor);
                CheckParameters();
                UpdateMaterialPropertiesRG(universalCameraData);
                UpdateShaderKeywordsRG(universalCameraData);


                var historyBuffers = GetCurrentCameraHistoryBuffersRG(universalCameraData);
                historyBuffers?.historyRTSystem.SwapAndSetReferenceSize(aoDesc.width, aoDesc.height);


                using (var builder = renderGraph.AddUnsafePass<PassData>("HBAO Pass", out var passData, m_ProfilingSampler))
                {
                    // Shader keyword changes are considered as global state modifications
                    builder.AllowGlobalStateModification(true);
                    builder.AllowPassCulling(false);

                    TextureHandle cameraDepthTexture = hbao.renderingPath.value == HBAO.RenderingPath.Deferred ? resourceData.activeDepthTexture : resourceData.cameraDepthTexture;
                    TextureHandle cameraNormalsTexture = resourceData.cameraNormalsTexture;
                    TextureHandle motionVectorsTexture = resourceData.motionVectorColor;

                    // source texture is the active color buffer
                    TextureHandle sourceTexture = resourceData.activeColorTexture;

                    // descriptor that match the size and format of the pipeline color buffer.
                    RenderTextureDescriptor targetDesc = universalCameraData.cameraTargetDescriptor;
                    targetDesc.depthBufferBits = 0; // we don't need the depth buffer
                    targetDesc.msaaSamples = 1;

                    bool passUsesCameraNormals = hbao.perPixelNormals.value == HBAO.PerPixelNormals.Camera;
                    bool passUsesMotionVectors = hbao.temporalFilterEnabled.value;
                    bool passUsesLitAO = hbao.mode.value == HBAO.Mode.LitAO;
                    bool passUsesBlur = hbao.blurType.value != HBAO.BlurType.None;
                    bool passUsesTemporalFilter = hbao.temporalFilterEnabled.value;
                    bool passUsesColorBleeding = hbao.colorBleedingEnabled.value;
                    bool passShowDebug = hbao.debugMode.value != HBAO.DebugMode.Disabled;

                    // fill up the passData with the data needed by the pass
                    passData.Material = material;
                    passData.TargetDescriptor = targetDesc;
                    passData.AOTextureDescriptor = aoDesc;
                    passData.CameraDepthTexture = cameraDepthTexture;
                    passData.SourceTexture = sourceTexture;
                    passData.AOTexture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, aoDesc, "_HBAO_AOTexture0", false, FilterMode.Bilinear);
                    passData.TempTexture = passUsesBlur ? UniversalRenderer.CreateRenderGraphTexture(renderGraph, aoDesc, "_HBAO_AOTexture1", false, FilterMode.Bilinear) : TextureHandle.nullHandle;
                    passData.DestinationTexture = passUsesLitAO && !passShowDebug ?
                            UniversalRenderer.CreateRenderGraphTexture(renderGraph, ssaoDesc, "_ScreenSpaceOcclusionTexture", false, FilterMode.Bilinear) :
                            UniversalRenderer.CreateRenderGraphTexture(renderGraph, targetDesc, "_ScreenSpaceOcclusionTexture", false, FilterMode.Bilinear);
                    passData.HistoryBuffers = historyBuffers;
                    passData.TemporalFilterRenderTargets = m_RtsTemporalFilter;
                    passData.FullscreenTriangle = fullscreenTriangle;
                    passData.MaterialProperties = materialPropertyBlock;
                    passData.UseLitAO = passUsesLitAO;
                    passData.UseColorBleeding = passUsesColorBleeding;
                    passData.UseBlur = passUsesBlur;
                    passData.UseTemporalFilter = passUsesTemporalFilter;
                    passData.DirectLightingStrength = hbao.directLightingStrength.value;
                    passData.ShowDebug = passShowDebug;
                    passData.ShowViewNormals = hbao.debugMode.value == HBAO.DebugMode.ViewNormals;
                    passData.RenderingInSceneView = universalCameraData.camera.cameraType == CameraType.SceneView;

                    // declare the textures used in this pass
                    builder.UseTexture(cameraDepthTexture, AccessFlags.Read);
                    if (passUsesCameraNormals)
                        builder.UseTexture(cameraNormalsTexture, AccessFlags.Read);
                    if (passUsesMotionVectors)
                        builder.UseTexture(motionVectorsTexture, AccessFlags.Read);
                    builder.UseTexture(passData.SourceTexture, passUsesLitAO && !passShowDebug ? AccessFlags.Read : AccessFlags.ReadWrite);
                    builder.UseTexture(passData.AOTexture, AccessFlags.ReadWrite);
                    if (passData.TempTexture.IsValid())
                        builder.UseTexture(passData.TempTexture, AccessFlags.ReadWrite);
                    builder.UseTexture(passData.DestinationTexture, passUsesLitAO && !passShowDebug ? AccessFlags.Write : AccessFlags.ReadWrite);

                    // for litAO make SSAO texture global after this pass
                    if (passUsesLitAO && !passShowDebug)
                    {
                        //resourceData.ssaoTexture = passData.DestinationTexture;
                        builder.SetGlobalTextureAfterPass(passData.DestinationTexture, ShaderProperties.screenSpaceOcclusionTexture);
                    }

                    builder.SetRenderFunc((PassData data, UnsafeGraphContext context) => ExecutePass(data, context));
                }
            }

            // ExecutePass is the render function for each of the blit render graph recordings. This is good
            // practice to avoid using variables outside of the lambda it is called from.
            // It is static to avoid using member variables which could cause unintended behaviour.
            private static void ExecutePass(PassData data, UnsafeGraphContext rgContext)
            {
                var cmd = CommandBufferHelpers.GetNativeCommandBuffer(rgContext.cmd);
                //var mpb = rgContext.renderGraphPool.GetTempMaterialPropertyBlock(); // allocate GC, replaced by custom solution below
                var mpb = data.MaterialProperties;

                mpb.SetTexture(ShaderProperties.cameraDepthTexture, data.CameraDepthTexture); // is it really required?

                if (!data.UseLitAO || data.ShowDebug)
                    BlitFullscreenTriangle(cmd, data.SourceTexture, data.DestinationTexture, data.Material, data.FullscreenTriangle, Pass.Copy, mpb);

                // AO
                mpb.SetVector(ShaderProperties.temporalParams, data.HistoryBuffers != null ? new Vector2(s_temporalRotations[data.HistoryBuffers.frameCount % 6] / 360.0f, s_temporalOffsets[data.HistoryBuffers.frameCount % 4]) : Vector2.zero);
                BlitFullscreenTriangleWithClear(cmd, data.SourceTexture, data.AOTexture, data.Material, new Color(0, 0, 0, 1), data.FullscreenTriangle, Pass.AO, mpb);

                // blur
                if (data.UseBlur)
                {
                    float width = data.AOTextureDescriptor.width;
                    float height = data.AOTextureDescriptor.height;
                    if (data.TargetDescriptor.useDynamicScale)
                    {
                        width *= ScalableBufferManager.widthScaleFactor;
                        height *= ScalableBufferManager.heightScaleFactor;
                    }

                    mpb.SetVector(ShaderProperties.blurDeltaUV, new Vector2(1f / width, 0));
                    BlitFullscreenTriangle(cmd, data.AOTexture, data.TempTexture, data.Material, data.FullscreenTriangle, Pass.Blur, mpb);

                    mpb.SetVector(ShaderProperties.blurDeltaUV, new Vector2(0, 1f / height));
                    BlitFullscreenTriangle(cmd, data.TempTexture, data.AOTexture, data.Material, data.FullscreenTriangle, Pass.Blur, mpb);
                }

                mpb.SetTexture(ShaderProperties.hbaoTex, data.AOTexture);

                // temporal filter
                if (data.UseTemporalFilter && !data.RenderingInSceneView && data.HistoryBuffers != null)
                {
                    mpb.SetVector(ShaderProperties.historyBufferRTHandleScale, data.HistoryBuffers.historyRTSystem.rtHandleProperties.rtHandleScale);

                    if (data.HistoryBuffers.frameCount == 0)
                    {
                        // buffers were just allocated this frame, clear them (previous frame RT)
                        RenderTargetIdentifier aoRenderTargetIdentifier = new RenderTargetIdentifier(data.HistoryBuffers.historyRTSystem.GetFrameRT((int)HistoryBufferType.AmbientOcclusion, 1), 0, CubemapFace.Unknown, RenderTargetIdentifier.AllDepthSlices);
                        cmd.SetRenderTarget(aoRenderTargetIdentifier, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                        cmd.ClearRenderTarget(false, true, Color.white);
                        if (data.UseColorBleeding)
                        {
                            RenderTargetIdentifier cbRenderTargetIdentifier = new RenderTargetIdentifier(data.HistoryBuffers.historyRTSystem.GetFrameRT((int)HistoryBufferType.ColorBleeding, 1), 0, CubemapFace.Unknown, RenderTargetIdentifier.AllDepthSlices);
                            cmd.SetRenderTarget(cbRenderTargetIdentifier, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                            cmd.ClearRenderTarget(false, true, new Color(0, 0, 0, 1));
                        }
                    }

                    var viewportRect = new Rect(Vector2.zero, data.HistoryBuffers.historyRTSystem.rtHandleProperties.currentViewportSize);

                    if (data.UseColorBleeding)
                    {
                        // For Color Bleeding we have 2 history buffers to fill so there are 2 render targets.
                        // AO is still contained in Color Bleeding history buffer (alpha channel) so that we
                        // can use it as a render texture for the composite pass.
                        var currentFrameAORT = data.HistoryBuffers.historyRTSystem.GetFrameRT((int)HistoryBufferType.AmbientOcclusion, 0);
                        var currentFrameCBRT = data.HistoryBuffers.historyRTSystem.GetFrameRT((int)HistoryBufferType.ColorBleeding, 0);
                        var previousFrameAORT = data.HistoryBuffers.historyRTSystem.GetFrameRT((int)HistoryBufferType.AmbientOcclusion, 1);
                        var previousFrameCBRT = data.HistoryBuffers.historyRTSystem.GetFrameRT((int)HistoryBufferType.ColorBleeding, 1);
                        data.TemporalFilterRenderTargets[0] = currentFrameAORT;
                        data.TemporalFilterRenderTargets[1] = currentFrameCBRT;
                        //cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
                        mpb.SetTexture(ShaderProperties.tempTex, previousFrameCBRT);
                        BlitFullscreenTriangle(cmd, previousFrameAORT, data.TemporalFilterRenderTargets, viewportRect, data.Material, data.FullscreenTriangle, Pass.Temporal_Filter, mpb);
                        //cmd.SetViewProjectionMatrices(cameraData.camera.worldToCameraMatrix, cameraData.camera.projectionMatrix);

                        mpb.SetTexture(ShaderProperties.hbaoTex, currentFrameCBRT);
                    }
                    else
                    {
                        // AO history buffer contains ao in aplha channel so we can just use history as
                        // a render texture for the composite pass.
                        var currentFrameRT = data.HistoryBuffers.historyRTSystem.GetFrameRT((int)HistoryBufferType.AmbientOcclusion, 0);
                        var previousFrameRT = data.HistoryBuffers.historyRTSystem.GetFrameRT((int)HistoryBufferType.AmbientOcclusion, 1);
                        BlitFullscreenTriangle(cmd, previousFrameRT, currentFrameRT, viewportRect, data.Material, data.FullscreenTriangle, Pass.Temporal_Filter, mpb);

                        mpb.SetTexture(ShaderProperties.hbaoTex, currentFrameRT);
                    }

                    // increment buffers frameCount for next frame, track last buffer use
                    data.HistoryBuffers.frameCount++;
                    data.HistoryBuffers.lastRenderedFrame = Time.frameCount;
                }
                else
                    mpb.SetVector(ShaderProperties.historyBufferRTHandleScale, Vector4.one);

                // composite
                if (data.UseLitAO && !data.ShowDebug)
                {
                    BlitFullscreenTriangle(cmd, data.SourceTexture, data.DestinationTexture, data.Material, data.FullscreenTriangle, Pass.Composite, mpb);

                    // set global ambient occlusion keyword and param so that it is used by lit shaders
                    cmd.SetKeyword(ShaderProperties.screenSpaceOcclusionKeyword, true);
                    cmd.SetGlobalVector(ShaderProperties.screenSpaceOcclusionParam, new Vector4(1f, 0f, 0f, data.DirectLightingStrength));
                }
                else
                    BlitFullscreenTriangle(cmd, data.DestinationTexture, data.SourceTexture, data.Material, data.FullscreenTriangle, data.ShowViewNormals ? Pass.Debug_ViewNormals : Pass.Composite, mpb);
            }
#endif

            /// Cleanup any allocated resources that were created during the execution of this render pass.
            public override void FrameCleanup(CommandBuffer cmd)
            {
                if (hbao.mode.value == HBAO.Mode.LitAO)
                {
                    cmd.ReleaseTemporaryRT(ShaderProperties.ssaoTex); // TODO: should not be called with RenderGraph rendering

#if UNITY_2023_3_OR_NEWER
                    cmd.SetKeyword(ShaderProperties.screenSpaceOcclusionKeyword, false);
#else
                    CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.ScreenSpaceOcclusion, false);
#endif
                }

                // we release any camera history buffers that has not rendered for more than 1 frames
                for (var i = m_CameraHistoryBuffers.Count - 1; i >= 0; i--)
                {
                    var buffers = m_CameraHistoryBuffers[i];
                    if (Time.frameCount - buffers.lastRenderedFrame > 1)
                    {
                        ReleaseCameraHistoryBuffers(ref buffers);
                    }
                }
            }

            public void Cleanup()
            {
                for (var i = m_CameraHistoryBuffers.Count - 1; i >= 0; i--)
                {
                    var buffers = m_CameraHistoryBuffers[i];
                    ReleaseCameraHistoryBuffers(ref buffers);
                }

                CoreUtils.Destroy(material);
                CoreUtils.Destroy(noiseTex);
            }

            private void FetchVolumeComponent()
            {
                if (hbao == null)
                    hbao = VolumeManager.instance.stack.GetComponent<HBAO>();
            }

            private void FetchRenderParameters(RenderTextureDescriptor cameraTextureDesc)
            {
                cameraTextureDesc.msaaSamples = 1;
                cameraTextureDesc.depthBufferBits = 0;
                sourceDesc = cameraTextureDesc;

                var width = cameraTextureDesc.width;
                var height = cameraTextureDesc.height;
                var downsamplingFactor = hbao.resolution.value == HBAO.Resolution.Full ? 1 : hbao.deinterleaving.value == HBAO.Deinterleaving.Disabled ? 2 : 1;
                if (downsamplingFactor > 1)
                {
                    width = (width + width % 2) / downsamplingFactor;
                    height = (height + height % 2) / downsamplingFactor;
                }

                aoDesc = GetStereoCompatibleDescriptor(width, height, format: colorFormat, readWrite: RenderTextureReadWrite.Linear);
                ssaoDesc = GetStereoCompatibleDescriptor(width, height, format: ssaoFormat, readWrite: RenderTextureReadWrite.Linear);

                if (hbao.deinterleaving.value != HBAO.Deinterleaving.Disabled)
                {
                    var reinterleavedWidth = cameraTextureDesc.width + (cameraTextureDesc.width % 4 == 0 ? 0 : 4 - (cameraTextureDesc.width % 4));
                    var reinterleavedHeight = cameraTextureDesc.height + (cameraTextureDesc.height % 4 == 0 ? 0 : 4 - (cameraTextureDesc.height % 4));
                    var deinterleavedWidth = reinterleavedWidth / 4;
                    var deinterleavedHeight = reinterleavedHeight / 4;

                    deinterleavedDepthDesc = GetStereoCompatibleDescriptor(deinterleavedWidth, deinterleavedHeight, format: depthFormat, readWrite: RenderTextureReadWrite.Linear);
                    deinterleavedNormalsDesc = GetStereoCompatibleDescriptor(deinterleavedWidth, deinterleavedHeight, format: normalsFormat, readWrite: RenderTextureReadWrite.Linear);
                    deinterleavedAoDesc = GetStereoCompatibleDescriptor(deinterleavedWidth, deinterleavedHeight, format: colorFormat, readWrite: RenderTextureReadWrite.Linear);
                    reinterleavedAoDesc = GetStereoCompatibleDescriptor(reinterleavedWidth, reinterleavedHeight, format: colorFormat, readWrite: RenderTextureReadWrite.Linear);
                }
            }

            private RTHandle HistoryBufferAllocator(RTHandleSystem rtHandleSystem, int frameIndex)
            {
                var texDimension = TextureDimension.Tex2D;
                var sliceCount = 1;
#if ENABLE_VR_MODULE && ENABLE_VR
                if (XRSettings.enabled && XRSettings.stereoRenderingMode == XRSettings.StereoRenderingMode.SinglePassInstanced)
                {
                    texDimension = TextureDimension.Tex2DArray;
                    sliceCount = 2;
                }
#endif
                return rtHandleSystem.Alloc(Vector2.one, colorFormat: graphicsColorFormat, useDynamicScale: true, name: "HBAO_HistoryBuffer_" + frameIndex, dimension: texDimension, slices: sliceCount);
                //return rtHandleSystem.Alloc(scaleFactor: Vector2.one, slices: TextureXR.slices, colorFormat: graphicsColorFormat, dimension: TextureXR.dimension, useDynamicScale: true, name: "HBAO_HistoryBuffer_" + frameIndex);
            }

            private void AllocCameraHistoryBuffers(ref CameraHistoryBuffers buffers)
            {
                buffers = new CameraHistoryBuffers();
                buffers.camera = cameraData.camera;
                buffers.frameCount = 0;
                buffers.historyRTSystem = new BufferedRTHandleSystem(); // https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@12.0/manual/rthandle-system-using.html
                buffers.historyRTSystem.AllocBuffer((int)HistoryBufferType.AmbientOcclusion, HistoryBufferAllocator, 2);
                if (hbao.colorBleedingEnabled.value)
                    buffers.historyRTSystem.AllocBuffer((int)HistoryBufferType.ColorBleeding, HistoryBufferAllocator, 2);

                m_CameraHistoryBuffers.Add(buffers);
            }

#if UNITY_2023_3_OR_NEWER
            private void AllocCameraHistoryBuffersRG(UniversalCameraData cameraData, ref CameraHistoryBuffers buffers)
            {
                buffers = new CameraHistoryBuffers();
                buffers.camera = cameraData.camera;
                buffers.frameCount = 0;
                buffers.historyRTSystem = new BufferedRTHandleSystem(); // https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@12.0/manual/rthandle-system-using.html
                buffers.historyRTSystem.AllocBuffer((int)HistoryBufferType.AmbientOcclusion, HistoryBufferAllocator, 2);
                if (hbao.colorBleedingEnabled.value)
                    buffers.historyRTSystem.AllocBuffer((int)HistoryBufferType.ColorBleeding, HistoryBufferAllocator, 2);

                m_CameraHistoryBuffers.Add(buffers);
            }
#endif

            private void ReleaseCameraHistoryBuffers(ref CameraHistoryBuffers buffers)
            {
                buffers.historyRTSystem.ReleaseAll();
                buffers.historyRTSystem.Dispose();

                m_CameraHistoryBuffers.Remove(buffers);

                buffers = null;
            }

            private CameraHistoryBuffers GetCurrentCameraHistoryBuffers()
            {
                CameraHistoryBuffers buffers = null;
                if (hbao.temporalFilterEnabled.value && !renderingInSceneView)
                {
                    for (var i = 0; i < m_CameraHistoryBuffers.Count; i++)
                    {
                        if (m_CameraHistoryBuffers[i].camera == cameraData.camera)
                        {
                            buffers = m_CameraHistoryBuffers[i];
                            break;
                        }
                    }

                    if ((m_PreviousColorBleedingEnabled != hbao.colorBleedingEnabled.value ||
#if ENABLE_VR_MODULE && ENABLE_VR
                         m_PrevStereoRenderingMode != XRSettings.stereoRenderingMode ||
#endif
                         m_PreviousResolution != hbao.resolution.value)
                         && buffers != null)
                    {
                        ReleaseCameraHistoryBuffers(ref buffers);
                        m_PreviousColorBleedingEnabled = hbao.colorBleedingEnabled.value;
                        m_PreviousResolution = hbao.resolution.value;
#if ENABLE_VR_MODULE && ENABLE_VR
                        m_PrevStereoRenderingMode = XRSettings.stereoRenderingMode;
#endif
                    }

                    if (buffers == null)
                        AllocCameraHistoryBuffers(ref buffers);
                }

                return buffers;
            }

#if UNITY_2023_3_OR_NEWER
            private CameraHistoryBuffers GetCurrentCameraHistoryBuffersRG(UniversalCameraData cameraData)
            {
                CameraHistoryBuffers buffers = null;
                if (hbao.temporalFilterEnabled.value && !(cameraData.cameraType == CameraType.SceneView))
                {
                    for (var i = 0; i < m_CameraHistoryBuffers.Count; i++)
                    {
                        if (m_CameraHistoryBuffers[i].camera == cameraData.camera)
                        {
                            buffers = m_CameraHistoryBuffers[i];
                            break;
                        }
                    }

                    if ((m_PreviousColorBleedingEnabled != hbao.colorBleedingEnabled.value ||
#if ENABLE_VR_MODULE && ENABLE_VR
                         m_PrevStereoRenderingMode != XRSettings.stereoRenderingMode ||
#endif
                         m_PreviousResolution != hbao.resolution.value)
                         && buffers != null)
                    {
                        ReleaseCameraHistoryBuffers(ref buffers);
                        m_PreviousColorBleedingEnabled = hbao.colorBleedingEnabled.value;
                        m_PreviousResolution = hbao.resolution.value;
#if ENABLE_VR_MODULE && ENABLE_VR
                        m_PrevStereoRenderingMode = XRSettings.stereoRenderingMode;
#endif
                    }

                    if (buffers == null)
                        AllocCameraHistoryBuffersRG(cameraData, ref buffers);
                }

                return buffers;
            }
#endif

            private void CopySource(CommandBuffer cmd)
            {
                BlitFullscreenTriangle(cmd, source, ShaderProperties.inputTex, material, fullscreenTriangle, Pass.Copy);
            }

            private void AO(CommandBuffer cmd)
            {
                BlitFullscreenTriangleWithClear(cmd, hbao.mode.value == HBAO.Mode.LitAO ? source : ShaderProperties.inputTex, ShaderProperties.hbaoTex, material, new Color(0, 0, 0, 1), fullscreenTriangle, Pass.AO);
            }

            private void DeinterleavedAO(CommandBuffer cmd)
            {
                // Deinterleave depth & normals (4x4)
                //cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
                for (int i = 0; i < 4; i++)
                {
                    m_RtsDepth[0] = ShaderProperties.depthSliceTex[(i << 2) + 0];
                    m_RtsDepth[1] = ShaderProperties.depthSliceTex[(i << 2) + 1];
                    m_RtsDepth[2] = ShaderProperties.depthSliceTex[(i << 2) + 2];
                    m_RtsDepth[3] = ShaderProperties.depthSliceTex[(i << 2) + 3];
                    m_RtsNormals[0] = ShaderProperties.normalsSliceTex[(i << 2) + 0];
                    m_RtsNormals[1] = ShaderProperties.normalsSliceTex[(i << 2) + 1];
                    m_RtsNormals[2] = ShaderProperties.normalsSliceTex[(i << 2) + 2];
                    m_RtsNormals[3] = ShaderProperties.normalsSliceTex[(i << 2) + 3];

                    int offsetX = (i & 1) << 1; int offsetY = (i >> 1) << 1;
                    cmd.SetGlobalVector(ShaderProperties.deinterleaveOffset[0], new Vector2(offsetX + 0, offsetY + 0));
                    cmd.SetGlobalVector(ShaderProperties.deinterleaveOffset[1], new Vector2(offsetX + 1, offsetY + 0));
                    cmd.SetGlobalVector(ShaderProperties.deinterleaveOffset[2], new Vector2(offsetX + 0, offsetY + 1));
                    cmd.SetGlobalVector(ShaderProperties.deinterleaveOffset[3], new Vector2(offsetX + 1, offsetY + 1));
                    for (int j = 0; j < 4; j++)
                    {
                        cmd.GetTemporaryRT(ShaderProperties.depthSliceTex[j + 4 * i], deinterleavedDepthDesc, FilterMode.Point);
                        cmd.GetTemporaryRT(ShaderProperties.normalsSliceTex[j + 4 * i], deinterleavedNormalsDesc, FilterMode.Point);
                    }
                    BlitFullscreenTriangle(cmd, BuiltinRenderTextureType.CameraTarget, m_RtsDepth, material, fullscreenTriangle, Pass.Deinterleave_Depth); // outputs 4 render textures
                    BlitFullscreenTriangle(cmd, BuiltinRenderTextureType.CameraTarget, m_RtsNormals, material, fullscreenTriangle, Pass.Deinterleave_Normals); // outputs 4 render textures
                }
                //cmd.SetViewProjectionMatrices(cameraData.camera.worldToCameraMatrix, cameraData.camera.projectionMatrix);

                // AO on each layer
                for (int i = 0; i < 4 * 4; i++)
                {
                    cmd.SetGlobalTexture(ShaderProperties.depthTex, ShaderProperties.depthSliceTex[i]);
                    cmd.SetGlobalTexture(ShaderProperties.normalsTex, ShaderProperties.normalsSliceTex[i]);
                    cmd.SetGlobalVector(ShaderProperties.jitter, s_jitter[i]);
                    cmd.GetTemporaryRT(ShaderProperties.aoSliceTex[i], deinterleavedAoDesc, FilterMode.Point);
                    BlitFullscreenTriangleWithClear(cmd, hbao.mode.value == HBAO.Mode.LitAO ? source : ShaderProperties.inputTex, ShaderProperties.aoSliceTex[i], material, new Color(0, 0, 0, 1), fullscreenTriangle, Pass.AO_Deinterleaved); // ao
                    cmd.ReleaseTemporaryRT(ShaderProperties.depthSliceTex[i]);
                    cmd.ReleaseTemporaryRT(ShaderProperties.normalsSliceTex[i]);
                }

                // Atlas Deinterleaved AO, 4x4
                cmd.GetTemporaryRT(ShaderProperties.tempTex, reinterleavedAoDesc, FilterMode.Point);
                for (int i = 0; i < 4 * 4; i++)
                {
                    cmd.SetGlobalVector(ShaderProperties.atlasOffset, new Vector2(((i & 1) + (((i & 7) >> 2) << 1)) * deinterleavedAoDesc.width, (((i & 3) >> 1) + ((i >> 3) << 1)) * deinterleavedAoDesc.height));
                    BlitFullscreenTriangle(cmd, ShaderProperties.aoSliceTex[i], ShaderProperties.tempTex, material, fullscreenTriangle, Pass.Atlas_AO_Deinterleaved); // atlassing
                    cmd.ReleaseTemporaryRT(ShaderProperties.aoSliceTex[i]);
                }

                // Reinterleave AO
                BlitFullscreenTriangle(cmd, ShaderProperties.tempTex, ShaderProperties.hbaoTex, material, fullscreenTriangle, Pass.Reinterleave_AO); // reinterleave
                cmd.ReleaseTemporaryRT(ShaderProperties.tempTex);
            }

            private void Blur(CommandBuffer cmd)
            {
                if (hbao.blurType.value != HBAO.BlurType.None)
                {
                    float width = aoDesc.width;
                    float height = aoDesc.height;
                    if (sourceDesc.useDynamicScale)
                    {
                        width *= ScalableBufferManager.widthScaleFactor;
                        height *= ScalableBufferManager.heightScaleFactor;
                    }

                    cmd.GetTemporaryRT(ShaderProperties.tempTex, aoDesc, FilterMode.Bilinear);
                    cmd.SetGlobalVector(ShaderProperties.blurDeltaUV, new Vector2(1f / width, 0));
                    BlitFullscreenTriangle(cmd, ShaderProperties.hbaoTex, ShaderProperties.tempTex, material, fullscreenTriangle, Pass.Blur);
                    cmd.SetGlobalVector(ShaderProperties.blurDeltaUV, new Vector2(0, 1f / height));
                    BlitFullscreenTriangle(cmd, ShaderProperties.tempTex, ShaderProperties.hbaoTex, material, fullscreenTriangle, Pass.Blur);
                    cmd.ReleaseTemporaryRT(ShaderProperties.tempTex);
                }
            }

            private void TemporalFilter(CommandBuffer cmd, CameraHistoryBuffers buffers)
            {
                if (hbao.temporalFilterEnabled.value && !renderingInSceneView && buffers != null)
                {
                    cmd.SetGlobalVector(ShaderProperties.historyBufferRTHandleScale, buffers.historyRTSystem.rtHandleProperties.rtHandleScale);

                    if (buffers.frameCount == 0)
                    {
                        // buffers were just allocated this frame, clear them (previous frame RT)
                        cmd.SetRenderTarget(buffers.historyRTSystem.GetFrameRT((int)HistoryBufferType.AmbientOcclusion, 1), 0, CubemapFace.Unknown, -1);
                        cmd.ClearRenderTarget(false, true, Color.white);
                        if (hbao.colorBleedingEnabled.value)
                        {
                            cmd.SetRenderTarget(buffers.historyRTSystem.GetFrameRT((int)HistoryBufferType.ColorBleeding, 1), 0, CubemapFace.Unknown, -1);
                            cmd.ClearRenderTarget(false, true, new Color(0, 0, 0, 1));
                        }
                    }

                    var viewportRect = new Rect(Vector2.zero, buffers.historyRTSystem.rtHandleProperties.currentViewportSize);

                    if (hbao.colorBleedingEnabled.value)
                    {
                        // For Color Bleeding we have 2 history buffers to fill so there are 2 render targets.
                        // AO is still contained in Color Bleeding history buffer (alpha channel) so that we
                        // can use it as a render texture for the composite pass.
                        var currentFrameAORT = buffers.historyRTSystem.GetFrameRT((int)HistoryBufferType.AmbientOcclusion, 0);
                        var currentFrameCBRT = buffers.historyRTSystem.GetFrameRT((int)HistoryBufferType.ColorBleeding, 0);
                        var previousFrameAORT = buffers.historyRTSystem.GetFrameRT((int)HistoryBufferType.AmbientOcclusion, 1);
                        var previousFrameCBRT = buffers.historyRTSystem.GetFrameRT((int)HistoryBufferType.ColorBleeding, 1);
                        var rts = new RenderTargetIdentifier[] {
                            currentFrameAORT,
                            currentFrameCBRT
                        };
                        //cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
                        cmd.SetGlobalTexture(ShaderProperties.tempTex, previousFrameCBRT);
                        BlitFullscreenTriangle(cmd, previousFrameAORT, rts, viewportRect, material, fullscreenTriangle, Pass.Temporal_Filter);
                        //cmd.SetViewProjectionMatrices(cameraData.camera.worldToCameraMatrix, cameraData.camera.projectionMatrix);
                        cmd.SetGlobalTexture(ShaderProperties.hbaoTex, currentFrameCBRT);
                    }
                    else
                    {
                        // AO history buffer contains ao in aplha channel so we can just use history as
                        // a render texture for the composite pass.
                        var currentFrameRT = buffers.historyRTSystem.GetFrameRT((int)HistoryBufferType.AmbientOcclusion, 0);
                        var previousFrameRT = buffers.historyRTSystem.GetFrameRT((int)HistoryBufferType.AmbientOcclusion, 1);
                        BlitFullscreenTriangle(cmd, previousFrameRT, currentFrameRT, viewportRect, material, fullscreenTriangle, Pass.Temporal_Filter);
                        cmd.SetGlobalTexture(ShaderProperties.hbaoTex, currentFrameRT);
                    }

                    // increment buffers frameCount for next frame, track last buffer use
                    buffers.frameCount++;
                    buffers.lastRenderedFrame = Time.frameCount;
                }
                else
                    cmd.SetGlobalVector(ShaderProperties.historyBufferRTHandleScale, Vector4.one);
            }

            private void Composite(CommandBuffer cmd)
            {
                BlitFullscreenTriangle(cmd,
                                   hbao.mode.value == HBAO.Mode.LitAO ? source : ShaderProperties.inputTex,
                                   hbao.mode.value == HBAO.Mode.LitAO && hbao.debugMode.value == HBAO.DebugMode.Disabled ? ShaderProperties.ssaoTex : source,
                                   material, fullscreenTriangle,
                                   hbao.debugMode.value == HBAO.DebugMode.ViewNormals ? Pass.Debug_ViewNormals : Pass.Composite
                );

                if (hbao.mode.value == HBAO.Mode.LitAO)
                {
                    cmd.SetGlobalTexture("_ScreenSpaceOcclusionTexture", ShaderProperties.ssaoTex);
                    cmd.SetGlobalVector("_AmbientOcclusionParam", new Vector4(1f, 0f, 0f, hbao.directLightingStrength.value));
                }
            }

            private void UpdateMaterialProperties()
            {
                var sourceWidth = cameraData.cameraTargetDescriptor.width;
                var sourceHeight = cameraData.cameraTargetDescriptor.height;

#if ENABLE_VR_MODULE && ENABLE_VR
                int eyeCount = XRSettings.enabled && XRSettings.stereoRenderingMode == XRSettings.StereoRenderingMode.SinglePassInstanced && !renderingInSceneView ? 2 : 1;
#else
                int eyeCount = 1;
#endif
                for (int viewIndex = 0; viewIndex < eyeCount; viewIndex++)
                {
                    var projMatrix = cameraData.GetProjectionMatrix(viewIndex);
                    float invTanHalfFOVxAR = projMatrix.m00; // m00 => 1.0f / (tanHalfFOV * aspectRatio)
                    float invTanHalfFOV    = projMatrix.m11; // m11 => 1.0f / tanHalfFOV
                    m_UVToViewPerEye[viewIndex] = new Vector4(2.0f / invTanHalfFOVxAR, -2.0f / invTanHalfFOV, -1.0f / invTanHalfFOVxAR, 1.0f / invTanHalfFOV);
                    m_RadiusPerEye[viewIndex] = hbao.radius.value * 0.5f * (sourceHeight / (hbao.deinterleaving.value == HBAO.Deinterleaving.x4 ? 4 : 1) / (2.0f / invTanHalfFOV));
                }

                //float tanHalfFovY = Mathf.Tan(0.5f * cameraData.camera.fieldOfView * Mathf.Deg2Rad);
                //float invFocalLenX = 1.0f / (1.0f / tanHalfFovY * (sourceHeight / (float)sourceWidth));
                //float invFocalLenY = 1.0f / (1.0f / tanHalfFovY);
                float maxRadInPixels = Mathf.Max(16, hbao.maxRadiusPixels.value * Mathf.Sqrt(sourceWidth * sourceHeight / (1080.0f * 1920.0f)));
                maxRadInPixels /= hbao.deinterleaving.value == HBAO.Deinterleaving.x4 ? 4 : 1;

                var targetScale = hbao.deinterleaving.value == HBAO.Deinterleaving.x4 ?
                                      new Vector4(reinterleavedAoDesc.width / (float)sourceWidth, reinterleavedAoDesc.height / (float)sourceHeight, 1.0f / (reinterleavedAoDesc.width / (float)sourceWidth), 1.0f / (reinterleavedAoDesc.height / (float)sourceHeight)) :
                                          hbao.resolution.value == HBAO.Resolution.Half /*&& (settings.perPixelNormals.value == HBAO.PerPixelNormals.Reconstruct2Samples || settings.perPixelNormals.value == HBAO.PerPixelNormals.Reconstruct4Samples)*/ ?
                                              new Vector4((sourceWidth + 0.5f) / sourceWidth, (sourceHeight + 0.5f) / sourceHeight, 1f, 1f) :
                                              Vector4.one;

                material.SetTexture(ShaderProperties.noiseTex, noiseTex);
                material.SetVector(ShaderProperties.inputTexelSize, new Vector4(1f / sourceWidth, 1f / sourceHeight, sourceWidth, sourceHeight));
                if (sourceDesc.useDynamicScale)
                    material.SetVector(ShaderProperties.aoTexelSize, new Vector4(1f / (aoDesc.width * ScalableBufferManager.widthScaleFactor), 1f / (aoDesc.height * ScalableBufferManager.heightScaleFactor), aoDesc.width * ScalableBufferManager.widthScaleFactor, aoDesc.height * ScalableBufferManager.heightScaleFactor));
                else
                    material.SetVector(ShaderProperties.aoTexelSize, new Vector4(1f / aoDesc.width, 1f / aoDesc.height, aoDesc.width, aoDesc.height));
                material.SetVector(ShaderProperties.deinterleavedAOTexelSize, new Vector4(1.0f / deinterleavedAoDesc.width, 1.0f / deinterleavedAoDesc.height, deinterleavedAoDesc.width, deinterleavedAoDesc.height));
                material.SetVector(ShaderProperties.reinterleavedAOTexelSize, new Vector4(1f / reinterleavedAoDesc.width, 1f / reinterleavedAoDesc.height, reinterleavedAoDesc.width, reinterleavedAoDesc.height));
                material.SetVector(ShaderProperties.targetScale, targetScale);
                //material.SetVector(ShaderProperties.uvToView, new Vector4(2.0f * invFocalLenX, -2.0f * invFocalLenY, -1.0f * invFocalLenX, 1.0f * invFocalLenY));
                material.SetVectorArray(ShaderProperties.uvToView, m_UVToViewPerEye);
                //material.SetMatrix(ShaderProperties.worldToCameraMatrix, cameraData.camera.worldToCameraMatrix);
                //material.SetFloat(ShaderProperties.radius, hbao.radius.value * 0.5f * ((sourceHeight / (hbao.deinterleaving.value == HBAO.Deinterleaving.x4 ? 4 : 1)) / (tanHalfFovY * 2.0f)));
                //material.SetFloat(ShaderProperties.radius, hbao.radius.value * 0.5f * ((sourceHeight / (hbao.deinterleaving.value == HBAO.Deinterleaving.x4 ? 4 : 1)) / (invFocalLenY * 2.0f)));
                material.SetFloatArray(ShaderProperties.radius, m_RadiusPerEye);
                material.SetFloat(ShaderProperties.maxRadiusPixels, maxRadInPixels);
                material.SetFloat(ShaderProperties.negInvRadius2, -1.0f / (hbao.radius.value * hbao.radius.value));
                material.SetFloat(ShaderProperties.angleBias, hbao.bias.value);
                material.SetFloat(ShaderProperties.aoMultiplier, 2.0f * (1.0f / (1.0f - hbao.bias.value)));
                material.SetFloat(ShaderProperties.intensity, isLinearColorSpace ? hbao.intensity.value : hbao.intensity.value * 0.454545454545455f);
                material.SetFloat(ShaderProperties.multiBounceInfluence, hbao.multiBounceInfluence.value);
                material.SetFloat(ShaderProperties.offscreenSamplesContrib, hbao.offscreenSamplesContribution.value);
                material.SetFloat(ShaderProperties.maxDistance, hbao.maxDistance.value);
                material.SetFloat(ShaderProperties.distanceFalloff, hbao.distanceFalloff.value);
                material.SetColor(ShaderProperties.baseColor, hbao.baseColor.value);
                material.SetFloat(ShaderProperties.blurSharpness, hbao.sharpness.value);
                material.SetFloat(ShaderProperties.colorBleedSaturation, hbao.saturation.value);
                material.SetFloat(ShaderProperties.colorBleedBrightnessMask, hbao.brightnessMask.value);
                material.SetVector(ShaderProperties.colorBleedBrightnessMaskRange, AdjustBrightnessMaskToGammaSpace(new Vector2(Mathf.Pow(hbao.brightnessMaskRange.value.x, 3), Mathf.Pow(hbao.brightnessMaskRange.value.y, 3))));
            }

#if UNITY_2023_3_OR_NEWER
            private void UpdateMaterialPropertiesRG(UniversalCameraData cameraData)
            {
                var sourceWidth = cameraData.cameraTargetDescriptor.width;
                var sourceHeight = cameraData.cameraTargetDescriptor.height;

#if ENABLE_VR_MODULE && ENABLE_VR
                int eyeCount = XRSettings.enabled && XRSettings.stereoRenderingMode == XRSettings.StereoRenderingMode.SinglePassInstanced && !(cameraData.cameraType == CameraType.SceneView) ? 2 : 1;
#else
                int eyeCount = 1;
#endif
                for (int viewIndex = 0; viewIndex < eyeCount; viewIndex++)
                {
                    var projMatrix = cameraData.GetProjectionMatrix(viewIndex);
                    float invTanHalfFOVxAR = projMatrix.m00; // m00 => 1.0f / (tanHalfFOV * aspectRatio)
                    float invTanHalfFOV = projMatrix.m11; // m11 => 1.0f / tanHalfFOV
                    m_UVToViewPerEye[viewIndex] = new Vector4(2.0f / invTanHalfFOVxAR, -2.0f / invTanHalfFOV, -1.0f / invTanHalfFOVxAR, 1.0f / invTanHalfFOV);
                    m_RadiusPerEye[viewIndex] = hbao.radius.value * 0.5f * (sourceHeight / (hbao.deinterleaving.value == HBAO.Deinterleaving.x4 ? 4 : 1) / (2.0f / invTanHalfFOV));
                }

                //float tanHalfFovY = Mathf.Tan(0.5f * cameraData.camera.fieldOfView * Mathf.Deg2Rad);
                //float invFocalLenX = 1.0f / (1.0f / tanHalfFovY * (sourceHeight / (float)sourceWidth));
                //float invFocalLenY = 1.0f / (1.0f / tanHalfFovY);
                float maxRadInPixels = Mathf.Max(16, hbao.maxRadiusPixels.value * Mathf.Sqrt(sourceWidth * sourceHeight / (1080.0f * 1920.0f)));
                maxRadInPixels /= hbao.deinterleaving.value == HBAO.Deinterleaving.x4 ? 4 : 1;

                var targetScale = hbao.deinterleaving.value == HBAO.Deinterleaving.x4 ?
                                      new Vector4(reinterleavedAoDesc.width / (float)sourceWidth, reinterleavedAoDesc.height / (float)sourceHeight, 1.0f / (reinterleavedAoDesc.width / (float)sourceWidth), 1.0f / (reinterleavedAoDesc.height / (float)sourceHeight)) :
                                          hbao.resolution.value == HBAO.Resolution.Half /*&& (settings.perPixelNormals.value == HBAO.PerPixelNormals.Reconstruct2Samples || settings.perPixelNormals.value == HBAO.PerPixelNormals.Reconstruct4Samples)*/ ?
                                              new Vector4((sourceWidth + 0.5f) / sourceWidth, (sourceHeight + 0.5f) / sourceHeight, 1f, 1f) :
                                              Vector4.one;

                material.SetTexture(ShaderProperties.noiseTex, noiseTex);
                material.SetVector(ShaderProperties.inputTexelSize, new Vector4(1f / sourceWidth, 1f / sourceHeight, sourceWidth, sourceHeight));
                if (sourceDesc.useDynamicScale)
                    material.SetVector(ShaderProperties.aoTexelSize, new Vector4(1f / (aoDesc.width * ScalableBufferManager.widthScaleFactor), 1f / (aoDesc.height * ScalableBufferManager.heightScaleFactor), aoDesc.width * ScalableBufferManager.widthScaleFactor, aoDesc.height * ScalableBufferManager.heightScaleFactor));
                else
                    material.SetVector(ShaderProperties.aoTexelSize, new Vector4(1f / aoDesc.width, 1f / aoDesc.height, aoDesc.width, aoDesc.height));
                material.SetVector(ShaderProperties.deinterleavedAOTexelSize, new Vector4(1.0f / deinterleavedAoDesc.width, 1.0f / deinterleavedAoDesc.height, deinterleavedAoDesc.width, deinterleavedAoDesc.height));
                material.SetVector(ShaderProperties.reinterleavedAOTexelSize, new Vector4(1f / reinterleavedAoDesc.width, 1f / reinterleavedAoDesc.height, reinterleavedAoDesc.width, reinterleavedAoDesc.height));
                material.SetVector(ShaderProperties.targetScale, targetScale);
                //material.SetVector(ShaderProperties.uvToView, new Vector4(2.0f * invFocalLenX, -2.0f * invFocalLenY, -1.0f * invFocalLenX, 1.0f * invFocalLenY));
                material.SetVectorArray(ShaderProperties.uvToView, m_UVToViewPerEye);
                //material.SetMatrix(ShaderProperties.worldToCameraMatrix, cameraData.camera.worldToCameraMatrix);
                //material.SetFloat(ShaderProperties.radius, hbao.radius.value * 0.5f * ((sourceHeight / (hbao.deinterleaving.value == HBAO.Deinterleaving.x4 ? 4 : 1)) / (tanHalfFovY * 2.0f)));
                //material.SetFloat(ShaderProperties.radius, hbao.radius.value * 0.5f * ((sourceHeight / (hbao.deinterleaving.value == HBAO.Deinterleaving.x4 ? 4 : 1)) / (invFocalLenY * 2.0f)));
                material.SetFloatArray(ShaderProperties.radius, m_RadiusPerEye);
                material.SetFloat(ShaderProperties.maxRadiusPixels, maxRadInPixels);
                material.SetFloat(ShaderProperties.negInvRadius2, -1.0f / (hbao.radius.value * hbao.radius.value));
                material.SetFloat(ShaderProperties.angleBias, hbao.bias.value);
                material.SetFloat(ShaderProperties.aoMultiplier, 2.0f * (1.0f / (1.0f - hbao.bias.value)));
                material.SetFloat(ShaderProperties.intensity, isLinearColorSpace ? hbao.intensity.value : hbao.intensity.value * 0.454545454545455f);
                material.SetFloat(ShaderProperties.multiBounceInfluence, hbao.multiBounceInfluence.value);
                material.SetFloat(ShaderProperties.offscreenSamplesContrib, hbao.offscreenSamplesContribution.value);
                material.SetFloat(ShaderProperties.maxDistance, hbao.maxDistance.value);
                material.SetFloat(ShaderProperties.distanceFalloff, hbao.distanceFalloff.value);
                material.SetColor(ShaderProperties.baseColor, hbao.baseColor.value);
                material.SetFloat(ShaderProperties.blurSharpness, hbao.sharpness.value);
                material.SetFloat(ShaderProperties.colorBleedSaturation, hbao.saturation.value);
                material.SetFloat(ShaderProperties.colorBleedBrightnessMask, hbao.brightnessMask.value);
                material.SetVector(ShaderProperties.colorBleedBrightnessMaskRange, AdjustBrightnessMaskToGammaSpace(new Vector2(Mathf.Pow(hbao.brightnessMaskRange.value.x, 3), Mathf.Pow(hbao.brightnessMaskRange.value.y, 3))));
            }
#endif

            private void UpdateShaderKeywords()
            {
                if (m_ShaderKeywords == null || m_ShaderKeywords.Length != 12) m_ShaderKeywords = new string[12];

                m_ShaderKeywords[0] = ShaderProperties.GetOrthographicProjectionKeyword(cameraData.camera.orthographic);
                m_ShaderKeywords[1] = ShaderProperties.GetQualityKeyword(hbao.quality.value);
                m_ShaderKeywords[2] = ShaderProperties.GetNoiseKeyword(hbao.noiseType.value);
                m_ShaderKeywords[3] = ShaderProperties.GetDeinterleavingKeyword(hbao.deinterleaving.value);
                m_ShaderKeywords[4] = ShaderProperties.GetDebugKeyword(hbao.debugMode.value);
                m_ShaderKeywords[5] = ShaderProperties.GetMultibounceKeyword(hbao.useMultiBounce.value, hbao.mode.value == HBAO.Mode.LitAO);
                m_ShaderKeywords[6] = ShaderProperties.GetOffscreenSamplesContributionKeyword(hbao.offscreenSamplesContribution.value);
                m_ShaderKeywords[7] = ShaderProperties.GetPerPixelNormalsKeyword(hbao.perPixelNormals.value);
                m_ShaderKeywords[8] = ShaderProperties.GetBlurRadiusKeyword(hbao.blurType.value);
                m_ShaderKeywords[9] = ShaderProperties.GetVarianceClippingKeyword(hbao.varianceClipping.value);
                m_ShaderKeywords[10] = ShaderProperties.GetColorBleedingKeyword(hbao.colorBleedingEnabled.value, hbao.mode.value == HBAO.Mode.LitAO);
                m_ShaderKeywords[11] = ShaderProperties.GetModeKeyword(hbao.mode.value);

                material.shaderKeywords = m_ShaderKeywords;
            }

#if UNITY_2023_3_OR_NEWER
            private void UpdateShaderKeywordsRG(UniversalCameraData cameraData)
            {
                if (m_ShaderKeywords == null || m_ShaderKeywords.Length != 12) m_ShaderKeywords = new string[12];

                m_ShaderKeywords[0] = ShaderProperties.GetOrthographicProjectionKeyword(cameraData.camera.orthographic);
                m_ShaderKeywords[1] = ShaderProperties.GetQualityKeyword(hbao.quality.value);
                m_ShaderKeywords[2] = ShaderProperties.GetNoiseKeyword(hbao.noiseType.value);
                m_ShaderKeywords[3] = ShaderProperties.GetDeinterleavingKeyword(hbao.deinterleaving.value);
                m_ShaderKeywords[4] = ShaderProperties.GetDebugKeyword(hbao.debugMode.value);
                m_ShaderKeywords[5] = ShaderProperties.GetMultibounceKeyword(hbao.useMultiBounce.value, hbao.mode.value == HBAO.Mode.LitAO);
                m_ShaderKeywords[6] = ShaderProperties.GetOffscreenSamplesContributionKeyword(hbao.offscreenSamplesContribution.value);
                m_ShaderKeywords[7] = ShaderProperties.GetPerPixelNormalsKeyword(hbao.perPixelNormals.value);
                m_ShaderKeywords[8] = ShaderProperties.GetBlurRadiusKeyword(hbao.blurType.value);
                m_ShaderKeywords[9] = ShaderProperties.GetVarianceClippingKeyword(hbao.varianceClipping.value);
                m_ShaderKeywords[10] = ShaderProperties.GetColorBleedingKeyword(hbao.colorBleedingEnabled.value, hbao.mode.value == HBAO.Mode.LitAO);
                m_ShaderKeywords[11] = ShaderProperties.GetModeKeyword(hbao.mode.value);

                material.shaderKeywords = m_ShaderKeywords;
            }
#endif

            private void CheckParameters()
            {
                if (hbao.deinterleaving.value != HBAO.Deinterleaving.Disabled && SystemInfo.supportedRenderTargetCount < 4)
                    hbao.SetDeinterleaving(HBAO.Deinterleaving.Disabled);

                if (hbao.temporalFilterEnabled.value && !motionVectorsSupported)
                    hbao.EnableTemporalFilter(false);

                if (hbao.colorBleedingEnabled.value && hbao.temporalFilterEnabled.value && SystemInfo.supportedRenderTargetCount < 2)
                    hbao.EnableTemporalFilter(false);

                if (hbao.colorBleedingEnabled.value && hbao.mode.value == HBAO.Mode.LitAO)
                    hbao.EnableColorBleeding(false);

                // Noise texture
                if (noiseTex == null || m_PreviousNoiseType != hbao.noiseType.value)
                {
                    CoreUtils.Destroy(noiseTex);

                    CreateNoiseTexture();

                    m_PreviousNoiseType = hbao.noiseType.value;
                }
            }

            private RenderTextureDescriptor GetStereoCompatibleDescriptor(int width, int height, RenderTextureFormat format = RenderTextureFormat.Default, int depthBufferBits = 0, RenderTextureReadWrite readWrite = RenderTextureReadWrite.Default)
            {
                // Inherit the VR setup from the camera descriptor
                var desc = sourceDesc;
                desc.depthBufferBits = depthBufferBits;
                desc.msaaSamples = 1;
                desc.width = width;
                desc.height = height;
                desc.colorFormat = format;

                if (readWrite == RenderTextureReadWrite.sRGB)
                    desc.sRGB = true;
                else if (readWrite == RenderTextureReadWrite.Linear)
                    desc.sRGB = false;
                else if (readWrite == RenderTextureReadWrite.Default)
                    desc.sRGB = isLinearColorSpace;

                return desc;
            }
            
            private static void BlitFullscreenTriangle(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier destination, Material material, Mesh fullscreenTriangle, int passIndex = 0)
            {
                cmd.SetGlobalTexture(ShaderProperties.mainTex, source);
                cmd.SetRenderTarget(destination, 0, CubemapFace.Unknown, RenderTargetIdentifier.AllDepthSlices);
                cmd.DrawMesh(fullscreenTriangle, Matrix4x4.identity, material, 0, passIndex);
            }

            private static void BlitFullscreenTriangle(CommandBuffer cmd, RenderTexture source, RenderTargetIdentifier destination, Material material, Mesh fullscreenTriangle, int passIndex, MaterialPropertyBlock properties)
            {
                properties.SetTexture(ShaderProperties.mainTex, source);
                cmd.SetRenderTarget(new RenderTargetIdentifier(destination, 0, CubemapFace.Unknown, RenderTargetIdentifier.AllDepthSlices), RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                cmd.DrawMesh(fullscreenTriangle, Matrix4x4.identity, material, 0, passIndex, properties);
            }

            private static void BlitFullscreenTriangle(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier destination, Rect viewportRect, Material material, Mesh fullscreenTriangle, int passIndex = 0)
            {
                cmd.SetGlobalTexture(ShaderProperties.mainTex, source);
                cmd.SetRenderTarget(destination, 0, CubemapFace.Unknown, RenderTargetIdentifier.AllDepthSlices);
                cmd.SetViewport(viewportRect);
                cmd.DrawMesh(fullscreenTriangle, Matrix4x4.identity, material, 0, passIndex);
            }

            private static void BlitFullscreenTriangle(CommandBuffer cmd, RenderTexture source, RenderTargetIdentifier destination, Rect viewportRect, Material material, Mesh fullscreenTriangle, int passIndex, MaterialPropertyBlock properties)
            {
                properties.SetTexture(ShaderProperties.mainTex, source);
                cmd.SetRenderTarget(new RenderTargetIdentifier(destination, 0, CubemapFace.Unknown, RenderTargetIdentifier.AllDepthSlices), RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                cmd.SetViewport(viewportRect);
                cmd.DrawMesh(fullscreenTriangle, Matrix4x4.identity, material, 0, passIndex, properties);
            }

            private static void BlitFullscreenTriangle(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier[] destinations, Material material, Mesh fullscreenTriangle, int passIndex = 0)
            {
                cmd.SetGlobalTexture(ShaderProperties.mainTex, source);
                cmd.SetRenderTarget(destinations, destinations[0], 0, CubemapFace.Unknown, RenderTargetIdentifier.AllDepthSlices);
                cmd.DrawMesh(fullscreenTriangle, Matrix4x4.identity, material, 0, passIndex);
            }

            private static void BlitFullscreenTriangle(CommandBuffer cmd, RenderTexture source, RenderTargetIdentifier[] destinations, Material material, Mesh fullscreenTriangle, int passIndex, MaterialPropertyBlock properties)
            {
                properties.SetTexture(ShaderProperties.mainTex, source);
                cmd.SetRenderTarget(destinations, destinations[0], 0, CubemapFace.Unknown, RenderTargetIdentifier.AllDepthSlices);
                cmd.DrawMesh(fullscreenTriangle, Matrix4x4.identity, material, 0, passIndex, properties);
            }

            private static void BlitFullscreenTriangle(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier[] destinations, Rect viewportRect, Material material, Mesh fullscreenTriangle, int passIndex = 0)
            {
                cmd.SetGlobalTexture(ShaderProperties.mainTex, source);
                cmd.SetRenderTarget(destinations, destinations[0], 0, CubemapFace.Unknown, RenderTargetIdentifier.AllDepthSlices);
                cmd.SetViewport(viewportRect);
                cmd.DrawMesh(fullscreenTriangle, Matrix4x4.identity, material, 0, passIndex);
            }

            private static void BlitFullscreenTriangle(CommandBuffer cmd, RenderTexture source, RenderTargetIdentifier[] destinations, Rect viewportRect, Material material, Mesh fullscreenTriangle, int passIndex, MaterialPropertyBlock properties)
            {
                properties.SetTexture(ShaderProperties.mainTex, source);
                cmd.SetRenderTarget(destinations, destinations[0], 0, CubemapFace.Unknown, RenderTargetIdentifier.AllDepthSlices);
                cmd.SetViewport(viewportRect);
                cmd.DrawMesh(fullscreenTriangle, Matrix4x4.identity, material, 0, passIndex, properties);
            }

            private static void BlitFullscreenTriangleWithClear(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier destination, Material material, Color clearColor, Mesh fullscreenTriangle, int passIndex = 0)
            {
                cmd.SetGlobalTexture(ShaderProperties.mainTex, source);
                cmd.SetRenderTarget(destination, 0, CubemapFace.Unknown, RenderTargetIdentifier.AllDepthSlices);
                cmd.ClearRenderTarget(false, true, clearColor);
                cmd.DrawMesh(fullscreenTriangle, Matrix4x4.identity, material, 0, passIndex);
            }

            private static void BlitFullscreenTriangleWithClear(CommandBuffer cmd, RenderTexture source, RenderTargetIdentifier destination, Material material, Color clearColor, Mesh fullscreenTriangle, int passIndex, MaterialPropertyBlock properties)
            {
                properties.SetTexture(ShaderProperties.mainTex, source);
                cmd.SetRenderTarget(new RenderTargetIdentifier(destination, 0, CubemapFace.Unknown, RenderTargetIdentifier.AllDepthSlices), RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                cmd.ClearRenderTarget(false, true, clearColor);
                cmd.DrawMesh(fullscreenTriangle, Matrix4x4.identity, material, 0, passIndex, properties);
            }

            private Vector2 AdjustBrightnessMaskToGammaSpace(Vector2 v)
            {
                return isLinearColorSpace ? v : ToGammaSpace(v);
            }

            private float ToGammaSpace(float v)
            {
                return Mathf.Pow(v, 0.454545454545455f);
            }

            private Vector2 ToGammaSpace(Vector2 v)
            {
                return new Vector2(ToGammaSpace(v.x), ToGammaSpace(v.y));
            }

            private void CreateNoiseTexture()
            {
                noiseTex = new Texture2D(4, 4, SystemInfo.SupportsTextureFormat(TextureFormat.RGHalf) ? TextureFormat.RGHalf : TextureFormat.RGB24, false, true);
                noiseTex.filterMode = FilterMode.Point;
                noiseTex.wrapMode = TextureWrapMode.Repeat;
                int z = 0;
                for (int x = 0; x < 4; ++x)
                {
                    for (int y = 0; y < 4; ++y)
                    {
                        float r1 = hbao.noiseType.value != HBAO.NoiseType.Dither ? 0.25f * (0.0625f * ((x + y & 3) << 2) + (x & 3)) : MersenneTwister.Numbers[z++];
                        float r2 = hbao.noiseType.value != HBAO.NoiseType.Dither ? 0.25f * ((y - x) & 3) : MersenneTwister.Numbers[z++];
                        Color color = new Color(r1, r2, 0);
                        noiseTex.SetPixel(x, y, color);
                    }
                }
                noiseTex.Apply();

                for (int i = 0, j = 0; i < s_jitter.Length; ++i)
                {
                    float r1 = MersenneTwister.Numbers[j++];
                    float r2 = MersenneTwister.Numbers[j++];
                    s_jitter[i] = new Vector2(r1, r2);
                }
            }
        }

        [SerializeField, HideInInspector]
        private Shader shader;
        private HBAORenderPass m_HBAORenderPass;

        void OnDisable()
        {
            m_HBAORenderPass?.Cleanup();
        }

        public override void Create()
        {
            if (!isActive)
            {
                m_HBAORenderPass?.Cleanup();
                m_HBAORenderPass = null;
                return;
            }

            name = "HBAO";

            m_HBAORenderPass = new HBAORenderPass();
            m_HBAORenderPass.FillSupportedRenderTextureFormats();
        }

        protected override void Dispose(bool disposing)
        {
            m_HBAORenderPass?.Cleanup();
            m_HBAORenderPass = null;
        }

        // Here you can inject one or multiple render passes in the renderer.
        // This method is called when setting up the renderer once per-camera.
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            shader = Shader.Find("Hidden/Universal Render Pipeline/HBAO");
            if (shader == null)
            {
                Debug.LogWarning("HBAO shader was not found. Please ensure it compiles correctly");
                return;
            }

            if (renderingData.cameraData.postProcessEnabled)
            {
                m_HBAORenderPass.Setup(shader, renderer, renderingData);
                renderer.EnqueuePass(m_HBAORenderPass);
            }
        }
    }
}
