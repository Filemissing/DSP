using UnityEngine;
using System.Collections;

public abstract class DSP_CharacterAppearEffect : DSP_BaseEffect { }

public class DSP_CharacterAppearFade : DSP_CharacterAppearEffect
{
    public override IEnumerator PlayEffect(float duration, AnimationCurve curve)
    {
        StartEffect();
        
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        float elapsed = 0f;
        while (elapsed < duration)
        {
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, curve.Evaluate(elapsed / duration));
            elapsed += Time.deltaTime;
            yield return null;
        }
        canvasGroup.alpha = 1f;
        
        CompleteEffect();
    }
}

public class DSP_CharacterAppearScale : DSP_CharacterAppearEffect
{
    public override IEnumerator PlayEffect(float duration, AnimationCurve curve)
    {
        StartEffect();
        
        Vector3 originalScale = transform.localScale;
        transform.localScale = Vector3.zero;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            transform.localScale = Vector3.Lerp(Vector3.zero, originalScale, curve.Evaluate(elapsed / duration));
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localScale = originalScale;
        
        CompleteEffect();
    }
}