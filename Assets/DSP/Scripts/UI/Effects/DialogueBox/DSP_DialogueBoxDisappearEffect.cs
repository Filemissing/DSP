using UnityEngine;
using System.Collections;

public abstract class DSP_DialogueBoxDisappearEffect : DSP_BaseEffect { }

public class DSP_DialogueBoxDisappearFade : DSP_DialogueBoxDisappearEffect
{
    public override IEnumerator PlayEffect(float duration, AnimationCurve curve)
    {
        StartEffect();
        
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        float elapsed = 0f;
        while (elapsed < duration)
        {
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, curve.Evaluate(elapsed / duration));
            elapsed += Time.deltaTime;
            yield return null;
        }
        canvasGroup.alpha = 0f;
        
        CompleteEffect();
    }
}

public class DSP_DialogueBoxDisappearScale : DSP_DialogueBoxDisappearEffect
{
    public override IEnumerator PlayEffect(float duration, AnimationCurve curve)
    {
        StartEffect();
        
        Vector3 originalScale = transform.localScale;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, curve.Evaluate(elapsed / duration));
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localScale = Vector3.zero;
        
        CompleteEffect();
    }
}