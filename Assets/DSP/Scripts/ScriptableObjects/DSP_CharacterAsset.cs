using UnityEngine;

namespace DSP
{
	[CreateAssetMenu(fileName = "DSP_CharacterAsset", menuName = "DSP/Character Asset")]
	public class DSP_CharacterAsset : ScriptableObject
	{
		public string characterName;
		public Sprite characterImage;
	} 
}
