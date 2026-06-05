using System;
using UnityEngine;
using System.Collections;
using System.Text.RegularExpressions;
using TMPro;
using Random = UnityEngine.Random;
using System.Text;


public enum DSP_EffectType
{
    None,
    CharacterAppearFade,
    CharacterAppearScale,
    CharacterDisappearFade,
    CharacterDisappearScale,
    ContinueAppearFade,
    ContinueAppearScale,
    ContinueDisappearFade,
    ContinueDisappearScale,
    DialogueBoxAppearFade,
    DialogueBoxAppearScale,
    DialogueBoxAppearSlide,
    DialogueBoxDisappearFade,
    DialogueBoxDisappearScale,
    OptionsAppearFade,
    OptionsAppearScale,
    OptionsAppearSequential,
    OptionsDisappearFade,
    OptionsDisappearScale,
    TextRevealTypewriter,
    TextRevealFade,
    TextRevealAdvanced
}

public static class DSP_Effects
{
    public static void PlayEffect(DSP_EffectType effectType, GameObject target, float duration, AnimationCurve curve)
    {
        if (effectType == DSP_EffectType.None || target == null) return;
        DSP_EffectRunner.Instance.RunEffect(effectType, target, duration, curve);
    }
    
    public static IEnumerator PlayEffectCoroutine(DSP_EffectType effectType, GameObject target, float duration, AnimationCurve curve)
    {
        if (effectType == DSP_EffectType.None || target == null) yield break;

        switch (effectType)
        {
            case DSP_EffectType.CharacterAppearFade:
                yield return FadeEffect(target, 0f, 1f, duration * 0.8f, GetBounceCurve());
                break;
            case DSP_EffectType.CharacterAppearScale:
                yield return ScaleEffect(target, Vector3.zero, Vector3.one, duration * 0.6f, GetBounceCurve());
                break;
            case DSP_EffectType.CharacterDisappearFade:
                yield return FadeEffect(target, 1f, 0f, duration * 0.5f, GetEaseOutCurve());
                break;
            case DSP_EffectType.CharacterDisappearScale:
                yield return ScaleEffect(target, Vector3.one, Vector3.zero, duration * 0.4f, GetEaseInCurve());
                break;
            case DSP_EffectType.ContinueAppearFade:
                yield return FadeEffect(target, 0f, 1f, duration * 0.4f, GetEaseOutCurve());
                break;
            case DSP_EffectType.ContinueAppearScale:
                yield return ScaleEffect(target, new Vector3(0.5f, 0.5f, 1f), Vector3.one, duration * 0.3f, GetBounceCurve());
                break;
            case DSP_EffectType.ContinueDisappearFade:
                yield return FadeEffect(target, 1f, 0f, duration * 0.3f, GetEaseInCurve());
                break;
            case DSP_EffectType.ContinueDisappearScale:
                yield return ScaleEffect(target, Vector3.one, new Vector3(0.8f, 0.8f, 1f), duration * 0.2f, GetEaseInCurve());
                break;
            case DSP_EffectType.DialogueBoxAppearFade:
                yield return FadeEffect(target, 0f, 1f, duration * 0.5f, GetEaseOutCurve());
                break;
            case DSP_EffectType.DialogueBoxAppearScale:
                yield return ScaleEffect(target, new Vector3(0.8f, 0.8f, 1f), Vector3.one, duration * 0.4f, GetBounceCurve());
                break;
            case DSP_EffectType.DialogueBoxAppearSlide:
                yield return SlideEffect(target, new Vector2(0, -150f), Vector2.zero, duration * 0.6f, GetBounceCurve());
                break;
            case DSP_EffectType.DialogueBoxDisappearFade:
                yield return FadeEffect(target, 1f, 0f, duration * 0.4f, GetEaseInCurve());
                break;
            case DSP_EffectType.DialogueBoxDisappearScale:
                yield return ScaleEffect(target, Vector3.one, new Vector3(0.7f, 0.7f, 1f), duration * 0.3f, GetEaseInCurve());
                break;
            case DSP_EffectType.OptionsAppearFade:
                yield return FadeEffect(target, 0f, 1f, duration * 0.3f, GetEaseOutCurve());
                break;
            case DSP_EffectType.OptionsAppearScale:
                yield return ScaleEffect(target, new Vector3(0.6f, 0.6f, 1f), Vector3.one, duration * 0.4f, GetBounceCurve());
                break;
            case DSP_EffectType.OptionsAppearSequential:
                yield return OptionsSequentialAppear(target, duration, GetBounceCurve());
                break;
            case DSP_EffectType.OptionsDisappearFade:
                yield return FadeEffect(target, 1f, 0f, duration * 0.2f, GetEaseInCurve());
                break;
            case DSP_EffectType.OptionsDisappearScale:
                yield return ScaleEffect(target, Vector3.one, new Vector3(0.5f, 0.5f, 1f), duration * 0.3f, GetEaseInCurve());
                break;
            case DSP_EffectType.TextRevealTypewriter:
                yield return TypewriterEffect(target, duration * 1.2f);
                break;
            case DSP_EffectType.TextRevealFade:
                yield return TextFadeEffect(target, duration * 0.8f, GetEaseOutCurve());
                break;
            case DSP_EffectType.TextRevealAdvanced:
                yield return AdvancedTextReveal(target, duration * 1.5f);
                break;
        }
    }
    
    // Improved animation curves
    private static AnimationCurve GetBounceCurve()
    {
        return new AnimationCurve(
            new Keyframe(0, 0, 0, 0),
            new Keyframe(0.2f, 1.25f, 0, 0),  // More overshoot
            new Keyframe(0.4f, 0.85f, 0, 0),  // Bounce back
            new Keyframe(0.6f, 1.08f, 0, 0),  // Smaller overshoot
            new Keyframe(0.8f, 0.97f, 0, 0),  // Bounce back
            new Keyframe(1, 1, 0, 0)          // Settle
        );
    }
    
    private static AnimationCurve GetEaseOutCurve()
    {
        return new AnimationCurve(
            new Keyframe(0, 0, 0, 3),
            new Keyframe(1, 1, 0, 0)
        );
    }
    
    private static AnimationCurve GetEaseInCurve()
    {
        return new AnimationCurve(
            new Keyframe(0, 0, 0, 0),
            new Keyframe(1, 1, 3, 0)
        );
    }
    
    private static AnimationCurve GetEaseInOutCurve()
    {
        return AnimationCurve.EaseInOut(0, 0, 1, 1);
    }
    
    // Effect methods
    private static IEnumerator FadeEffect(GameObject target, float from, float to, float duration, AnimationCurve curve)
    {
        CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = target.AddComponent<CanvasGroup>();
        canvasGroup.alpha = from;
        
        float elapsed = 0f;
        while (elapsed < duration)
        {
            canvasGroup.alpha = Mathf.Lerp(from, to, curve.Evaluate(elapsed / duration));
            elapsed += Time.deltaTime;
            yield return null;
        }
        canvasGroup.alpha = to;
    }
    
    private static IEnumerator ScaleEffect(GameObject target, Vector3 from, Vector3 to, float duration, AnimationCurve curve)
    {
        target.transform.localScale = from;
        
        float elapsed = 0f;
        while (elapsed < duration)
        {
            target.transform.localScale = Vector3.Lerp(from, to, curve.Evaluate(elapsed / duration));
            elapsed += Time.deltaTime;
            yield return null;
        }
        target.transform.localScale = to;
    }
    
    private static IEnumerator SlideEffect(GameObject target, Vector2 from, Vector2 to, float duration, AnimationCurve curve)
    {
        RectTransform rectTransform = target.GetComponent<RectTransform>();
        if (rectTransform == null) yield break;
        
        Vector2 originalPos = rectTransform.anchoredPosition;
        rectTransform.anchoredPosition = originalPos + from;
        
        float elapsed = 0f;
        while (elapsed < duration)
        {
            rectTransform.anchoredPosition = originalPos + Vector2.Lerp(from, to, curve.Evaluate(elapsed / duration));
            elapsed += Time.deltaTime;
            yield return null;
        }
        rectTransform.anchoredPosition = originalPos + to;
    }
    
    private static IEnumerator TypewriterEffect(GameObject target, float duration)
    {
        TMP_Text textComponent = target.GetComponent<TMP_Text>();
        if (textComponent == null) yield break;
        
        string fullText = textComponent.text;
        textComponent.text = "";
        
        float charactersPerSecond = Mathf.Max(8f, fullText.Length / duration);
        float delay = 1f / charactersPerSecond;
        
        for (int i = 0; i <= fullText.Length; i++)
        {
            textComponent.text = fullText.Substring(0, i);
            
            // Speed up on spaces, slow down on punctuation
            if (i < fullText.Length)
            {
                char currentChar = fullText[i];
                float charDelay = delay;
                
                if (char.IsWhiteSpace(currentChar)) charDelay *= 0.3f;
                if (currentChar == '.' || currentChar == '!' || currentChar == '?') charDelay *= 2f;
                if (currentChar == ',' || currentChar == ';') charDelay *= 1.5f;
                
                yield return new WaitForSeconds(charDelay);
            }
        }
    }
    
    private static IEnumerator OptionsSequentialAppear(GameObject target, float duration, AnimationCurve curve)
    {
        // Target should be the options container
        if (target.transform.childCount == 0) yield break;
    
        float delayBetweenOptions = duration / (target.transform.childCount + 1);
    
        // First, hide all options
        foreach (Transform child in target.transform)
        {
            CanvasGroup cg = child.GetComponent<CanvasGroup>();
            if (cg == null) cg = child.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            child.localScale = Vector3.zero;
        }
    
        // Sequential appear with overshoot
        for (int i = 0; i < target.transform.childCount; i++)
        {
            Transform option = target.transform.GetChild(i);
        
            // Start both fade and scale effects using the effect runner
            DSP_EffectRunner.Instance.RunEffect(DSP_EffectType.OptionsAppearFade, option.gameObject, duration * 0.4f, curve);
            DSP_EffectRunner.Instance.RunEffect(DSP_EffectType.OptionsAppearScale, option.gameObject, duration * 0.5f, curve);
        
            // Wait before showing next option
            yield return new WaitForSeconds(delayBetweenOptions);
        }
    
        // Wait for all animations to complete
        yield return new WaitForSeconds(duration * 0.6f);
    }


private static IEnumerator SequentialFadeEffect(GameObject target, float from, float to, float duration, AnimationCurve curve)
{
    CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();
    if (canvasGroup == null) canvasGroup = target.AddComponent<CanvasGroup>();
    canvasGroup.alpha = from;
    
    float elapsed = 0f;
    while (elapsed < duration)
    {
        canvasGroup.alpha = Mathf.Lerp(from, to, curve.Evaluate(elapsed / duration));
        elapsed += Time.deltaTime;
        yield return null;
    }
    canvasGroup.alpha = to;
}

private static IEnumerator SequentialScaleEffect(GameObject target, Vector3 from, Vector3 to, float duration, AnimationCurve curve)
{
    target.transform.localScale = from;
    
    float elapsed = 0f;
    while (elapsed < duration)
    {
        // Use bounce curve for overshoot effect
        float t = curve.Evaluate(elapsed / duration);
        target.transform.localScale = Vector3.LerpUnclamped(from, to, t);
        elapsed += Time.deltaTime;
        yield return null;
    }
    target.transform.localScale = to;
}
    
    private static IEnumerator TextFadeEffect(GameObject target, float duration, AnimationCurve curve)
    {
        TMP_Text textComponent = target.GetComponent<TMP_Text>();
        if (textComponent == null) yield break;
        
        Color originalColor = textComponent.color;
        Color transparentColor = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
        
        float elapsed = 0f;
        while (elapsed < duration)
        {
            textComponent.color = Color.Lerp(transparentColor, originalColor, curve.Evaluate(elapsed / duration));
            elapsed += Time.deltaTime;
            yield return null;
        }
        textComponent.color = originalColor;
    }
    
    private static IEnumerator AdvancedTextReveal(GameObject target, float duration)
{
    TMP_Text textComponent = target.GetComponent<TMP_Text>();
    if (textComponent == null) yield break;
    
    string fullText = textComponent.text;
    textComponent.text = "";
    
    // Remove rich text tags for character counting
    string visibleText = Regex.Replace(fullText, "<.*?>", "");
    float charactersPerSecond = Mathf.Max(15f, visibleText.Length / duration);
    float baseDelay = 1f / charactersPerSecond;
    
    StringBuilder currentText = new StringBuilder();
    bool isInsideTag = false;
    string currentTag = "";
    
    for (int i = 0; i <= fullText.Length; i++)
    {
        if (i < fullText.Length)
        {
            char currentChar = fullText[i];
            
            // Handle rich text tags
            if (currentChar == '<')
            {
                isInsideTag = true;
                currentTag = "<";
                continue;
            }
            else if (isInsideTag)
            {
                currentTag += currentChar;
                if (currentChar == '>')
                {
                    isInsideTag = false;
                    currentText.Append(currentTag);
                    currentTag = "";
                }
                continue;
            }
            else
            {
                currentText.Append(currentChar);
            }
        }
        
        textComponent.text = currentText.ToString();
        
        if (i < fullText.Length)
        {
            char currentChar = fullText[i];
            float charDelay = baseDelay;
            
            // Skip timing for tag characters
            if (isInsideTag)
            {
                continue;
            }
            
            // Intelligent pacing based on punctuation
            if (char.IsWhiteSpace(currentChar))
            {
                charDelay *= 0.1f; // Very fast for spaces
            }
            else if (IsSentenceEnd(currentChar))
            {
                charDelay *= 1.5f; // The punctuation itself
                yield return new WaitForSeconds(charDelay);
                
                // Check if this is truly the end of a sentence (not an abbreviation)
                if (IsTrueSentenceEnd(fullText, i))
                {
                    float sentencePause = baseDelay * 10f; // Long pause for sentence end
                    yield return new WaitForSeconds(sentencePause);
                }
                continue;
            }
            else if (IsClauseSeparator(currentChar))
            {
                charDelay *= 3f; // Medium pause for clauses
                yield return new WaitForSeconds(charDelay);
                continue;
            }
            else
            {
                // Normal character with slight variation
                charDelay *= Random.Range(0.8f, 1.2f);
            }
            
            yield return new WaitForSeconds(charDelay);
        }
    }
    
    // Final pause after all text is revealed
    yield return new WaitForSeconds(baseDelay * 4f);
}

private static bool IsSentenceEnd(char c)
{
    return c == '.' || c == '!' || c == '?';
}

private static bool IsClauseSeparator(char c)
{
    return c == ',' || c == ';' || c == ':';
}

private static bool IsTrueSentenceEnd(string text, int position)
{
    // Check if this is likely a true sentence end (not an abbreviation)
    if (position <= 1) return true;
    
    // Check for common abbreviations before the dot
    string previousChars = text.Substring(Math.Max(0, position - 3), Math.Min(3, position));
    string[] abbreviations = { "Mr", "Mrs", "Dr", "Prof", "Inc", "Ltd", "Co", "Corp", "vs", "etc", "eg", "ie" };
    
    foreach (string abbr in abbreviations)
    {
        if (previousChars.EndsWith(abbr) && abbr.Length <= previousChars.Length)
        {
            return false; // It's probably an abbreviation
        }
    }
    
    // Check if next character is whitespace or end of string, and previous is not whitespace
    if (position + 1 < text.Length)
    {
        char nextChar = text[position + 1];
        char prevChar = text[position - 1];
        return char.IsWhiteSpace(nextChar) && !char.IsWhiteSpace(prevChar);
    }
    
    return true;
}

    
    private static bool IsInsideTag(string text, int position)
    {
        int lastOpen = text.LastIndexOf('<', position);
        int lastClose = text.LastIndexOf('>', position);
        return lastOpen > lastClose;
    }
}