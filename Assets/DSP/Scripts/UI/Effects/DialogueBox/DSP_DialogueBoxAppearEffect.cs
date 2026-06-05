using UnityEngine;
using System.Collections;

public abstract class DSP_DialogueBoxAppearEffect : DSP_BaseEffect { }

public class DSP_DialogueBoxAppearFade : DSP_DialogueBoxAppearEffect
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

public class DSP_DialogueBoxAppearScale : DSP_DialogueBoxAppearEffect
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

public class DSP_DialogueBoxAppearSlide : DSP_DialogueBoxAppearEffect
{
    [SerializeField] private Vector2 slideFrom = new Vector2(0, -100f);
    
    public override IEnumerator PlayEffect(float duration, AnimationCurve curve)
    {
        StartEffect();
        
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null) yield break;
        
        Vector2 originalPosition = rectTransform.anchoredPosition;
        Vector2 startPosition = originalPosition + slideFrom;
        
        float elapsed = 0f;
        while (elapsed < duration)
        {
            rectTransform.anchoredPosition = Vector2.Lerp(startPosition, originalPosition, curve.Evaluate(elapsed / duration));
            elapsed += Time.deltaTime;
            yield return null;
        }
        rectTransform.anchoredPosition = originalPosition;
        
        CompleteEffect();
    }
}