using System.Collections.Generic;
using TMPro;
using UnityEngine;
using DG.Tweening;

public class DSP_EffectsHandler
{
    // Dialogue Box Effects
    public static void AppearDialogueBox(GameObject obj)
    {
        obj.GetComponent<CanvasGroup>().interactable = true;
        obj.GetComponent<CanvasGroup>().blocksRaycasts = true;
        obj.GetComponent<CanvasGroup>().alpha = 1;
    }

    public static void DisappearDialogueBox(GameObject obj)
    {
        obj.GetComponent<CanvasGroup>().interactable = false;
        obj.GetComponent<CanvasGroup>().blocksRaycasts = false;
        obj.GetComponent<CanvasGroup>().alpha = 0;
    }
    
    // Continue Button Effects
    public static void AppearContinueButton(GameObject obj)
    {
        obj.GetComponent<CanvasGroup>().interactable = true;
        obj.GetComponent<CanvasGroup>().blocksRaycasts = true;
        obj.GetComponent<CanvasGroup>().alpha = 1;
    }

    public static void DisappearContinueButton(GameObject obj)
    {
        obj.GetComponent<CanvasGroup>().interactable = false;
        obj.GetComponent<CanvasGroup>().blocksRaycasts = false;
        obj.GetComponent<CanvasGroup>().alpha = 0;
    }
    
    // Options Effects
    public static void AppearOptions(List<GameObject> options)
    {
        float delayMultiplier = 0.08f;
        
        for (int i = 0; i < options.Count; i++)
        {
            GameObject obj = options[i];
            CanvasGroup canvasGroup = obj.GetComponent<CanvasGroup>();
            
            // Delay
            float delay = i * delayMultiplier;
            
            // Transparency
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 0;
            canvasGroup.DOFade(1, 0.15f).SetEase(Ease.OutBack).SetDelay(delay);
            
            // Scale
            obj.transform.localScale = Vector3.one * .4f;
            obj.transform.DOScale(Vector3.one, 0.15f).SetEase(Ease.OutBack).SetDelay(delay);
        }
    }

    public static void DisappearOptions(List<GameObject> options)
    {
        float delayMultiplier = 0.04f;
        
        for (int i = 0; i < options.Count; i++)
        {
            GameObject obj = options[i];
            CanvasGroup canvasGroup = obj.GetComponent<CanvasGroup>();
            
            // Delay
            float delay = i * delayMultiplier;
            
            // Transparency
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1;
            canvasGroup.DOFade(0, 0.15f).SetEase(Ease.InCubic).SetDelay(delay);
            
            // Scale
            obj.transform.localScale = Vector3.one;
            obj.transform.DOScale(Vector3.one * .4f, 0.15f).SetEase(Ease.InCubic).SetDelay(delay);
        }
    }
    
    // Character Effects
    public static void AppearCharacterImage(GameObject obj)
    {
        obj.GetComponent<CanvasGroup>().interactable = true;
        obj.GetComponent<CanvasGroup>().blocksRaycasts = true;
        obj.GetComponent<CanvasGroup>().alpha = 1;
    }

    public static void DisappearCharacterImage(GameObject obj)
    {
        obj.GetComponent<CanvasGroup>().interactable = false;
        obj.GetComponent<CanvasGroup>().blocksRaycasts = false;
        obj.GetComponent<CanvasGroup>().alpha = 0;
    }
    
    // NameBox Effects
    public static void AppearNameBox(GameObject obj)
    {
        obj.GetComponent<CanvasGroup>().interactable = true;
        obj.GetComponent<CanvasGroup>().blocksRaycasts = true;
        obj.GetComponent<CanvasGroup>().alpha = 1;
    }

    public static void DisappearNameBox(GameObject obj)
    {
        obj.GetComponent<CanvasGroup>().interactable = false;
        obj.GetComponent<CanvasGroup>().blocksRaycasts = false;
        obj.GetComponent<CanvasGroup>().alpha = 0;
    }
    
    // Text Effects
    public static void RevealText(GameObject obj, string text)
    {
        TMP_Text textComponent = obj.GetComponent<TMP_Text>();
        
        textComponent.text = text;
        textComponent.maxVisibleCharacters = 0;

        DOTween.To(
            () => textComponent.maxVisibleCharacters,
            x => textComponent.maxVisibleCharacters = x,
            text.Length,
            .025f * text.Length
        );
    }
}