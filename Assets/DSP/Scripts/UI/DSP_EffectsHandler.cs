using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using DG.Tweening;

public class DSP_EffectsHandler : MonoBehaviour
{
    [SerializeField] private DSP_DialogueBoxVisualizer dialogueBoxVisualizer;
    private bool isTyping = false;
    
    // Dialogue Box Effects
    public void AppearDialogueBox(GameObject obj)
    {
		// References
		CanvasGroup canvasGroup = obj.GetComponent<CanvasGroup>();

		// Transparency
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 0;
        canvasGroup.DOFade(1, 0.15f).SetEase(Ease.OutBack);
        
        // Scale
        obj.transform.localScale = Vector3.one * .4f;
        obj.transform.DOScale(Vector3.one, 0.15f).SetEase(Ease.OutBack);
    }

    public void DisappearDialogueBox(GameObject obj)
    {
        // References
		CanvasGroup canvasGroup = obj.GetComponent<CanvasGroup>();

		// Transparency
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 1;
        canvasGroup.DOFade(0, 0.15f).SetEase(Ease.OutBack);
        
        // Scale
        obj.transform.localScale = Vector3.one;
        obj.transform.DOScale(Vector3.one * .4f, 0.15f).SetEase(Ease.OutBack);
    }
    
    // Continue Button Effects
    public void AppearContinueButton(GameObject obj)
    {
        // References
        CanvasGroup canvasGroup = obj.GetComponent<CanvasGroup>();
        
        // Hide
        canvasGroup.alpha = 0;
        
        // Effect
        StartCoroutine(DoEffect());
        IEnumerator DoEffect()
        {
            yield return new WaitUntil(() => !isTyping);
            
            // Transparency
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.DOFade(1, 0.15f).SetEase(Ease.OutBack);
        }
    }

    public void DisappearContinueButton(GameObject obj)
    {
        // References
        CanvasGroup canvasGroup = obj.GetComponent<CanvasGroup>();

        // Transparency
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 1;
        canvasGroup.DOFade(0, 0.15f).SetEase(Ease.OutBack);
    }
    
    // Options Effects
    public void AppearOptions(List<GameObject> options)
    {
        // Hide
        for (int i = 0; i < options.Count; i++)
        {
            GameObject obj = options[i];
            CanvasGroup canvasGroup = obj.GetComponent<CanvasGroup>();
            
            canvasGroup.alpha = 0;
        }
        
        // Effect
        StartCoroutine(DoEffect());
        IEnumerator DoEffect()
        {
            yield return new WaitUntil(() => !isTyping);
            
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
                canvasGroup.DOFade(1, 0.15f).SetEase(Ease.OutBack).SetDelay(delay);
            
                // Scale
                obj.transform.localScale = Vector3.one * .4f;
                obj.transform.DOScale(Vector3.one, 0.15f).SetEase(Ease.OutBack).SetDelay(delay);
            }
        }
    }

    public void DisappearOptions(List<GameObject> options)
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
    public void AppearCharacterImage(GameObject obj)
    {
        obj.GetComponent<CanvasGroup>().interactable = true;
        obj.GetComponent<CanvasGroup>().blocksRaycasts = true;
        obj.GetComponent<CanvasGroup>().alpha = 1;
    }

    public void DisappearCharacterImage(GameObject obj)
    {
        obj.GetComponent<CanvasGroup>().interactable = false;
        obj.GetComponent<CanvasGroup>().blocksRaycasts = false;
        obj.GetComponent<CanvasGroup>().alpha = 0;
    }
    
    // NameBox Effects
    public void AppearNameBox(GameObject obj)
    {
        // References
        CanvasGroup canvasGroup = obj.GetComponent<CanvasGroup>();

        // Transparency
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 0;
        canvasGroup.DOFade(1, 0.15f).SetEase(Ease.OutBack);
    }

    public void DisappearNameBox(GameObject obj)
    {
        // References
        CanvasGroup canvasGroup = obj.GetComponent<CanvasGroup>();

        // Transparency
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 1;
        canvasGroup.DOFade(0, 0.15f).SetEase(Ease.OutBack);
    }
    
    // Text Effects
    public void RevealText(GameObject obj)
    {
        isTyping = true;
        
        TMP_Text textComponent = obj.GetComponent<TMP_Text>();
        float duration = .025f * textComponent.text.Length;
        
        textComponent.maxVisibleCharacters = 0;
        
        DOTween.To(
            () => textComponent.maxVisibleCharacters,
            x => textComponent.maxVisibleCharacters = x, textComponent.text.Length, duration
        );

        StartCoroutine(StopTyping());
        IEnumerator StopTyping()
        {
            yield return new WaitForSeconds(duration);
            isTyping = false;
        }
    }
    
    // Helpers
    void ExecuteEffect(IEnumerable<GameObject> objects, DSP_DialogueBoxVisualizer.EffectType type)
    {
        List<GameObject> objs = objects.ToList();
        
        switch (type)
        {
            case DSP_DialogueBoxVisualizer.EffectType.AppearDialogueBox:
                AppearDialogueBox(objs[0]);
                break;
            case DSP_DialogueBoxVisualizer.EffectType.DisappearDialogueBox:
                DisappearDialogueBox(objs[0]);
                break;
            case DSP_DialogueBoxVisualizer.EffectType.AppearContinueButton:
                AppearContinueButton(objs[0]);
                break;
            case DSP_DialogueBoxVisualizer.EffectType.DisappearContinueButton:
                DisappearContinueButton(objs[0]);
                break;
            case DSP_DialogueBoxVisualizer.EffectType.AppearOptions:
                AppearOptions(objs);
                break;
            case DSP_DialogueBoxVisualizer.EffectType.DisappearOptions:
                DisappearOptions(objs);
                break;
            case DSP_DialogueBoxVisualizer.EffectType.AppearCharacterImage:
                AppearCharacterImage(objs[0]);
                break;
            case DSP_DialogueBoxVisualizer.EffectType.DisappearCharacterImage:
                DisappearCharacterImage(objs[0]);
                break;
            case DSP_DialogueBoxVisualizer.EffectType.AppearNameBox:
                AppearNameBox(objs[0]);
                break;
            case DSP_DialogueBoxVisualizer.EffectType.DisappearNameBox:
                DisappearNameBox(objs[0]);
                break;
            case DSP_DialogueBoxVisualizer.EffectType.RevealText:
                RevealText(objs[0]);
                break;
        }
    }
    
    // Event Bindings
    void OnEnable()
    {
        if (dialogueBoxVisualizer != null)
        {
            dialogueBoxVisualizer.PlayEffect += ExecuteEffect;
        }
    }

    private void OnDisable()
    {
        if (dialogueBoxVisualizer != null)
        {
            dialogueBoxVisualizer.PlayEffect -= ExecuteEffect;
        }
    }
}