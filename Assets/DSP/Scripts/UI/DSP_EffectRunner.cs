using UnityEngine;
using System.Collections;

public class DSP_EffectRunner : MonoBehaviour
{
    private static DSP_EffectRunner _instance;
    
    public static DSP_EffectRunner Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("DSP_EffectRunner");
                _instance = go.AddComponent<DSP_EffectRunner>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }
    
    public void RunEffect(DSP_EffectType effectType, GameObject target, float duration, AnimationCurve curve)
    {
        StartCoroutine(DSP_Effects.PlayEffectCoroutine(effectType, target, duration, curve));
    }
}