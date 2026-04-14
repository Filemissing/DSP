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
}
