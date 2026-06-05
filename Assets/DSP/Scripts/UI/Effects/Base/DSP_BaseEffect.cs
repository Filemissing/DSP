using UnityEngine;
using System.Collections;
using System;

public abstract class DSP_BaseEffect : MonoBehaviour
{
    public event Action OnEffectStarted;
    public event Action OnEffectCompleted;
    
    public abstract IEnumerator PlayEffect(float duration, AnimationCurve curve);
    
    protected void StartEffect() => OnEffectStarted?.Invoke();
    protected void CompleteEffect() => OnEffectCompleted?.Invoke();
}