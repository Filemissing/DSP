using UnityEngine;
using System.Collections;
using TMPro;

// TEXT REVEAL EFFECTS
public abstract class DSP_TextRevealEffect : ScriptableObject
{
    public abstract IEnumerator PlayEffect(GameObject target, float duration, AnimationCurve curve);
}

[CreateAssetMenu(menuName = "DSP/Effects/Text/Reveal/Typewriter")]
public class DSP_TextRevealTypewriter : DSP_TextRevealEffect
{
    [SerializeField] private float charactersPerSecond = 30f;
    
    public override IEnumerator PlayEffect(GameObject target, float duration, AnimationCurve curve)
    {
        TMP_Text textComponent = target.GetComponent<TMP_Text>();
        if (textComponent == null) yield break;
        
        string fullText = textComponent.text;
        textComponent.text = "";
        
        float delay = 1f / charactersPerSecond;
        int totalCharacters = fullText.Length;
        
        for (int i = 0; i <= totalCharacters; i++)
        {
            textComponent.text = fullText.Substring(0, i);
            yield return new WaitForSeconds(delay);
        }
    }
}

[CreateAssetMenu(menuName = "DSP/Effects/Text/Reveal/Fade")]
public class DSP_TextRevealFade : DSP_TextRevealEffect
{
    public override IEnumerator PlayEffect(GameObject target, float duration, AnimationCurve curve)
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
}