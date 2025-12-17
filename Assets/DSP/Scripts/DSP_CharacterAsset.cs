using UnityEngine;

[CreateAssetMenu(fileName = "DSP_CharacterAsset", menuName = "DSP/Character Asset")]
public class DSP_CharacterAsset : ScriptableObject
{
    new public string name;
    public Sprite characterImage;
    public DSP_ConversationGraphAsset[] conversations;
     

}
