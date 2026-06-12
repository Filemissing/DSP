using System;
using UnityEngine;

public class DSP_BackgroundInteractionShowcase : MonoBehaviour
{
    // References
    public DSP_SceneEvent showcase_ClearMidday;
    public DSP_SceneEvent showcase_ClearSunset;
    public DSP_SceneEvent showcase_CloudyMidday;
    public DSP_SceneEvent showcase_CloudySunset;

    public Material clear_Midday;
    public Material clear_Sunset;
    public Material cloudy_Midday;
    public Material cloudy_Sunset;

    public MeshRenderer target;
    
    
    
    // Functions
    void SetMaterial(Material mat)
    {
        target.material = mat;
    }

    bool SetMaterial_ClearMidday()
    {
        SetMaterial(clear_Midday);
        return true;
    }
    
    bool SetMaterial_ClearSunset()
    {
        SetMaterial(clear_Sunset);
        return true;
    }
    
    bool SetMaterial_CloudyMidday()
    {
        SetMaterial(cloudy_Midday);
        return true;
    }
    
    bool SetMaterial_CloudySunset()
    {
        SetMaterial(cloudy_Sunset);
        return true;
    }



    // Event handling
    private void OnEnable()
    {
        showcase_ClearMidday.Subscribe(SetMaterial_ClearMidday);
        showcase_ClearSunset.Subscribe(SetMaterial_ClearSunset);
        showcase_CloudyMidday.Subscribe(SetMaterial_CloudyMidday);
        showcase_CloudySunset.Subscribe(SetMaterial_CloudySunset);
    }

    private void OnDisable()
    {
        showcase_ClearMidday.Unsubscribe(SetMaterial_ClearMidday);
        showcase_ClearSunset.Unsubscribe(SetMaterial_ClearSunset);
        showcase_CloudyMidday.Unsubscribe(SetMaterial_CloudyMidday);
        showcase_CloudySunset.Unsubscribe(SetMaterial_CloudySunset);
    }
}
