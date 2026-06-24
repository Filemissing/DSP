using DSP;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

public static class DSP_GraphAssetHandler
{
    [OnOpenAsset]
    public static bool OnOpen(int instanceID, int line)
    {
        Object obj = EditorUtility.InstanceIDToObject(instanceID);

        if (obj is DSP_ConversationGraphAsset graphAsset)
        {
            DSP_EditorWindow.Open(graphAsset);
            return true;
        }

        return false;
    }
}