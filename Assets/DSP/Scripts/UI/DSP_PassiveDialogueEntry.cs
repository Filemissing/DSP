using DG.Tweening;
using TMPro;
using UnityEngine;

public class DSP_PassiveDialogueEntry : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text dialogueLabel;
    [SerializeField] private TMP_Text characterLabel;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Settings")]
    [SerializeField] private float effectDuration = .2f;
    [SerializeField] private float effectSizeY = 0;
    private Vector2 defaultSize;
    



    public void Setup(string dialogue, string character)
    {
        dialogueLabel.text = dialogue;
        characterLabel.text = character;

        AppearEffect();
    }

    public void Kill()
    {
        DisappearEffect();
    }

    void AppearEffect()
    {
        RectTransform rectTransform = transform as RectTransform;
        defaultSize = rectTransform.sizeDelta;
        
        canvasGroup.alpha = 0;
        rectTransform.sizeDelta = new Vector2(defaultSize.x, effectSizeY);
        
        canvasGroup.DOFade(1, effectDuration).SetEase(Ease.OutCubic);
        rectTransform.DOSizeDelta(defaultSize, effectDuration).SetEase(Ease.OutCubic);
    }

    void DisappearEffect()
    {
        canvasGroup.alpha = 1;
        
        canvasGroup.DOFade(0, effectDuration).SetEase(Ease.OutCubic).OnComplete((() => {Destroy(gameObject);}));
    }
}
