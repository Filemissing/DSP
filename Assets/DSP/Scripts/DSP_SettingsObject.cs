using TMPro;
using UnityEngine;

[CreateAssetMenu(menuName = "DSP/Settings")]
public class DSP_SettingsObject : ScriptableObject
{
    [Header("Sprites")]
    public Sprite textboxSprite;
    public Sprite skipSprite;
    public Sprite continueSprite;

    [Header("Fonts")]
    public TMP_FontAsset mainFont;
    public TMP_FontAsset characterNameFont;

    [Header("Dialogue Box Effects")]
    public DSP_EffectType dialogueBoxAppearEffect = DSP_EffectType.DialogueBoxAppearFade;
    public DSP_EffectType dialogueBoxDisappearEffect = DSP_EffectType.DialogueBoxDisappearFade;
    
    [Header("Continue Button Effects")]
    public DSP_EffectType continueButtonAppearEffect = DSP_EffectType.ContinueAppearFade;
    public DSP_EffectType continueButtonDisappearEffect = DSP_EffectType.ContinueDisappearFade;
    
    [Header("Options Effects")]
    public DSP_EffectType optionsAppearEffect = DSP_EffectType.OptionsAppearFade;
    public DSP_EffectType optionsDisappearEffect = DSP_EffectType.OptionsDisappearFade;
    
    [Header("Character Effects")]
    public DSP_EffectType characterAppearEffect = DSP_EffectType.CharacterAppearFade;
    public DSP_EffectType characterDisappearEffect = DSP_EffectType.CharacterDisappearFade;
    
    [Header("Text Effects")]
    public DSP_EffectType textRevealEffect = DSP_EffectType.TextRevealTypewriter;
}