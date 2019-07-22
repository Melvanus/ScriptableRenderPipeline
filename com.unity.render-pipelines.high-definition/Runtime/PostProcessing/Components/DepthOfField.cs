using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    public enum DepthOfFieldMode
    {
        Off,
        UsePhysicalCamera,
        Manual
    }

    public enum DepthOfFieldResolution : int
    {
        Quarter = 4,
        Half = 2,
        Full = 1
    }


    // TODO: Tooltips
    [Serializable, VolumeComponentMenu("Post-processing/Depth Of Field")]
    public sealed class DepthOfField : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("Specifies the mode that HDRP uses to set the focus for the depth of field effect.")]
        public DepthOfFieldModeParameter focusMode = new DepthOfFieldModeParameter(DepthOfFieldMode.Off);

        [Tooltip("Whether to use quality settings for the effect.")]
        public BoolParameter useQualitySettings = new BoolParameter(false);

        [Tooltip("Specifies the mode that HDRP uses to set the focus for the depth of field effect.")]
        public QualitySettingParameter quality = new QualitySettingParameter(VolumeQualitySettingsLevels.Low);


        // Physical settings
        [Tooltip("Sets the distance to the focus point from the Camera.")]
        public MinFloatParameter focusDistance = new MinFloatParameter(10f, 0.1f);

        // Manual settings
        // Note: because they can't mathematically be mapped to physical settings, interpolating
        // between manual & physical is not supported
        [Tooltip("Sets the distance from the Camera at which the near field blur begins to decrease in intensity.")]
        public MinFloatParameter nearFocusStart = new MinFloatParameter(0f, 0f);
        [Tooltip("Sets the distance from the Camera at which the near field does not blur anymore.")]
        public MinFloatParameter nearFocusEnd = new MinFloatParameter(4f, 0f);

        [Tooltip("Sets the distance from the Camera at which the far field starts blurring.")]
        public MinFloatParameter farFocusStart = new MinFloatParameter(10f, 0f);
        [Tooltip("Sets the distance from the Camera at which the far field blur reaches its maximum blur radius.")]
        public MinFloatParameter farFocusEnd = new MinFloatParameter(20f, 0f);

        // Shared settings
        public int nearSampleCount
        {
            get
            {
                if (!useQualitySettings.value || (HDRenderPipeline)RenderPipelineManager.currentPipeline == null)
                {
                    return m_NearSampleCount.value;
                }
                else
                {
                    HDRenderPipeline pipeline = (HDRenderPipeline)RenderPipelineManager.currentPipeline;
                    int qualityLevel = (int)quality.value;
                    return pipeline.currentPlatformRenderPipelineSettings.postProcessQualitySettings.NearBlurSampleCount[qualityLevel];
                }
            }
            set { m_NearSampleCount.value = nearSampleCount; }
        }

        public float nearMaxBlur
        {
            get
            {
                if (!useQualitySettings.value || (HDRenderPipeline)RenderPipelineManager.currentPipeline == null)
                {
                    return m_NearMaxBlur.value;
                }
                else
                {
                    HDRenderPipeline pipeline = (HDRenderPipeline)RenderPipelineManager.currentPipeline;
                    int qualityLevel = (int)quality.value;
                    return pipeline.currentPlatformRenderPipelineSettings.postProcessQualitySettings.NearBlurMaxRadius[qualityLevel];
                }
            }
            set { m_NearMaxBlur.value = nearMaxBlur; }
        }

        public int farSampleCount
        {
            get
            {
                if(!useQualitySettings.value || (HDRenderPipeline)RenderPipelineManager.currentPipeline == null)
                {
                    return m_FarSampleCount.value;
                }
                else
                {
                    HDRenderPipeline pipeline = (HDRenderPipeline)RenderPipelineManager.currentPipeline;
                    int qualityLevel = (int)quality.value;
                    return pipeline.currentPlatformRenderPipelineSettings.postProcessQualitySettings.FarBlurSampleCount[qualityLevel];
                }
            }
            set { m_FarSampleCount.value = farSampleCount; }
        }

        public float farMaxBlur
        {
            get
            {
                if (!useQualitySettings.value || (HDRenderPipeline)RenderPipelineManager.currentPipeline == null)
                {
                    return m_FarMaxBlur.value;
                }
                else
                {
                    HDRenderPipeline pipeline = (HDRenderPipeline)RenderPipelineManager.currentPipeline;
                    int qualityLevel = (int)quality.value;
                    return pipeline.currentPlatformRenderPipelineSettings.postProcessQualitySettings.FarBlurMaxRadius[qualityLevel];
                }
            }
            set { m_FarMaxBlur.value = farMaxBlur; }
        }

        public bool highQualityFiltering
        {
            get
            {
                if (!useQualitySettings.value || (HDRenderPipeline)RenderPipelineManager.currentPipeline == null)
                {
                    return m_HighQualityFiltering.value;
                }
                else
                {
                    HDRenderPipeline pipeline = (HDRenderPipeline)RenderPipelineManager.currentPipeline;
                    int qualityLevel = (int)quality.value;
                    return pipeline.currentPlatformRenderPipelineSettings.postProcessQualitySettings.HighQualityFiltering[qualityLevel];
                }
            }
            set { m_HighQualityFiltering.value = highQualityFiltering; }
        }

        public DepthOfFieldResolution resolution
        {
            get
            {
                if (!useQualitySettings.value || (HDRenderPipeline)RenderPipelineManager.currentPipeline == null)
                {
                    return m_Resolution.value;
                }
                else
                {
                    HDRenderPipeline pipeline = (HDRenderPipeline)RenderPipelineManager.currentPipeline;
                    int qualityLevel = (int)quality.value;
                    return pipeline.currentPlatformRenderPipelineSettings.postProcessQualitySettings.Resolution[qualityLevel];
                }
            }
            set { m_Resolution.value = resolution; }
        }

        [Tooltip("Sets the number of samples to use for the near field.")]
        [SerializeField, FormerlySerializedAs("nearSampleCount")]
        ClampedIntParameter m_NearSampleCount = new ClampedIntParameter(5, 3, 8);

        [SerializeField, FormerlySerializedAs("nearMaxBlur")]
        [Tooltip("Sets the maximum radius the near blur can reach.")]
        ClampedFloatParameter m_NearMaxBlur = new ClampedFloatParameter(4f, 0f, 8f);

        [Tooltip("Sets the number of samples to use for the far field.")]
        [SerializeField, FormerlySerializedAs("farSampleCount")]
        ClampedIntParameter m_FarSampleCount = new ClampedIntParameter(7, 3, 16);
        [Tooltip("Sets the maximum radius the far blur can reach.")]
        [SerializeField, FormerlySerializedAs("farMaxBlur")]
        ClampedFloatParameter m_FarMaxBlur = new ClampedFloatParameter(8f, 0f, 16f);

        // Advanced settings
        [Tooltip("When enabled, HDRP uses bicubic filtering instead of bilinear filtering for the depth of field effect.")]
        [SerializeField, FormerlySerializedAs("highQualityFiltering")]
        BoolParameter m_HighQualityFiltering = new BoolParameter(true);

        [Tooltip("Specifies the resolution at which HDRP processes the depth of field effect.")]
        [SerializeField, FormerlySerializedAs("resolution")]
        DepthOfFieldResolutionParameter m_Resolution = new DepthOfFieldResolutionParameter(DepthOfFieldResolution.Half);

        public bool IsActive()
        {
            return focusMode.value != DepthOfFieldMode.Off && (IsNearLayerActive() || IsFarLayerActive());
        }

        public bool IsNearLayerActive() => nearMaxBlur > 0f && nearFocusEnd.value > 0f;

        public bool IsFarLayerActive() => farMaxBlur > 0f;
    }

    [Serializable]
    public sealed class DepthOfFieldModeParameter : VolumeParameter<DepthOfFieldMode> { public DepthOfFieldModeParameter(DepthOfFieldMode value, bool overrideState = false) : base(value, overrideState) { } }

    [Serializable]
    public sealed class DepthOfFieldResolutionParameter : VolumeParameter<DepthOfFieldResolution> { public DepthOfFieldResolutionParameter(DepthOfFieldResolution value, bool overrideState = false) : base(value, overrideState) { } }
}
