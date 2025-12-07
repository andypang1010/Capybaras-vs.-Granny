using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VolumeController : MonoBehaviour
{
    public struct VignetteSettings
    {
        public Color color;
        public float intensity;
        public float smoothness;
    }

    public struct ColorAdjustmentSettings
    {
        public float saturation;
        public Color colorFilter;
    }

    public struct ChromaticAberrationSettings
    {
        public float intensity;
    }

    [Header("Post Processing")]
    public Volume postProcessingVolume;
    Vignette vignette;
    ColorAdjustments colorAdjustments;
    ChromaticAberration chromaticAberration;

    [Header("Saturation Settings")]
    public float transitionDuration = 1.0f; // Duration of the transition in seconds
    public AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // Easing curve
    private Coroutine transitionCoroutine;

    public VignetteSettings defaultVignetteSettings;
    public ColorAdjustmentSettings defaultColorAdjustmentSettings;
    public ChromaticAberrationSettings defaultChromaticAberrationSettings;

    void Start()
    {
        postProcessingVolume = GetComponent<Volume>();
        if (postProcessingVolume != null)
        {
            if (postProcessingVolume.profile.TryGet(out vignette))
            {
                vignette.active = true;
                defaultVignetteSettings = new VignetteSettings
                {
                    color = vignette.color.value,
                    intensity = vignette.intensity.value,
                    smoothness = vignette.smoothness.value,
                };
            }

            if (postProcessingVolume.profile.TryGet(out colorAdjustments))
            {
                colorAdjustments.active = true;
                defaultColorAdjustmentSettings = new ColorAdjustmentSettings
                {
                    saturation = colorAdjustments.saturation.value,
                };
            }

            if (postProcessingVolume.profile.TryGet(out chromaticAberration))
            {
                chromaticAberration.active = true;
                defaultChromaticAberrationSettings = new ChromaticAberrationSettings
                {
                    intensity = chromaticAberration.intensity.value,
                };
            }
        }
    }

    void Update() { }

    #region Color Saturation Functions

    // public void SetColorAdjustmentSaturation(float saturationValue)
    // {
    //     saturationValue = Mathf.Clamp(saturationValue, -100f, 100f);
    //     colorAdjustments.saturation.value = saturationValue;
    //     colorAdjustments.active = true;
    // }

    public void SetVignette(Color color, float intensity, float smoothness)
    {
        vignette.color.value = color;
        vignette.intensity.value = intensity;
        vignette.smoothness.value = smoothness;
    }

    public void SetChromaticAberrationIntensity(float intensity)
    {
        if (chromaticAberration)
            chromaticAberration.intensity.value = intensity;
    }

    public void SetColorAdjustmentSaturation(float targetSaturationValue, float duration)
    {
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }
        float useDuration = duration;
        transitionCoroutine = StartCoroutine(
            TransitionSaturation(colorAdjustments, targetSaturationValue, useDuration)
        );
    }

    private IEnumerator TransitionSaturation(
        ColorAdjustments colorAdjustments,
        float targetValue,
        float duration
    )
    {
        targetValue = Mathf.Clamp(targetValue, -100f, 100f);
        float startValue = colorAdjustments.saturation.value;
        // colorAdjustments.active = true;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            float easedProgress = transitionCurve.Evaluate(progress);
            float currentValue = Mathf.Lerp(startValue, targetValue, easedProgress);
            colorAdjustments.saturation.value = currentValue;
            yield return null;
        }
        colorAdjustments.saturation.value = targetValue;
        // transitionCoroutine = null;
    }

    // public void ModifyPostProcessingEffect<T>(System.Action<T> modifyEffect)
    //     where T : VolumeComponent
    // {
    //     if (postProcessingVolume == null)
    //     {
    //         Debug.LogWarning("Post Processing Volume is not assigned!");
    //         return;
    //     }

    //     if (postProcessingVolume.profile.TryGet<T>(out T effect))
    //     {
    //         modifyEffect?.Invoke(effect);
    //         effect.active = true;
    //     }
    //     else
    //     {
    //         Debug.LogWarning($"{typeof(T).Name} effect not found in the Volume Profile!");
    //     }
    // }

    #endregion

    #region Color Filter Functions

    public void SetColorAdjustmentFilter(Color color)
    {
        colorAdjustments.colorFilter.value = color;
    }

    #endregion
}
